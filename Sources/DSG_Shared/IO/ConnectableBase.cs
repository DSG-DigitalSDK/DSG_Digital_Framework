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
    /// Abstract class for workflow management<br/>
    /// Supports:
    /// <list type="bullet">
    /// <item>Connection/Disconnectionitem</item>
    /// <item>Read/Write dataflow</item>
    /// <item>Event handling</item>
    /// <item>Error handling</item>
    /// </list>
    /// </summary>
    public abstract class ConnectableBase : CreateBase, IConnectable
    {
        static readonly string sC = nameof(ConnectableBase);

        SemaphoreSlim oConnSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim oReadSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim oWriteSemaphore = new SemaphoreSlim(1, 1);

        string sID => $"'{Name}/{ConnectionName}'";

        public bool Connected { get; protected set; }
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }

        public int ConnectionTimeoutMs { get; set; }
        public int ReadTimeoutMs { get; set; } = 1000;
        public int WriteTimeoutMs { get; set; } = 1000;

        public StatisticCounters ReadCounters { get; private set; } = new  StatisticCounters();
        public StatisticCounters WriteCounters { get; private set; } = new StatisticCounters();


        public event EventHandler? OnConnecting;
        public event EventHandler<ResultEventArgs>? OnConnect;
        public event EventHandler<ResultEventArgs>? OnConnectError;
        public event EventHandler? OnDisconnecting;
        public event EventHandler<ResultEventArgs>? OnDisconnect;
        public event EventHandler<ResultEventArgs>? OnDisconnectError;
        //
        public event EventHandler? OnReading;
        public event EventHandler<ResultEventArgs>? OnRead;
        public event EventHandler<ResultEventArgs>? OnReadError;
        public event EventHandler? OnWriting;
        public event EventHandler<ResultEventArgs>? OnWrite;
        public event EventHandler<ResultEventArgs>? OnWriteError;
        //
        //protected event EventHandler<ResultEventArgs>? OnConnectImplementation;
        //protected event EventHandler<ResultEventArgs>? OnDisconnectImplementation;
        public event Func<object, ResultEventArgs, Task>? OnConnectImplementationAsync;
        public event Func<object, ResultEventArgs, Task>? OnDisconnectImplementationAsync;
        //
        protected abstract Task<Result> ReadImplementationAsync();
        protected abstract Task<Result> WriteImplementationAsync( object oWriteObj );
        //

        public void ResetReadCounters() => ReadCounters.ResetCounters();
        public void ResetWriteCounters() => WriteCounters.ResetCounters();
        public ConnectableBase()
        {
            ResetReadCounters();
            ResetWriteCounters();
        }

        public async Task<Result> ConnectAsync()
        {
            string sM = nameof(ConnectAsync);
            await oConnSemaphore.WaitAsync();
            try
            {
                if (!Enabled)
                {
                    LogMan.Error(sC, sM, $"'{sID}' : can't connect using a DISABLED instance");
                    return Result.CreateResultError(OperationResult.Error, $"'{sID}' : can't connect using a DISABLED instance", 0);
                }
                if (!Initialized)
                {
                    var ResC = await CreateAsync();
                    if (!Initialized)
                    {
                        LogMan.Error(sC, sM, $"'{sID}' : Creation Error");
                        return ResC;
                    }
                }
                if (Connected)
                {
                    LogMan.Trace(sC, sM, $"{sID} already connected");
                    return Result.CreateResultSuccess();
                }
                OnConnecting?.Invoke(this, EventArgs.Empty);
                LogMan.Trace(sC, sM, $"Connecting to '{sID}'");
                var oArgs = new ResultEventArgs();
                if (OnConnectImplementationAsync != null)
                    await OnConnectImplementationAsync.Invoke(this, oArgs);
                //if (OnConnectImplementation != null)
                //    OnConnectImplementation.Invoke(this, oArgs);
                //if (OnConnectImplementation == null && OnConnectImplementationAsync == null)
                else
                    return HandleError(sC, sM, OperationResult.Error, $"{sID} : {nameof(OnConnectImplementationAsync)} not provided", 0, null, OnConnectError);

                if (oArgs.Valid)
                {
                    LogMan.Message(sC, sM, $"{sID} Connected ");
                    Connected = true;
                    OnConnect?.Invoke(this, oArgs);
                    return Result.CreateResultSuccess();
                }
                else
                {
                    LogMan.Error(sC, sM, $"Error Connecting to {sID} : {oArgs.ResultError.ErrorMessage} ");
                    Connected = false;
                    OnConnectError?.Invoke(this, oArgs);
                    return oArgs.ResultError;
                }
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, OnConnectError);
            }
            finally
            {
                oConnSemaphore.Release();   
            }
        }

        public Result Connect()
        {
            string sM = nameof(Connect);
            try
            {
                return ConnectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }

        public async Task<Result> DisconnectAsync()
        {
            string sM = nameof(DisconnectAsync);
            await oConnSemaphore.WaitAsync();
            try
            {
                var bConn = Connected;
                if (bConn)
                {
                    OnDisconnecting?.Invoke(this, EventArgs.Empty);
                }
                if (OnDisconnectImplementationAsync == null)
                {
                    return HandleError(sC, sM, OperationResult.Error, $"{sID} : {nameof(OnDisconnectImplementationAsync)} not provided", 0, null, OnDisconnectError);
                }
                LogMan.Trace(sC, sM, $"Disconnecting from {sID}");
                var oArgs = new ResultEventArgs();
                if (OnDisconnectImplementationAsync != null)
                    await OnDisconnectImplementationAsync(this, oArgs);
                //if( OnDisconnectImplementation != null)
                //    OnDisconnectImplementation(this, oArgs);
                //if (OnDisconnectImplementation == null && OnDisconnectImplementationAsync == null)
                else
                    return HandleError(sC, sM, OperationResult.Error, $"{sID} : {nameof(OnDisconnectImplementationAsync)} not provided", 0, null, OnDisconnectError);

                if (oArgs.Valid)
                {
                    LogMan.Message(sC, sM, $"{sID} Disconnected");
                    Connected = false;
                    if (bConn)
                    {
                        OnDisconnect?.Invoke(this, oArgs);
                    }
                    return Result.CreateResultSuccess();
                }
                else
                {
                    if (bConn)
                    {
                        OnDisconnectError?.Invoke(this, oArgs);
                    }
                }
                return oArgs.ResultError;
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, OnDisconnectError);
            }
            finally
            {
                oConnSemaphore.Release();
            }
        }

        public Result Disconnect()
        {
            string sM = nameof(Disconnect);
            try
            {
                return DisconnectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }

        public async Task<Result> ReadDataAsync()
        {
            string sMethod = nameof (ReadDataAsync);
            await oReadSemaphore.WaitAsync();
            try
            {
                if (!Connected)
                {
                    var oResConn = await ConnectAsync();
                    if (oResConn.HasError)
                    {
                        LogMan.Error(sC, sMethod, $"{sID} : Cannot open communication channel");
                        OnReadError?.Invoke(this, ResultEventArgs.CreateEventArgs(oResConn));
                        return oResConn;
                    }
                }
                LogMan.Trace(sC, sMethod, $"{sID} : Reading Data");
                OnReading?.Invoke(this, EventArgs.Empty);
                Result oRes;

                ReadCounters.TimeStart();
                oRes = await ReadImplementationAsync();

                if (oRes.Valid)
                {
                    ReadCounters.AddStatisticTime();
                    ReadCounters.AddValidEvent();
                    LogMan.Trace(sC, sMethod, $"{sID} : Read Data Successfull");
                    if (oRes.Tag != null)
                    {
                        OnRead?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                    }
                }
                else
                {
                    if (oRes.OperationResult == OperationResult.ErrorTimeout)
                    {
                        ReadCounters.AddTimeoutEvent();
                    }
                    else
                    {
                        ReadCounters.AddErrorEvent();
                        LogMan.Error(sC, sMethod, $"{sID} : Read Error");
                    }
                    OnReadError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                }
                return oRes;
            }
            catch (Exception ex)
            {
                return HandleError(sC, sMethod, ex, OnReadError);
            }
            finally
            {
                oReadSemaphore.Release();
            }
        }

        public Result ReadData()
        {
            string sM = nameof(ReadData);
            try
            {
                return ReadDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);    
            }
        }

        public async Task<Result> WriteDataAsync(object oObj)
        {
            string sMethod = nameof(WriteDataAsync);
            await oWriteSemaphore.WaitAsync();
            try
            {
                if (oObj == null)
                {
                    LogMan.Error(sC, sMethod, $"{sID} : object null");
                    return Result.CreateResultError(OperationResult.Error, "null object", 0);
                }
                if (!Connected)
                {
                    var oResConn = await ConnectAsync();
                    if (oResConn.HasError)
                    {
                        LogMan.Error(sC, sMethod, $"{sID} : Cannot open communication channel");
                        OnWriteError?.Invoke(this, ResultEventArgs.CreateEventArgs(oResConn));
                        return oResConn;
                    }
                }
                LogMan.Trace(sC, sMethod, $"{sID} : Writing Data");
                OnWriting?.Invoke(this, EventArgs.Empty);
                Result oRes;

                WriteCounters.TimeStart();
                oRes = await WriteImplementationAsync(oObj);

                if (oRes.Valid)
                {
                    WriteCounters.AddStatisticTime();
                    WriteCounters.AddValidEvent();
                    LogMan.Trace(sC, sMethod, $"{sID} : Write Data Successfull");
                    OnWrite?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                }
                else
                {
                    if (oRes.OperationResult == OperationResult.ErrorTimeout)
                    {
                        WriteCounters.AddTimeoutEvent();
                    }
                    else
                    {
                        WriteCounters.AddErrorEvent();
                        LogMan.Error(sC, sMethod, $"{sID} : Write Error");
                    }
                    OnWriteError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                }
                return oRes;
            }
            catch (Exception ex)
            {
                WriteCounters.AddErrorEvent();
                return HandleError(sC, sMethod, ex, OnWriteError);
            }
            finally
            {
                oWriteSemaphore.Release();
            }
        }

        public Result WriteData(object oObj )
        {
            string sM = nameof(WriteData);
            try
            {
                return WriteDataAsync(oObj).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Result.CreateResultError(ex);
            }
        }
    }
}
