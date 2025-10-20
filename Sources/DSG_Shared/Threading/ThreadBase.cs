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
    public class ThreadBase : CreateBase
    {
        private static readonly string sC = nameof(ThreadBase);

        /// <summary>
        /// Signal type for the thread
        /// </summary>
        public enum EnumSignalType
        {
            Unknown = 0,
            Exception = 1,
            Quit = 2,
            Trigger = 3,
            Timeout = 4,
            TimeDrop = 5,
        }

        private Thread? oThread;
        private readonly AutoResetEvent oSignalExecute = new(false);
        private readonly AutoResetEvent oSignalQuit = new(false);
        private readonly AutoResetEvent oSignalWakeupRestart = new(false);

        private CancellationTokenSource oCts = new();

        private readonly SemaphoreSlim _evtLock = new(1, 1); // inizializzato a 1, blocca l’accesso concorrente

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
        public event Func<object, ThreadEventArgs, Task>? OnThreadTriggerAsync;

        /// <summary>
        /// Raised on polling timer
        /// </summary>
        public event Func<object, ThreadEventArgs, Task>? OnThreadWakeupAsync;

        /// <summary>
        /// Raised on quit signal
        /// </summary>
        public event Func<object, ThreadEventArgs, Task>? OnThreadQuitAsync;

        public ThreadBase()
        {
            OnCreateImplementationAsync += CreateThreadAsync;
            OnDestroyImplementationAsync += DestroyThreadAsync;
            OnDisposing += ThreadBase_OnDisposing;
        }

        private void ThreadBase_OnDisposing(object? sender, EventArgs e)
        {
            ThreadQuit();
            oCts?.Dispose();
            oSignalExecute?.Dispose();
            oSignalQuit?.Dispose();
            oSignalWakeupRestart?.Dispose();
        }

        public void TimerStart() => TimerEnabled = true;
        public void TimerStop() => TimerEnabled = false;

        public void ThreadSignal() => oSignalExecute.Set();

        public void ThreadQuit()
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
                LogMan.Message(sC, nameof(DestroyThreadAsync), $"{Name}: Destroying thread");

                ThreadQuit();

                if (oThread != null && oThread.IsAlive)
                {
                    if (!oThread.Join(5000))
                        LogMan.Warning(sC, nameof(DestroyThreadAsync), $"Thread {Name} did not stop in 5s.");
                }

                oThread = null;
                e.AddResult(Result.CreateResultSuccess());
            });
        }

        #endregion

        #region Thread Execution

        private void ThreadJob()
        {
            string sM = nameof(ThreadJob);
            LogMan.Message(sC, sM, $"{Name}: Thread started");

            var events = new WaitHandle[] { oSignalQuit, oSignalExecute, oSignalWakeupRestart };

            try
            {
                while (true)
                {
                    if (oCts?.IsCancellationRequested == true)
                        break;

                    int waitTime = WakeupTimeMs == 0 ? Timeout.Infinite : WakeupTimeMs;
                    int index = WaitHandle.WaitAny(events, waitTime);

                    if (index == 0 || oCts?.IsCancellationRequested == true)
                        break;

                    ProcessEvent(index);
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, Name, ex);
            }

            LogMan.Message(sC, sM, $"{Name}: Thread ended");
        }


        private void ProcessEvent(int index)
        {
            try
            {
                switch (index)
                {
                    case 1:
                        _ = RaiseAsync(OnThreadTriggerAsync, "Trigger");
                        break;
                    case 2:
                        if (TimerEnabled)
                            _ = RaiseAsync(OnThreadWakeupAsync, "Wakeup");
                        break;
                    default:
                        _ = RaiseAsync(OnThreadQuitAsync, "Quit");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, nameof(ProcessEvent), Name, ex);
            }
        }

        private async Task RaiseAsync(Func<object, ThreadEventArgs, Task>? evt, string tag)
        {
            string sM = nameof(RaiseAsync);
            if (evt == null) return;

            try
            {
                var args = new ThreadEventArgs
                {
                    Thread = oThread,
                    CancellationTokenSource = oCts
                };

                if (AllowEventOverlap)
                    _ = Task.Run(() => evt.Invoke(this, args)).ContinueWith(LogUnhandledTaskException);
                else
                {
                    // Esecuzione bloccante: un task per volta
                    await _evtLock.WaitAsync();
                    try
                    {
                        await evt.Invoke(this, args);
                    }
                    finally
                    {
                        _evtLock.Release();
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
                LogMan.Exception(sC, sM, Name, t.Exception);
        }

        #endregion

        /// <summary>
        /// Wait synchronously for any thread signal
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds, 0 = infinite</param>
        /// <returns>Signal type received</returns>
        public EnumSignalType WaitForSignal(int timeoutMs)
        {
            var events = new WaitHandle[] { oSignalQuit, oSignalExecute, oSignalWakeupRestart };
            int waitTime = timeoutMs == 0 ? Timeout.Infinite : timeoutMs;
            int index = WaitHandle.WaitAny(events, waitTime);

            return index switch
            {
                0 => EnumSignalType.Quit,
                1 => EnumSignalType.Trigger,
                2 => TimerEnabled ? EnumSignalType.Timeout : EnumSignalType.TimeDrop,
                _ => EnumSignalType.Unknown
            };
        }

    }
}
