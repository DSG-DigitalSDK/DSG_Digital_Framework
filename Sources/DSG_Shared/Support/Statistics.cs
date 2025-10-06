
using DSG.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Shared
{
    /// <summary>
    /// Incapsulates grabber statistics information
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Statistics
    {        
        BinCounter Intervals = new BinCounter();

        public double IntervalStart { get; set; } = 0;
        public double IntervalEnd { get; set; } = 100000;
        public double IntervalResolution { get; set; } = 10;

        public void Create()
        {
            Intervals = new BinCounter();
            Intervals.Init(0d, 1000d, 5d);
            ResetCounters();
        }

        public void ResetCounters()
        {
            Intervals.ResetBins();
        }

        public void AddValue(double dValue)
        {
            Intervals.BinAdd(dValue);
        }
        public void AddValue(int iValue)
        {
            Intervals.BinAdd(iValue);
        }
    }
}
