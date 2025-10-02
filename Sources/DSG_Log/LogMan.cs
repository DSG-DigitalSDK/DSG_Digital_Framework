using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{

    /// <summary>
    /// Log Manager
    /// </summary>
    public static class LogMan
    {
        static string sClassName = nameof(LogMan);

        #region Support Classes
        //internal class LogMessage
        //{
        //    internal DateTime TimeStamp { get; set; }
        //    internal string? Class { get; set; }
        //    internal LogLevel Level { get; set; }
        //    internal string? Method { get; set; }
        //    internal string? Message { get; set; }

        //    public
        //    internal Exception? Exception { get; set; }
        //}
        #endregion

        #region Events
        public static event EventHandler<LogEventArgs> OnLogMessage;
        #endregion

        #region Properties
        [Browsable(true), Category("Config")]
        [Description("Log minimum level. Can be set any time")]
        public static LogLevel MinLogLevel { get; set; } = LogLevel.Trace;

        static List<ILogConsumer> lLogConsumers = new List<ILogConsumer>();

        static List<LogEventArgs> lPendingMessages = new List<LogEventArgs>();
        public static bool Initialized { get; private set; } = false;
        
        #endregion

        public static bool RegisterLogConsumer(ILogConsumer log)
        {
            string sMethod = nameof(RegisterLogConsumer);
            if (log == null)
            {
                return false;
            }
            if (!lLogConsumers.Contains(log))
            {
                Message(sClassName, sMethod, $"Registering {log.Name}");
                lLogConsumers.Add(log);
                return true;
            }
            else
            {
                Message(sClassName, sMethod, $"{log.Name} already registered");
                return false;
            }
        }

        public static bool UnregisterLogConsumer(ILogConsumer log)
        {
            string sMethod = nameof(UnregisterLogConsumer);
            if (log == null)
            {
                return false;
            }
            if (lLogConsumers.Contains(log))
            {
                Message(sClassName, sMethod, $"De-registering {log.Name}");
                lLogConsumers.Remove(log);
                return true;
            }
            else
            {
                Message(sClassName, sMethod, $"{log.Name} doesn't exists");
                return false;
            }
        }

        public static bool Create()
        {
            string sMethod = nameof(Create);
            Message(sClassName, sMethod, "Creating Log Manager");
            foreach (var l in lLogConsumers)
            {
                try
                {
                    l.Create();
                    OnLogMessage += l.ProcessMessage;
                }
                catch (Exception ex)
                {
                    Exception(sClassName, sMethod, $"Error creating Logger {l.Name}", ex);
                }
            }
            Initialized = true;
            foreach ( var log in lPendingMessages)
            {
                Log(log);
            }
            return true;
        }

        public static bool Destroy()
        {
            string sMethod =nameof(Destroy);
            Message(sClassName, sMethod, "Destroying Log Manager");
            foreach (var l in lLogConsumers)
            {
                try
                {
                    OnLogMessage-= l.ProcessMessage;    
                    l.Destroy();
                }
                catch { }
            }
            lLogConsumers.Clear();
            lPendingMessages.Clear();   
            Initialized = false;    
            return true;
        }

        static string GetExceptionMessage(Exception? ex)
        {
            if (ex == null)
            {
                return "";
            }
            string sMsg = $"{Environment.NewLine}Exception Dump{Environment.NewLine}";
            do
            {
                sMsg += $"InnerMessage : {ex.Message}{Environment.NewLine}";
                ex = ex.InnerException;
            }
            while (ex != null);
            return sMsg;
        }


        /// <summary>
        /// Generic log message handler
        /// </summary>
        static void Log(LogEventArgs oArgs)
        {
            if (oArgs == null)
            { 
            return;
            }
            if (oArgs.Level > MinLogLevel)
            {
                return;
            }
            try
            {
                OnLogMessage?.Invoke(null, oArgs);
            }
            catch(Exception ex)
            {
            }

        }


        /// <summary>
        /// Generic log message handler
        /// </summary>
        /// <param name="dtTimeStamp">Timestamp</param>
        /// <param name="eLevel">Log Level</param>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        static void Log(DateTime dtTimeStamp, LogLevel eLevel, string? sClass, string? sMethod, string? sMessage, Exception? ex)
        {
            var oArgs = new LogEventArgs()
            {
                TimeStamp = dtTimeStamp,
                Class = sClass,
                Level = eLevel,
                Method = sMethod,
                Message = sMessage,
                Exception = ex,
                FormattedMessage = $"{dtTimeStamp:yyyy-MM-dd HH:mm:ss.fff} | {eLevel} | {sClass}.{sMethod} : {sMessage} {GetExceptionMessage(ex)}"
            };

            if (!Initialized)
            {
                lPendingMessages.Add(oArgs);
            }
            else
            {
                Log(oArgs);
            }
            
            
        }

        /// <summary>
        /// Trace a Fatal message (application hangs)
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        public static void Fatal(string sClass, string sMethod, string sMessage, Exception ex)
        {
            Log(DateTime.Now,LogLevel.Fatal, sClass, sMethod, sMessage, ex); 
        }

        /// <summary>
        /// Trace an Exception message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        public static void Exception(string sClass, string sMethod, string sMessage, Exception ex)
        {
            Log(DateTime.Now, LogLevel.Exception, sClass, sMethod, sMessage, ex);
        }

        /// <summary>
        /// Trace an Exception message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="ex">Exception info</param>
        public static void Exception(string sClass, string sMethod, Exception ex)
        {
            Log(DateTime.Now, LogLevel.Exception, sClass, sMethod, "Exception raised", ex);
        }


        /// <summary>
        /// Trace an Error message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        public static void Error(string sClass, string sMethod, string sMessage, Exception ex)
        {
            var eLevel = ex == null ? LogLevel.Error : LogLevel.Fatal;
            Log(DateTime.Now, eLevel, sClass, sMethod, sMessage, ex);
        }

        /// <summary>
        /// Trace an Error message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        public static void Error(string sClass, string sMethod, string sMessage)
        {
            Log(DateTime.Now, LogLevel.Error, sClass, sMethod, sMessage, null);
        }

        /// <summary>
        /// Trace a Warning message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        public static void Warning(string sClass, string sMethod, string sMessage)
        {
            Log(DateTime.Now, LogLevel.Warning, sClass, sMethod, sMessage, null);
        }

        /// <summary>
        /// Trace a Pass OK message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        public static void Pass(string sClass, string sMethod, string sMessage)
        {
            Log(DateTime.Now, LogLevel.Pass, sClass, sMethod, sMessage, null);
        }

        /// <summary>
        /// Trace a  message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        public static void Message(string sClass, string sMethod, string sMessage)
        {
            Log(DateTime.Now, LogLevel.Message, sClass, sMethod, sMessage, null);
        }

        /// <summary>
        /// Trace a debug message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        public static void Debug(string sClass, string sMethod, string sMessage)
        {
            Log(DateTime.Now, LogLevel.Debug, sClass, sMethod, sMessage, null);
        }

        /// <summary>
        /// Trace lowest level message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        public static void Trace(string sClass, string sMethod, string sMessage)
        {
            Log(DateTime.Now, LogLevel.Trace, sClass, sMethod, sMessage, null);
        }


        public static void Test()
        {
            string sMethod = nameof(Test);
            Exception(sClassName, sMethod, new Exception("Test Exception"));
            Error(sClassName, sMethod, "Test Error");
            Warning(sClassName, sMethod, "Test Warning");
            Pass(sClassName, sMethod, "Test Pass");
            Message(sClassName, sMethod, "Test Message");
            Debug(sClassName, sMethod, "Test Debug");
            Trace(sClassName, sMethod, "Test Trace");
        }


        public static void CreateAndRegisterDefaultLoggers(bool bNLog, bool bTrace, bool bWebLog )
        {
            if (bNLog)
            {
                var oLog = new NLogger();
                LogMan.RegisterLogConsumer(oLog);
            }
            if (bTrace)
            {
                var oLog = new TraceLogger();
                LogMan.RegisterLogConsumer(oLog);
            }
        }
    }
}
