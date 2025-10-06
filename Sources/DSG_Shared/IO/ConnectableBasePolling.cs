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

        public bool UsePollingReader { get; set; } = true;
        public int PollingReadMs { get; set; } = 1000;
        public bool UsePollingWriter { get; set; } = true;
        public int PollingWriteMs { get; set; } = 1000;

        QueueHandler<object> oQueue = new  QueueHandler<object>();
        public int WriteQueueLength => oQueue.Count;
        
        public ConnectableBasePolling()
        {
            oReadThread.OnWakeup += TaskReadData;
            oWriteThread.OnSignal += TaskWriteData;
        }

        protected override Result CreateImpl()
        {
            if (UsePollingReader)
            {
                oReadThread.Name = $"{Name}.Reader";
                oReadThread.WakeupTimeMs = (uint)PollingReadMs;
                oReadThread.Create();
            }
            if (UsePollingWriter)
            {
                oWriteThread.Name = $"{Name}.Writer";
                oWriteThread.WakeupTimeMs = (uint)PollingWriteMs;
                oWriteThread.Create();
            }
            return Result.CreateResultSuccess();
        }

        protected override Result DestroyImpl()
        {
            oReadThread.Destroy();
            oWriteThread.Destroy();
            oQueue.Clear();
            return Result.CreateResultSuccess();
        }

        protected override Result ConnectImpl()
        {
            if (UsePollingReader)
            {
                oReadThread.TimerStart();
            }
            if (UsePollingWriter)
            {
                oWriteThread.TimerStart();
            }
            return Result.CreateResultSuccess();
        }


        protected override Result DisconnectImpl()
        {
            if (UsePollingReader)
            {
                oReadThread.TimerStop();
            }
            if (UsePollingWriter)
            {
                oReadThread.TimerStop();
            }
            return Result.CreateResultSuccess();    
        }

        public void EnqueueWriteData(DataBuffer oBuffer)
        {
            if (oBuffer == null)
                return;
            oQueue.Enqueue(oBuffer);
            oWriteThread.ThreadSignal();
        }
        public void EnqueueWriteData(string sMessage)
        {
            oQueue.Enqueue(sMessage);
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
            while (oQueue.Count > 0)
            {
                var obj = oQueue.Dequeue();
                var res = WriteData(obj);                    
                if (PollingWriteMs > 0)
                {
                    Thread.Sleep(PollingWriteMs);  
                }
            }
        }
    }
}
