using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Threading
{
    /// <summary>
    /// Thread signal maps
    /// </summary>
    public enum ThreadSignalType
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
    }
}
