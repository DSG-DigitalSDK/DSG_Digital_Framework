using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{
    public interface ILogger
    {

        #region Events
        public static event EventHandler<LogEventArgs> OnLogMessage;
        #endregion

        /// <summary>
        /// Generic log message handler
        /// </summary>
        /// <param name="dtTimeStamp">Timestamp</param>
        /// <param name="eLevel">Log Level</param>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        void Log(DateTime dtTimeStamp, LogLevel eLevel, string? sClass, string? sMethod, string? sMessage, Exception? ex);

        /// <summary>
        /// Trace a Fatal message (application hangs)
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        void Fatal(string sClass, string sMethod, string sMessage, Exception ex);

        /// <summary>
        /// Trace an Exception message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        void Exception(string sClass, string sMethod, string sMessage, Exception ex);

        /// <summary>
        /// Trace an Exception message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="ex">Exception info</param>
        void Exception(string sClass, string sMethod, Exception ex);


        /// <summary>
        /// Trace an Error message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        /// <param name="ex">Exception info</param>
        void Error(string sClass, string sMethod, string sMessage, Exception ex);

        /// <summary>
        /// Trace an Error message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void Error(string sClass, string sMethod, string sMessage);

        /// <summary>
        /// Trace a Warning message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void Warning(string sClass, string sMethod, string sMessage);

        /// <summary>
        /// Trace a Pass OK message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void Pass(string sClass, string sMethod, string sMessage);

        /// <summary>
        /// Track a User Operation
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void TrackUser(string sClass, string sMethod, string sMessage);

        /// <summary>
        /// Trace a  message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void Message(string sClass, string sMethod, string sMessage);

        /// <summary>
        /// Trace a debug message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void Debug(string sClass, string sMethod, string sMessage);

        /// <summary>
        /// Trace lowest level message
        /// </summary>
        /// <param name="sClass">Source Class</param>
        /// <param name="sMethod">Source Method</param>
        /// <param name="sMessage">Log message</param>
        void Trace(string sClass, string sMethod, string sMessage);
    }
}
