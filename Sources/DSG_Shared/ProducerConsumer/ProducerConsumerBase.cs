using DSG.Base;
using DSG.Log;
using DSG.Shared;
using DSG.Threading;
using Microsoft.AspNetCore.Http;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Result = DSG.Base.Result;

namespace DSG.ProducerConsumer
{

    public abstract class ProducerConumerBase<T> : CreateBase, IProducerConsumer<T> where T : class
    {
        static string sC = nameof(ProducerConumerBase<T>);

        #region Support Classes
        public class ProducerConsumerDataContainer<T> where T : class
        {
            public DateTime Timestamp { get; init; } = DateTime.Now;
            public required T Data { get; set; }
            public required Result ProductionResult { get; set; }
        }

        #endregion

        #region Events

        public event EventHandler Producing;
        public event EventHandler<ResultEventArgs> Produced;
        public event EventHandler<ResultEventArgs> ProduceDrop;
        public event EventHandler<ResultEventArgs> ProduceError;
        public event EventHandler Consuming;
        public event EventHandler<ResultEventArgs> Consumed;
        public event EventHandler<ResultEventArgs> ConsumeDrop;
        public event EventHandler<ResultEventArgs> ConsumeError;


        #endregion

        #region Fields

        int iSize = 0;

        StatisticCounters oProducerCounter = new();

        StatisticCounters oConsumerCounter = new();

        QueueHandler<ProducerConsumerDataContainer<T>> oProducerQueue = new();


        public int MaxProductionQueueSize
        {
            get => oProducerQueue.MaxQueueSize;
            set => oProducerQueue.MaxQueueSize = value;
        }
        public int ProductionCount { get; }

        public int MaxConsumerParallelism { get; set; } = 1;

        ThreadBase oConsumerThread = new ThreadBase();

        #endregion

        #region Abstract/Virtual Methods
       // protected abstract Result ReshapeImpl(int iNewSize);
        protected abstract Result ProduceDataImpl();
        protected abstract Result ConsumeDataImpl(ProducerConsumerDataContainer<T> oDataProduced);

        #endregion


        public ProducerConumerBase()
        {
            oConsumerThread.OnThreadTriggerAsync += ConsumerTaskAsync;
            OnCreateImplementationAsync  += ProducerConumerBase_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += ProducerConumerBase_OnDestroyImplementationAsync;
        }

        private async Task ProducerConumerBase_OnCreateImplementationAsync(object? sender, ResultEventArgs e)
        {
            await Task.Run(() =>
            {
                string sM = nameof(ProducerConumerBase_OnCreateImplementationAsync);
                oConsumerThread.Name = $"{Name}.ConsumerThread";
                oConsumerThread.WakeupTimeMs = 0;
                oConsumerThread.OnThreadTriggerAsync += ConsumerTaskAsync;
                oConsumerThread.AllowEventOverlap = false;
                e.AddResult(oConsumerThread.Create());
            });
        }
        private async Task ProducerConumerBase_OnDestroyImplementationAsync(object? sender, ResultEventArgs e)
        {
            await Task.Run(() =>
            {
                string sM = nameof(ProducerConumerBase_OnDestroyImplementationAsync);
                var res = oConsumerThread?.Destroy() ?? Result.CreateResultSuccess();
                e.AddResult(res);
            });
        }
        

        public Result Consume()
        {
            string sM = nameof(Consume);    
            try
            {
                var oQueueItem = oProducerQueue.Dequeue();
                if (oQueueItem != null)
                {
                    Consuming?.Invoke(this, EventArgs.Empty);
                    var oTE = oConsumerCounter.TimeStart();
                    var oConsumeRes = ConsumeDataImpl(oQueueItem);
                    var oArgs = ResultEventArgs.CreateEventArgs(oConsumeRes, null);
                    oArgs.Timestamp = oQueueItem.Timestamp;
                    if (oConsumeRes.Valid)
                    {
                        LogMan.Trace(sC, sM, $"{Name} : Data Consumed");
                        Consumed?.Invoke(this, oArgs);
                        oConsumerCounter.AddStatisticTime(oTE);
                        oConsumerCounter.AddValidEvent();
                    }
                    else
                    {
                        LogMan.Error(sC, sM, $"{Name} : Data Consume Error : {oConsumeRes.ErrorMessage}");
                        ConsumeError?.Invoke(this, oArgs);
                        oConsumerCounter.AddErrorEvent();
                    }
                    return oConsumeRes;
                }
                return Result.CreateResultError(OperationResult.ErrorQueueEmpty, "Producer Consumer Queue Empty",0);
            }
            catch (OperationCanceledException canc)
            {
                oConsumerCounter.AddDropEvent();
                LogMan.Trace(sC, sM, $"{Name} : Data Dropped");
                return Result.CreateResultError(canc);
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                oConsumerCounter.AddErrorEvent();
                ConsumeError?.Invoke(this, ResultEventArgs.CreateEventArgs(Result.CreateResultError(ex), null));
                return Result.CreateResultError(ex);
            }
        }




        async Task ConsumerTaskAsync(object? sender, ThreadEventArgs e)
        {
            await Task.Run(() =>
            {
                string sM = nameof(ConsumerTaskAsync);
                try
                {
                    int iPar = Math.Max(1, MaxConsumerParallelism);
                    while (!oProducerQueue.QueueEmpty)
                    {
                        e.CancellationTokenSource?.Token.ThrowIfCancellationRequested();
                        Parallel.For(0, iPar, X =>
                        {
                            Consume();
                        });
                    }
                }
                catch (Exception ex)
                {
                    oConsumerCounter.AddErrorEvent();
                    HandleError(sM, sM, ex, ConsumeError);
                }
                //          });
            });
        }


        public Result Produce()
        {
            string sM = nameof(Produce);
            try
            {
                LogMan.Trace(sC, sM, $"{Name} : Producing Data");
                if (MaxProductionQueueSize > 0)
                {
                    if (ProductionCount > MaxProductionQueueSize)
                    {
                        LogMan.Error(sC, sM, $"{Name} : Production Overflow");
                        var oArgs = ResultEventArgs.CreateEventArgs(Result.CreateResultError(OperationResult.ErrorDropData, "Drop data due to Overflow", 0));
                        ProduceError?.Invoke(this, oArgs);
                        return oArgs.ResultError;
                    }
                }
                Producing?.Invoke(this, EventArgs.Empty);
                var oTE = oProducerCounter.TimeStart();
                var oProduceRes = ProduceDataImpl();
                if (oProduceRes.Valid)
                {
                    if (oProduceRes.Tag == null)
                    {
                        throw new ArgumentNullException($"{sC}:{sM} : {Name} : Tag null : Correct BUG on code");                        
                    }
                    if (oProduceRes.Tag is T item)
                    {
                        oProducerQueue.Enqueue(new ProducerConsumerDataContainer<T>()
                        {
                            Data = item,
                            ProductionResult = oProduceRes,
                        });
                        oConsumerThread.ThreadSignal();
                    }
                    else
                    {
                        throw new ArgumentNullException($"{sC}:{sM} : {Name} : Tag object not recognized : Correct BUG on code");
                    }
                    oProducerCounter.AddStatisticTime(oTE);
                    oProducerCounter.AddValidEvent();
                    LogMan.Trace(sC, sM, $"{Name} : Data Produced");
                    Produced?.Invoke(this, ResultEventArgs.CreateEventArgs(oProduceRes));
                }
                else
                {
                    oProducerCounter.AddErrorEvent();
                    LogMan.Error(sC, sM, $"{Name} : Data Production Error : {oProduceRes.ErrorMessage}");
                    ProduceError?.Invoke(this, ResultEventArgs.CreateEventArgs(oProduceRes));
                }
                return oProduceRes;
            }
            catch (Exception ex)
            {
                oProducerCounter.AddErrorEvent();
                return HandleError(sC, sM, ex, ProduceError);
            }
        }

    }
}
