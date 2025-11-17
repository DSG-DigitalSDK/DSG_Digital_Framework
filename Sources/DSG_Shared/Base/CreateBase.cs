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
    public abstract class CreateBase : DisposableBase, CreataInterface
    {
        static string sC = nameof(CreateBase);

        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool Initialized { get; private set; }
        public bool ThrowExceptions { get; set; }

        readonly SemaphoreSlim createSemaphore= new SemaphoreSlim(1,1);   

        public event EventHandler? Creating;
        public event EventHandler<ResultEventArgs>? Created;
        public event EventHandler<ResultEventArgs>? CreateError;
        public event EventHandler? Destroying;
        public event EventHandler<ResultEventArgs>? Destroyed;
        public event EventHandler<ResultEventArgs>? DestroyError;

        //- DEPRECATED -
        //public event EventHandler<ResultEventArgs>? OnCreateImplementation;
        //public event EventHandler<ResultEventArgs>? OnDestroyImplementation;
        public event Func<object, ResultEventArgs, Task>? OnCreateImplementationAsync;
        public event Func<object, ResultEventArgs, Task>? OnDestroyImplementationAsync;

        protected override void Dispose(bool disposing)
        {
            Task.Run(async ()=>await DestroyNoLockAsync());
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
        protected Result HandleError(string sClass, string sMethod, OperationResult eResult, string? sErrorMessage, int iErrorCode, EventHandler<ResultEventArgs>? oEvent)
            => HandleError(sClass, sMethod, ResultEventArgs.CreateEventArgs(Result.CreateResultError(eResult, sErrorMessage, iErrorCode, null)), oEvent);
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
            await createSemaphore.WaitAsync();
            try
            {
                // Using await implies the use of a task to execute the method.
                // then the task runs it enters in the createSemaphore.WaitAsync()
                // that is currentily used in this task => DEADLOCK!
                await DestroyNoLockAsync();

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
                Creating?.Invoke(this, EventArgs.Empty);
                var oArgs = new ResultEventArgs();

                if (OnCreateImplementationAsync != null)
                    await OnCreateImplementationAsync(this, oArgs);
                //if (OnCreateImplementation != null)
                //    OnCreateImplementation(this, oArgs);
                //if(OnCreateImplementationAsync == null && OnCreateImplementation == null )
                else
                    return HandleError(sC, sM, OperationResult.ErrorResource, $"{Name}: No Create implementation registered", 0, null, CreateError);
 
                Initialized = oArgs.Valid;
                if (Initialized)
                {
                    LogMan.Trace(sC, sM, $"'{Name}' created");
                    Created?.Invoke(this, oArgs);
                    return Result.CreateResultSuccess();
                }
                else
                {
                    return HandleError(sC, sM, oArgs, CreateError);
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, Name, ex);
                try
                {
                    // Using await implies the use of a task to execute the method.
                    // then the task runs it enters in the createSemaphore.WaitAsync()
                    // that is currentily used in this task => DEADLOCK!
                    await DestroyNoLockAsync();
                }
                catch { }
                if (ThrowExceptions)
                {
                    throw;
                }
                return HandleError(sC, sM, ex, CreateError);
            }
            finally
            {
                createSemaphore.Release();    
            }
        }

       
        /// <summary>
        /// Deallocates resources, wothout lock to avoid DEADLOCK
        /// </summary>
        /// <returns>Operation Result</returns>
        async Task<Result> DestroyNoLockAsync()
        {
            string sM = nameof(DestroyNoLockAsync);
            try
            {
                LogMan.Trace(sC, sM, $"Destroying '{Name}'");
                if (Initialized)
                {
                    Destroying?.Invoke(this, EventArgs.Empty);
                }
                var oArgs = new ResultEventArgs();
                if (OnDestroyImplementationAsync != null)
                    await OnDestroyImplementationAsync(this, oArgs);
                //if (OnDestroyImplementation != null)
                //    OnDestroyImplementation(this, oArgs);
                //if (OnDestroyImplementationAsync == null && OnDestroyImplementation == null)
                else
                    return HandleError(sC, sM, OperationResult.ErrorResource, $"{Name}: No Destroy implementation registered", 0, null, DestroyError);
                if (oArgs.Valid)
                {
                    LogMan.Trace(sC, sM, $"'{Name}' destroyed");
                    if (Initialized)
                    {
                        Destroyed?.Invoke(this, oArgs);
                    }
                    Initialized = false;
                    return Result.CreateResultSuccess();
                }
                else
                {
                    if (Initialized)
                    {
                        return HandleError(sC, sM, oArgs, DestroyError);
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
                return HandleError(sC, sM, ex, DestroyError);
            }
        }


        /// <summary>
        /// Deallocate resources, using the Lock<br/>
        /// Method defines a workflow. use <see cref="OnDestroyImplementation"> to implement specific object instantiation</see>
        /// </summary
        /// <returns>operation result</returns>
        public async Task<Result> DestroyAsync()
        {
            string sM = nameof(DestroyAsync);
            await createSemaphore.WaitAsync();
            try
            {
                return await DestroyNoLockAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                createSemaphore.Release(); 
            }
        }
    }
}
