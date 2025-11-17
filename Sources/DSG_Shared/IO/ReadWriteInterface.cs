using DSG.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.IO
{
    public interface ReadWriteInterface
    {
        int ReadTimeoutMs { get; set; }
        int WriteTimeoutMs { get; set; }


        event EventHandler? DataReading;
        event EventHandler<ResultEventArgs>? DataReaded;
        event EventHandler<ResultEventArgs>? DataReadError;
        event EventHandler? DataWriting;
        event EventHandler<ResultEventArgs>? DataWritten;
        event EventHandler<ResultEventArgs>? DataWriteError;

        Task<Result> ReadDataAsync();
        Task<Result> WriteDataAsync(object oBuffer);

        public Result FlushRead();
        public Result FlushWrite();
        public Result Flush();
    }
}
