using System.Data.Common;
using DSG.Drivers.SerialPort;
using DSG.Drivers.Siemens;
using DSG.Imaging;
using DSG.IO;
using DSG.Log;
using DSG.ProducerConsumer;
using DSG.Threading;


namespace WinFormTesterDemo
{
    internal static class Program
    {
        static string sC = nameof(Program);

        static void SerialTest()
        {
            string sM = nameof(SerialTest);

            SerialHandler oSer1 = new DSG.Drivers.SerialPort.SerialHandler()
            {
                Name = "Serial1",
                ConnectionName = "SerialTest",
                ConnectionString = "COM1/57600/ODD/7/1/NONE",
                PollingReadMs = 200,
                PollingWriteMs = 0,
                ReadTimeoutMs = 100,
                WriteTimeoutMs = 100,
                EnableReader = true,
                EnableWriter = true,
                DataMode = DSG.Base.StreamMode.Text
            };
            SerialHandler oSer2 = new DSG.Drivers.SerialPort.SerialHandler()
            {
                Name = "Serial2",
                ConnectionName = "SerialTest",
                ConnectionString = "COM2/57600/ODD/7/1/NONE",
                PollingReadMs = 200,
                PollingWriteMs = 0,
                ReadTimeoutMs = 100,
                WriteTimeoutMs = 100,
                EnableReader = true,
                EnableWriter = true,
                DataMode = DSG.Base.StreamMode.Text
            };
            oSer1.DataReaded += ((s, e) =>
            {
                LogMan.Message(sC, sM, $"{oSer1.Name} : Data Readed");
                var oObj = e.ResultList.FirstOrDefault(X => X.Tag != null);
                string sMsg = "Boh!";
                if (oObj?.Tag is DataBuffer oB) sMsg = oB.ToStringAscii();
                if (oObj?.Tag is String oS) sMsg = oS;
                LogMan.Message(sC, sM, $"{oSer1.Name} : Readed : {sMsg}");
            });
            oSer2.DataReaded += ((s, e) =>
            {
                LogMan.Message(sC, sM, $"{oSer2.Name} : Data Readed");
                var oObj = e.ResultList.FirstOrDefault(X => X.Tag != null);
                string sMsg = "Boh!";
                if (oObj?.Tag is DataBuffer oB) sMsg = oB.ToStringAscii();
                if (oObj?.Tag is String oS) sMsg = oS;
                LogMan.Message(sC, sM, $"{oSer2.Name} : Readed : {sMsg}");
            });
            oSer1.DataWritten += ((s, e) =>
            {
                LogMan.Message(sC, sM, $"{oSer1.Name} : Data Written");
            });
            oSer2.DataWritten += ((s, e) =>
            {
                LogMan.Message(sC, sM, $"{oSer2.Name} : Data Written");
            });


            oSer1.WriteData("S1 Ciao!");
            oSer2.WriteData("S2 Ciao!");
            oSer1.ReadData();
            oSer2.ReadData();

            Task.Run(() =>
            {
                for (int i = 1; i <= 100; i++)
                {
                    oSer1.EnqueueWriteData($"{oSer1.Name} : {i:f0} : Ciao!");
                }
            });

            Task.Run(() =>
            {
                for (int i = 1; i <= 100; i++)
                {
                    oSer2.EnqueueWriteData($"{oSer2.Name} : {i:f0} : Ciao!");
                    Thread.Sleep(200);
                }
            });
        }

        public static void ThreadTest()
        {
            string sM = nameof(ThreadTest); 
            var oTH = new ThreadBase()
            {
                Name = "Test Thread",
                TimerEnabled = true,
            };
            oTH.OnThreadWakeupAsync += (async (s, e) => 
            { 
                LogMan.Message(sC, sM, "Timeout Start");
                await Task.Delay(2000);
                LogMan.Message(sC, sM, "Timeout End"); 
            });
            oTH.OnThreadTriggerAsync += (s, e) => { LogMan.Message(sC, sM, "Signal"); return Task.CompletedTask; };
            oTH.Create();

            Task.Run(async ()=>
            {
                await Task.Delay(5000);
                oTH.TimerStop();
                await Task.Delay(5000);
                for (int i = 0; i < 5; i++)
                {
                    oTH.ThreadSignal();
                    await Task.Delay(500);
                }
                await Task.Delay(5000);
                oTH.TimerStart();
                await Task.Delay(5000);
                oTH.TimerStop();
                oTH.WakeupTimeMs = 10;
                oTH.AllowEventOverlap = true;
                oTH.TimerStart();
                await Task.Delay(5000);
                oTH.AllowEventOverlap = false;
                await Task.Delay(5000);
                oTH.Destroy();
            });
        }

        static void ProducerConsumerTest(int iQueueSize, int iMaxParallelism )
        {
            ProducerConsumerTester oTester = new()
            {
                MaxConsumerParallelism = iMaxParallelism,
                MaxProductionQueueSize = iQueueSize
            };
            Task.Run(() =>
            {
                oTester.Create();
                for( int i = 0; i < 100; i++ )
                {
                    oTester.Produce();
                }
            });
        }

        static void ConnectionTest( int iObjects, int iLoop)
        {
            List<IConnectable> oList = new List<IConnectable>();
            for (int i = 0; i < iObjects; i++)
            {
                oList.Add(new ConnectableTester()
                {
                    Name = $"Test {i + 1}",
                    ConnectionName = $"ConnTest {i + 1}",
                    ConnectionTimeoutMs = 1000,
                    PollingReadMs = 1000,
                    PollingWriteMs = 1000,
                    ReadTimeoutMs = 1000,
                    EnableReader = false,
                    EnableWriter = false,
                    SleepRandomMaxMs = 10000,
                    SleepMs = 2000,
                });
            }

            //foreach ( var o in oList) 
            Task.Run(() => Parallel.ForEach(oList, async o =>
            {
                await o.CreateAsync();
                await o.ConnectAsync();
            }));

            Task.Run(() => Parallel.ForEach(oList, async o =>
            {
                await o.DestroyAsync();
            }));

            Task.Run(() =>
            {

                for (int i = 0; i < iLoop; i++)
                {
                    //foreach (var o in oList)
                    Parallel.ForEach(oList, async o =>
                    {
                        await o.ReadDataAsync();
                    });
                    Parallel.ForEach(oList, async o =>
                    {
                        await o.WriteDataAsync($"{o.Name} : {i + 1}");
                    });
                }
            });
        }

        static void SiemensTest(string sPlcIP, int iRack, int iSlot)
        {
            S7DataHandler oPlc = new S7DataHandler()
            {
                Name = "Siemens Plc",
                ConnectionName = "Test",
                ConnectionString = $"{sPlcIP}\\{iRack}\\{iSlot}",
                EnableReader = true,
                PollingReadMs = 1000, 
                EnableWriter = false,                 
            };
            oPlc.ReadDataListTemplate.Add(S7PlcDataItem.Create(S7PlcArea.DB, 50, 0, 40));
            oPlc.ReadDataListTemplate.Add(S7PlcDataItem.Create(S7PlcArea.DB, 50, 50, 10));
            oPlc.ReadDataListTemplate.Add(S7PlcDataItem.Create(S7PlcArea.DB, 50, 60, 100));
            oPlc.Create();
            //oPlc.Connect();
        }


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            string sM = nameof(Program);
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            LogMan.MinLogLevel = LogLevel.Message;
            LogMan.CreateAndRegisterDefaultLoggers(true, true, false);
            LogMan.Create();

            //            ConnectionTest(1, 1000);
            // SerialTest();

            // SiemensTest("127.0.0.1", 0, 0);

            ThreadTest();
            //var oPlcItem = DSG.Drivers.Siemens.S7PlcDataItem.Create(DSG.Drivers.Siemens.S7PlcArea.DB, 50, 30, 100);
            //for (int i = 0; i < 100; i++)
            //    oPlcItem.Data[i] = (byte)(i + 1);

            //var s7 = DSG.Drivers.Siemens.S7DataConversion.ToS7DataItem(oPlcItem);
            //GC.Collect();
            //GC.Collect();

            //var oItemBack = DSG.Drivers.Siemens.S7DataConversion.ToPlcDataItem(s7.Value);

            ProducerConsumerTest(50,5);

            Application.Run(new Form1());
            LogMan.Destroy();
        }
    }
}