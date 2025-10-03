using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSG.Threading;
using DSG.Base;
using System.Collections.Concurrent;

namespace DSG_Shared.Base
{
    public abstract class ConnectableBasePolling : ConnectableBase
    {
        static string sC = nameof(ConnectableBasePolling);   
        
        ThreadBase oReadThread = new ThreadBase();
        ThreadBase oWriteThread = new ThreadBase();

        public bool UsePollingReader { get; set; } = true;
        public int PollingReadMs { get; set; } = 1000;
        public bool UsePollingWriter { get; set; } = true;
        public int PollingWriteMs { get; set; } = 1000;

        ConcurrentQueue<object> oQueue = new ConcurrentQueue<object>();
        public int WriteQueueLength => oQueue.Count;

        public ConnectableBasePolling()
        {
            OnCreate += ConnectableBaseReader_OnCreate;
            OnDestroy += ConnectableBaseReader_OnDestroy;
            OnConnect += ConnectableBaseReader_OnConnect;
            OnDisconnect += ConnectableBaseReader_OnDisconnect;
            oReadThread.OnWakeup += TaskReadData;
            oWriteThread.OnSignal += TaskWriteData;
        }

       

        private void ConnectableBaseReader_OnCreate(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oReadThread.Name = $"{Name}.Reader";
                oReadThread.msPollingTime = PollingReadMs;
                oReadThread.AllowTaskOverlap = false;
                oReadThread.PollingAutomaticStart = false;    
                oReadThread.Create();
            }
            if (UsePollingWriter)
            {
                oWriteThread.Name = $"{Name}.Writer";
                oWriteThread.msPollingTime = PollingWriteMs;
                oWriteThread.AllowTaskOverlap = false;
                oWriteThread.PollingAutomaticStart = false;
                oWriteThread.Create();
            }
        }

        private void ConnectableBaseReader_OnDestroy(object? sender, EventArgs e)
        {
            oReadThread.Destroy();
            oWriteThread.Destroy();
            oQueue.Clear();
        }

        private void ConnectableBaseReader_OnConnect(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oReadThread.TimerStart();
            }
            if (UsePollingWriter)
            {
                oWriteThread.TimerStart();
            }
        }

        private void ConnectableBaseReader_OnDisconnect(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oReadThread.TimerStop();
            }
            if (UsePollingWriter)
            {
                oReadThread.TimerStop();
            }
        }

        public void EnqueueWriteData(DataBuffer oBuffer)
        {
            if (oBuffer == null)
                return;
            oQueue.Enqueue(oBuffer);
        }
        public void EnqueueWriteData(String sMessage)
        {
            oQueue.Enqueue(sMessage);
            oWriteThread.SignalAction();
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
            while (oQueue.Count > 0)
            {
                if (oQueue.TryDequeue(out var obj))
                {
                    var res = WriteData(obj);                    
                }
                if (PollingWriteMs > 0)
                {
                    Thread.Sleep(PollingWriteMs);  
                }
            }
        }
    }
}
