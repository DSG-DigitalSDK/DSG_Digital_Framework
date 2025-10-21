using DSG.Base;
using DSG.Log;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace DSG.Threading
{
    /// <summary>
    /// ThreadBase provides a thread with simple signaling mechanism.<br/>
    /// Supports:
    /// <list type="bullet">
    /// <item>External trigger signal</item>
    /// <item>Quit signal</item>
    /// <item>Polling timer signal (Wakeup)</item>
    /// </list>
    /// </summary>
    public class ThreadBaseSimple : CreateBase
    {
        private static readonly string sC = nameof(ThreadBaseSimple);

        private Thread? oThread;
        private readonly AutoResetEvent oSignalExecute = new(false);
        private readonly AutoResetEvent oSignalQuit = new(false);
        private readonly AutoResetEvent oSignalWakeup = new(false);

        private CancellationTokenSource oCts = new();

        //private readonly SemaphoreSlim _evtLock = new(1, 1); // inizializzato a 1, blocca l’accesso concorrente
        private readonly object oLocker = new();

        private int iWakeupTimeMs = 1000;

        /// <summary>
        /// Enable or disable the timer (Wakeup events)
        /// </summary>
        public bool TimerEnabled { get; set; } = true;

        /// <summary>
        /// Polling interval in milliseconds. Set 0 to wait indefinitely
        /// </summary>
        public int WakeupTimeMs
        {
            get => Volatile.Read(ref iWakeupTimeMs);
            set
            {
                Volatile.Write(ref iWakeupTimeMs, value);
                oSignalWakeup.Set(); // Wake up thread to reset timer
            }
        }

        public bool AllowEventOverlap { get; set; } = false;

        /// <summary>
        /// True if thread is alive
        /// </summary>
        public bool Running => oThread?.IsAlive ?? false;

       
        /// <summary>
        /// Raised on external trigger
        /// </summary>
        public event EventHandler<ThreadEventArgs> Trigger;

        /// <summary>
        /// Raised on polling timer
        /// </summary>
        public event EventHandler<ThreadEventArgs> Wakeup;

        /// <summary>
        /// Raised on quit signal
        /// </summary>
        public event EventHandler<ThreadEventArgs> Quit;

      
        public ThreadBaseSimple()
        {
            OnCreateImplementationAsync += CreateThreadAsync;
            OnDestroyImplementationAsync += DestroyThreadAsync;
            OnDisposing += ThreadBase_OnDisposing;
        }

        private void ThreadBase_OnDisposing(object? sender, EventArgs e)
        {
            SignalQuit();
            oCts?.Dispose();
            oSignalExecute?.Dispose();
            oSignalQuit?.Dispose();
            oSignalWakeup?.Dispose();
        }

        public void TimerStart() => TimerEnabled = true;
        public void TimerStop() => TimerEnabled = false;

        public void SignalTrigger() => oSignalExecute.Set();

        public void SignalQuit()
        {
            oCts?.Cancel();
            oSignalQuit?.Set();
        }

        #region Thread Lifecycle

        private async Task CreateThreadAsync(object? sender, ResultEventArgs e)
        {
            await Task.Run(() =>
            {
                LogMan.Message(sC, nameof(CreateThreadAsync), $"{Name}: Creating thread");

                oCts = new CancellationTokenSource();
                oSignalExecute.Reset();
                oSignalQuit.Reset();
                oSignalWakeup.Reset();

                oThread = new Thread(ThreadJob)
                {
                    IsBackground = true,
                    Name = Name
                };
                oThread.Start();

                e.AddResult(Result.CreateResultSuccess());
            });
        }

        private async Task DestroyThreadAsync(object? sender, ResultEventArgs e)
        {
            await Task.Run(() =>
            {
                if (oThread == null)
                {
                    e.AddResult(Result.CreateResultSuccess());
                }
                else
                {
                    LogMan.Message(sC, nameof(DestroyThreadAsync), $"{Name}: Destroying thread");

                    SignalQuit();

                    if (oThread != null && oThread.IsAlive)
                    {
                        if (!oThread.Join(5000))
                            LogMan.Warning(sC, nameof(DestroyThreadAsync), $"Thread {Name} did not stop in 5s.");
                    }

                    oThread = null;

                    oSignalExecute.Reset();
                    oSignalQuit.Reset();
                    oSignalWakeup.Reset();

                    e.AddResult(Result.CreateResultSuccess());
                }
            });
        }

        #endregion

        #region Thread Execution

        private void ThreadJob()
        {
            string sM = nameof(ThreadJob);
            LogMan.Message(sC, sM, $"{Name}: Thread started");

            var events = new WaitHandle[] { oSignalQuit, oSignalExecute, oSignalWakeup };

                bool bExit = false;
            while (!bExit)
            {
                try
                {
                    if (oCts?.IsCancellationRequested == true)
                        break;

                    int waitTime = WakeupTimeMs == 0 ? Timeout.Infinite : WakeupTimeMs;
                    int index = WaitHandle.WaitAny(events, waitTime);
                    switch (index)
                    {
                        case 0:
                            bExit = true;
                            LogMan.Message(sC, sM, $"{Name} : Detected Quit Signal ");
                            Quit?.Invoke(this, new ThreadEventArgs()
                            {
                                CancellationToken=oCts?.Token,
                                Thread = oThread,
                            });
                            break;
                        case 1:
                            LogMan.Message(sC, sM, $"{Name} : Detected Trigger Signal ");
                            Trigger?.Invoke(this, new ThreadEventArgs()
                            {
                                CancellationToken = oCts?.Token,
                                Thread = oThread,
                            });
                            break;
                        case 2:
                        default:
                            if (TimerEnabled)
                            {
                                if (iWakeupTimeMs > 0)
                                {
                                    LogMan.Message(sC, sM, $"{Name} : Detected Wakeup ");
                                    Wakeup?.Invoke(this, new ThreadEventArgs()
                                    {
                                        CancellationToken = oCts?.Token,
                                        Thread = oThread,
                                    });
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogMan.Exception(sC, sM, Name, ex);
                }

            }

            LogMan.Message(sC, sM, $"{Name}: Thread ended");
        }



        #endregion

    }
}
