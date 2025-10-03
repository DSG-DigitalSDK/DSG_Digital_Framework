using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Threading
{
    public class TaskEventArgs : EventArgs
    {
        public string? Name { get; internal set; }
        public CancellationTokenSource? CancellationTokenSource { get; internal set; }
    }
}
