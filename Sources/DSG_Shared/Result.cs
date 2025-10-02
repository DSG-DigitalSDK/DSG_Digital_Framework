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
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

    }
}
