using DSG.Base;
using DSG.Log;
using DSG.Shared;
using DSG_Shared.ProducerConsumer;
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

        ProducerConsumerCounters oProducerCounter = new ProducerConsumerCounters();
        ProducerConsumerCounters oConsumerCounter = new ProducerConsumerCounters();
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
        }

        protected override Result CreateImpl()
        {
            string sM = nameof(CreateImpl);
            oConsumerThread.Name = $"{Name}.ConsumerThread";
            oConsumerThread.WakeupTimeMs = 0;
            oConsumerThread.OnSignal += ConsumerTask;
            var res = oConsumerThread.Create();
            return res;
        }
        protected override Result DestroyImpl()
        {
            string sM = nameof(DestroyImpl);
            var res = oConsumerThread?.Destroy() ?? Result.CreateResultSuccess();
            return res;
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
                LogMan.Exception(sC, sM, Name, ex);
                oConsumerCounter.AddErrorEvent();
                var ret = Result.CreateResultError(ex);
                OnConsumeError?.Invoke(this, new ResultEventArgs { Result = ret });
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
                        OnProduceError?.Invoke(this, new ResultEventArgs { Result = Result.CreateResultError(OperationResult.ErrorDropData, "Drop data due to Overflow", 0) });
                        return Result.CreateResultError(OperationResult.ErrorDropData, "Drop data due to Overflow", 0);
                    }
                }
                OnProducing?.Invoke(this, EventArgs.Empty);   
                var res = ProduceDataImpl();
                if (res.Valid)
                {
                    LogMan.Trace(sC, sM, $"{Name} : Data Produced");
                    OnProduce?.Invoke(this, new ResultEventArgs { Result = res });
                }
                else
                {
                    LogMan.Error(sC, sM, $"{Name} : Data Production Error : {res.ErrorMessage}");
                    OnProduceError?.Invoke(this, new ResultEventArgs {Result = res});
                }
                return res;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, Name, ex);
                return Result.CreateResultError(ex);
            }
        }
    }
}
