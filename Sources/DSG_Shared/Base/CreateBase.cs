using DSG.Base;
using DSG.Log;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public abstract class CreateBase : DisposableBase, ICreatable
    {
        static string sC = nameof(CreateBase);

        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool Initialized { get; protected set; }
        public bool ThrowExceptions { get; set; }

        readonly SemaphoreSlim semaphore= new SemaphoreSlim(1,1);   

        public event EventHandler? OnCreating;
        public event EventHandler<ResultEventArgs>? OnCreate;
        public event EventHandler<ResultEventArgs>? OnCreateError;
        public event EventHandler? OnDestroying;
        public event EventHandler<ResultEventArgs>? OnDestroy;
        public event EventHandler<ResultEventArgs>? OnDestroyError;

        //- DEPRECATED -
        //public event EventHandler<ResultEventArgs>? OnCreateImplementation;
        //public event EventHandler<ResultEventArgs>? OnDestroyImplementation;
        public event Func<object, ResultEventArgs, Task>? OnCreateImplementationAsync;
        public event Func<object, ResultEventArgs, Task>? OnDestroyImplementationAsync;

        protected override void Dispose(bool disposing)
        {
            Destroy();
            base.Dispose(disposing);
        }

        protected Result HandleError(string sClass, string sMethod, ResultEventArgs oArgs, EventHandler<ResultEventArgs>? oEvent)
        {
            string sM = nameof(HandleError);
            var oRes = oArgs?.ResultError ?? Result.CreateResultErrorUnknown();
            LogMan.Error(sClass, sMethod, $"{Name} : {oRes.ErrorCode} : {oRes.ErrorMessage} ({oRes.ErrorCode:X8})", oRes.Exception);
            try
            {
                if (oEvent != null && oArgs != null)
                {
                    oEvent?.Invoke(this, oArgs);
                }
            }
            catch (Exception exx)
            {
                LogMan.Exception(sC, sM, exx);
            }
            return oRes;
        }

        protected Result HandleError(string sClass, string sMethod, OperationResult eResult, string? sErrorMessage, int iErrorCode, Exception? ex, EventHandler<ResultEventArgs>? oEvent)
            => HandleError(sClass, sMethod, ResultEventArgs.CreateEventArgs(Result.CreateResultError(eResult,sErrorMessage, iErrorCode, ex)), oEvent);  
        protected Result HandleError(string sClass, string sMethod, Exception ex, EventHandler<ResultEventArgs>? oEvent)
            => HandleError(sClass, sMethod, ResultEventArgs.CreateEventArgs(Result.CreateResultError( OperationResult.ErrorException, null, 0, ex)), oEvent);

        /// <summary>
        /// Allocate resources <br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>
        public async Task<Result> CreateAsync()
        {
            string sM = nameof(CreateAsync);
            await semaphore.WaitAsync();
            try
            {
                //Destroy();
                if (!Enabled)
                {
                    LogMan.Trace(sC, sM, $"'{Name}' DISABLED");
                    return Result.CreateResultSuccess();
                }
                if (Initialized)
                {
                    LogMan.Trace(sC, sM, $"'{Name}' already created");
                    return Result.CreateResultSuccess();
                }
                LogMan.Trace(sC, sM, $"Creating '{Name}'");
                OnCreating?.Invoke(this, EventArgs.Empty);
                var oArgs = new ResultEventArgs();

                if (OnCreateImplementationAsync != null)
                    await OnCreateImplementationAsync(this, oArgs);
                //if (OnCreateImplementation != null)
                //    OnCreateImplementation(this, oArgs);
                //if(OnCreateImplementationAsync == null && OnCreateImplementation == null )
                else
                    return HandleError(sC, sM, OperationResult.ErrorResource, $"{Name}: No Create implementation registered", 0, null, OnCreateError);
 
                Initialized = oArgs.Valid;
                if (Initialized)
                {
                    LogMan.Trace(sC, sM, $"'{Name}' created");
                    OnCreate?.Invoke(this, oArgs);
                    return Result.CreateResultSuccess();
                }
                else
                {
                    return HandleError(sC, sM, oArgs, OnCreateError);
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, Name, ex);
                try
                {
                    await DestroyAsync(false);
                }
                catch { }
                if (ThrowExceptions)
                {
                    throw;
                }
                return HandleError(sC, sM, ex, OnCreateError);
            }
            finally
            {
                semaphore.Release();    
            }
        }

       


        /// <summary>
        /// Free resources <br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>
        async Task<Result> DestroyAsync(bool bLock)
        {
            string sM = nameof(DestroyAsync);
            if (bLock)
            {
                await semaphore.WaitAsync();
            }
            try
            {
                LogMan.Trace(sC, sM, $"Destroying '{Name}'");
                if (Initialized)
                {
                    OnDestroying?.Invoke(this, EventArgs.Empty);
                }
                var oArgs = new ResultEventArgs();
                if (OnDestroyImplementationAsync != null)
                    await OnDestroyImplementationAsync(this, oArgs);
                //if (OnDestroyImplementation != null)
                //    OnDestroyImplementation(this, oArgs);
                //if (OnDestroyImplementationAsync == null && OnDestroyImplementation == null)
                else
                    return HandleError(sC, sM, OperationResult.ErrorResource, $"{Name}: No Destroy implementation registered", 0, null, OnDestroyError);
                if (oArgs.Valid)
                {
                    LogMan.Trace(sC, sM, $"'{Name}' destroyed");
                    if (Initialized)
                    {
                        OnDestroy?.Invoke(this, oArgs);
                    }
                    Initialized = false;
                    return Result.CreateResultSuccess();
                }
                else
                {
                    if (Initialized)
                    {
                        return HandleError(sC, sM, oArgs, OnDestroyError);
                    }
                    return oArgs.ResultError;
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, Name, ex);
                if (ThrowExceptions)
                {
                    throw;
                }
                return HandleError(sC, sM, ex, OnDestroyError);
            }
            finally
            {
                if (bLock)
                {
                    semaphore.Release();
                }
            }

        }

        public async Task<Result> DestroyAsync() => await DestroyAsync(true);

        public Result Create()
        {
            string sM = nameof(Create);
            try
            {
                return CreateAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (ThrowExceptions)
                {
                    throw;
                }
                return HandleError(sC, sM, ex, null);
            }
        }
        public Result Destroy()
        {
            string sM = nameof(Destroy);
            try
            {
                return DestroyAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (ThrowExceptions)
                {
                    throw;
                }
                return HandleError(sC, sM, ex, null);
            }
        }
    }
}
