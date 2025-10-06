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
        Result ProduceData();
        Result ConsumeData();

        int MaxProductionSize { get; set; }
        int MaxParallelism { get; set; }

        public event EventHandler<ProducerConsumerEventArgs<T>> OnProducing;
        public event EventHandler<ResultEventArgs> OnProduce;
        public event EventHandler<ResultEventArgs> OnProduceDrop;
        public event EventHandler<ResultEventArgs> OnProduceError;
        public event EventHandler OnConsuming;
        public event EventHandler<ResultEventArgs> OnConsume;
        public event EventHandler<ResultEventArgs> OnConsumeDrop;
        public event EventHandler<ResultEventArgs> OnConsumeError;

    }
}
