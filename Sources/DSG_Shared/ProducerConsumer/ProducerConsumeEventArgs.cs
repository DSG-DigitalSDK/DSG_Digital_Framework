using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.ProducerConsumer
{
    public class ProducerConsumerEventArgs<T> : EventArgs
    {
       public DateTime Timestamp { get; private set; } =  DateTime.Now;
       public List<T> DataList { get; set; }=new List<T>();       
    }
}
