using DSG.Base;
using DSG.IO;
using DSG.Log;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Sharp7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


/*------------------------------------------------------------------------------------------
  SNIPPET 1 
------------------------------------------------------------------------------------------

//This class allows to fill the S7DataItem[] and performs a ReadMultivar or WriteMultivar without using unsafe code.
//The best way to understand the use of this class is analyzing an example.
//The process is simple:
//1.    Instantiate the class.
//2.    Add the vars reference (i.e. your buffer) specifying the kind of element that we want read or write.
//3.    Call Read or Write method.

// Multi Reader Instance specifying Client
S7MultiVar Reader = new S7MultiVar(Client);

// Our buffers

byte[] DB_A = new byte[1024];
byte[] DB_B = new byte[1024];
byte[] DB_C = new byte[1024];

// Our DB number references

int DBNumber_A = 1; // DB1
int DBNumber_B = 1; // DB2
int DBNumber_C = 1; // DB3

// Add Items def. specifying 16 bytes to read starting from 0

Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_A, 0, 16, ref DB_A);
Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_B, 0, 16, ref DB_B);
Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_C, 0, 16, ref DB_C);

// Performs the Read
int Result = Reader.Read();

*/

/*------------------------------------------------------------------------------------------
  SNIPPET 2 
------------------------------------------------------------------------------------------

Private Structure ComponentResult
    Public SerialNumber As String   ' Component Serial Number
    Public TestResult As Integer    ' Result code 0:Unknown, 1:Good, 2:Scrap
    Public LeakDetected As Double   ' Leak value [cc/min]
    Public TestDateTime As DateTime ' Test Timestamp
End Structure

private ComponentResult LeakResult()
{

    ComponentResult Result = new ComponentResult();
    byte[] Buffer = new byte[26];
    // Reads the buffer.
    Client.DBRead(100, 0, 26, Buffer);
    // Extracts the fields and inserts them into the struct
    Result.SerialNumber = S7.GetCharsAt(Buffer, 0, 12);
    Result.TestResult = S7.GetIntAt(Buffer, 12);
    Result.LeakDetected = S7.GetRealAt(Buffer, 14);
    Result.TestDateTime = S7.GetDateTimeAt(Buffer, 18);
    return Result;
}

*/



namespace DSG.Drivers.Siemens
{
    public class S7DataHandler : ReadWriteBase
    {
        string sC = nameof(S7DataHandler);

        static string ConnectionTemplate = @"IP(192.168.0.1),Rack(0),Slot(0)";
        static string splitter = @",";

        Sharp7.S7Client? s7client = null;

        /// <summary>
        /// List to populate with template of data
        /// </summary>
        public List<S7PlcDataItem> ReadDataListTemplate { get; private set; } = new List<S7PlcDataItem>();

        public new bool Connected => s7client?.Connected ?? false;

        public S7DataHandler()
        {
            ResetClass();
        }


               
        void ResetClass()
        {
            // Class Initialization
            this.Name = "S7_Client";
            this.ConnectionString = ConnectionTemplate;
            this.EnableReader = true;
            this.PollingReadMs = 1000;
            // Registring Events
            OnCreateImplementationAsync += Event_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += Event_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += Event_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync += Event_OnDisconnectImplementationAsync;
        }

        private Task Event_OnCreateImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            s7client = new Sharp7.S7Client();
            oArgs.AddResult(Result.CreateResultSuccess());
            return Task.CompletedTask;
        }

        private Task Event_OnDestroyImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            s7client?.Disconnect();
            s7client = null;
            oArgs.AddResult(Result.CreateResultSuccess());
            return Task.CompletedTask;
        }

        private async Task Event_OnConnectImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnCreateImplementationAsync);
            await Task.Run(async () =>
            {
                var aConn = ConnectionString.Split(splitter, StringSplitOptions.TrimEntries);
                if (aConn.Length != 3)
                {
                    LogMan.Error(sC, sM, $"{ConnectionName} : Connection String Error : '{ConnectionString}' :  expected '{ConnectionTemplate}'");
                }
                string sIP = aConn[0];
                int iRack = Convert.ToInt32(aConn[1]);
                int iSlot = Convert.ToInt32(aConn[2]);
                var result = s7client.ConnectTo(sIP, iRack, iSlot);
                if (result != 0)
                {
                    // Bug on 
                    LogMan.Warning(sC, sM, $"{ConnectionName} : Connection Refused, retry");
                    await Task.Delay(500).ConfigureAwait(false);
                    result = s7client.ConnectTo(sIP, iRack, iSlot);
                    if (result != 0)
                    {
                        oArgs.AddResult(Result.CreateResultError(OperationResult.ErrorResource, s7client.ErrorText(result), result));
                        return;
                    }
                }
                oArgs.AddResult(Result.CreateResultSuccess());
            });
        }

        private async Task Event_OnDisconnectImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnDisconnectImplementationAsync);
            await Task.Run(() =>
            {
                s7client?.Disconnect();
                oArgs.AddResult(Result.CreateResultSuccess());
                return Task.CompletedTask;
            });
        }

        protected override async Task<Result> ReadImplementationAsync()
        {
            string sM = nameof(ReadImplementationAsync);
            return await Task.Run(() =>
            {
                try
                {
                    if (ReadDataListTemplate.Count == 0)
                    {
                        return Result.CreateResultError(OperationResult.ErrorResource, $"No template data to read", 0);
                    }
                    S7MultiVar s7Reader = new S7MultiVar(s7client);
                    List<S7PlcDataItem> readList = new List<S7PlcDataItem>();
                    foreach (var item in ReadDataListTemplate)
                    {
                        var readItem = S7PlcDataItem.Create(item);
                        var eArea = S7DataConversion.ToS7Area(item.Area);
                        s7Reader.Add((int)eArea, (int)S7WordLength.Byte, item.DbNum, item.Offset, item.Length, ref readItem.oData);
                        readList.Add(readItem); 
                    }
                    var resultRead = s7Reader.Read();
                    if (resultRead != 0)
                    {
                        // Bug on read, retry
                        //if (result == ???)
                        //{
                        //    result = s7client.ReadMultiVars(list.ToArray(), list.Count);
                        //}
                        if (resultRead != 0)
                        {
                            return Result.CreateResultError(OperationResult.Error, s7client?.ErrorText(resultRead), resultRead);
                        }
                    }
                    foreach (var result in s7Reader.Results)
                    {
                        if (result != 0)
                        {
                            return Result.CreateResultError(OperationResult.Error, $"S7 item read error: {s7client?.ErrorText(result)}", result);
                        }
                    }
                    return Result.CreateResultSuccess(readList);
                }
                catch (Exception ex)
                {
                    throw;
                }
            });
        }

        Result WriteImplementation(List<S7Client.S7DataItem> oList)
        {
            if (oList == null)
                return Result.CreateResultError(OperationResult.Error, "null write list", 0);
            var list = new List<S7Client.S7DataItem>();

            var result = s7client.WriteMultiVars(list.ToArray(), list.Count);
            if (result != 0)
            {
                return Result.CreateResultError(OperationResult.Error, s7client.ErrorText(result), result);
            }
            return Result.CreateResultSuccess(oList);
        }

        Result WriteImplementation(List<S7PlcDataItem> oList)
        {
            if (oList == null)
            {
                return Result.CreateResultError(OperationResult.Error, "null write list", 0);
            }
            var list = new List<S7Client.S7DataItem>();
            try
            {
                foreach (var item in oList)
                {
                    var s7item = S7DataConversion.ToS7DataItem(item);
                    if (s7item == null)
                        return Result.CreateResultError(OperationResult.Error, "null write list", 0);
                    list.Add(s7item.Value);
                }
                var res = WriteImplementation(list);
                if (res.Valid)
                {
                    return Result.CreateResultSuccess(oList);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                foreach (var item in list)
                {
                    if (item.pData != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(item.pData);
                    }
                }
            }
            return Result.CreateResultSuccess(oList);
        }

        protected override async Task<Result> WriteImplementationAsync(object oWriteObj)
        {
            string sM = nameof(WriteImplementationAsync);
            return await Task.Run(() =>
            {
                if (oWriteObj == null)
                {
                    return Result.CreateResultError(OperationResult.Error, "write data null",0);
                }
                if (oWriteObj is S7PlcDataItem oData)
                {
                    return WriteImplementation(new List<S7PlcDataItem> { oData });
                }
                if (oWriteObj is List<S7PlcDataItem> lData)
                {
                    return WriteImplementation(lData);
                }
                if (oWriteObj is S7Client.S7DataItem oS7Data)
                {
                    return WriteImplementation(new List<S7Client.S7DataItem> { oS7Data });
                }
                if (oWriteObj is List<S7Client.S7DataItem> lS7Data)
                {
                    return WriteImplementation(lS7Data);
                }
                return Result.CreateResultError(OperationResult.Error, $"cannot handle '{oWriteObj.GetType().Name}' ",0);
            });
        }

        public override Result FlushRead()        
        {
            return Result.CreateResultSuccess();
        }

        public override Result FlushWrite()
        {
            return Result.CreateResultSuccess();
        }
    }
}


