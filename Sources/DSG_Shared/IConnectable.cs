using DSG.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG
{
    public interface IConnectable
    {
        Task<OperationResult> ConnectAsync();
        OperationResult Connect();
        Task<OperationResult> DisconnectAsync();
        OperationResult Disconnect();
        bool Connected { get; }
        string ConnectionName { get; }

        public Task<OperationResult> ReadDataAsync();
        public OperationResult ReadData();
        public Task<OperationResult> WriteDataAsync();
        public OperationResult WriteData();    

        public int msConnectionTimeout { get; }
        public int msReadTimeout { get; }
        public int msWriteTimeout { get; }
        Dictionary<string, object> ConnectionParameters { get; set; }

        event EventHandler OnConnecting;
        event EventHandler OnConnect;
        event EventHandler OnConnectError;
        event EventHandler OnDisconnecting;
        event EventHandler OnDisconnect;
        event EventHandler OnDisconnectError;

        event EventHandler OnReading;
        event EventHandler OnRead;
        event EventHandler OnReadError;
        event EventHandler OnWriting;
        event EventHandler OnWrite;
        event EventHandler OnWriteError;
    }
}
