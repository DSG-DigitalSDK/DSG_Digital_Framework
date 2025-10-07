using DSG.Base;
using DSG.Log;
using DSG.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DSG.Threading
{
    /// <summary>
    /// Simple Thread Handler<br/> 
    /// <para>
    /// This class provides a synchronization event mechanism for simple operations.<br/>
    /// Only three AutoResetEvent are provided:<br/>
    /// <list type="bullet">Trigger Signal - raised by a source (task,thread, another async caller method) to communicate that a condition occours</list>
    /// <list type="bullet">Quit Signal - raised by another task od by the destructor to inform about the abort of the source</list>
    /// <list type="bullet">Tiemout Signal (when used) - raised periodically</list>
    /// </para>
    /// <para>
    /// This class can also manage automatically a Thread for long time operations<br/>
    /// The created task runs and wait for any signal that can raise a .net event accordingly<br/>
    /// <seealso cref="ThreadBase.OnSignal"/> <seealso cref="ThreadBase.OnQuit"/> <seealso cref="ThreadBase.OnWakeup"/><br/>
    /// This is the core class for the producer consumer implementation<br/>
    public class ThreadBase: CreateBase
    {
        static readonly string sClassName = nameof(ThreadBase);

        /// <summary>
        /// Thread signal maps
        /// </summary>
        public enum EnumSignalType
        {
            /// <summary>
            /// Unknown or unmapped signal
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Operation aborted due to exception during AutoresetEvent handling
            /// </summary>
            Exception = 1,
            /// <summary>
            /// Quit AutoresetEvent is set
            /// </summary>
            Quit = 2,
            /// <summary>
            /// Trigger AutoresetEvent is set
            /// </summary>
            Trigger = 3,
            /// <summary>
            /// Timer polling AutoresetEvent is set
            /// </summary>
            Timeout = 4,
            /// <summary>
            /// Timer polling AutoresetEvent is set
            /// </summary>
            TimeDrop = 4,
        }

        // Microsoft specifications : for Long time running operations is suggested to use Thread instead of Task
        Thread? oThread;      

        readonly AutoResetEvent oSignalExecute = new AutoResetEvent(false);
        readonly AutoResetEvent oSignalQuit = new AutoResetEvent(false);
        readonly AutoResetEvent oSignalWakeupRestart = new AutoResetEvent(false);

        /// <summary>
        /// Cancellation oken to notify chain processing that the Thread is about to abort
        /// </summary>
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        int iWakeupTime = 1000;
        private CancellationTokenSource oCancellationTokenSource;

        public bool TimerEnabled{get;set;} = true;

        /// <summary>
        /// Setup polling time. Set to Zero to disable (Infinite wait time)
        /// </summary>
        public int WakeupTimeMs
        {
            get
            {
                return iWakeupTime;
            }
            set
            {
                iWakeupTime = value;
                // Set the dummy event to wake-up the thread and restart polling timer
                oSignalWakeupRestart.Set();
            }
        }

     

        /// <summary>
        /// Notify that the thread is running
        /// </summary>
        public bool Running => oThread?.IsAlive ?? false;


        /// <summary>
        /// Raised when external trigger event rises 
        /// </summary>
        public event EventHandler<ThreadEventArgs>? OnSignal;

        /// <summary>
        /// Raised on timeout
        /// </summary>
        public event EventHandler<ThreadEventArgs>? OnWakeup;

        /// <summary>
        /// Raised when quit event rises 
        /// </summary>
        public event EventHandler? OnQuit;


        public ThreadBase()
        {
            OnCreateImplementation += ThreadBase_CreateImplementation;
            OnDestroyImplementation += ThreadBase_DestroyImplementation;
        }


        public bool TimerStart()=> TimerEnabled = true;
        public bool TimerStop() => TimerEnabled = false;

        private void ThreadBase_CreateImplementation(object? sender, ResultEventArgs e)
        {
            string sMethod = nameof(ThreadBase_CreateImplementation);
            cancellationTokenSource = new CancellationTokenSource();

            LogMan.Message(sClassName, sMethod, $"{Name} : Creating thread");

            oSignalExecute.Reset();
            oSignalQuit.Reset();
            oSignalWakeupRestart.Reset();

            oThread = new Thread(ThreadJob);
            oThread.IsBackground = true;
            oThread.Start();

            e.AddResult(Result.CreateResultSuccess());
        }

        protected void ThreadBase_DestroyImplementation(object? sender, ResultEventArgs e)
        {
            string sMethod = nameof(ThreadBase_DestroyImplementation);
            LogMan.Message(sClassName, sMethod, $"{Name} : Destroying thread");
            ThreadQuit();
            if (oThread != null && oThread.IsAlive)
            {
                if (!oThread.Join(5000))
                {
                    LogMan.Warning(sClassName, sMethod, $"Task {Name} Doesn't stop");
                }
            }
            oThread = null;
            Initialized = false;
            e.AddResult(Result.CreateResultSuccess());
        }

        /// <summary>
        /// Signal trigger on working thread
        /// </summary>
        public void ThreadSignal()
        {
            oSignalExecute?.Set();
        }

        /// <summary>
        /// Raise quit trigger on working thread
        /// </summary>
        public void ThreadQuit()
        {
            cancellationTokenSource?.Cancel();
            oSignalQuit?.Set();
        }

        #region Working thread code



        /// <summary>
        /// The thread loop job
        /// </summary>
        void ThreadJob()
        {
            string sMethod = nameof(ThreadJob);
            bool bExit = false;
            LogMan.Message(sClassName, sMethod, $"{Name} : Starting thread");
            try
            {
                AutoResetEvent[] oEvents = { oSignalQuit, oSignalExecute, oSignalWakeupRestart };
                while (!bExit)
                {
                    int iTime = iWakeupTime == 0 ? Timeout.Infinite : (int)iWakeupTime;
                    int iEventID = WaitHandle.WaitAny(oEvents, iTime);
                    {
                        bExit = iEventID == 0;
                        if (!Running)
                        {
                            continue;
                        }
                        ProcessEvents(iEventID);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sClassName, sMethod, Name, ex);
            }
            LogMan.Message(sClassName, sMethod, $"{Name} : Thread end");
        }

        /// <summary>
        /// Processes AutoReseEvent ID and raises events accordingly
        /// </summary>
        /// <param name="iEventID"></param>
        /// <returns></returns>
        EnumSignalType ProcessEvents(int iEventID)
        {
            string sMethod = nameof(ProcessEvents);
            switch (iEventID)
            {
                case 0:
                    {
                        LogMan.Message(sClassName, sMethod, $"{Name} : Detected quit signal");
                        try
                        {
                            OnQuit?.Invoke(this, EventArgs.Empty);
                            return EnumSignalType.Quit;
                        }
                        catch (Exception ex)
                        {
                            LogMan.Exception(sClassName, sMethod, Name, ex);
                            return EnumSignalType.Exception;
                        }
                    }
                case 1:
                    {
                        try
                        {
                            OnSignal?.Invoke(this, new ThreadEventArgs()
                            {
                                Thread = oThread,
                                CancellationTokenSource = oCancellationTokenSource,
                            });
                            return EnumSignalType.Trigger;
                        }
                        catch (Exception ex)
                        {
                            LogMan.Exception(sClassName, sMethod, Name, ex);
                            return EnumSignalType.Exception;
                        }
                    }
                case 2:
                default:
                    {
                        try
                        {
                            if (!TimerEnabled)
                            {
                                return EnumSignalType.TimeDrop;
                            }
                            OnWakeup?.Invoke(this, new ThreadEventArgs()
                            {
                                Thread = oThread,
                                CancellationTokenSource = oCancellationTokenSource,
                            });
                            return EnumSignalType.Timeout;
                        }
                        catch (Exception ex)
                        {
                            LogMan.Exception(sClassName, sMethod, Name, ex);
                            return EnumSignalType.Exception;
                        }
                    }
            }
        }
        #endregion

        /// <summary>
        /// Wait for an AutoResetEvent signal.
        /// <para>Raises events accordingly </para> 
        /// <para>Any method caller will block until a signal comes</para> 
        /// </summary>
        /// <param name="iWakeupTime">maximum timeout delay: 0 for infinite timeout</param>
        /// <returns></returns>
        public EnumSignalType WaitForSignal(int iWakeupTime)
        {
            string sMethod = nameof(WaitForSignal);
            LogMan.Message(sClassName, sMethod, $"{Name} : Waiting for event");
            try
            {
                AutoResetEvent[] oEvents = { oSignalQuit, oSignalExecute, oSignalWakeupRestart };
                int iTime = iWakeupTime == 0 ? Timeout.Infinite : (int)iWakeupTime;
                EnumSignalType eRet = EnumSignalType.Unknown;
                while ((eRet = ProcessEvents(WaitHandle.WaitAny(oEvents, iTime))) == EnumSignalType.TimeDrop);
                return eRet;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sClassName, sMethod, Name, ex);
                return EnumSignalType.Exception;
            }
        }

      
    }
}
