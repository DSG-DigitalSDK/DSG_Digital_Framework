using DSG.Log;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public abstract class CreateBase : DisposableBase, ICreatable
    {
        static string className = nameof(CreateBase);

        Dictionary<string, object> oCreationParameters = new Dictionary<string, object>();
        public Dictionary<string, object> CreationParameters => oCreationParameters;
        public bool Initialized { get; protected set; }

        public string Name { get; set; } = "NameID";

        public event EventHandler? OnCreating;
        public event EventHandler? OnCreate;
        public event EventHandler? OnCreateError;
        public event EventHandler? OnDestroying;
        public event EventHandler? OnDestroy;
        public event EventHandler? OnDestroyError;

        protected abstract Result CreateImp();
        protected abstract Result DestroyImp();

        Result CreateExceptionResult(Exception ex, string? message) => new Result()
        {
            OperationResult = OperationResult.ErrorException,
            Exception = ex,
            ErrorMessage = message ?? ex.Message
        };

        Result CreateExceptionResult(Exception ex) => CreateExceptionResult(ex, null);


        public Result Create()
        {
            string sMethod = nameof(Create);
            try
            {
                if (Initialized)
                {
                    LogMan.Trace(className, sMethod, $"'{Name}' already created");
                    return new Result() { OperationResult = OperationResult.Success };
                }
                LogMan.Trace(className, sMethod, $"Creating '{Name}'");
                OnCreating?.Invoke( this, EventArgs.Empty );    
                var Res = CreateImp();
                Initialized = Res.Valid;
                if (Res.Valid)
                {
                    LogMan.Trace(className, sMethod, $"'{Name}' created");
                    OnCreate?.Invoke( this, EventArgs.Empty );
                }
                else
                {
                    LogMan.Error(className, sMethod, $"Error Creating '{Name}' : {Res.ErrorMessage} ");
                    OnCreateError?.Invoke( this, EventArgs.Empty );
                    Destroy();
                }
                return Res; 
            }
            catch (Exception ex)
            {
                LogMan.Exception(className, sMethod, Name, ex);
                Destroy();
                return CreateExceptionResult(ex); 
            }
        }

        public async Task<Result> CreateAsync()
        {
            return await Task.Run(() => Create() );
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
                var Res = DestroyImp();
                if (Res.Valid)
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
                        OnDestroyError?.Invoke(this, EventArgs.Empty);
                    }
                    LogMan.Error(className, sMethod, $"Error Destroyng '{Name}' : {Res.ErrorMessage} ");
                    Destroy();
                }
                return Res;
            }
            catch (Exception ex)
            {
                LogMan.Exception(className, sMethod, Name, ex);
                Destroy();
                return CreateExceptionResult(ex);
            }
        }

        public async Task<Result> DestroyAsync()
        {
            return await Task.Run(() => Destroy());
        }
    }
}
