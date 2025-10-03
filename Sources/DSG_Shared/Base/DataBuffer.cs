using DSG.Base;
using DSG.Log;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG_Shared.Base
{
    public class DataBuffer
    {
        static readonly string className = nameof(DataBuffer); 
        public byte[]? Data { get; set; }
        public int DataStartOffset { get; set; }
        public int DataByteCount { get; set; }
        public int DataLenght => Data?.Length ?? 0;

        public DataBuffer()
        {
            Free();
        }
        public DataBuffer(int iSize)
        {           
            Alloc(iSize);
        }

        public Result Alloc(int iSize)
        {
            Free();
            if (iSize > 0)
            {
                Data = new byte[iSize];
                DataStartOffset = 0;
                DataByteCount = iSize;
                return Result.CreateResultSuccess();
            }
            return Result.CreateResultError(OperationResult.ErrorResource, $"Invalid buffer size {iSize}", 0);
        }

        public void Free()
        {
            Data = null;
            DataStartOffset = 0;
            DataByteCount = 0;
        }

        public MemoryStream? ToStream(int iBufferOffset, int iBufferLength, bool allowPartialCopy ) 
        {
            string sMethod =nameof(ToStream);
            if (Data == null)
            {
                return null;
            }
            if (iBufferLength <= 0)
            {
                return null;    
            }
            try
            { 
                var len = iBufferOffset+ iBufferLength;
                if (len > Data.Length && !allowPartialCopy)
                {
                    return null;
                }
                var ms = new MemoryStream(len);
                ms.Read(Data, iBufferOffset,iBufferLength);
                return ms;
            }
            catch (Exception ex)
            {
                LogMan.Error(className, sMethod, "Buffer Error", ex);
                return null;
            }
        }
        public MemoryStream? ToStream(int iBufferOffset, int iBufferLength) => ToStream(iBufferOffset, iBufferLength, false);
        public MemoryStream? ToStream() => ToStream(DataStartOffset, DataByteCount, false);
        public MemoryStream? ToStreamDump() => ToStream(0,DataLenght, false);

        public string ToStringAscii(int iBufferOffset, int iBufferLength, bool allowPartialCopy)
        {
            string sMethod = nameof(ToStringAscii);
            if (Data == null)
            {
                return "";
            }
            if (iBufferLength <= 0)
            {
                return "";
            }
            try
            {
                var len = iBufferOffset + iBufferLength;
                if (len > Data.Length && !allowPartialCopy)
                {
                    return "";
                }
                var sTempString = Encoding.ASCII.GetString(Data);
                return sTempString.Substring(iBufferOffset, len);
            }
            catch (Exception ex)
            {
                LogMan.Error(className, sMethod, "Buffer Error", ex);
                return null;
            }
        }

        public string ToStringAscii() => ToStringAscii(DataStartOffset, DataByteCount, false);
        public string ToStringAsciiDump() => ToStringAscii(0, DataLenght, false);


        static public DataBuffer? FromStream(Stream stream, int lenght)
        {
            string sMethod = nameof(FromStream);
            if (stream == null)
                return null;
            if(!stream.CanRead)
                return null;
            if (lenght <= 0)
                return null;
            try
            {
                var Buffer = new DataBuffer(lenght);
                stream.Write(Buffer.Data, 0, lenght);
                return Buffer;
            }
            catch (Exception ex)
            {
                LogMan.Error(className, sMethod, "Buffer Error", ex);
                return null;
            }
        }
        static public DataBuffer? FromStream(Stream stream, int offset, int lenght)
        {
            string sMethod = nameof(FromStream);
            if (stream == null)
                return null;
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                return FromStream(stream, lenght);
            }
            catch (Exception ex)
            {
                LogMan.Error(className, sMethod, "Buffer Error", ex);
                return null;
            }
        }

        static public DataBuffer FromStream(Stream stream) => FromStream( stream,(int)stream.Length);


        static public DataBuffer FromString(string sMessage, Encoding oEncoding )
        {
            var oBuffer = oEncoding.GetBytes(sMessage);
            var oData = new DataBuffer()
            {
                Data = oBuffer,
                DataStartOffset = 0,
                DataByteCount = oBuffer.Length
            };
            return oData;
        }
        static public DataBuffer FromAsciiString(string sMessage)
        {
            var oBuffer = Encoding.ASCII.GetBytes(sMessage);
            var oData = new DataBuffer()
            {
                Data = oBuffer,
                DataStartOffset = 0,
                DataByteCount = oBuffer.Length
            };
            return oData;
        }
    }
}