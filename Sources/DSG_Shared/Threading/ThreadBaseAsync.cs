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
    public class ThreadBaseAsync : CreateBase
    {
        private static readonly string sC = nameof(ThreadBaseAsync);

        private Thread? oThread;
        private readonly AutoResetEvent oSignalTrigger = new(false);
        private readonly AutoResetEvent oSignalQuit = new(false);
        private readonly AutoResetEvent oSignalWakeupRestart = new(false);

        private CancellationTokenSource oCts = new();

        //private readonly SemaphoreSlim _evtLock = new(1, 1); // inizializzato a 1, blocca l’accesso concorrente

        private readonly SemaphoreSlim _oLock = new(1, 1);


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
                oSignalWakeupRestart.Set(); // Wake up thread to reset timer
            }
        }

        public bool AllowEventOverlap { get; set; } = false;

        /// <summary>
        /// True if thread is alive
        /// </summary>
        public bool Running => oThread?.IsAlive ?? false;

        public bool EventTaskEnable { get; private set; }

        //  public object OnThreadWakeup { get; set; }
        //  public object OnTaskWakeup { get; set; }

        /// <summary>
        /// Raised on external trigger
        /// </summary>
        public event Func<object, ThreadEventArgs, Task>? TriggerAsync;

        /// <summary>
        /// Raised on polling timer
        /// </summary>
        public event Func<object, ThreadEventArgs, Task>? WakeupAsync;

        /// <summary>
        /// Raised on quit signal
        /// </summary>
        public event Func<object, ThreadEventArgs, Task>? QuitAsync;


        public ThreadBaseAsync()
        {
            OnCreateImplementationAsync += CreateThreadAsync;
            OnDestroyImplementationAsync += DestroyThreadAsync;
            OnDisposing += ThreadBase_OnDisposing;
        }

        private void ThreadBase_OnDisposing(object? sender, EventArgs e)
        {
            SignalQuit();
            oCts?.Dispose();
            oSignalTrigger?.Dispose();
            oSignalQuit?.Dispose();
            oSignalWakeupRestart?.Dispose();
        }

        public void TimerStart() => TimerEnabled = true;
        public void TimerStop() => TimerEnabled = false;

        public void SignalTrigger() => oSignalTrigger?.Set();

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
                oSignalTrigger.Reset();
                oSignalQuit.Reset();
                oSignalWakeupRestart.Reset();

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
                            {
                            LogMan.Warning(sC, nameof(DestroyThreadAsync), $"Thread {Name} did not stop in 5s.");
                        }
                    }

                    oThread = null;

//                    oSignalTrigger.Reset();
  //                  oSignalQuit.Reset();
    //                oSignalWakeupRestart.Reset();

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

            var events = new WaitHandle[] { oSignalQuit, oSignalTrigger, oSignalWakeupRestart };

                bool bExit = false;
            while (!bExit)
            {
                try
                {
                    if (oCts?.IsCancellationRequested == true)
                        {
                        break;
                    }

                    int waitTime = WakeupTimeMs == 0 ? Timeout.Infinite : WakeupTimeMs;
                    int index = WaitHandle.WaitAny(events, waitTime);
                    switch (index)
                    {
                        case 0:
                            bExit = true;
                            RaiseAsync(QuitAsync, "Quit").GetAwaiter().GetResult();
                            break;
                        case 1:
                            RaiseAsync(TriggerAsync, "Trigger").GetAwaiter().GetResult();
                            break;
                        case 2:
                        default:
                            if (TimerEnabled)
                                {
                                if (iWakeupTimeMs > 0)
                                {
                                    RaiseAsync(WakeupAsync, "Wakeup").GetAwaiter().GetResult();
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


       
        private async Task RaiseAsync(Func<object, ThreadEventArgs, Task>? evt, string tag )
        {
            string sM = nameof(RaiseAsync);
            if (evt == null)
            {
                return;
            }
            try
            {
                var args = new ThreadEventArgs
                {
                    Thread = oThread,
                     CancellationToken = oCts.Token
                };

                if (AllowEventOverlap)
                {
                    LogMan.Message(sC, sM, $"{Name} Detected {tag}");
                    await evt.Invoke(this, args).ContinueWith(LogUnhandledTaskException);
                    LogMan.Message(sC, sM, $"{Name} {tag} completed");
                }
                else
                {
                    // Esecuzione bloccante: un task per volta
                    await _oLock.WaitAsync();
                    LogMan.Message(sC, sM, $"{Name}/{tag} Enter Lock");
                    try
                    {
                        LogMan.Message(sC, sM, $"{Name} Detected {tag}");
                        await evt.Invoke(this, args);
                        LogMan.Message(sC, sM, $"{Name} {tag} completed");
                    }
                    finally
                    {
                        LogMan.Message(sC, sM, $"{Name}/{tag} Exit Lock");
                        _oLock.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, $"{Name}/{tag}", ex);
            }
        }

        private void LogUnhandledTaskException(Task t)
        {
            String sM = nameof(LogUnhandledTaskException);
            if (t.IsFaulted && t.Exception != null)
                {
                LogMan.Exception(sC, sM, Name, t.Exception);
            }
        }

        #endregion

 
    }
}
