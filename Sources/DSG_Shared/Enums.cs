using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public enum OperationResult
    {
        Success = 0,
        Error,
        ErrorResource,
        ErrorFailure,
        ErrorException,
        ErrorTimeout,
        ErrorDropData,
        ErrorNotImplemented,
        ErrorQueueEmpty,
        ErrorQueueFull,
    }

    public enum StreamMode
    {
        Text,
        Binary
    }

    public enum Params
    {
        Unknown = 0,
        Name,
        ThrowExceptions,
        ConnectionName,
        ConnectionTimeout,
        ConnectionReadTimeout,
        ConnectionWriteTimeout,
        ConnectionString,
        ConnectionReadBufferSize,
        ConnectionWriteBufferSize,
        WriteTextNewLine,
        ReadTextNewLine,
        TextNewLine,
        TextEncoding,
        StreamMode,
        TaskRead,
        TaskReadPolling,
        TaskWrite,
        TaskWritePolling,
    }
}
