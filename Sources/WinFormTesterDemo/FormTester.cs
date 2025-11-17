using DSG.Drivers.Siemens;
using DSG.IO;
using DSG.Log;
using DSG_Streaming;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WinFormTesterDemo
{
    public partial class FormTester : Form
    {
        static readonly string sC = nameof(Form);
        public FormTester()
        {
            InitializeComponent();
            LogMan.OnLogMessage += LogMan_OnLogMessage;
           // MyExtensions.SetDoubleBuffered(lbLog);
          //  MyExtensions.SetDoubleBuffered(lbPlc);
        }

        ConcurrentQueue<string> oQueueMessage = new();

        private void LogMan_OnLogMessage(object? sender, LogEventArgs e)
        {
            oQueueMessage.Enqueue(e.FormattedMessage);
            while (oQueueMessage.Count > 100)
            {
                oQueueMessage.TryDequeue(out var s);
            }
        }


        private void oTimerLog_Tick(object sender, EventArgs e)
        {
            if (DesignMode)
            {
                return;
            }
            SuspendLayout();
            var oList = oQueueMessage.ToList();
            oList.Reverse();
            lbLog.Items.Clear();
            lbLog.Items.AddRange(oList.ToArray());
            ResumeLayout();
        }

        #region SerialPort Test

        DSG.Drivers.SerialPort.SerialHandler? oSerialA;
        DSG.Drivers.SerialPort.SerialHandler? oSerialB;
        CancellationTokenSource? oSerialCTS;

        async Task SerialTestStartAsync(CancellationToken oToken)
        {
            string sM = nameof(SerialTestStartAsync);
            oSerialA = new()
            {
                Name = "Ser A Test",
                ConnectionString = textBox1.Text,
                EnableReader = true,
                PollingReadMs = 100,
                ReadTimeoutMs = 20000,
                WriteTimeoutMs = 2000,
                TextNewLine = "\r\n",
                DataMode = DSG.Base.StreamMode.Text,
            };
            oSerialB = new()
            {
                Name = "Ser B Test",
                ConnectionString = textBox2.Text,
                EnableReader = true,
                PollingReadMs = 100,
                ReadTimeoutMs = 20000,
                WriteTimeoutMs = 2000,
                TextNewLine = "\r\n",
                DataMode = DSG.Base.StreamMode.Text,
            };

            pgSer1.SelectedObject = oSerialA;
            pgSer2.SelectedObject = oSerialB;

            oSerialA.DataReaded += (s, e) =>
                LogMan.Message(sC, sM, $"{oSerialA.Name} : {e.Timestamp: HH:mm:ss.fff} : Received {e.ResultList.FirstOrDefault()?.Tag?.ToString()}");
            oSerialA.DataWritten += (s, e) =>
                LogMan.Message(sC, sM, $"{oSerialA.Name} : {e.Timestamp: HH:mm:ss.fff} : Written {e.ResultList.FirstOrDefault()?.Tag?.ToString()}");
            oSerialB.DataReaded += (s, e) =>
                LogMan.Message(sC, sM, $"{oSerialB.Name} : {e.Timestamp: HH:mm:ss.fff} : Received {e.ResultList.FirstOrDefault()?.Tag?.ToString()}");
            oSerialB.DataWritten += (s, e) =>
                LogMan.Message(sC, sM, $"{oSerialB.Name} : {e.Timestamp: HH:mm:ss.fff} : Written {e.ResultList.FirstOrDefault()?.Tag?.ToString()}");

            string sMessA = $"{oSerialA.ConnectionName} {textBox3.Text}";
            string sMessB = $"{oSerialB.ConnectionName} {textBox3.Text}";
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(async () =>
            {
                int i = 0;
                await oSerialA.CreateAsync();
                oSerialA.Connect();
                while (i < 100)
                {
                    oToken.ThrowIfCancellationRequested();
                    await oSerialA.WriteDataAsync($"{sMessA} {++i}");
                    await Task.Delay(100);
                }
            }));

            tasks.Add(Task.Run(async () =>
            {
                int i = 0;
                await oSerialB.CreateAsync();
                oSerialB.Connect();
                while (i < 100)
                {
                    oToken.ThrowIfCancellationRequested();
                    await oSerialB.WriteDataAsync($"{sMessB} {++i}");
                    await Task.Delay(100);
                }

            }));
        }
        async Task SerialTestEndAsync()
        {
            oSerialCTS?.Cancel();
            oSerialCTS = null;
            if (oSerialA != null)
                await oSerialA.DestroyAsync();
            if (oSerialB != null)
                await oSerialB.DestroyAsync();
            oSerialA = null;
            oSerialB = null;
            pgSer1.SelectedObject = null;
            pgSer2.SelectedObject = null;
        }


        private async void btnSerStart_Click(object sender, EventArgs e)
        {
            await SerialTestEndAsync();
            oSerialCTS = new CancellationTokenSource();
            await SerialTestStartAsync(oSerialCTS.Token);
        }

        private async void btnSerStop_Click(object sender, EventArgs e)
        {
            await SerialTestEndAsync();
        }

        #endregion

        #region PLC Test

        DSG.Drivers.Siemens.S7DataHandler2? oS7Handler;
        DSG.Drivers.Siemens.S7PlcDataItem? oS7DbItem;

        public async Task PlcTestAsync()
        {
            string sM = nameof(PlcTestAsync);
            try
            {
                if( oS7Handler != null )
                    await oS7Handler.DestroyAsync();
                oS7Handler = new();
                oS7Handler.ConnectionString = tbS7Conn.Text;
                var oDB = tbS7DB.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (oDB.Length != 3)
                {
                    LogMan.Error(sC, sM, "Invalid DB data");
                    return;
                }
                oS7DbItem = new DSG.Drivers.Siemens.S7PlcDataItem()
                {
                    Area = DSG.Drivers.Siemens.S7PlcArea.DB,
                    DbNum = int.Parse(oDB[0]),
                    Offset = int.Parse(oDB[1]),
                    Length = int.Parse(oDB[2])
                };
                oS7Handler.ReadDataListTemplate.Add(oS7DbItem);
                oS7Handler.EnableReader = true;
                oS7Handler.PollingReadMs = 500;
                pgPLC.SelectedObject = oS7Handler;
                oS7Handler.DataReaded += ((s, oArgs) =>
                {
                    Invoke(() =>
                    {
                        SuspendLayout();
                        try
                        {
                            lbPlc.Items.Clear();
                            if (oArgs.ResultList.FirstOrDefault()?.Tag is List<S7PlcDataItem> dataList)
                            {
                                foreach (var item in dataList)
                                {
                                    var sb = new StringBuilder(4000);
                                    foreach (var buff in item.Data)
                                    {
                                        if (buff < 16)
                                            sb.Append($"{buff},");
                                        else
                                            sb.Append($"{(char)buff},");
                                        if (sb.Length > 32)
                                        {
                                            lbPlc.Items.Add(sb.ToString());
                                            sb.Clear();
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            ResumeLayout();
                        }
                    });
                });
                if ((await oS7Handler.CreateAsync()).HasError)
                {
                    return;
                }
                if ((await oS7Handler.ConnectAsync()).HasError)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
            }
        }

        #endregion

        private async void btnPlcStart_Click(object sender, EventArgs e)
        {
            await PlcTestAsync();
            pgPLC.SelectedObject = oS7Handler;
        }

        

        private async void btnPlcStop_Click(object sender, EventArgs e)
        {
            await oS7Handler?.DestroyAsync();
        }
    }

    public static class MyExtensions
    {

        public static void SetDoubleBuffered(this Control panel)
        {
            typeof(Panel).InvokeMember(
               "DoubleBuffered",
               BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
               null,
               panel,
               new object[] { true });
        }
    }
}
