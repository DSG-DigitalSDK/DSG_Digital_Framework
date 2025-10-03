using System.Data.Common;
using DSG.Drivers.SerialPort;
using DSG.Imaging;
using DSG.Log;
using DSG_Shared.Base;

namespace WinFormTesterDemo
{
    internal static class Program
    {
        static string className = nameof(Program);
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string sMethod = nameof(Program);
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            LogMan.MinLogLevel = LogLevel.Message;
            LogMan.CreateAndRegisterDefaultLoggers(true, true, false);
            LogMan.Create();

            using (SerialPort oSer1 = new DSG.Drivers.SerialPort.SerialPort()
            {
                Name = "Serial1",
                ConnectionName = "SerialTest",
                ConnectionString = "COM1/57600/ODD/7/1/NONE",
                StreamMode = DSG.Base.StreamMode.Text,
                PollingReadMs = 1000,
                PollingWriteMs= 500,
                UsePollingReader = true,
                UsePollingWriter = false,
            })
            using (SerialPort oSer2 = new DSG.Drivers.SerialPort.SerialPort()
            {
                Name = "Serial2",
                ConnectionName = "SerialTest",
                ConnectionString = "COM2/57600/ODD/7/1/NONE",
                StreamMode = DSG.Base.StreamMode.Text,
                PollingReadMs = 1000,
                PollingWriteMs = 0,
                UsePollingReader = false,
                UsePollingWriter = true,
            })
            {
                oSer1.OnRead += ((s, e) =>
                    {
                        string sMsg1 = (e.Result.Tag as DataBuffer)?.ToStringAscii();
                        string sMsg2 = (e.Result.Tag?.ToString()) ?? "BOH!";
                        LogMan.Message(className, sMethod, $"Readed : {sMsg1 ?? sMsg2}");
                    });
                oSer2.OnWrite += ((s, e) => LogMan.Message(className,sMethod, $"Written : { e.Result.Tag?.ToString()}"));
                for (int i = 0; i < 200; i++)
                {
                    oSer2.EnqueueWriteData($"{i:f0} : Ciao!");
                }
                oSer2.Connect();
                Thread.Sleep(2500);
                oSer1.Connect();
                Task.Run(() =>
                {
                    for (int i = 10; i < 100; i++)
                    {
                        oSer2.EnqueueWriteData($"{i:f0} : Ciao!");
                        Thread.Sleep(250);
                    }
                });

                // var oRead = oPort1.ReadData();

                //            LogMan.Test();

                var oBmp = BitmapUtility.Create(@"C:\Temp\frame_10004_cropped.bmp");
                BitmapUtility.Save(oBmp, @"C:\Temp\000", ImageSaveFormat.jpg, 50);

                Application.Run(new Form1());
                LogMan.Destroy();
            }
        }
    }
}