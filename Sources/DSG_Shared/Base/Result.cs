using DSG.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Base
{
    public class Result
    {
        public OperationResult OperationResult { get; set; }
        public bool Valid => OperationResult == OperationResult.Success; 
        public bool HasError => OperationResult != OperationResult.Success;
        public int ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public object? Tag { get; set; }

        public static Result CreateResultError(Exception ex, string? message) => new Result()
        {
            OperationResult = OperationResult.ErrorException,
            Exception = ex,
            ErrorMessage = message ?? ex.Message
        };

        public static Result CreateResultError(OperationResult eResult, string? sErrorMessage, int iErrorCode, Exception? ex) => new Result()
        {
            OperationResult = eResult,
            ErrorCode = iErrorCode,
            ErrorMessage = sErrorMessage,
            Exception = ex  
        };
        public static Result CreateResultError(OperationResult eResult, string? sErrorMessage, int iErrorCode) => CreateResultError(eResult, sErrorMessage, iErrorCode, null);
        public static Result CreateResultError(Exception ex) => CreateResultError(ex, null);
        public static Result CreateResultError() => CreateResultError( OperationResult.Error,null,0);
        public static Result CreateResultErrorUnknown() => CreateResultError( OperationResult.Error, "Unknown Error", 0, null);
        public static Result CreateResultError(OperationResult eResult) => CreateResultError(eResult, eResult.ToString(),0);

        public static Result CreateResultSuccess(object? oTag ) => new Result()
        {
            OperationResult = OperationResult.Success,
            Tag = oTag 
        };
        public static Result CreateResultSuccess() => CreateResultSuccess(null);
    }
}
