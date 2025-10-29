using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Shared
{
    /// <summary>
    /// Bin counter for hits and total events. Get statistics (hits/total). 
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public struct BinData
    {
        /// <summary>
        /// Resource instantiated
        /// </summary>
        public bool Valid { get; private set; }

        /// <summary>
        /// Bin lower bound
        /// </summary>
        public double BinIntervalLO { get; private set; }

        /// <summary>
        /// Bun upper bound
        /// </summary>
        public double BinIntervalHI { get; private set; }

        /// <summary>
        /// Hits event counter
        /// </summary>
        public long Hits { get; private set; }

        /// <summary>
        /// Total events conuter
        /// </summary>
        public long Counter { get; private set; }

        /// <summary>
        /// % Hits (respect Total) for this bin
        /// </summary>
        public double Percent
        {
            get { return (Counter > 0 ? (double)Hits / Counter * 100d : 0); }
        }

        /// <summary>
        /// Clear class
        /// </summary>
        internal void Clear()
        {
            Valid = false;
            BinIntervalLO = BinIntervalHI = 0;
            Hits = Counter = 0;
        }

        /// <summary>
        /// Create class
        /// </summary>
        /// <param name="fBinRangeLO">Bin Lower bound</param>
        /// <param name="fBinRangeHI">Bin Upper bound</param>
        internal void Create(double fBinRangeLO, double fBinRangeHI)
        {
            Valid = true;
            BinIntervalLO = fBinRangeLO;
            BinIntervalHI = fBinRangeHI;
            Hits = Counter = 0;
        }

        /// <summary>
        /// Reset counters
        /// </summary>
        internal void ResetCounters()
        {
            Hits = Counter = 0;
        }

        /// <summary>
        /// Set Hit Counter
        /// </summary>
        /// <param name="iHits"></param>
        internal void SetHits(long iHits)
        {
            Hits = iHits;
        }

        /// <summary>
        /// Set Total Counter
        /// </summary>
        /// <param name="iTotal"></param>
        internal void SetTotal(long iTotal)
        {
            Counter = iTotal;
        }

        /// <summary>
        /// Add Hits events
        /// <para>It does not updates total events</para>
        /// </summary>
        /// <param name="iHits"></param>
        internal void AddHits(long iHits)
        {
            Hits += iHits;
        }

        /// <summary>
        /// Add Total events
        /// </summary>
        internal void AddTotal(long iTotal)
        {
            Counter += iTotal;
        }

        /// <summary>
        /// Add Hits events
        /// <para>It does not updates total events</para>
        /// </summary>
        /// <param name="iHits"></param>
        internal void AddEvents(long iHits, long iTotal)
        {
            AddHits(iHits);
            AddTotal(iTotal);
        }


        public override string ToString()
        {
            if (!Valid) return "[Not Valid]";
            else return (string.Format("[R=[{0:f2}-{1:f2})][C={2} P={4:f2}%]", BinIntervalLO, BinIntervalHI, Hits, Counter, Percent));
        }
    }

    /// <summary>
    /// Separates an interval into bins
    /// Counts elements and gets statistics
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BinCounter
    {
       /// <summary>
       /// Bin min value
       /// </summary>
        public double BinMin { get; private set; }

        /// <summary>
        /// Bin max value
        /// </summary>
        public double BinMax { get; private set; }

        /// <summary>
        /// Bin mean value
        /// </summary>
        public double BinMean { get; private set; }


        /// <summary>
        /// Interval lower bound
        /// </summary>
        public double GlobalIntervalLO { get; private set; }

        /// <summary>
        /// Interval upper bound
        /// </summary>
        public double GlobalIntervalHI { get; private set; }

        /// <summary>
        /// Width of the Interval
        /// </summary>
        public double GlobalIntervalWidth { get { return GlobalIntervalHI - GlobalIntervalLO; } }

        /// <summary>
        /// Width of a Bin
        /// </summary>
        public double BinIntervalWidth
        {
            get
            {
                long iBins = BinsNumber;
                if (iBins > 0)
                {
                    return (GlobalIntervalHI - GlobalIntervalLO) / iBins;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Total Bins in the interval
        /// </summary>
        public int BinsNumber
        {
            get { return (Bins == null ? 0 : Bins.Length); }
        }

        bool bValid = false;

        /// <summary>
        /// Bin Array
        /// </summary>
        public BinData[] Bins { get; private set; }

        /// <summary>
        /// Clears all data
        /// </summary>
        void Reset()
        {
            bValid = false;
            GlobalIntervalHI = 0;
            GlobalIntervalLO = 0;
            Bins = null;
            BinMin = double.NaN;
            BinMax = double.NaN;
            BinMean = double.NaN;
        }

        /// <summary>
        /// Resets bin data
        /// </summary>
        public void ResetBins()
        {
            if (!bValid) return;
            for (int i = 0; i < Bins.Length; i++)
            {
                Bins[i].ResetCounters();
            }
            BinMin = double.NaN;
            BinMax = double.NaN;
            BinMean = double.NaN;
        }

        /// <summary>
        /// Create bins form an interval
        /// </summary>
        /// <param name="dIntervalLO">Interval lower bound</param>
        /// <param name="dIntervalHI">Intervasl higher bound</param>
        /// <param name="iBinsNumber">Total bins wanted</param>
        public void Create(double dIntervalLO, double dIntervalHI, int iBinsNumber)
        {
            Reset();
            if (iBinsNumber > 1 && dIntervalLO < dIntervalHI )// && dIntervalLO >= 0)
            {
                GlobalIntervalLO = dIntervalLO;
                GlobalIntervalHI = dIntervalHI;
                List<BinData> oList = new List<BinData>();

                // First bin, from -Infinite to LowerBound
                {
                    BinData oItem = new BinData();
                    oItem.Create(double.NegativeInfinity, dIntervalLO);
                    oList.Add(oItem);
                }
                // Interval Bins
                double dBinIntervalWidth = (dIntervalHI - dIntervalLO) / iBinsNumber;
                for (int i = 0; i < iBinsNumber; i++)
                {
                    double fLO = dIntervalLO + i * dBinIntervalWidth;
                    double fHI = fLO + dBinIntervalWidth;
                    BinData oItem = new BinData();
                    oItem.Create(fLO, fHI);
                    oList.Add(oItem);
                }
                // Last bin, from UpperBound to Infinite
                {
                    BinData oItem = new BinData();
                    oItem.Create(dIntervalHI, float.PositiveInfinity);
                    oList.Add(oItem);
                }
                Bins = oList.ToArray();
                bValid = true;
            }
        }

        /// <summary>
        /// Create bins form an interval
        /// </summary>
        /// <param name="dIntervalLO">Interval lower bound</param>
        /// <param name="dIntervalHI">Intervasl higher bound</param>
        /// <param name="dBinIntervalWidth">Width of the Bin (last may be resized automatically)</param>
        public void Create(double dIntervalLO, double dIntervalHI, double dBinIntervalWidth)
        {
            Reset();
            if (dBinIntervalWidth > 0 && dIntervalLO < dIntervalHI )// && dIntervalLO >= 0)
            {
                GlobalIntervalLO = dIntervalLO;
                GlobalIntervalHI = dIntervalHI;
                List<BinData> oList = new List<BinData>();

                // First bin, from -Infinite to LowerBound
                {
                    BinData oItem = new BinData();
                    oItem.Create(double.NegativeInfinity, dIntervalLO);
                    oList.Add(oItem);
                }
                // Interval Bins
                double dIntervalStart = dIntervalLO;
                while (dIntervalStart < dIntervalHI)
                {
                    double dIntervalEnd = Math.Min(dIntervalHI, dIntervalStart + dBinIntervalWidth);
                    BinData oItem = new BinData();
                    oItem.Create(dIntervalStart, dIntervalEnd);
                    oList.Add(oItem);
                    dIntervalStart = dIntervalEnd;
                }
                // Last bin, from UpperBound to Infinite
                {
                    BinData oItem = new BinData();
                    oItem.Create(dIntervalHI, float.PositiveInfinity);
                    oList.Add(oItem);
                }
                Bins = oList.ToArray();
                bValid = true;
            }
        }

        int GetBinPos(double fPosition)
        {
            if (!bValid) return -1;
            if (fPosition <= GlobalIntervalLO)
            {
                return 0;
            }
            if (fPosition >= GlobalIntervalHI)
            {
                return Bins.Length - 1;
            }
            var PosA = 0;
            var PosB = Bins.Length - 1;
            while (PosA < PosB)
            {
                var PosM = (PosA + PosB) / 2;
                if (fPosition < Bins[PosM].BinIntervalLO)
                {
                    PosB = PosM - 1;
                }
                else if (fPosition > Bins[PosM].BinIntervalHI)
                {
                    PosA = PosM + 1;
                }
                else
                {
                    return PosM;
                }
            }
            return Math.Max(0,Math.Min(PosB, PosA));
        }

        static double dA = 0.99;
        static double dB = 1.0-dA;

        /// <summary>
        /// Update Statistic
        /// </summary>
        /// <param name="dValue">Value to insert</param>
        public void UpdateStats(double dValue)
        {
            if (double.IsNaN( BinMin ))
            {
                BinMin = BinMax = BinMean = dValue;
            }
            else
            {
                BinMax = Math.Max(BinMax, dValue);
                BinMin = Math.Min(BinMin, dValue);
                BinMean = dA*BinMean + dB*dValue;
            }
        }

        /// <summary>
        /// Populate Bins with a value 
        /// </summary>
        /// <param name="dValue">Value to insert</param>
        public void BinAdd(double dValue)
        {
            UpdateStats(dValue);
            int iPos = GetBinPos(dValue);
            if (iPos < 0) return;
            Bins[iPos].AddHits(1);

            for (int i = 0; i < Bins.Length; i++)
            {
                Bins[i].AddTotal(1);
            }
        }

        /// <summary>
        /// Populate Bins with a value 
        /// </summary>
        /// <param name="dValue">Value to insert</param>
        public void BinAdd(int iValue)
        {
            UpdateStats((double)iValue);
            int iPos = GetBinPos((double)iValue);
            if (iPos < 0) return;
            Bins[iPos].AddHits(1);
            for (int i = 0; i < Bins.Length; i++)
            {
                Bins[i].AddTotal(1);
            }
        }

        /// <summary>
        /// Get Bin Data (from value)
        /// </summary>
        /// <param name="dValue">Value to get</param>
        /// <returns>Returns the bin data associated to the value</returns>
        public BinData BinGet(double dValue)
        {
            if (!bValid) return new BinData();
            int iPos = GetBinPos(dValue);
            if (iPos < 0) return new BinData();
            return Bins[iPos];
        }

        /// <summary>
        /// Get Bin Data (from Bin Index)
        /// </summary>
        /// <param name="iBinPos">Bin Index</param>
        /// <returns>Returns the bin data associated to the Bin Index</returns>
        public BinData BinGet(int iBinPos)
        {
            if (!bValid) return new BinData();
            if (iBinPos < 0 || iBinPos >= Bins.Length) return new BinData();
            return Bins[iBinPos];
        }

        /// <summary>
        /// Get Bin Data (from Bin Index)
        /// </summary>
        /// <param name="index">Bin Index</param>
        /// <returns>Returns the bin data associated to the Bin Index</returns>
        public BinData this[int index]
        {
            get
            {
                return BinGet(index);
            }
        }

        /// <summary>
        /// Returns populated bin list
        /// </summary>
        /// <returns></returns>
        public List<BinData> GetBinList()
        {
            List<BinData> oList = new List<BinData>();
            for (int i = 0; i < Bins.Length; i++)
            {
                if (Bins[i].Valid && Bins[i].Hits > 0)
                {
                    oList.Add(Bins[i]);
                }
            }
            return oList;
        }
    }
}