using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSG.Threading;
using System.Collections.Concurrent;
using DSG.Shared;
using DSG.IO;
using DSG.Base;
using Result = DSG.Base.Result;

namespace DSG.IO
{
    public abstract class ConnectableBasePolling : ConnectableBase
    {
        static string sC = nameof(ConnectableBasePolling);   
        

//        ThreadBaseSimple oReadThread = new();
        ThreadBaseAsync oReadThread = new();

        public bool EnableReader 
        {
            get => oReadThread.Enabled;
            set => oReadThread.Enabled = value;
        }
        public int PollingReadMs 
        {
            get => oReadThread.WakeupTimeMs;
            set => oReadThread.WakeupTimeMs = value;
        }
       

        public ConnectableBasePolling()
        {
            //oReadThread.Wakeup += TaskReadData;
            oReadThread.WakeupAsync += TaskReadDataAsync;
            OnCreateImplementationAsync += ConnectableBasePolling_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += ConnectableBasePolling_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += ConnectableBasePolling_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync += ConnectableBasePolling_OnDisconnectImplementationAsync;
        }

        

        private async Task ConnectableBasePolling_OnCreateImplementationAsync(object sender, ResultEventArgs e)
        {
            oReadThread.Name = $"{Name}.Reader";
            e.AddResult(await oReadThread.CreateAsync());
          }

        private async Task ConnectableBasePolling_OnDestroyImplementationAsync(object sender, ResultEventArgs e)
        {
            e.AddResult(await oReadThread.DestroyAsync());
        }

        private Task ConnectableBasePolling_OnConnectImplementationAsync(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStart();
            return Task.CompletedTask;
        }

        private Task ConnectableBasePolling_OnDisconnectImplementationAsync(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStop();
            return Task.CompletedTask;
        }

       

        //private void TaskReadData(object? sender, EventArgs e)
        //{
        //    string sM = nameof(TaskReadData);
        //    Result res;
        //    do
        //    {
        //        res =  ReadDataAsync().GetAwaiter().GetResult();
        //    }
        //    while (res.Valid);
        //    if (res.HasError && res.OperationResult != OperationResult.ErrorTimeout )
        //    {
        //        LogMan.Error(sC, sM, $"{oReadThread.Name} : Error reading data : {res.ErrorMessage}");
        //    }
        //}

        private async Task TaskReadDataAsync(object arg1, ThreadEventArgs arg2)
        {
            string sM = nameof(TaskReadDataAsync);
            Result res;
            do
            {
                res = await ReadDataAsync();
                //await Task.Delay(10000);
                //res = Result.CreateResultError();
            }
            while (res.Valid);
            if (res.HasError && res.OperationResult != OperationResult.ErrorTimeout)
            {
                LogMan.Error(sC, sM, $"{oReadThread.Name} : Error reading data : {res.ErrorMessage}");
            }
        }
    }
}
