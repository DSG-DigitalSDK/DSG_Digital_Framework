using DSG.Base;
using Result = DSG.Base.Result;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.ProducerConsumer
{
    public interface IProducerConsumer<T> where T : class
    {
        Result Produce();
        Result Consume();

        int MaxProductionQueueSize { get; set; }
        int MaxConsumerParallelism { get; set; }

        public event EventHandler Producing;
        public event EventHandler<ResultEventArgs> Produced;
        public event EventHandler<ResultEventArgs> ProduceDrop;
        public event EventHandler<ResultEventArgs> ProduceError;
        public event EventHandler Consuming;
        public event EventHandler<ResultEventArgs> Consumed;
        public event EventHandler<ResultEventArgs> ConsumeDrop;
        public event EventHandler<ResultEventArgs> ConsumeError;

    }
}
