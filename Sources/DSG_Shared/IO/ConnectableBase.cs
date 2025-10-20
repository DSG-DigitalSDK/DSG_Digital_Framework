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
    public abstract class ConnectableBase : CreateBase, IConnectable
    {
        // Class name for logging
        static readonly string sC = nameof(ConnectableBase);

        // Semaphores to make operations thread-safe
        SemaphoreSlim oConnSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim oReadSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim oWriteSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Unique identifier for this instance used in logs and events.
        /// Format: '{Name}/{ConnectionName}'
        /// </summary>
        protected string ObjectID => $"'{Name}/{ConnectionName}'";

        /// <summary>
        /// Indicates whether the resource is currently connected
        /// </summary>
        public bool Connected { get; protected set; }

        /// <summary>
        /// Human-readable connection name
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// Connection string containing connection parameters
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Timeout for connection attempts in milliseconds
        /// </summary>
        public int ConnectionTimeoutMs { get; set; }

        /// <summary>
        /// Timeout for read operations in milliseconds
        /// </summary>
        public int ReadTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Timeout for write operations in milliseconds
        /// </summary>
        public int WriteTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Enables automatic reconnection if the connection is lost
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// Counters for read operations (success, error, timeout, elapsed time)
        /// </summary>
        public StatisticCounters ReadCounters { get; private set; } = new StatisticCounters();

        /// <summary>
        /// Counters for write operations (success, error, timeout, elapsed time)
        /// </summary>
        public StatisticCounters WriteCounters { get; private set; } = new StatisticCounters();

        #region Events

        // Connection events
        public event EventHandler? Connecting;
        public event EventHandler<ResultEventArgs>? Connection;
        public event EventHandler<ResultEventArgs>? ConnectionError;
        public event EventHandler? Disconnecting;
        public event EventHandler<ResultEventArgs>? Disconnected;
        public event EventHandler<ResultEventArgs>? DisconnectError;

        // Read/Write events
        public event EventHandler? DataReading;
        public event EventHandler<ResultEventArgs>? DataReaded;
        public event EventHandler<ResultEventArgs>? DataReadError;
        public event EventHandler? DataWriting;
        public event EventHandler<ResultEventArgs>? DataWritten;
        public event EventHandler<ResultEventArgs>? DataWriteError;

        // Async implementation events for connection/disconnection
        public event Func<object, ResultEventArgs, Task>? OnConnectImplementationAsync;
        public event Func<object, ResultEventArgs, Task>? OnDisconnectImplementationAsync;

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
            ResetReadCounters();
            ResetWriteCounters();
        }

        /// <summary>
        /// Resets read operation counters
        /// </summary>
        public void ResetReadCounters() => ReadCounters.ResetCounters();

        /// <summary>
        /// Resets write operation counters
        /// </summary>
        public void ResetWriteCounters() => WriteCounters.ResetCounters();

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
                    LogMan.Error(sC, sM, $"'{ObjectID}' : can't connect using a DISABLED instance");
                    return Result.CreateResultError(OperationResult.Error, $"'{ObjectID}' : can't connect using a DISABLED instance", 0);
                }

                if (!Initialized)
                {
                    var ResC = await CreateAsync();
                    if (!Initialized)
                    {
                        LogMan.Error(sC, sM, $"'{ObjectID}' : Creation Error");
                        return ResC;
                    }
                }

                if (Connected)
                {
                    LogMan.Trace(sC, sM, $"{ObjectID} already connected");
                    return Result.CreateResultSuccess();
                }

                // Raise connecting event
                Connecting?.Invoke(this, EventArgs.Empty);
                LogMan.Trace(sC, sM, $"Connecting to '{ObjectID}'");

                var oArgs = new ResultEventArgs();
                if (OnConnectImplementationAsync != null)
                    await OnConnectImplementationAsync.Invoke(this, oArgs);
                else
                    return HandleError(sC, sM, OperationResult.Error, $"{ObjectID} : {nameof(OnConnectImplementationAsync)} not provided", 0, null, ConnectionError);

                if (oArgs.Valid)
                {
                    LogMan.Message(sC, sM, $"{ObjectID} Connected");
                    Connected = true;
                    Connection?.Invoke(this, oArgs);
                    return Result.CreateResultSuccess();
                }
                else
                {
                    LogMan.Error(sC, sM, $"Error Connecting to {ObjectID} : {oArgs.ResultError.ErrorMessage}");
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
                    return HandleError(sC, sM, OperationResult.Error, $"{ObjectID} : {nameof(OnDisconnectImplementationAsync)} not provided", 0, null, DisconnectError);
                }

                LogMan.Trace(sC, sM, $"Disconnecting from {ObjectID}");
                var oArgs = new ResultEventArgs();
                await OnDisconnectImplementationAsync(this, oArgs);

                if (oArgs.Valid)
                {
                    LogMan.Message(sC, sM, $"{ObjectID} Disconnected");
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
        async Task<Result> CheckConnection(string sMethod, EventHandler<ResultEventArgs>? oEvent)
        {
            if (Connected)
                return Result.CreateResultSuccess();

            if (!AutoReconnect)
                return HandleError(sC, sMethod, OperationResult.Error, $"{ObjectID} : Communication channel closed", 0, oEvent);

            var oResConn = await ConnectAsync();
            if (oResConn.HasError)
                HandleError(sC, sMethod, OperationResult.Error, $"{ObjectID} : Cannot open communication channel", 0, oEvent);

            return oResConn;
        }

        /// <summary>
        /// Manages operation results: updates counters, logs, and raises appropriate events
        /// </summary>
        /// <param name="sMethod">Calling method name</param>
        /// <param name="oRes">Operation result</param>
        /// <param name="oCounters">Statistic counters</param>
        /// <param name="oEventOk">Success event handler</param>
        /// <param name="oEventError">Error event handler</param>
        /// <returns>Same operation result for chaining</returns>
        Result ManageResult(string sMethod, Result oRes,TimeElapser oTStart, StatisticCounters oCounters, EventHandler<ResultEventArgs>? oEventOk, EventHandler<ResultEventArgs>? oEventError)
        {
            if (oRes.Valid)
            {
                oCounters.AddStatisticTime(oTStart);
                oCounters.AddValidEvent();
                LogMan.Trace(sC, sMethod, $"{ObjectID} : Operation Successful");
                oEventOk?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
            }
            else
            {
                if (oRes.OperationResult == OperationResult.ErrorTimeout)
                {
                    oCounters.AddTimeoutEvent();
                    LogMan.Trace(sC, sMethod, $"{ObjectID} : Operation Timeout");
                }
                else
                {
                    oCounters.AddErrorEvent();
                    LogMan.Error(sC, sMethod, $"{ObjectID} : Operation Error : {oRes.ErrorMessage}");
                }
                oEventError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
            }
            return oRes;
        }

        #endregion

        #region Read / Write Methods

        /// <summary>
        /// Asynchronously reads data from the resource
        /// </summary>
        /// <returns>Result of the read operation</returns>
        public async Task<Result> ReadDataAsync()
        {
            string sM = nameof(ReadDataAsync);
            await oReadSemaphore.WaitAsync();
            try
            {
                LogMan.Trace(sC, sM, $"{ObjectID} : Reading Data");
                DataReading?.Invoke(this, EventArgs.Empty);

                Result oRes = await CheckConnection(sM, DataReadError);
                if (oRes.HasError) return oRes;

                var oTS = ReadCounters.TimeStart();
                oRes = await ReadImplementationAsync();

                return ManageResult(sM, oRes, oTS, ReadCounters, DataReaded, DataReadError);
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, DataReadError);
            }
            finally
            {
                oReadSemaphore.Release();
            }
        }

        /// <summary>
        /// Synchronous wrapper for ReadDataAsync
        /// </summary>
        /// <returns>Result of the read operation</returns>
        public Result ReadData()
        {
            try
            {
                return ReadDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }

        /// <summary>
        /// Asynchronously writes data to the resource
        /// </summary>
        /// <param name="oObj">Object containing data to write</param>
        /// <returns>Result of the write operation</returns>
        public async Task<Result> WriteDataAsync(object oObj)
        {
            string sM = nameof(WriteDataAsync);
            await oWriteSemaphore.WaitAsync();
            try
            {
                Result oRes = await CheckConnection(sM, DataWriteError);
                if (oRes.HasError) return oRes;

                LogMan.Trace(sC, sM, $"{ObjectID} : Writing Data");
                DataWriting?.Invoke(this, EventArgs.Empty);

                var oTS = WriteCounters.TimeStart();
                oRes = await WriteImplementationAsync(oObj);

                return ManageResult(sM, oRes, oTS, WriteCounters, DataWritten, DataWriteError);
            }
            catch (Exception ex)
            {
                WriteCounters.AddErrorEvent();
                return HandleError(sC, sM, ex, DataWriteError);
            }
            finally
            {
                oWriteSemaphore.Release();
            }
        }

        /// <summary>
        /// Synchronous wrapper for WriteDataAsync
        /// </summary>
        /// <param name="oObj">Object containing data to write</param>
        /// <returns>Result of the write operation</returns>
        public Result WriteData(object oObj)
        {
            try
            {
                return WriteDataAsync(oObj).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }

        #endregion
    }
}
