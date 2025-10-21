using DSG.Base;
using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.IO
{
    public class ConnectableTester : ConnectableBasePolling
    {
        static readonly string sC=nameof(ConnectableTester);

        public int SleepMs { get; set; } = 1000;
        public int SleepRandomMaxMs { get; set; } = 1000;

        Random oRand = new Random((int)DateTime.Now.Ticks);

        async Task SleepAsync(string sMethod )
        {
            int iSleepA = SleepMs + oRand.Next(SleepRandomMaxMs);
            LogMan.Message(sC, sMethod, $"{Name} : Sleep for {iSleepA} ms");
                await Task.Delay(SleepMs);
        }
        void SimException(string sMethod)
        {
            if (oRand.NextDouble() < 1 / 100)
            {
                LogMan.Error(sC, sMethod, $"{Name} : Raising exception");
                throw new ArgumentException($"{Name}: Simulated Exception");
            }
        }

        async Task SimOperationAsync(string sMethod)
        {
            LogMan.Trace(sC, sMethod, $"{Name} : Simulating Operation");
            await SleepAsync(sMethod);
            SimException(sMethod);
        }

        public ConnectableTester()
        {
            OnCreateImplementationAsync += ConnectableTester_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += ConnectableTester_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += ConnectableTester_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync += ConnectableTester_OnDisconnectImplementationAsync;
        }

        private async Task ConnectableTester_OnDisconnectImplementationAsync(object arg1, ResultEventArgs arg2)
        {
            string sM = nameof(ConnectableTester_OnDisconnectImplementationAsync);
            await SimOperationAsync(sM);
            arg2.AddResult(Result.CreateResultSuccess("Disconnect OK"));
        }

        private async Task ConnectableTester_OnConnectImplementationAsync(object arg1, ResultEventArgs arg2)
        {
            string sM = nameof(ConnectableTester_OnConnectImplementationAsync);
            await SimOperationAsync(sM);
            arg2.AddResult(Result.CreateResultSuccess("Connect OK"));
        }

        private async Task ConnectableTester_OnDestroyImplementationAsync(object arg1, ResultEventArgs arg2)
        {
            string sM = nameof(ConnectableTester_OnDestroyImplementationAsync); 
            await SimOperationAsync(sM);
            arg2.AddResult(Result.CreateResultSuccess("Destroy OK"));
        }

        private async Task ConnectableTester_OnCreateImplementationAsync(object arg1, ResultEventArgs arg2)
        {
            string sM = nameof(ConnectableTester_OnCreateImplementationAsync);
            await SimOperationAsync(sM);
            arg2.AddResult(Result.CreateResultSuccess("Create OK"));
        }

        protected override async Task<Result> ReadImplementationAsync()
        {
            string sM = nameof(ReadImplementationAsync);
            await SimOperationAsync(sM);
            return Result.CreateResultSuccess("Read OK!");
        }

        protected override async Task<Result> WriteImplementationAsync(object oWriteObj)
        {
            string sM = nameof(WriteImplementationAsync);
            await SimOperationAsync(sM);
            return Result.CreateResultSuccess("Write OK!");
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
