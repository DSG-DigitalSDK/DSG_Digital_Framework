using DSG.Drivers.Siemens;
using DSG.IO;
using DSG.Log;
using DSG_Streaming;
using System.Collections.Concurrent;
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

        private async void btnPlcTest_Click(object sender, EventArgs e)
        {
            lbPlc.Items.Clear();
            DSG.Drivers.Siemens.S7DataHandler oPLC = new ();
            oPLC.ConnectionString = "192.168.17.37,0,0";
            var item = new DSG.Drivers.Siemens.S7PlcDataItem()
            {
                 Area = DSG.Drivers.Siemens.S7PlcArea.DB,
                 DbNum  = 0,
                 Length = 1,
                 Offset = 0,
            };
            oPLC.ReadDataListTemplate.Add(item);
            oPLC.EnableReader = false;
            pgPLC.SelectedObject = oPLC;
            var res1 = await oPLC.CreateAsync();
            if (res1.HasError)
            {
                return;
            }
            await Task.Run(async () =>
            {
                for (int i = 0; i <= 50; i++)
                {
                    for (int l = 1; l <= 100; l++)
                    {
                        item.DbNum = i;
                        item.Length = l;
                        var res2 = await oPLC.ReadDataAsync();
                        if (res2.Valid)
                        {
                            Invoke(() =>
                            {
                                if (res2.Tag is List<S7PlcDataItem> dataList)
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
                                        }
                                        lbPlc.Items.Add(sb.ToString());
                                    }
                                }
                            });
                        }
                    }
                }
            });
        }
    }
}
