using DSG.Base;
using DSG.Log;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public abstract class CreateBase : DisposableBase, ICreatable
    {
        static string className = nameof(CreateBase);

        Dictionary<string, object> oDictParameters = new Dictionary<string, object>();
        public Dictionary<string, object> ParameterCollection => oDictParameters;
        public bool Initialized { get; protected set; }

        public string Name
        {
            get => GetDictionaryParam(nameof(Params.Name), "") as string;
            set => SetDictionaryParam(nameof(Parameter.Name), value );
        }
        
        public bool ThrowExceptions
        {
            get => (bool)GetDictionaryParam(nameof(Params.ThrowExceptions), false);
            set => SetDictionaryParam(nameof(Params.ThrowExceptions), value);
        }

        public event EventHandler? OnCreating;
        public event EventHandler? OnCreate;
        public event EventHandler<ResultEventArgs>? OnCreateError;
        public event EventHandler? OnDestroying;
        public event EventHandler? OnDestroy;
        public event EventHandler<ResultEventArgs>? OnDestroyError;

        protected abstract Result CreateImpl(); //=> Result.CreateResultSuccess();
        protected abstract Result DestroyImpl();// => Result.CreateResultSuccess();
        protected override void Dispose(bool disposing)
        {
            Destroy();
            base.Dispose(disposing);
        }

        protected Result RaiseEventException(EventHandler<ResultEventArgs> oEvent, Exception ex)
        {
            string sMethod = nameof(RaiseEventException);
            var oRes = Result.CreateResultError(ex);
            try
            {
                oEvent?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
            }
            catch (Exception exx)
            { 
                LogMan.Exception(className,sMethod, exx);
            }
            return oRes;
        }
        protected object GetDictionaryParam(string sKey, object defaultValue)
        {
            if( oDictParameters.TryGetValue(sKey, out var value) )  
                return value;
            SetDictionaryParam(sKey, defaultValue);
            return defaultValue;    
        }
        protected object GetDictionaryParam(string sKey)
        {
            if (oDictParameters.TryGetValue(sKey, out var value))
                return value;
            return null;
        }

        protected void SetDictionaryParam(string sKey, object oValue)
        {
            if (string.IsNullOrWhiteSpace(sKey))
                return;
            oDictParameters[sKey] = oValue;
        }
        protected void SetDictionaryParam(string sKey)
        {
            if (string.IsNullOrWhiteSpace(sKey))
                return;
            oDictParameters[sKey] = null;
        }




        public Result Create()
        {
            string sMethod = nameof(Create);
            try
            {
                if (Initialized)
                {
                    LogMan.Trace(className, sMethod, $"'{Name}' already created");
                    return Result.CreateResultSuccess();
                }
                LogMan.Trace(className, sMethod, $"Creating '{Name}'");
                OnCreating?.Invoke( this, EventArgs.Empty );    
                var oRes = CreateImpl();
                Initialized = oRes.Valid;
                if (oRes.Valid)
                {
                    LogMan.Trace(className, sMethod, $"'{Name}' created");
                    OnCreate?.Invoke( this, EventArgs.Empty );
                }
                else
                {
                    LogMan.Error(className, sMethod, $"Error Creating '{Name}' : {oRes.ErrorMessage} ");
                    OnCreateError?.Invoke( this, ResultEventArgs.CreateEventArgs(oRes));
                    Destroy();
                }
                return oRes; 
            }
            catch (Exception ex)
            {
                LogMan.Exception(className, sMethod, Name, ex);
                try
                {
                    Destroy();
                }
                catch { }
                return RaiseEventException(OnCreateError, ex);   
            }
        }


        public Result Destroy()
        {
            string sMethod = nameof(Destroy);
            try
            {
                LogMan.Trace(className, sMethod, $"Destroying '{Name}'");
                if (Initialized)
                {
                    OnDestroying?.Invoke(this, EventArgs.Empty);
                }
                var oRes = DestroyImpl();
                if (oRes.Valid)
                {
                    LogMan.Trace(className, sMethod, $"'{Name}' destroyed");
                    if (Initialized)
                    {
                        OnDestroy?.Invoke(this, EventArgs.Empty); 
                    }
                    Initialized = false;
                }
                else
                {
                    if (Initialized)
                    {
                        OnDestroyError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                    }
                    LogMan.Error(className, sMethod, $"Error Destroyng '{Name}' : {oRes.ErrorMessage} ");
                    Destroy();
                }
                return oRes;
            }
            catch (Exception ex)
            {
                LogMan.Exception(className, sMethod, Name, ex);
                return RaiseEventException(OnDestroyError, ex);
            }
        }

        public async Task<Result> CreateAsync() => await Task.Run(() => Create());
        public async Task<Result> DestroyAsync() => await Task.Run(() => Destroy());
    }
}
