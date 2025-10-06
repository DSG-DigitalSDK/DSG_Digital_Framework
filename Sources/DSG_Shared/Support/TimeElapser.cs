using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Shared
{
    public class TimeElapser
    {
        DateTime dtRef;
        DateTime dtStop;

        public TimeElapser()
        {
            Reset();
        }

        public void Reset() { dtRef = DateTime.Now; dtStop = DateTime.MinValue; }
        public TimeSpan Stop() { dtStop = DateTime.Now; return TimeSpan; }

        public TimeSpan TimeSpan => dtRef == DateTime.MinValue ? TimeSpan.Zero : dtStop == DateTime.MinValue ? (DateTime.Now - dtRef) : (dtStop- dtRef);
        public double TimeElapsedMs=> TimeSpan.TotalMilliseconds;
        public double TimeElapsedS => TimeSpan.TotalSeconds;
        public double TimeElapsedM => TimeSpan.TotalMinutes;
        public double TimeElapsedH => TimeSpan.TotalHours;  
    }
}
