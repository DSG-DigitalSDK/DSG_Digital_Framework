using DSG.Base;
using DSG.Log;
using DSG.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSG.Threading;
using Result = DSG.Base.Result;
using NLog.LayoutRenderers.Wrappers;

namespace DSG.ProducerConsumer
{

    public abstract class ProducerConumerBase<T> : CreateBase where T : class
    {
        static string sC = nameof(ProducerConumerBase<T>);

        public event EventHandler? OnProducing;
        public event EventHandler<ResultEventArgs>? OnProduce;
        public event EventHandler<ResultEventArgs>? OnProduced;
        public event EventHandler<ResultEventArgs>? OnProduceError;
        public event EventHandler? OnConsuming;
        public event EventHandler<ResultEventArgs>? OnConsume;
        public event EventHandler<ResultEventArgs>? OnConsumed;
        public event EventHandler<ResultEventArgs>? OnConsumeError;

        int iSize = 0;

        public abstract Result ReshapeImpl(int iNewSize);
        public abstract Result ProduceDataImpl();
        public abstract Result ConsumeDataImpl();

        StatisticCounters oProducerCounter = new StatisticCounters();
        StatisticCounters oConsumerCounter = new StatisticCounters();
        QueueHandler<T> oProducerQueue = new QueueHandler<T>();  
       
        public int MaxProductionSize
        {
            get=>iSize;
            set
            {
                if (iSize != value)
                {
                    ReshapeImpl(iSize);
                }
            }
        }

        public abstract int ProductionCount { get; }

        public int MaxParallelism { get; set; } = 1;

        public Result Reshape(int iNewSize)
        {
            var res = ReshapeImpl(iNewSize);
            if (res.Valid)
            {
                iSize = iNewSize;
            }
            return res; 
        }

        ThreadBase oConsumerThread = new ThreadBase();

        public ProducerConumerBase()
        {
            oConsumerThread.OnSignal += ConsumerTask;
            OnCreateImplementation  += ProducerConumerBase_OnCreateImplementation;
            OnDestroyImplementation += ProducerConumerBase_OnDestroyImplementation;
        }


        private void ProducerConumerBase_OnCreateImplementation(object? sender, ResultEventArgs e)
        {
            string sM = nameof(ProducerConumerBase_OnCreateImplementation);
            oConsumerThread.Name = $"{Name}.ConsumerThread";
            oConsumerThread.WakeupTimeMs = 0;
            oConsumerThread.OnSignal += ConsumerTask;
            e.AddResult( oConsumerThread.Create() );
        }
        private void ProducerConumerBase_OnDestroyImplementation(object? sender, ResultEventArgs e)
        {
            string sM = nameof(ProducerConumerBase_OnDestroyImplementation);
            var res = oConsumerThread?.Destroy() ?? Result.CreateResultSuccess();
            e.AddResult(res);
        }


        void ConsumerTask(object? sender, ThreadEventArgs e)
        {
            string sM = nameof(ConsumerTask);
            try
            {
                int iPar = Math.Max(1,MaxParallelism);
                while (!oProducerQueue.QueueEmpty)
                {
                    e.CancellationTokenSource?.Token.ThrowIfCancellationRequested();
                    Parallel.For(0, iPar, X =>
                    {
                        try
                        {
                            OnConsuming?.Invoke(this, EventArgs.Empty);
                            var oT = oProducerQueue.Dequeue();
                            if (oT != null)
                            {
                                LogMan.Trace(sC, sM, $"{Name} : Data Consumed");
                                oConsumerCounter.TimeStart();
                                OnConsume?.Invoke(this, new ResultEventArgs { CancellationTokenSource = e.CancellationTokenSource });
                                oConsumerCounter.AddStatisticTime();
                                oConsumerCounter.AddValidEvent();
                            }
                        }
                        catch (OperationCanceledException canc)
                        {
                            LogMan.Trace(sC, sM, $"{Name} : Data Dropped");
                            oConsumerCounter.AddDropEvent();
                        }
                        catch (Exception ex)
                        {
                            LogMan.Exception(sC, sM, ex);
                            oConsumerCounter.AddErrorEvent();
                            OnConsumeError?.Invoke(this, new ResultEventArgs { CancellationTokenSource = e.CancellationTokenSource });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                oConsumerCounter.AddErrorEvent();
                HandleError( sM,sM, ex,OnConsumeError );
            }
        }


        public Result ProduceData()
        {
            string sM = nameof(ProduceData);
            try
            {
                LogMan.Trace(sC, sM, $"{Name} : Producing Data");
                if (MaxProductionSize > 0)
                {
                    if (ProductionCount > MaxProductionSize)
                    {
                        LogMan.Error(sC, sM, $"{Name} : Production Overflow");
                        var oArgs = ResultEventArgs.CreateEventArgs(Result.CreateResultError(OperationResult.ErrorDropData, "Drop data due to Overflow", 0));
                        OnProduceError?.Invoke(this, oArgs);
                        return oArgs.ResultError;
                    }
                }
                OnProducing?.Invoke(this, EventArgs.Empty);
                var res = ProduceDataImpl();
                if (res.Valid)
                {
                    LogMan.Trace(sC, sM, $"{Name} : Data Produced");
                    OnProduce?.Invoke(this, ResultEventArgs.CreateEventArgs(res));
                }
                else
                {
                    LogMan.Error(sC, sM, $"{Name} : Data Production Error : {res.ErrorMessage}");
                    OnProduce?.Invoke(this, ResultEventArgs.CreateEventArgs(res));
                }
                return res;
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, OnProduceError);
            }
        }
    }
}
