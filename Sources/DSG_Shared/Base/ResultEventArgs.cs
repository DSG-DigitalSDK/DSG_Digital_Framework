using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public class ResultEventArgs : EventArgs
    {
        public Result? Result { get; set; }

        public CancellationTokenSource? CancellationTokenSource { get; set; }
        //-----------------------------------------------
        //public DataBuffer? BufferRead { get; set; }
        //public DataBuffer? BufferWrite { get; set; }
        //public String? MessageRead { get; set; }
        //public String? MessageWrite { get; set; }


        //public static ResultEventArgs CreateEventArgs(Result? oRes, DataBuffer? oReadBuffer, DataBuffer? oWriteBuffer) 
        //    => new ResultEventArgs() 
        //    {
        //        BufferRead = oReadBuffer,
        //        BufferWrite = oWriteBuffer,
        //        Result = oRes 
        //    };
        //public static ResultEventArgs CreateEventArgs(Result? oRes, string? sReadBuffer, String? sWriteBuffer)
        //    => new ResultEventArgs()
        //    {
        //        MessageRead = sReadBuffer,
        //        MessageWrite = sWriteBuffer,
        //        Result = oRes
        //    };
        public static ResultEventArgs CreateEventArgs(Result? oRes) 
            => new ResultEventArgs()
            {
                Result = oRes
            };


    }
}
