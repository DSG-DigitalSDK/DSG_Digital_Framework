using System.Data.Common;
using DSG.Log;

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

            LogMan.CreateAndRegisterDefaultLoggers(true, true, false);
            LogMan.Create();
            LogMan.Test();
            Application.Run(new Form1());
            LogMan.Destroy();
        }
    }
}