using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Shared.Base
{
    public interface IConnectable
    {
        Task<Result> ConnectAsync();
        Result Connect();
        Task<Result> DisconnectAsync();
        Result Disconnect();
        bool Connected { get; }
        string ConnectionName { get; }

        Task<Result> ReadDataAsync();
        Result ReadData();
        Task<Result> WriteDataAsync(DataBuffer oBuffer);
        Result WriteData(DataBuffer oBuffer);        
        Task<Result> WriteDataAsync(string sMessage);        
        Result WriteData(string sMessage);

        int ConnectionTimeoutMs { get; }
        int ReadTimeoutMs { get; }
        int WriteTimeoutMs { get; }

        event EventHandler? OnConnecting;
        event EventHandler? OnConnect;
        event EventHandler<ResultEventArgs>? OnConnectError;
        event EventHandler? OnDisconnecting;
        event EventHandler? OnDisconnect;
        event EventHandler<ResultEventArgs>? OnDisconnectError;

        event EventHandler? OnReading;
        event EventHandler<ResultEventArgs>? OnRead;
        event EventHandler<ResultEventArgs>? OnReadError;
        event EventHandler? OnWriting;
        event EventHandler<ResultEventArgs>? OnWrite;
        event EventHandler<ResultEventArgs>? OnWriteError;
    }
}
