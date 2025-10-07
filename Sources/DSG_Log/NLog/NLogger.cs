using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{
    public class NLogger : ILogConsumer
    {

        static string className = nameof(NLogger);

        static readonly string sLogFileName = "Log";
        static readonly string sLogFolder = "Logs";
        static readonly string sLogArchive = "LogsArchive";
        static readonly string sLogTarget = "DeltaLog";
        static readonly string sLogRule = "DeltaRule";
        static readonly string sLogID = "DefaultLog";

        static NLog.Logger NLogInstance => LogManager.GetLogger(sLogID);

        public string Name { get; set; } = "NLog";
        public bool Registered { get; internal set; }

        LoggingConfiguration nLogConf =null;

        public bool Create()
        {
            string sMethod = nameof(Create);    
            try
            {
                LogManager.Setup();
                Directory.CreateDirectory($"./{sLogFolder}");
                // Creazione configurazione
                nLogConf = new LoggingConfiguration();
                // Target per scrivere su file
                var fTarget = new FileTarget(sLogTarget);
                fTarget.FileName = "${basedir}" + $"/{sLogFolder}/{sLogFileName}.txt";
                //fTarget.FileName = "C:/TestX/" + sLogFileName + ".txt";
                //fTarget.Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss}.${date:format=fff}" +
                //    "|${level:uppercase=true}" +
                //    "|${message}" +
                //    " ${onexception: ${exception:format=ToString,format=Data};}";
                fTarget.Layout = "${message}" +
                    "${onexception: | ${exception:format=ToString,format=Data};}";
                fTarget.KeepFileOpen = true;
                fTarget.Encoding = Encoding.UTF8;
                fTarget.CreateDirs = true;
                fTarget.ArchiveAboveSize = 4 * 1024 * 1024;
                fTarget.ArchiveSuffixFormat = $"{0:yyyyMMdd-HHmmss}_{1:000}";
                //fTarget.ArchiveDateFormat = "yyyyMMdd-HHmmss";
                fTarget.ArchiveFileName = "${basedir}" + $"/{sLogFolder}/{sLogArchive}/{sLogFileName}" + "_{#}.txt";
                //fTarget.ArchiveNumbering = ArchiveNumberingMode.Sequence;
                fTarget.MaxArchiveFiles = 100;
                //fTarget.EnableArchiveFileCompression = true;
                //fTarget.ConcurrentWrites = true;

                nLogConf.AddTarget(sLogID, fTarget);
                nLogConf.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, fTarget);
                LogManager.Configuration = nLogConf;
                return true;
            }
            catch (Exception ex)
            {
                LogMan.Exception(className, sMethod, $"Logger:{Name}", ex);
                return false;
            }
        }

        public bool Destroy()
        {
            LogManager.Shutdown();
            nLogConf = null;
            return true;
        }

        public void ProcessMessage(object sender, LogEventArgs oArgs)
        {
            switch (oArgs.Level)
            {
                case LogLevel.Fatal:
                    NLogInstance?.Fatal(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.Exception:
                case LogLevel.Error:
                    NLogInstance?.Error(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.Warning:
                    NLogInstance?.Warn(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.Pass:
                    NLogInstance?.Info(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.TrackUser:
                    NLogInstance?.Info(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.Message:
                    NLogInstance?.Info(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.Debug:
                    NLogInstance?.Debug(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                case LogLevel.Trace:
                    NLogInstance?.Trace(oArgs.Exception, oArgs.FormattedMessage);
                    break;
                default:
                    NLogInstance?.Info(oArgs.Exception, oArgs.FormattedMessage);
                    break;
            }
        }
    }
}

