using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{
    public class TraceLogger : ILogConsumer
    {
        public string Name { get; set; } = "System.Diagnostic.Trace";

        public bool Registered { get; internal set; }

        public bool Create()
        {
            return true;
        }

        public bool Destroy()
        {
            return true;
        }

        public void ProcessMessage(object sender, LogEventArgs oArgs)
        {
            switch (oArgs.Level)
            {
                case LogLevel.Fatal:
                    System.Diagnostics.Trace.TraceError(oArgs.FormattedMessage);
                    break;
                case LogLevel.Exception:
                case LogLevel.Error:
                    System.Diagnostics.Trace.TraceError(oArgs.FormattedMessage);
                    break;
                case LogLevel.Warning:
                    System.Diagnostics.Trace.TraceWarning(oArgs.FormattedMessage);
                    break;
                default:
                    System.Diagnostics.Trace.TraceInformation(oArgs.FormattedMessage);
                    break;
            }
        }
    }
}
