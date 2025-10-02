using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Log
{
    public interface ILogConsumer
    {
        string Name { get; }
        public bool Create();
        public bool Destroy();
        void ProcessMessage(object sender, LogEventArgs oArgs);
    }
}
