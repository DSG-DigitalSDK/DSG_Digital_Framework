using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{
    public class LogEventArgs : EventArgs
    {
        internal DateTime TimeStamp { get; set; }
        internal string? Class { get; set; }
        internal LogLevel Level { get; set; }
        internal string? Method { get; set; }
        internal string? Message { get; set; }
        internal string? FormattedMessage { get; set; }
        internal Exception? Exception { get; set; }

        public override string ToString()
        {
            return $"{FormattedMessage??Message}";
        }
    }
}
