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

            LogMan.Pass(sC, sM, "App started");

            Application.Run(new FormTester());

            LogMan.Pass(sC, sM, "App end");
            LogMan.Destroy();
        }
    }
}