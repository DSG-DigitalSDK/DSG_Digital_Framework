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
        

        ThreadBase oReadThread = new ThreadBase();
        ThreadBase oWriteThread = new ThreadBase();

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
        public bool EnableWriter
        {
            get => oWriteThread.Enabled;
            set => oWriteThread.Enabled = value;
        }
        public int PollingWriteMs
        {
            get => oWriteThread.WakeupTimeMs;
            set => oWriteThread.WakeupTimeMs = value;
        }

        QueueHandler<object> oWriteQueue = new  QueueHandler<object>();
        public int WriteQueueCount => oWriteQueue.Count;
        public int WriteQueueCapacity
        {
            get => oWriteQueue.MaxQueueSize;
            set => oWriteQueue.MaxQueueSize = value;
        }

        public ConnectableBasePolling()
        {
            oReadThread.OnThreadWakeupAsync += TaskReadData;
            oWriteThread.OnThreadTriggerAsync += TaskWriteData;
            OnCreateImplementationAsync += ConnectableBasePolling_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += ConnectableBasePolling_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += ConnectableBasePolling_OnCreateImplementationAsync;
            OnDisconnectImplementationAsync += ConnectableBasePolling_OnDisconnectImplementationAsync;
        }

        private Task OReadThread_OnThreadWakeup(object arg1, ThreadEventArgs arg2)
        {
            throw new NotImplementedException();
        }

        private async Task ConnectableBasePolling_OnCreateImplementationAsync(object sender, ResultEventArgs e)
        {
            await Task.Run(() =>
            {
                oReadThread.Name = $"{Name}.Reader";
                e.AddResult(oReadThread.Create());
                oWriteThread.Name = $"{Name}.Writer";
                e.AddResult(oWriteThread.Create());
                oWriteQueue.CreateQueue();
            });
        }

        private async Task ConnectableBasePolling_OnDestroyImplementationAsync(object sender, ResultEventArgs e)
        {
            await Task.Run(() =>
            {
                e.AddResult(oReadThread.Destroy());
                e.AddResult(oWriteThread.Destroy());
                oWriteQueue.Clear();
            });
        }

        private Task ConnectableBasePolling_OnConnectImplementationAsync(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStart();
            oWriteThread.TimerStart();
            return Task.CompletedTask;
        }

        private Task ConnectableBasePolling_OnDisconnectImplementationAsync(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStop();
            oWriteThread.TimerStop();
            return Task.CompletedTask;
        }

        public void EnqueueWriteData( object oBuffer)
        {
            if (oBuffer == null)
                return;
            oWriteQueue.Enqueue(oBuffer);
            oWriteThread.ThreadSignal();
        }

        private Task TaskReadData(object? sender, EventArgs e)
        {
            string sM = nameof(TaskReadData);
            Result res;
            do
            {
                res = ReadData();
            }
            while (res.Valid);
            if (res.HasError && res.OperationResult != OperationResult.ErrorTimeout )
            {
                LogMan.Error(sC, sM, $"{oReadThread.Name} : Error reading data : {res.ErrorMessage}");
            }
            return Task.CompletedTask;  
        }

        private Task TaskWriteData(object? sender, EventArgs e)
        {
            string sM = nameof(TaskWriteData);
            while (oWriteQueue.Count > 0)
            {
                object oBuffer = oWriteQueue.Dequeue();
                var res = WriteData(oBuffer);                    
                if (PollingWriteMs > 0)
                {
                    Thread.Sleep(PollingWriteMs);  
                }
            }
            return Task.CompletedTask;  
        }
    }
}
