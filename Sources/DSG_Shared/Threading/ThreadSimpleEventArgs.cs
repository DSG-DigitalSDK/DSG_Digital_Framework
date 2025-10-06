using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Threading
{
    /// <summary>
    /// EventArgs that contains ThreadSimple execution context
    /// </summary>
    public class ThreadEventArgs : EventArgs
    {
        /// <summary>
        /// Running thread
        /// </summary>
        public Thread? Thread { get; internal set; }

        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource { get; internal set; }
    }
}
