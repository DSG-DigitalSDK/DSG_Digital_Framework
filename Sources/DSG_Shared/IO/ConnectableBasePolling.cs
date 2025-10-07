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
            oReadThread.OnWakeup += TaskReadData;
            oWriteThread.OnSignal += TaskWriteData;
            OnCreateImplementation += ConnectableBasePolling_OnCreateImplementation;
            OnDestroyImplementation += ConnectableBasePolling_OnDestroyImplementation;
            OnConnectImplementation += ConnectableBasePolling_OnConnectImplementation;
            OnDisconnectImplementation += ConnectableBasePolling_OnDisconnectImplementation;
        }


        private void ConnectableBasePolling_OnCreateImplementation(object sender, ResultEventArgs e)
        {
            oReadThread.Name = $"{Name}.Reader";
            e.AddResult(oReadThread.Create());
            oWriteThread.Name = $"{Name}.Writer";
            e.AddResult(oWriteThread.Create());
            oWriteQueue.CreateQueue();
        }

        private void ConnectableBasePolling_OnDestroyImplementation(object sender, ResultEventArgs e)
        {
            e.AddResult(oReadThread.Destroy());
            e.AddResult(oWriteThread.Destroy());
            oWriteQueue.Clear();
        }

        private void ConnectableBasePolling_OnConnectImplementation(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStart();
            oWriteThread.TimerStart();
        }

        private void ConnectableBasePolling_OnDisconnectImplementation(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStop();
            oReadThread.TimerStop();
        }

        public void EnqueueWriteData( object oBuffer)
        {
            if (oBuffer == null)
                return;
            oWriteQueue.Enqueue(oBuffer);
            oWriteThread.ThreadSignal();
        }

        private void TaskReadData(object? sender, EventArgs e)
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
        }

        private void TaskWriteData(object? sender, EventArgs e)
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
        }
    }
}
