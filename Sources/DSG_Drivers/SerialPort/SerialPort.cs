using DSG.Base;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using DSG.Log;
using System.ComponentModel.DataAnnotations;
using DSG.IO;


namespace DSG.Drivers.SerialPort
{
    public class SerialPort : ConnectableBasePolling
    {
        static readonly string sClassName = nameof(Drivers.SerialPort);

        System.IO.Ports.SerialPort oSerialPort = null;
        public System.IO.Ports.SerialPort SerialPortNative => oSerialPort;

        public event EventHandler<SerialDataReceivedEventArgs> OnDataReceived;

        public int ReadBufferSize
        {
            get => (int)GetDictionaryParam(nameof(Params.ConnectionReadBufferSize), 0);
            set => SetDictionaryParam(nameof(Params.ConnectionReadBufferSize), value);
        }
        public int WriteBufferSize
        {
            get => (int)GetDictionaryParam(nameof(Params.ConnectionWriteBufferSize), 0);
            set => SetDictionaryParam(nameof(Params.ConnectionWriteBufferSize), value);
        }

        public string TextNewLine
        {
            get => (string)GetDictionaryParam(nameof(Params.TextNewLine), "\r\n");
            set => SetDictionaryParam(nameof(Params.TextNewLine), value);
        }

        public System.Text.Encoding Encoding 
        {
            get => (System.Text.Encoding)GetDictionaryParam(nameof(Params.TextEncoding), System.Text.Encoding.ASCII);
            set => SetDictionaryParam(nameof(Params.TextEncoding), value);
        }

        public new bool Connected => oSerialPort?.IsOpen ?? false;

        static string sConnectionInfo = "COM(COM1-COM8)/Baudrate(9600-57600)/Parity(ODD-EVEN-NONE)/DataBit(7-8)/StopBit(0-1-1.5-2)/Handshake(NONE-RTS-XON)";
        public SerialPort()
        {
            ConnectionString = sConnectionInfo;
        }

        protected override Result CreateImpl()
        {
            string sMethod = nameof(CreateImpl);

            base.CreateImpl();

            var strArr = ConnectionString.Split("/", StringSplitOptions.TrimEntries).ToList();
            if (strArr.Count != 6)
            {
                LogMan.Error(sClassName, sMethod, $"'{Name}/{ConnectionName}' : Connection string error : '{ConnectionString}'");
                return Result.CreateResultError(OperationResult.ErrorResource, "Connection String Error", 0);
            }
            string sParity = strArr[2].ToUpper();
            string sStop = strArr[4].ToUpper();
            string sHand = strArr[5].ToUpper();
            var sPortName = strArr[0];
            int iBaudRate = Convert.ToInt32(strArr[1]);
            Parity eParity = Parity.None;
            switch (sParity)
            {
                case "ODD":
                    eParity = Parity.Odd;
                    break;
                case "EVEN":
                    eParity = Parity.Even;
                    break;
                case "NONE":
                    eParity = Parity.None;
                    break;
                case "MARK":
                    eParity = Parity.Mark;
                    break;
                default:
                    throw new ArgumentException("Use  'ODD', 'EVEN', 'NONE', 'MARK' ", "Parity");
            }
            int iDataBits = Convert.ToInt32(strArr[3]);
            StopBits eStopBits = StopBits.None;
            switch (sStop)
            {
                case "NONE":
                    eStopBits =  StopBits.None;
                    break;
                case "1":
                    eStopBits = StopBits.One;
                    break;
                case "1.5":
                    eStopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    eStopBits = StopBits.OnePointFive;
                    break;
                default:
                    throw new ArgumentException("Use  '1', '1.5', '2', 'NONE' ", "StopBits");
            }
            Handshake eHandshake = Handshake.None;  
            switch (sHand)
            {
                case "NONE":
                    eHandshake =  Handshake.None;
                    break;
                case "RTS":
                    eHandshake = Handshake.RequestToSend;
                    break;
                case "XON":
                    eHandshake = Handshake.XOnXOff;
                    break;
                default:
                    throw new ArgumentException("Use  'NONE', 'RTS', 'XON'", "HandShake");
            }
            oSerialPort = new System.IO.Ports.SerialPort(sPortName, iBaudRate, eParity, iDataBits, eStopBits);
            oSerialPort.Handshake = eHandshake;
            if (ReadTimeoutMs > 0)
                oSerialPort.ReadTimeout = ReadTimeoutMs;
            if (WriteTimeoutMs > 0)
                oSerialPort.WriteTimeout = WriteTimeoutMs;
            if (ReadBufferSize > 0 )
                oSerialPort.ReadBufferSize = ReadBufferSize;
            if (WriteBufferSize > 0)
                oSerialPort.WriteBufferSize = WriteBufferSize;
            oSerialPort.NewLine = TextNewLine;
            oSerialPort.Encoding = Encoding;
            oSerialPort.DataReceived += OSerialPort_DataReceived;
            return Result.CreateResultSuccess();
        }

        private void OSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);    
        }

        protected override Result DestroyImpl()
        {
            base.DestroyImpl();
            if (oSerialPort != null)
            {
                oSerialPort.Close();
                oSerialPort.Dispose();
            }
            oSerialPort = null;
            return Result.CreateResultSuccess();
        }

        protected override Result ConnectImpl()
        {
            var res = base.ConnectImpl();
            if (res.Valid)
            {
                oSerialPort.Open();
            }
            return res;
        }

        protected override Result DisconnectImpl()
        {
            var res = base.DisconnectImpl();
            if (res.Valid)
            {
                oSerialPort.Close();
            }
            return res;
        }

        protected override Result ReadDataImpl()
        {
            try
            {
                switch (StreamMode)
                {
                    case StreamMode.Text:
                        {
                            var sMessage = oSerialPort.ReadLine();
                            return Result.CreateResultSuccess(sMessage);
                        }
                    case StreamMode.Binary:
                        {
                            int iBytesToRead = oSerialPort.BytesToRead;
                            if (iBytesToRead == 0)
                            {
                                Thread.Sleep(PollingReadMs);
                            }
                            iBytesToRead = oSerialPort.BytesToRead;
                            if (iBytesToRead > 0)
                            {
                                DataBuffer oBuffer = new DataBuffer(iBytesToRead);
                                oSerialPort.Read(oBuffer.Data, 0, iBytesToRead);
                                return Result.CreateResultSuccess(oBuffer);
                            }
                            else
                            {
                                return Result.CreateResultError( OperationResult.ErrorTimeout, "Timeout occours",0);
                            }
                        }
                    default:
                        {
                            return Result.CreateResultError( OperationResult.Error, $"{nameof(StreamMode)} not supported : {StreamMode}",0);
                        }
                }
            }
            catch( TimeoutException exT) 
            { 
                return Result.CreateResultError(OperationResult.ErrorTimeout,exT.Message,0); 
            }
        }

        protected override Result WriteDataImpl(DataBuffer oBuffer)
        {
            oSerialPort.Write(oBuffer.Data, oBuffer.DataStartOffset, oBuffer.DataLenght);
            return Result.CreateResultSuccess(oBuffer);
        }
        protected override Result WriteDataImpl(string sMessage)
        {
            if (!sMessage.EndsWith(TextNewLine))
            {
                oSerialPort.Write(sMessage + TextNewLine);
            }
            else
            {
                oSerialPort.Write(sMessage);
            }
            return Result.CreateResultSuccess(sMessage);  
        }
    }
}
