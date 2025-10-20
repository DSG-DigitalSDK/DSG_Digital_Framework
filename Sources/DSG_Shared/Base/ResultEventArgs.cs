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
        public DateTime Timestamp { get; internal set; } = DateTime.Now;
        public List<Result> ResultList { get; private set; } = new List<Result>();
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        public bool HasError => ResultList.Any(X => X.HasError);
        public bool Valid => ResultList.All(X => X.Valid);
        public Result ResultError => ResultList.FirstOrDefault(X => X.HasError);

        public void AddResult(Result oRes) => ResultList.Add(oRes);

        public static ResultEventArgs CreateEventArgs(Result oRes, CancellationTokenSource oTokenSource )
        {
            var oResult = new ResultEventArgs()
            {
                CancellationTokenSource = oTokenSource
            };
            if (oRes != null)
            {
                oResult.ResultList.Add(oRes);
            }
            return oResult;
        }

        public static ResultEventArgs CreateEventArgs(Result oRes) => CreateEventArgs(oRes, null);
        public static ResultEventArgs CreateEventArgs(CancellationTokenSource oTokenSource) => CreateEventArgs(null, oTokenSource);

    }
}
