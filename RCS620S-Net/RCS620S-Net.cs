using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace RCS620S_Net
{
    public class RCS620S : IDisposable
    {
        public string IDm { get; set; }
        public string PMm { get; set; }

        public GeneralStatus GeneralStatus { get; set; }
      
        private Receiver Receiver;
        private Sender Sender;

        public RCS620S(SerialPort serialPort, int timeOutMilliSec = 1000)
        {
            if (!serialPort.IsOpen)
            {
                try
                {
                    serialPort.Open();
                }
                catch(Exception e)
                {
                    throw e;
                }
            }
            Receiver = new Receiver(serialPort, timeOutMilliSec);
            Sender = new Sender(serialPort);
            GeneralStatus = new GeneralStatus();
        }

        public void Dispose()
        {
            Receiver.Dispose();
        }

        #region コマンド送信

        private int Cancel()
        {
            // ホストコマンドの中断時はACKを送信する。
            return Sender.WriteSerial(Define.ACK_DATA);
        }

        public int Initialize()
        {
            if (RFConfiguration(RFConfigurationItem.VariousTimings) != Define.OK)
                return Define.ERROR;
            if (RFConfiguration(RFConfigurationItem.MaxRetries) != Define.OK)
                return Define.ERROR;
            if (RFConfiguration(RFConfigurationItem.WaitTime) != Define.OK)
                return Define.ERROR;
            return Define.OK;
        }


        public int GetFirmWareVersion()
        {
            var command = SubCommand.GetFirmwareVersion;
            Sender.Send(command, null);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    if (Receiver.ResponseData[1] != (byte)SubResponse.GetFirmwareVersion)
                    {
                        return Define.ERROR;
                    }
                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }

        public int GetGeneralStatus()
        {
            var command = SubCommand.GetGeneralStatus;
            Sender.Send(command, null);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    if (Receiver.ResponseData[1] != (byte)SubResponse.GetGeneralStatus)
                    {
                        return Define.ERROR;
                    }
                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }


        public int SetParameters(bool AutomaticATR_RES = true)
        {
            var command = SubCommand.SetParameters;
            // パラメータ設定
            var parameter = new byte[1];
            parameter[0] = (byte)(AutomaticATR_RES ? 0x1c : 0x14);
            Sender.Send(command, parameter);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    if (Receiver.ResponseData[1] != (byte)SubResponse.SetParameters)
                    {
                        return Define.ERROR;
                    }
                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }



        //public int PowerDown()
        //{
        //    // TODO パワーダウンすると、応答を返さずACKだけで終了し、再開時に応答が帰る。同期でロックしてしまってよいか、仕様を検討する。
        //}


        public int RFConfiguration_RFField(bool isOn)
        {
            var command = SubCommand.RFConfiguration;
            // パラメータ設定
            var parameter = new byte[2];
            parameter[0] = (byte)RFConfigurationItem.RFField;
            parameter[1] = (byte)(isOn ? 0x2 : 0x1);
            Sender.Send(command, parameter);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    if (Receiver.ResponseData[1] != (byte)SubResponse.RFConfiguration)
                    {
                        return Define.ERROR;
                    }
                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }


        public int RFConfiguration(RFConfigurationItem item)
        {
            var command = SubCommand.RFConfiguration;
            // パラメータ設定
            var parameter = CreateRFConfigurationParameter(item);
            Sender.Send(command, parameter);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    if (Receiver.ResponseData[1] != (byte)SubResponse.RFConfiguration)
                    {
                        return Define.ERROR;
                    }
                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }


        private byte[] CreateRFConfigurationParameter(RFConfigurationItem item)
        {
            byte[] ret = null;
            switch (item)
            {
                // RFFieldはパラメータ可変するため放置
                case RFConfigurationItem.VariousTimings:
                    ret = new byte[4];
                    ret[0] = (byte)item;
                    break;
                case RFConfigurationItem.MaxRetries:
                    ret = new byte[4];
                    ret[0] = (byte)item;
                    break;
                case RFConfigurationItem.WaitTime:
                    ret = new byte[2];
                    ret[0] = (byte)item;
                    ret[1] = 0xb7; // 24ms
                    break;
                case RFConfigurationItem.DEPTimeout:
                    ret = new byte[4];
                    ret[0] = (byte)item;
                    ret[1] = 0x0e; // とりあえずデフォルト値。
                    ret[2] = 0x07;
                    ret[3] = 0x0e;
                    break;
                default:
                    break;
            }
            return ret;
        }
        

        //// Resetコマンドは省略中

        public int ReadFelica()
        {
            var command = SubCommand.InListPassiveTarget;
            // パラメータ設定
            var parameter = new byte[7];
            parameter[0] = (byte)0x01;
            parameter[1] = (byte)0x01;
            parameter[2] = (byte)0x00;
            parameter[3] = (byte)0xff;
            parameter[4] = (byte)0xff;
            parameter[5] = (byte)0x00;
            parameter[6] = (byte)0x00;
            Sender.Send(command, parameter);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    if (Receiver.ResponseData[1] != (byte)SubResponse.InListPassiveTarget ||
                        Receiver.ResponseData[2] != 0x01 ||
                        Receiver.ResponseData[3] != 0x01 ||
                        Receiver.ResponseData[4] != 0x12 ||
                        Receiver.ResponseData[5] != 0x01 )
                    {
                        return Define.ERROR;
                    }
                    var text = "";
                    foreach (var elm in Receiver.ResponseData)
                    {
                        text += elm.ToString("x2") + " ";
                    }

                    //Console.WriteLine($"ReadFelica res:{text}");


                    var idm = Receiver.ResponseData.Skip(6).Take(8);
                    var pmm = Receiver.ResponseData.Skip(14).Take(8);
                    this.IDm = string.Empty;
                    this.PMm = string.Empty;
                    foreach (var elm in idm)
                    {
                        this.IDm += elm.ToString("x2");
                    }
                    foreach (var elm in pmm)
                    {
                        this.PMm += elm.ToString("x2");
                    }

                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }
        /// <summary>
        /// MIFARE TypeA読み出し
        /// </summary>
        /// <returns>UID</returns>
        public int ReadMIFARE()
        {
            Receiver.ResponseData?.Initialize();
            var command = SubCommand.InListPassiveTarget;
            // パラメータ設定
            var parameter = new byte[2];
            parameter[0] = (byte)0x01;
            parameter[1] = (byte)0x00;
            Sender.Send(command, parameter);
            try
            {
                if (Receiver.Receive() == Define.OK)
                {
                    var text = "";
                    foreach(var elm in Receiver.ResponseData)
                    {
                        text += elm.ToString("x2") + " ";
                    }
                
                    //Console.WriteLine($"ReadMIFARE res:{text}");
                    if (Receiver.ResponseData[1] != (byte)SubResponse.InListPassiveTarget ||
                        Receiver.ResponseData[2] != 0x01 ||
                        Receiver.ResponseData[3] != 0x01 ||
                        Receiver.ResponseData[4] != 0x00)
                    {
                        return Define.ERROR;
                    }
                    var idmLength = Receiver.ResponseData[7];
                    var idm = Receiver.ResponseData.Skip(8).Take(idmLength);
                    this.IDm = string.Empty;
                    this.PMm = string.Empty;
                    foreach (var elm in idm)
                    {
                        this.IDm += elm.ToString("x2");
                    }

                    return Define.OK;
                }
            }
            catch(Exception e)
            {
                Cancel();
                throw e;
            }
            return Define.ERROR;
        }
        #endregion
    }
}
