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

        event EventHandler? Connecting;
        event EventHandler<ResultEventArgs>? Connection;
        event EventHandler<ResultEventArgs>? ConnectionError;
        event EventHandler? Disconnecting;
        event EventHandler<ResultEventArgs>? Disconnected;
        event EventHandler<ResultEventArgs>? DisconnectError;

        event EventHandler? DataReading;
        event EventHandler<ResultEventArgs>? DataReaded;
        event EventHandler<ResultEventArgs>? DataReadError;
        event EventHandler? DataWriting;
        event EventHandler<ResultEventArgs>? DataWritten;
        event EventHandler<ResultEventArgs>? DataWriteError;

        Task<Result> ConnectAsync();
        Result Connect();
        Task<Result> DisconnectAsync();
        Result Disconnect();


        Task<Result> ReadDataAsync();
  //      Result ReadData();
        Task<Result> WriteDataAsync(object oBuffer);
  //      Result WriteData(object oBuffer);


        public Result FlushRead();
        public Result FlushWrite();
        public Result Flush();

    }
}
