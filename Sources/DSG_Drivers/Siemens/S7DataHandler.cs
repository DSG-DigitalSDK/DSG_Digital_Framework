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

namespace DSG.Drivers.Siemens
{
    public class S7DataHandler : ConnectableBasePolling
    {
        string sC = nameof(S7DataHandler);

        static string ConnectionTemplate = @"IP(192.168.0.1)\Rack(0)\Slot(0)";
        static string splitter = @"\";

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
            this.Name = "Template";
            //this.ConnectionName = "Conn";
            this.ConnectionString = ConnectionTemplate;
            this.EnableReader = true;
            this.PollingReadMs = 1000;
         //   this.EnableWriter = false;
          //  this.PollingWriteMs = 50;
            // Registring Events
            OnCreateImplementationAsync += Event_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += Event_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += Event_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync += Event_OnDisconnectImplementationAsync;
            // Initialize CLient
        }

        private Task Event_OnCreateImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnCreateImplementationAsync);
            s7client = new Sharp7.S7Client();
            return Task.CompletedTask;
        }

        private Task Event_OnDestroyImplementationAsync(object sender, ResultEventArgs oArgs)
        {
            string sM = nameof(Event_OnDestroyImplementationAsync);
            s7client?.Disconnect();
            s7client = null;
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
                    LogMan.Error(sC, sM, $"{ObjectID} : Connection String Error : '{ConnectionString}' :  expected '{ConnectionTemplate}'");
                }
                string sIP = aConn[0];
                int iRack = Convert.ToInt32(aConn[1]);
                int iSlot = Convert.ToInt32(aConn[2]);
                var result = s7client.ConnectTo(sIP, iRack, iSlot);
                if (result != 0)
                {
                    // Bug on 
                    LogMan.Warning(sC, sM, $"{ObjectID} : Connection Refused, retry");
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
            });
        }

       

        protected override async Task<Result> ReadImplementationAsync()
        {
            string sM = nameof(ReadImplementationAsync);
            return await Task.Run(() =>
            {
                List<S7Client.S7DataItem> list = new List<S7Client.S7DataItem>();
                try
                {
                    foreach (var item in ReadDataListTemplate)
                    {
                        var s7item = S7DataConversion.ToS7DataItem(item);
                        if (s7item != null)
                        {
                            list.Add(s7item.Value);
                        }
                        else
                        {
                            return Result.CreateResultError(OperationResult.ErrorResource, $"Invalid DataBufferList item : {item}", 0);
                        }
                    }
                    if (list.Count == 0)
                    {
                        return Result.CreateResultError(OperationResult.ErrorResource, $"No data to read", 0);
                    }
                    var result = s7client.ReadMultiVars(list.ToArray(), list.Count);
                    if (result != 0)
                    {
                        // Bug on read, retry
                        //if (result == ???)
                        //{
                        //    result = s7client.ReadMultiVars(list.ToArray(), list.Count);
                        //}
                        if (result != 0)
                        {
                            return Result.CreateResultError(OperationResult.Error, s7client.ErrorText(result), result);
                        }
                    }
                    List<S7PlcDataItem> blockList = new List<S7PlcDataItem>();
                    foreach (var item in list)
                    {
                        if (item.Result != 0)
                        {
                            return Result.CreateResultError(OperationResult.Error, $"S7 item read error: {s7client.ErrorText(item.Result)}", item.Result);
                        }
                        var oData = S7DataConversion.ToPlcDataItem(item);
                        if (oData == null)
                        {
                            return Result.CreateResultError(OperationResult.Error, "S7 item conversion error", 0);
                        }
                        blockList.Add(oData);
                    }
                    return Result.CreateResultSuccess(blockList);
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
            });
        }

        Result WriteImplementation(List<S7Client.S7DataItem> oList)
        {
            if (oList == null)
                return Result.CreateResultError(OperationResult.Error, "null write list", 0);
            var list = new List<S7Client.S7DataItem>();
            try
            {
                var result = s7client.WriteMultiVars(list.ToArray(), list.Count);
                if (result != 0)
                {
                    return Result.CreateResultError(OperationResult.Error, s7client.ErrorText(result), result);
                }
                return Result.CreateResultSuccess(oList);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        Result WriteImplementation(List<S7PlcDataItem> oList)
        {
            if (oList == null)
                return Result.CreateResultError(OperationResult.Error, "null write list", 0);
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
