using System.Data.Common;
using DSG.Drivers.SerialPort;
using DSG.Imaging;
using DSG.IO;
using DSG.Log;


namespace WinFormTesterDemo
{
    internal static class Program
    {
        static string sC = nameof(Program);

        static void SerialTest()
        {
            string sM = nameof(SerialTest);

            using (SerialPort oSer1 = new DSG.Drivers.SerialPort.SerialPort()
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
            })
            using (SerialPort oSer2 = new DSG.Drivers.SerialPort.SerialPort()
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
            })
            {
                oSer1.OnRead += ((s, e) =>
                {
                    LogMan.Message(sC, sM, $"{oSer1.Name} : Data Readed");
                    var oObj = e.ResultList.FirstOrDefault(X => X.Tag != null);
                    string sMsg = "Boh!";
                    if (oObj?.Tag is DataBuffer oB) sMsg = oB.ToStringAscii();
                    if (oObj?.Tag is String oS) sMsg = oS;
                    LogMan.Message(sC, sM, $"{oSer1.Name} : Readed : {sMsg}");
                });
                oSer2.OnRead += ((s, e) =>
                {
                    LogMan.Message(sC, sM, $"{oSer2.Name} : Data Readed");
                    var oObj = e.ResultList.FirstOrDefault(X => X.Tag != null);
                    string sMsg = "Boh!";
                    if (oObj?.Tag is DataBuffer oB) sMsg = oB.ToStringAscii();
                    if (oObj?.Tag is String oS) sMsg = oS;
                    LogMan.Message(sC, sM, $"{oSer2.Name} : Readed : {sMsg}");
                });
                oSer1.OnWrite += ((s, e) =>
                {
                    LogMan.Message(sC, sM, $"{oSer1.Name} : Data Written");
                });
                oSer2.OnWrite += ((s, e) =>
                {
                    LogMan.Message(sC, sM, $"{oSer2.Name} : Data Written");
                });


                oSer1.WriteData("S1 Ciao!");
                oSer2.WriteData("S2 Ciao!");

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
                });
            }

            //foreach ( var o in oList) 
            Parallel.ForEach(oList, async o =>
            {
                await o.CreateAsync();
                await o.ConnectAsync();
            });

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

            ConnectionTest(5, 5);

            Application.Run(new Form1());
            LogMan.Destroy();
        }
    }
}