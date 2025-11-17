using DSG.Base;
using DSG.Log;
using DSG.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Result = DSG.Base.Result;

namespace DSG.IO
{
    /// <summary>
    /// Abstract base class for managing workflow with connected devices or resources.
    /// Supports:
    /// <list type="bullet">
    /// <item>Connection and disconnection logic</item>
    /// <item>Data read/write operations</item>
    /// <item>Event handling for operations</item>
    /// <item>Error handling and logging</item>
    /// </list>
    /// </summary>
    public abstract class ConnectableBase : CreateBase, ConnectableInterface
    {
        // Class name for logging
        static readonly string sC = nameof(ConnectableBase);

        // Semaphores to make operations thread-safe
        SemaphoreSlim oConnSemaphore = new SemaphoreSlim(1, 1);      

        /// <summary>
        /// Indicates whether the resource is currently connected
        /// </summary>
        public bool Connected { get; protected set; }

        /// <summary>
        /// Human-readable connection name
        /// </summary>
        public string ConnectionName => $"{Name}.Connection";

        /// <summary>
        /// Connection string containing connection parameters
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Timeout for connection attempts in milliseconds
        /// </summary>
        public int ConnectionTimeoutMs { get; set; }

     

        /// <summary>
        /// Enables automatic reconnection if the connection is lost
        /// </summary>
       // public bool AutoReconnect { get; set; } = true;

       
        public bool AutoReconnection { get; set; }
        public int ReconnectionMaxThenth { get; set; } = 3;
        public int ReconnectionWaitMs { get; set; } = 30 * 1000;
        
        #region Events

        // Connection events
        public event EventHandler? Connecting;
        public event EventHandler<ResultEventArgs>? Connection;
        public event EventHandler<ResultEventArgs>? ConnectionError;
        public event EventHandler? Disconnecting;
        public event EventHandler<ResultEventArgs>? Disconnected;
        public event EventHandler<ResultEventArgs>? DisconnectError;

      

        // Async implementation events for connection/disconnection
        public event Func<object, ResultEventArgs, Task>? OnConnectImplementationAsync;
        public event Func<object, ResultEventArgs, Task>? OnDisconnectImplementationAsync;
        public event EventHandler<ResultEventArgs>? AutoReconnecting;
        public event EventHandler<ResultEventArgs>? AutoReconnectError;
        public event EventHandler<ResultEventArgs>? ConnectionFailure;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Concrete classes must implement device-specific read logic
        /// </summary>
        /// <returns>Result of the read operation</returns>
        protected abstract Task<Result> ReadImplementationAsync();

        /// <summary>
        /// Concrete classes must implement device-specific write logic
        /// </summary>
        /// <param name="oWriteObj">Data object to write</param>
        /// <returns>Result of the write operation</returns>
        protected abstract Task<Result> WriteImplementationAsync(object oWriteObj);

        #endregion

        #region Constructor

        public ConnectableBase()
        {
           
        }

        event EventHandler<ResultEventArgs>? ConnectableInterface.AutoReconnect
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

     

        #endregion

        #region Connection Methods

        /// <summary>
        /// Asynchronously connects to the resource
        /// </summary>
        /// <returns>Result of the connection attempt</returns>
        public async Task<Result> ConnectAsync()
        {
            string sM = nameof(ConnectAsync);
            await oConnSemaphore.WaitAsync();
            try
            {
                if (!Enabled)
                {
                    LogMan.Error(sC, sM, $"'{ConnectionName}' : can't connect using a DISABLED instance");
                    return Result.CreateResultError(OperationResult.Error, $"'{ConnectionName}' : can't connect using a DISABLED instance", 0);
                }

                if (!Initialized)
                {
                    var ResC = await CreateAsync();
                    if (!Initialized)
                    {
                        LogMan.Error(sC, sM, $"'{ConnectionName}' : Creation Error");
                        return ResC;
                    }
                }

                if (Connected)
                {
                    LogMan.Trace(sC, sM, $"{ConnectionName} already connected");
                    return Result.CreateResultSuccess();
                }

                // Raise connecting event
                Connecting?.Invoke(this, EventArgs.Empty);
                LogMan.Trace(sC, sM, $"Connecting to '{ConnectionName}'");

                var oArgs = new ResultEventArgs();
                if (OnConnectImplementationAsync != null)
                    await OnConnectImplementationAsync.Invoke(this, oArgs);
                else
                    return HandleError(sC, sM, OperationResult.Error, $"{ConnectionName} : {nameof(OnConnectImplementationAsync)} not provided", 0, null, ConnectionError);

                if (oArgs.Valid)
                {
                    LogMan.Message(sC, sM, $"{ConnectionName} Connected");
                    Connected = true;
                    Connection?.Invoke(this, oArgs);
                    return Result.CreateResultSuccess();
                }
                else
                {
                    LogMan.Error(sC, sM, $"Error Connecting to {ConnectionName} : {oArgs.ResultError.ErrorMessage}");
                    Connected = false;
                    ConnectionError?.Invoke(this, oArgs);
                    return oArgs.ResultError;
                }
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, ConnectionError);
            }
            finally
            {
                oConnSemaphore.Release();
            }
        }

        /// <summary>
        /// Synchronous wrapper for ConnectAsync
        /// </summary>
        /// <returns>Result of the connection attempt</returns>
        public Result Connect()
        {
            try
            {
                return ConnectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }

        /// <summary>
        /// Asynchronously disconnects from the resource
        /// </summary>
        /// <returns>Result of the disconnection attempt</returns>
        public async Task<Result> DisconnectAsync()
        {
            string sM = nameof(DisconnectAsync);
            await oConnSemaphore.WaitAsync();
            try
            {
                var wasConnected = Connected;
                if (wasConnected)
                {
                    Disconnecting?.Invoke(this, EventArgs.Empty);
                }

                if (OnDisconnectImplementationAsync == null)
                {
                    return HandleError(sC, sM, OperationResult.Error, $"{ConnectionName} : {nameof(OnDisconnectImplementationAsync)} not provided", 0, null, DisconnectError);
                }

                LogMan.Trace(sC, sM, $"Disconnecting from {ConnectionName}");
                var oArgs = new ResultEventArgs();
                await OnDisconnectImplementationAsync(this, oArgs);

                if (oArgs.Valid)
                {
                    LogMan.Message(sC, sM, $"{ConnectionName} Disconnected");
                    Connected = false;
                    if (wasConnected) Disconnected?.Invoke(this, oArgs);
                    return Result.CreateResultSuccess();
                }
                else
                {
                    if (wasConnected) DisconnectError?.Invoke(this, oArgs);
                }

                return oArgs.ResultError;
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, DisconnectError);
            }
            finally
            {
                oConnSemaphore.Release();
            }
        }

        /// <summary>
        /// Synchronous wrapper for DisconnectAsync
        /// </summary>
        /// <returns>Result of the disconnection attempt</returns>
        public Result Disconnect()
        {
            try
            {
                return DisconnectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if the resource is connected, and optionally reconnects automatically
        /// </summary>
        /// <param name="sMethod">Calling method name</param>
        /// <param name="oEvent">Error event handler</param>
        /// <returns>Result of the connection check</returns>
        protected async Task<Result> CheckConnectionAsync(string sMethod, EventHandler<ResultEventArgs>? oEvent)
        {
            if (Connected)
                return Result.CreateResultSuccess();

            if (!AutoReconnection)
                return HandleError(sC, sMethod, OperationResult.Error, $"{ConnectionName} : Communication channel closed", 0, oEvent);

            var oResConn = await ConnectAsync();
            if (oResConn.HasError)
                HandleError(sC, sMethod, OperationResult.Error, $"{ConnectionName} : Cannot open communication channel", 0, oEvent);

            return oResConn;
        }

       

        #endregion

       
    }
}
