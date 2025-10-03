using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSG.Threading;

namespace DSG_Shared.Base
{
    public abstract class ConnectableBaseReader : ConnectableBase
    {
        static string sC = nameof(ConnectableBaseReader);   
        
        ThreadTimer oTimer = new ThreadTimer();

        public bool UsePollingReader { get; set; } = true;
        public int msPollingRead { get; set; } = 1000;

        public ConnectableBaseReader()
        {
            OnCreate += ConnectableBaseReader_OnCreate;
            OnDestroy += ConnectableBaseReader_OnDestroy;
            OnConnect += ConnectableBaseReader_OnConnect;
            OnDisconnect += ConnectableBaseReader_OnDisconnect;
            oTimer.OnWakeup += TaskReadData;
        }

        private void ConnectableBaseReader_OnCreate(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oTimer.Name = $"{Name}.Timer";
                oTimer.msPollingTime = msPollingRead;
                oTimer.AllowTaskOverlap = false;
                oTimer.PollingAutomaticStart = false;    
                oTimer.Create();
            }
        }

        private void ConnectableBaseReader_OnDestroy(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oTimer.Destroy();
            }
        }

        private void ConnectableBaseReader_OnConnect(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oTimer.TimerStart();
            }
        }

        private void ConnectableBaseReader_OnDisconnect(object? sender, EventArgs e)
        {
            if (UsePollingReader)
            {
                oTimer.TimerStop();
            }
        }

        private void TaskReadData(object? sender, EventArgs e)
        {
            string sM = nameof(TaskReadData);
            var res = ReadData();
            if (res.HasError)
            {
                LogMan.Error(sC, sM, $"{oTimer.Name} : Error reading data : {res.ErrorMessage}");
            }
        }
    }
}
