using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace RCS620S_Net
{
    /// <summary>
    /// メッセージ受信クラス
    /// </summary>
    class Receiver : IDisposable
    {
        enum Status
        {
            Undefine = 0,
            WaitAck,
            HeaderReading,
            BodyReading,
            CheckSumReading,
            Done,
        }
        private const int MAX_LENGTH = 512;
        /// <summary>
        /// 受信バッファ
        /// </summary>
        private byte[] receivedBuffer = new byte[MAX_LENGTH];
        private Status State { get; set; }
        private SerialPort SerialPort { get; set; }
        private System.Timers.Timer receiveTimeOut;
        /// <summary>
        /// 受信済バイト数
        /// </summary>
        private int ReceivedLength { get; set; }
        /// <summary>
        /// 受信予定のデータ部バイト数
        /// </summary>
        private int ExpectedBodyLength { get; set; }
        public byte[] ResponseData { get; private set; }

        public Receiver(SerialPort serialPort, int timeOutMilliSec)
        {
            SerialPort = serialPort;
            receiveTimeOut = new System.Timers.Timer();
            receiveTimeOut.Interval = (double)timeOutMilliSec;
            // TODO タイムアウト処理が未完成。
        }

        public void Dispose()
        {
            if (SerialPort != null)
            {
                try
                {
                    if (!SerialPort.IsOpen)
                    {
                        SerialPort.Close();
                    }
                }
                finally
                {
                    SerialPort.Dispose();
                }
            }
        }

        /// <summary>
        /// 受信動作エントリーポイント
        /// 受信完了するまで同期処理でブロックするため、UIから呼び出す場合は別スレッドコンテキストで動作させること。
        /// </summary>
        /// <returns>受信結果</returns>
        public int Receive()
        {
            // TODO 受信タイムアウト監視を開始する。
            Clear();
            // ACKを待つ
            State = Status.WaitAck;
            var ackResult = ReadAck();
            if(ackResult != Define.OK)
            {
                //Console.WriteLine("ACK error.");
                throw new Exception("ACK error.");
            }
            State = Status.HeaderReading;
            var headerResult = ReadHeader();
            if (headerResult != Define.OK)
            {
                throw new Exception("Header error.");
            }

            State = Status.BodyReading;
            var commandResult = ReadCommand();
            if( commandResult !=  Define.OK )
            {
                throw new Exception("Command recieve error.");
                //return Define.ERROR;
            }
            State = Status.CheckSumReading;
            var dcsResult = ReadCheckSum();
            if (dcsResult != Define.OK)
            {
                throw new Exception("DCS error.");
                //return Define.ERROR;
            }
            // タイムアウト監視を止める。
            this.receiveTimeOut.Stop();
            Clear();
            return Define.OK;
        }

        private void Clear()
        {
            receivedBuffer.Initialize();
            ReceivedLength = 0;
        }


        private int ReadAck()
        {
            var ack = ReadSerial(Define.ACK_LENGTH);
            var ackReceived = ack.SequenceEqual(Define.ACK_DATA);
            if (ackReceived)
                return Define.OK;
            return Define.ERROR;
        }

        private int ReadHeader()
        {
            // NormalフレームかExtendフレームか判定する。
            var header = ReadSerial(5);
            if (header[3] == 0xff && header[4] == 0xff)
            {
                // Extendフレームなら拡張分を受信する。
                var extendHeader = ReadSerial(3);
                // LEN/LCSの和の下位1バイトが0になるか照合する。
                if (((extendHeader[0] + extendHeader[1] + extendHeader[2]) & 0xff) != 0)
                {
                    throw new Exception($"checksum error. extend");
                    //return Define.ERROR;
                }
                ExpectedBodyLength = (extendHeader[0] << 8) | (extendHeader[1] << 0);
            }
            else
            {
                // LEN/LCSの和の下位1バイトが0になるか照合する。
                if (((header[3] + header[4]) & 0xff) != 0)
                {
                    throw new Exception($"checksum error. normal");
                    ///return Define.ERROR;
                }
                ExpectedBodyLength = header[3];
            }
            if(ExpectedBodyLength > 255)
            {
                throw new Exception($"Data length error. len:{ExpectedBodyLength}");
            }
            return Define.OK;
        }

        private int ReadCommand()
        {
            // レスポンスデータ部を保存する。
            ResponseData = ReadSerial(ExpectedBodyLength);
            if (ResponseData == null || ResponseData.Length == 0)
            {
                return Define.ERROR;
            }
            return Define.OK;
        }

        private int ReadCheckSum()
        {
            // DCS、Postambleのチェックを行う。
            var dcs = ReadSerial(2);
            if(((dcs[0] + ResponseData.Sum(value => (int)value)) & 0xff) != 0)
            {
                throw new Exception($"DCS error.");
            }
            if (dcs[1] != 0x00)
            {
                throw new Exception($"Postamble error.");
            }
            return Define.OK;
        }
        

        private byte[] ReadSerial(int leastExpectedLength)
        {
            // monoではSerialPort.DataReceivedイベントがサポートされていないので、非同期処理はあきらめる。
            // 代わりに、自前のポーリング方式とする
            while (ReceivedLength < leastExpectedLength)
            {
                // 受信可能なバイト数を確認し、存在すれば読み出す。
                if (SerialPort.BytesToRead > 0)
                {
                    var len = SerialPort.Read(receivedBuffer, ReceivedLength, MAX_LENGTH - ReceivedLength);
                    ReceivedLength += len;
                }
                // monoでSerialPort.DataReceivedイベントが動作しないため、やむなくループで読み出している。そのためのSleepを入れている。
                System.Threading.Thread.Sleep(50);
            }
            // 必要バイト数分が溜まったら、処理する。
            // 自発的にRCS620Sがメッセージ送信することは無いが、ACKとレスポンスの連続受信が起こる。
            // 必要な分だけ切り抜いて残りはキューイングしておく。
            var cutOutData = new byte[leastExpectedLength];
            Array.Copy(receivedBuffer, cutOutData, leastExpectedLength);
            // 切り抜いた残りを受信バッファの先頭まで繰り上げる。
            if (ReceivedLength >= leastExpectedLength)
            {
                var diff = ReceivedLength - leastExpectedLength;
                var tempBuf = new byte[MAX_LENGTH];
                Array.Copy(receivedBuffer, leastExpectedLength, tempBuf, 0, diff);
                receivedBuffer.Initialize();
                Array.Copy(tempBuf, receivedBuffer, diff);
                ReceivedLength -= leastExpectedLength;
            }
            return cutOutData;
        }
    }
}
