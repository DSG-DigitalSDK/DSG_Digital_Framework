
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DSG.Log;
using DSG_Shared.Base;


namespace DSG.Threading
{
    /// <summary>
    /// Encapsulates generic task timer behavior
    /// </summary>
    /// <typeparam name="T">class instance to store</typeparam>
    public class ThreadBase : CreateBase
    {
        string sC = nameof(ThreadBase);

        public int msPollingTime { get; set; } = 1000;
        public bool PollingAutomaticStart { get; set; } = true;
        public bool AllowTaskOverlap { get; set; } = true;

        object locker = new object();

        AutoResetEvent[] aAutoResetEvents = { new AutoResetEvent(false), new AutoResetEvent(false) };

        /// <summary>
        /// Raised when data is ready to be written
        /// </summary>
        public event EventHandler? OnWakeup;
        public event EventHandler? OnSignal;
        public event EventHandler? OnQuit;


        /// <summary>
        /// Start flag
        /// </summary>
        bool bTimerEnabled = false;

        Thread? oThreadTimer;
        CancellationTokenSource? oTokenSource;
     
        public void TimerStart()
        {
            bTimerEnabled = true;
        }
        public void TimerStop()
        {
            bTimerEnabled = false;
        }


        public void SignalAction() => aAutoResetEvents[0].Set();
        public void SignalQuit() => aAutoResetEvents[1].Set();


        protected override Result CreateImpl()
        {
            string sM = nameof(CreateImpl);
            lock (locker)
            {
                LogMan.Trace(sC, sM, $"Creating Thread '{Name}.Thread'");
                oTokenSource = new CancellationTokenSource();
                oThreadTimer = new Thread(new ThreadStart(ThreadJob));
                oThreadTimer.Start();
                oThreadTimer.Name = Name + ".Thread";
                return Result.CreateResultSuccess();
            }
        }

        protected override Result DestroyImpl()
        {
            string sM= nameof(DestroyImpl);
            lock (locker)
            {
                SignalQuit();
                if (oThreadTimer != null)// && !oTaskTimer.IsCompleted)
                {
                    LogMan.Trace(sC, sM, $"Killing Thread '{oThreadTimer.Name}'");
                    oTokenSource?.Cancel();
                    if (!oThreadTimer.Join(5000))
                    {
                        LogMan.Warning(sC, sM, $"Thread '{oThreadTimer.Name}' does not ends, leave it orphan");
                    }
                }
                oThreadTimer = null;
                // Renew the events
                aAutoResetEvents = new AutoResetEvent[2];
                aAutoResetEvents[0] = new AutoResetEvent(false);
                aAutoResetEvents[1] = new AutoResetEvent(false);
                return Result.CreateResultSuccess();
            }
        }


        Task? oTaskPolling;
        Task? oTaskSignal;

        void ThreadJob()
        {
            string sM = nameof(ThreadJob);
            try
            {
                var autoResetEvents = aAutoResetEvents;
                LogMan.Message(sC, sM, $"{Name} : Processing Task Start");
                bool bExit = false;
                while (!bExit)
                {
                    int iID = WaitHandle.WaitAny(autoResetEvents, msPollingTime);
                    switch (iID)
                    {
                        case 0:
                            {
                                try
                                {
                                    LogMan.Trace(sC, sM, $"{Name} : Detected Action Signal");
                                    if (oTaskPolling == null || oTaskPolling.IsCompleted )
                                    {
                                        oTaskPolling = Task.Run(() => OnSignal?.Invoke(this, new TaskEventArgs() { Name = Name, CancellationTokenSource = oTokenSource }));
                                    }
                                    else
                                    {
                                        LogMan.Trace(sC, sM, $"{Name} : Polling Read Task Already Running");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogMan.Exception(sC, sM, Name, ex);
                                }
                            }
                            break;
                        case 1:
                            {
                                bExit = true;
                                try
                                {
                                    LogMan.Message(sC, sM, $"{Name} : Detected Quit Signal");
                                    OnQuit?.Invoke(this, new TaskEventArgs() { Name = Name, CancellationTokenSource = oTokenSource });
                                }
                                catch (Exception ex)
                                {
                                    LogMan.Exception(sC, sM, Name, ex);
                                }
                            }
                            break;
                        default:
                            {
                                if (!bTimerEnabled)
                                {
                                    break;
                                }
                                LogMan.Trace(sC, sM, $"{Name} : Detected Timeout Signal");
                                if (oTaskPolling == null || oTaskPolling.IsCompleted)
                                {
                                    oTaskPolling = Task.Run(() => OnWakeup?.Invoke(this, new TaskEventArgs() { Name = Name, CancellationTokenSource = oTokenSource }));
                                }
                                else
                                {
                                    LogMan.Trace(sC, sM, $"{Name} : Polling Read Task Already Running");
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, $"{Name} : Unexpected Exception raised", ex);
            }
            LogMan.Message(sC, sM, $"{Name} : Processing Task End");
        }
    }
}
