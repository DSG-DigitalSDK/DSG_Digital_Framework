using DSG.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSG.Threading;
using System.Collections.Concurrent;
using DSG.Shared;
using DSG.IO;
using DSG.Base;
using Result = DSG.Base.Result;

namespace DSG.IO
{
    public abstract class ReadWriteBase : ConnectableBase, ReadWriteInterface
    {
        static string sC = nameof(ReadWriteBase);


        /// <summary>
        /// Timeout for read operations in milliseconds
        /// </summary>
        public int ReadTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Timeout for write operations in milliseconds
        /// </summary>
        public int WriteTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Counters for read operations (success, error, timeout, elapsed time)
        /// </summary>
        public StatisticCounters ReadCounters { get; private set; } = new StatisticCounters();

        /// <summary>
        /// Counters for write operations (success, error, timeout, elapsed time)
        /// </summary>
        public StatisticCounters WriteCounters { get; private set; } = new StatisticCounters();

        ThreadBaseAsync oReadThread = new();
        SemaphoreSlim oReadSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim oWriteSemaphore = new SemaphoreSlim(1, 1);

        public bool EnableReader 
        {
            get => oReadThread.Enabled;
            set => oReadThread.Enabled = value;
        }
        public int PollingReadMs 
        {
            get => oReadThread.WakeupTimeMs;
            set => oReadThread.WakeupTimeMs = value;
        }


        // Read/Write events
        public event EventHandler? DataReading;
        public event EventHandler<ResultEventArgs>? DataReaded;
        public event EventHandler<ResultEventArgs>? DataReadError;
        public event EventHandler? DataWriting;
        public event EventHandler<ResultEventArgs>? DataWritten;
        public event EventHandler<ResultEventArgs>? DataWriteError;

        public ReadWriteBase()
        {
            //
            RegisterEvents();
            ResetReadCounters();
            ResetWriteCounters();
        }

        void RegisterEvents()
        {
            oReadThread.WakeupAsync += TaskReadDataAsync;
            OnCreateImplementationAsync += ConnectableBasePolling_OnCreateImplementationAsync;
            OnDestroyImplementationAsync += ConnectableBasePolling_OnDestroyImplementationAsync;
            OnConnectImplementationAsync += ConnectableBasePolling_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync += ConnectableBasePolling_OnDisconnectImplementationAsync;
        }
        void UnregisterEvents()
        {
            oReadThread.WakeupAsync -= TaskReadDataAsync;
            OnCreateImplementationAsync -= ConnectableBasePolling_OnCreateImplementationAsync;
            OnDestroyImplementationAsync -= ConnectableBasePolling_OnDestroyImplementationAsync;
            OnConnectImplementationAsync -= ConnectableBasePolling_OnConnectImplementationAsync;
            OnDisconnectImplementationAsync -= ConnectableBasePolling_OnDisconnectImplementationAsync;
        }

        private async Task ConnectableBasePolling_OnCreateImplementationAsync(object sender, ResultEventArgs e)
        {
            oReadThread.Name = $"{ConnectionName}.TaskReader";
            e.AddResult(await oReadThread.CreateAsync());
          }

        private async Task ConnectableBasePolling_OnDestroyImplementationAsync(object sender, ResultEventArgs e)
        {
            e.AddResult(await oReadThread.DestroyAsync());
        }

        private Task ConnectableBasePolling_OnConnectImplementationAsync(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStart();
            return Task.CompletedTask;
        }

        private Task ConnectableBasePolling_OnDisconnectImplementationAsync(object? sender, ResultEventArgs e)
        {
            oReadThread.TimerStop();
            return Task.CompletedTask;
        }


        private async Task TaskReadDataAsync(object arg1, ThreadEventArgs arg2)
        {
            string sM = nameof(TaskReadDataAsync);
            Result res;
            do
            {
                res = await ReadDataAsync();
            }
            while (res.Valid);
            if (res.HasError && res.OperationResult != OperationResult.ErrorTimeout)
            {
                LogMan.Error(sC, sM, $"{oReadThread.Name} : Error reading data : {res.ErrorMessage}");
            }
        }

        /// <summary>
        /// Resets read operation counters
        /// </summary>
        public void ResetReadCounters() => ReadCounters.ResetCounters();

        /// <summary>
        /// Resets write operation counters
        /// </summary>
        public void ResetWriteCounters() => WriteCounters.ResetCounters();

        #region Read / Write Methods

        /// <summary>
        /// Manages operation results: updates counters, logs, and raises appropriate events
        /// </summary>
        /// <param name="sMethod">Calling method name</param>
        /// <param name="oRes">Operation result</param>
        /// <param name="oCounters">Statistic counters</param>
        /// <param name="oEventOk">Success event handler</param>
        /// <param name="oEventError">Error event handler</param>
        /// <returns>Same operation result for chaining</returns>
        Result ManageResult(string sMethod, Result oRes, TimeElapser oTStart, StatisticCounters oCounters, EventHandler<ResultEventArgs>? oEventOk, EventHandler<ResultEventArgs>? oEventError)
        {
            if (oRes.Valid)
            {
                oCounters.AddStatisticTime(oTStart);
                oCounters.AddValidEvent();
                LogMan.Trace(sC, sMethod, $"{ConnectionName} : Operation Successful");
                oEventOk?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
            }
            else
            {
                if (oRes.OperationResult == OperationResult.ErrorTimeout)
                {
                    oCounters.AddTimeoutEvent();
                    LogMan.Trace(sC, sMethod, $"{ConnectionName} : Operation Timeout");
                }
                else
                {
                    oCounters.AddErrorEvent();
                    LogMan.Error(sC, sMethod, $"{ConnectionName} : Operation Error : {oRes.ErrorMessage}");
                }
                oEventError?.Invoke(this, ResultEventArgs.CreateEventArgs(oRes));
            }
            return oRes;
        }


        /// <summary>
        /// Asynchronously reads data from the resource
        /// </summary>
        /// <returns>Result of the read operation</returns>
        public async Task<Result> ReadDataAsync()
        {
            string sM = nameof(ReadDataAsync);
            await oReadSemaphore.WaitAsync();
            try
            {
                LogMan.Trace(sC, sM, $"{ConnectionName} : Reading Data");
                DataReading?.Invoke(this, EventArgs.Empty);

                Result oRes = await CheckConnectionAsync(sM, DataReadError);
                if (oRes.HasError) return oRes;

                var oTS = ReadCounters.TimeStart();
                oRes = await ReadImplementationAsync();

                return ManageResult(sM, oRes, oTS, ReadCounters, DataReaded, DataReadError);
            }
            catch (Exception ex)
            {
                return HandleError(sC, sM, ex, DataReadError);
            }
            finally
            {
                oReadSemaphore.Release();
            }
        }



        /// <summary>
        /// Asynchronously writes data to the resource
        /// </summary>
        /// <param name="oObj">Object containing data to write</param>
        /// <returns>Result of the write operation</returns>
        public async Task<Result> WriteDataAsync(object oObj)
        {
            string sM = nameof(WriteDataAsync);
            await oWriteSemaphore.WaitAsync();
            try
            {
                Result oRes = await CheckConnectionAsync(sM, DataWriteError);
                if (oRes.HasError) return oRes;

                LogMan.Trace(sC, sM, $"{ConnectionName} : Writing Data");
                DataWriting?.Invoke(this, EventArgs.Empty);

                var oTS = WriteCounters.TimeStart();
                oRes = await WriteImplementationAsync(oObj);

                return ManageResult(sM, oRes, oTS, WriteCounters, DataWritten, DataWriteError);
            }
            catch (Exception ex)
            {
                WriteCounters.AddErrorEvent();
                return HandleError(sC, sM, ex, DataWriteError);
            }
            finally
            {
                oWriteSemaphore.Release();
            }
        }


        public abstract Result FlushRead();

        public abstract Result FlushWrite();

        public Result Flush()
        {
            var a = FlushRead();
            var b = FlushWrite();
            if (!a.Valid)
                return a;
            if (!b.Valid)
                return b;
            return Result.CreateResultSuccess();
        }

        #endregion
    }
}
