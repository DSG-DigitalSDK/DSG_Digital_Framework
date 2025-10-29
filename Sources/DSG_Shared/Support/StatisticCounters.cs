using DSG.Log;
using DSG.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Shared
{
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class StatisticCounters
    {
        static string sC = nameof(StatisticCounters);

        SpinLock oLocker = new SpinLock();
        public long EventTotal { get; private set; } = 0;
        public long EventCounter { get; private set; } = 0;
        public long EventValid { get; private set; } = 0;
        public long EventError { get; private set; } = 0;
        public long EventTimeout { get; private set; } = 0;
        public long EventDropped { get; private set; } = 0;
        public BinCounter TimeStatistics { get; private set; } = new();

        public TimeElapser TimeStart() => new TimeElapser();
        public void AddStatisticTime(TimeElapser oTE) => TimeStatistics.BinAdd( oTE.Stop().TotalMilliseconds );

        public void ResetCounters()
        {
            string sM = nameof(ResetCounters);   
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                TimeStatistics.ResetBins();
                EventValid = EventDropped = EventError = EventCounter = 0;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC,sM,ex); 
            }
            finally
            {
                if (bTaken) oLocker.Exit();
            }
        }

        public bool AddTimeoutEvent(int iAdder)
        {
            string sM = nameof(ResetCounters);
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                EventTotal += iAdder;
                EventTimeout += iAdder;
                EventCounter += iAdder;
                return true;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return false;
            }
            finally
            {
                if (bTaken) oLocker.Exit();
            }
        }

        public bool AddValidEvent( int iAdder )
        {
            string sM = nameof(ResetCounters);
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                EventTotal += iAdder;
                EventValid += iAdder;
                EventCounter += iAdder;    
                return true;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return false;
            }
            finally
            {
                if (bTaken) oLocker.Exit();
            }
        }
        public bool AddErrorEvent(int iAdder)
        {
            string sM = nameof(ResetCounters);
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                EventTotal += iAdder;
                EventError += iAdder;
                EventCounter += iAdder;
                return true;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return false;
            }
            finally
            {
                if (bTaken) oLocker.Exit();
            }
        }
        public bool AddDropEvent(int iAdder)
        {
            string sM = nameof(ResetCounters);
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                EventTotal += iAdder;
                EventError += iAdder;
                EventDropped += iAdder;
                EventCounter += iAdder;
                return true;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return false;
            }
            finally
            {
                if (bTaken) oLocker.Exit();
            }
        }

        public bool AddTimeoutEvent() => AddTimeoutEvent(1);
        public bool AddValidEvent() => AddValidEvent(1);
        public bool AddErrorEvent() => AddErrorEvent(1);
        public bool AddDropEvent() => AddDropEvent(1);


        public StatisticCounters()
        {
            TimeStatistics.Create(0, 10 * 1000, 1000);
            ResetCounters();
        }
    }
}
