using DSG.Log;
using DSG_Streaming;
using System.Collections.Concurrent;
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
                oQueueMessage.TryDequeue( out var s);
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
            lbLog.Items.AddRange( oList.ToArray() );
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

            propertyGrid1.SelectedObject = oSerialA;
            propertyGrid2.SelectedObject = oSerialB;

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
            tasks.Add( Task.Run(async () =>
            {
                int i = 0;
                await oSerialA.CreateAsync();
                oSerialA.Connect();
                while (i <= 0)
                {
                    oToken.ThrowIfCancellationRequested();
                    await oSerialA.WriteDataAsync($"{sMessA} {++i}");
                    await Task.Delay(10000);
                }
            }));
          
            tasks.Add(Task.Run(async () =>
            {
                int i = 0;
                await oSerialB.CreateAsync();
                oSerialB.Connect();
                while (i <= 0)
                {
                    oToken.ThrowIfCancellationRequested();
                    await oSerialB.WriteDataAsync($"{sMessB} {++i}");
                    await Task.Delay(10000);
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
            propertyGrid1.SelectedObject = null;
            propertyGrid2.SelectedObject = null;
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

    }
}
