using DSG.Base;
using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.ProducerConsumer
{
    public class ProducerConsumerTester : ProducerConumerBase<string>
    {
        static readonly string sC = nameof(ProducerConsumerTester);

        int counter = 0;
        protected override Result ConsumeDataImpl(ProducerConsumerDataContainer<string> oDataProduced)
        {
            string sM = nameof(ConsumeDataImpl);    
            LogMan.Message( sC,sM, $"{Name} : {oDataProduced.Timestamp:HH:mm:ss.fff} Consuming data '{oDataProduced.Data}'");
            Thread.Sleep(1000);
            LogMan.Message(sC, sM, $"{Name} : {oDataProduced.Timestamp:HH:mm:ss.fff} Data consumed '{oDataProduced.Data}'");
            return Result.CreateResultSuccess();
        }

        protected override Result ProduceDataImpl()
        {
            string sM = nameof(ProduceDataImpl);
            var ret = Result.CreateResultSuccess();
            LogMan.Message(sC, sM, $"{Name} : {ret.Timestamp:HH:mm:ss.fff} Producing data");
            // Production rate 10x than consuming rate
            Thread.Sleep(100);
            string sMessage = $"Hello World {++counter}";
            ret.Tag = sMessage;
            LogMan.Message(sC, sM, $"{Name} : {ret.Timestamp:HH:mm:ss.fff} Produced '{sMessage}'");
            return Result.CreateResultSuccess(sMessage);
        }
    }
}
