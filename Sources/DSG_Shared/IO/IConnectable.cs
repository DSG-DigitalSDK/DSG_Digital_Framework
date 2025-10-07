using DSG.Base;
using DSG.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Result = DSG.Base.Result;

namespace DSG.IO
{
    public interface IConnectable : ICreatable
    {
        bool Connected { get; }
        string ConnectionName { get; }
        int ConnectionTimeoutMs { get; }
        int ReadTimeoutMs { get; }
        int WriteTimeoutMs { get; }

        event EventHandler? OnConnecting;
        event EventHandler<ResultEventArgs>? OnConnect;
        event EventHandler<ResultEventArgs>? OnConnectError;
        event EventHandler? OnDisconnecting;
        event EventHandler<ResultEventArgs>? OnDisconnect;
        event EventHandler<ResultEventArgs>? OnDisconnectError;

        event EventHandler? OnReading;
        event EventHandler<ResultEventArgs>? OnRead;
        event EventHandler<ResultEventArgs>? OnReadError;
        event EventHandler? OnWriting;
        event EventHandler<ResultEventArgs>? OnWrite;
        event EventHandler<ResultEventArgs>? OnWriteError;

        Task<Result> ConnectAsync();
        Result Connect();
        Task<Result> DisconnectAsync();
        Result Disconnect();


        Task<Result> ReadDataAsync();
        Result ReadData();
        Task<Result> WriteDataAsync(object oBuffer);
        Result WriteData(object oBuffer);        

    }
}
