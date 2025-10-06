using DSG.Log;
using DSG.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Shared.ProducerConsumer
{
    public class ProducerConsumerCounters
    {
        static string sC = nameof(ProducerConsumerCounters);

        SpinLock oLocker = new SpinLock();

        TimeElapser oTE = new TimeElapser();
        public long EventCounterTotal { get; private set; } = 0;
        public long EventCounter { get; private set; } = 0;
        public long EventCounterValid { get; private set; } = 0;
        public long EventCounterError { get; private set; } = 0;
        public long EventCounterDropped { get; private set; } = 0;
        public Statistics TimeStatistics { get; private set; } = new Statistics();

        public void TimeStart() => oTE.Reset();
        public void AddStatisticTime() => TimeStatistics.AddValue( oTE.Stop().TotalMilliseconds );

       // public void ResetStatistics() => TimeStatistics.ResetCounters();
        public void ResetCounters()
        {
            string sM = nameof(ResetCounters);   
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                TimeStatistics.ResetCounters();
                EventCounterValid = EventCounterDropped = EventCounterError = EventCounter = 0;
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

        public bool AddValidEvent( int iAdder )
        {
            string sM = nameof(ResetCounters);
            bool bTaken = false;
            try
            {
                oLocker.Enter(ref bTaken);
                EventCounterTotal += iAdder;
                EventCounterValid += iAdder;
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
                EventCounterTotal += iAdder;
                EventCounterError += iAdder;
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
                EventCounterTotal += iAdder;
                EventCounterError += iAdder;
                EventCounterDropped += iAdder;
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

        public bool AddValidEvent() => AddValidEvent(1);
        public bool AddErrorEvent() => AddErrorEvent(1);
        public bool AddDropEvent() => AddDropEvent(1);


        public ProducerConsumerCounters()
        {
            TimeStatistics.IntervalStart = 0;
            TimeStatistics.IntervalEnd = 10*1000;
            TimeStatistics.IntervalResolution = 10;
            TimeStatistics.Create();
            ResetCounters();
        }
    }
}
