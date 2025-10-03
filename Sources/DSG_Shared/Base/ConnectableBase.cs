using DSG.Base;
using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Shared.Base
{
    public abstract class ConnectableBase : CreateBase, IConnectable
    {
        static readonly string sC = nameof(ConnectableBase);

        object ConnLocker = new object();
        object ConnLockerRead = new object();
        object ConnLockerWrite = new object();

        string sID => $"'{Name}/{ConnectionName}'";

        public bool Connected { get; protected set; }

        public string ConnectionName
        {
            get => (string)GetDictionaryParam(nameof(Params.ConnectionName), "");
             set => SetDictionaryParam(nameof(Params.ConnectionName), value);
        }
        public string ConnectionString
        {
            get => (string)GetDictionaryParam(nameof(Params.ConnectionString), "");
            set => SetDictionaryParam(nameof(Params.ConnectionString), value);
        }

        public int ConnectionTimeoutMs
        {
            get => (int)GetDictionaryParam(nameof(Params.ConnectionTimeout), 5000);
            set => SetDictionaryParam(nameof(Params.ConnectionTimeout), value);
        }

        public int ReadTimeoutMs
        {
            get => (int)GetDictionaryParam(nameof(Params.ConnectionReadTimeout), 1000);
            set => SetDictionaryParam(nameof(Params.ConnectionReadTimeout), value);
        }

        public int WriteTimeoutMs
        {
            get => (int) GetDictionaryParam(nameof(Params.ConnectionWriteTimeout), 5000);
            set => SetDictionaryParam(nameof(Params.ConnectionWriteTimeout), value);
        }

        public StreamMode StreamMode
        {
            get => (StreamMode)GetDictionaryParam(nameof(Params.StreamMode), StreamMode.Text);
            set => SetDictionaryParam(nameof(Params.StreamMode), value);
        }


        public event EventHandler? OnConnecting;
        public event EventHandler? OnConnect;
        public event EventHandler<ResultEventArgs>? OnConnectError;
        public event EventHandler? OnDisconnecting;
        public event EventHandler? OnDisconnect;
        public event EventHandler<ResultEventArgs>? OnDisconnectError;
        //
        public event EventHandler? OnReading;
        public event EventHandler<ResultEventArgs>? OnRead;
        public event EventHandler<ResultEventArgs>? OnReadError;
        public event EventHandler? OnWriting;
        public event EventHandler<ResultEventArgs>? OnWrite;
        public event EventHandler<ResultEventArgs>? OnWriteError;

        protected abstract Result ConnectImpl();
        protected abstract Result DisconnectImpl();
        protected abstract Result ReadDataImpl();
        protected abstract Result WriteDataImpl(DataBuffer oBuffer);
        protected abstract Result WriteDataImpl(string sMessage);

        public Result Connect()
        {
            string sM = nameof(Connect);
            lock (ConnLocker)
            {
                try
                {
                    if (!Initialized)
                    {
                        var ResC = Create();
                        if (!Initialized)
                        {
                            LogMan.Error(sC, sM, $"'{Name}' : Creation Error");
                            return ResC;
                        }
                    }
                    if (Connected)
                    {
                        LogMan.Trace(sC, sM, $"{sID} already connected");
                        return Result.CreateResultSuccess();
                    }
                    LogMan.Trace(sC, sM, $"Connecting to '{Name}/{ConnectionName}'");
                    OnConnecting?.Invoke(this, EventArgs.Empty);
                    var oRes = ConnectImpl();
                    if (oRes.Valid)
                    {
                        LogMan.Message(sC, sM, $"{sID} Connected ");
                        Connected = true;
                        OnConnect?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        LogMan.Error(sC, sM, $"Error Connecting to {sID} : {oRes.ErrorMessage} ");
                        Connected = false;
                        OnConnectError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                    }
                    return oRes;
                }
                catch (Exception ex)
                {
                    LogMan.Exception(sC, sM, Name, ex);
                    return RaiseEventException(OnConnectError, ex);
                }
            }
        }
        
        public async Task<Result> ConnectAsync() => await Task.Run(() => Connect());

        public Result Disconnect()
        {
            string sM = nameof(Connect);
            lock (ConnLocker)
            {
                try
                {
                    var bConn = Connected;
                    if (bConn)
                    {
                        OnDisconnecting?.Invoke(this, EventArgs.Empty);
                    }
                    LogMan.Trace(sC, sM, $"Disconnecting from {sID}");
                    var oRes = DisconnectImpl();
                    if (oRes.Valid)
                    {
                        LogMan.Message(sC, sM, $"{sID} Disconnected");
                        Connected = false;
                        if (bConn)
                        {
                            OnDisconnect?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        if (bConn)
                        {
                            OnDisconnectError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                        }
                    }
                    return oRes;
                }
                catch (Exception ex)
                {
                    LogMan.Exception(sC, sM, Name, ex);
                    return RaiseEventException(OnDisconnectError, ex);
                }
            }
        }

        public async Task<Result> DisconnectAsync() => await Task.Run(() => Disconnect());

        public Result ReadData()
        {
            string sMethod = nameof (ReadData);
            try
            {
                lock (ConnLocker)
                {
                    if (!Connected)
                    {
                        var oResConn = Connect();
                        if (oResConn.HasError)
                        {
                            LogMan.Error(sC, sMethod, $"'{Name}/{ConnectionName}' : Cannot open communication channel");
                            OnReadError?.Invoke(this, ResultEventArgs.CreateEventArgs(oResConn));
                            return oResConn;
                        }
                    }
                }
                LogMan.Trace(sC, sMethod, $"'{Name}/{ConnectionName}' : Reading Data");
                OnReading?.Invoke(this, EventArgs.Empty);
                Result oRes;
                lock (ConnLockerRead)
                {
                    oRes = ReadDataImpl();
                }
                if (oRes.Valid)
                {
                    LogMan.Trace(sC, sMethod, $"'{Name}/{ConnectionName}' : Read Data Successfull");
                    //                    OnRead?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes), oRes.Tag as DataBuffer, null));
                    OnRead?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                }
                else
                {
                    if (oRes.OperationResult != OperationResult.ErrorTimeout)
                    {
                        LogMan.Error(sC, sMethod, $"'{Name}/{ConnectionName}' : Read Error");
                        OnReadError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
//                        OnReadError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes, oRes.Tag as DataBuffer, null));
                    }
                }
                return oRes;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sMethod, Name, ex);
                return RaiseEventException(OnReadError, ex);
            }
        }

        public async Task<Result> ReadDataAsync() => await Task.Run(() => ReadData());
       
        protected Result WriteData(object oObj)
        {
            string sMethod = nameof(WriteData);
            try
            {
                lock (ConnLocker)
                {
                    if (!Connected)
                    {
                        var oResConn = Connect();
                        if (oResConn.HasError)
                        {
                            LogMan.Error(sC, sMethod, $"'{Name}/{ConnectionName}' : Cannot open communication channel");
                            OnWriteError?.Invoke(this, ResultEventArgs.CreateEventArgs(oResConn));
                            return oResConn;
                        }
                    }
                }
                LogMan.Trace(sC, sMethod, $"'{Name}/{ConnectionName}' : Writing Data");
                OnWriting?.Invoke(this, EventArgs.Empty);
                Result oRes;
                lock (ConnLockerWrite)
                {
                    if (oObj is DataBuffer oData)
                    {
                        oRes = WriteDataImpl(oData);       
                    }
                    else if (oObj is string sMessage)
                    {
                        oRes = WriteDataImpl(sMessage);
                    }
                    else
                    {
                        oRes = Result.CreateResultError(OperationResult.Error, $"Invalid Object Type : {oObj?.GetType().ToString() ?? "null object"}", 0);
                    }
                }
                if (oRes.Valid)
                {
                    LogMan.Trace(sC, sMethod, $"'{Name}/{ConnectionName}' : Write Data Successfull");
                    // OnWrite?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes, null, oRes.Tag as DataBuffer));
                    OnWrite?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                }
                else
                {
                    LogMan.Error(sC, sMethod, $"'{Name}/{ConnectionName}' : Write Error");
                    // OnWriteError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes, null, oRes.Tag as DataBuffer));
                    OnWriteError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
                }
                return oRes;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sMethod, Name, ex);
                return RaiseEventException(OnWriteError, ex);
            }
        }

        public Result WriteData(DataBuffer oBuffer) => WriteData((object)oBuffer);
        public Result WriteData(string sMessage) => WriteData((object)sMessage);
        public async Task<Result> WriteDataAsync(DataBuffer oBuffer) => await Task.Run(() => WriteData(oBuffer));
        public async Task<Result> WriteDataAsync(string sMessage) => await Task.Run(() => WriteData(sMessage));
    }
}
