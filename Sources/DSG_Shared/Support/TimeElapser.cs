using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Shared
{
    public class TimeElapser
    {
        DateTimeOffset dtRef;
        DateTimeOffset dtStop;

        public TimeElapser()
        {
            Reset();
        }

        public void Reset() { dtRef = DateTimeOffset.Now; dtStop = DateTimeOffset.MinValue; }
        public TimeSpan Stop() { dtStop = DateTimeOffset.Now; return TimeSpan; }

        public TimeSpan TimeSpan => dtRef == DateTimeOffset.MinValue ? TimeSpan.Zero : dtStop == DateTimeOffset.MinValue ? (DateTimeOffset.Now - dtRef) : (dtStop- dtRef);
        public double TimeElapsedMs=> TimeSpan.TotalMilliseconds;
        public double TimeElapsedS => TimeSpan.TotalSeconds;
        public double TimeElapsedM => TimeSpan.TotalMinutes;
        public double TimeElapsedH => TimeSpan.TotalHours;  
    }
}
