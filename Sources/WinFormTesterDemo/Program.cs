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
                UsePollingReader = true,
            })
            using (SerialPort oSer2 = new DSG.Drivers.SerialPort.SerialPort()
            {
                Name = "Serial2",
                ConnectionName = "SerialTest",
                ConnectionString = "COM2/57600/ODD/7/1/NONE",
                StreamMode = DSG.Base.StreamMode.Text,
                UsePollingReader = false
            })
            {
                oSer1.OnRead += ((s, e) =>
                    {
                        string sMsg1 = (e.Result.Tag as DataBuffer)?.ToStringAscii();
                        string sMsg2 = (e.Result.Tag?.ToString()) ?? "BOH!";
                        LogMan.Message(className, sMethod, sMsg1 ?? sMsg2);
                    });
                oSer1.Connect();
                oSer2.Connect();
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        oSer2.WriteData($"{i:f0} : Ciao!");
                        Thread.Sleep(1500);
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