using DSG.Base;
using DSG.IO;
using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Drivers.Siemens
{
    internal class ConnectableTemplate : ReadWriteBase
    {
        string sC = nameof(ConnectableTemplate);

        static string ConnectionTemplate = "Connection/Example";

        // Client here
        // Namespace.ObjectClient client;
        
        // Override Connected
        //public new bool Connected => client?.Connected ?? false;

        // Other methods

        public ConnectableTemplate()
        {
            ResetClass();
        }

        void ResetClass()
        {
            // Class Initialization
            this.Name = "Template";
            //this.ConnectionName = "Conn";
            this.ConnectionString = ConnectionTemplate;
            this.EnableReader = true;
            this.PollingReadMs = 1000;
           // this.EnableWriter = false;
            this.PollingReadMs = 0;
            // Registring Events
            OnCreateImplementationAsync += Event_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += Event_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += Event_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync += Event_OnDisconnectImplementationAsync;
            // Initialize CLient
            // client = new Sharp7.S7Client();
        }

        private async Task Event_OnDestroyImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnDestroyImplementationAsync);
            await Task.Run(() =>
            {
                return Result.CreateResultError(OperationResult.ErrorNotImplemented);
            });
        }

        private async Task Event_OnCreateImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnCreateImplementationAsync);
            await Task.Run(() =>
            {
                return Result.CreateResultError(OperationResult.ErrorNotImplemented);
            });
        }
        private async Task Event_OnConnectImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnCreateImplementationAsync);
            await Task.Run(() =>
            {
                return Result.CreateResultError(OperationResult.ErrorNotImplemented);
            });
        }
        private async Task Event_OnDisconnectImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnDisconnectImplementationAsync);
            await Task.Run(() =>
            {
                return Result.CreateResultError(OperationResult.ErrorNotImplemented);
            });
        }

        protected override async Task<Result> ReadImplementationAsync()
        {
            string sM = nameof(ReadImplementationAsync);
            return await Task.Run(() =>
            {
                return Result.CreateResultError(OperationResult.ErrorNotImplemented);
            });
        }

        protected override async Task<Result> WriteImplementationAsync(object oWriteObj)
        {
            string sM = nameof(WriteImplementationAsync);
            return await Task.Run(() =>
            {
                return Result.CreateResultError(OperationResult.ErrorNotImplemented);
            });
        }

        public override Result FlushRead()
        {
            return Result.CreateResultSuccess();
        }

        public override Result FlushWrite()
        {
            return Result.CreateResultSuccess();
        }
    }
}
