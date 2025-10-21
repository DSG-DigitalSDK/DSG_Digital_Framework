using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{
    public class LogEventArgs : EventArgs
    {
        public DateTime TimeStamp { get; set; }
        public string? Class { get; set; }
        public LogLevel Level { get; set; }
        public string? Method { get; set; }
        public string? Message { get; set; }
        public string? FormattedMessage { get; set; }
        public Exception? Exception { get; set; }

        public override string ToString()
        {
            return $"{FormattedMessage??Message}";
        }
    }
}
