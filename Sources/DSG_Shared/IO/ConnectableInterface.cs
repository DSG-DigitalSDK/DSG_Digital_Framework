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
    public interface ConnectableInterface : CreataInterface
    {
        bool Connected { get; }
        string ConnectionName { get; }
        int ConnectionTimeoutMs { get; set; }
        bool AutoReconnection { get; set; }
        int ReconnectionMaxThenth { get; set; }
        int ReconnectionWaitMs { get; set; }
      
        event EventHandler? Connecting;
        event EventHandler<ResultEventArgs>? Connection;
        event EventHandler<ResultEventArgs>? ConnectionError;
        event EventHandler? Disconnecting;
        event EventHandler<ResultEventArgs>? Disconnected;
        event EventHandler<ResultEventArgs>? DisconnectError;

        event EventHandler<ResultEventArgs>? AutoReconnecting;
        event EventHandler<ResultEventArgs>? AutoReconnect;
        event EventHandler<ResultEventArgs>? AutoReconnectError;
        event EventHandler<ResultEventArgs>? ConnectionFailure;

        Task<Result> ConnectAsync();
        Result Connect();
        Task<Result> DisconnectAsync();
        Result Disconnect();


    }
}
