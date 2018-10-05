using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace RCS620S_Net
{
    class Sender
    {
        private SerialPort SerialPort { get; set; }
        public Sender(SerialPort serialPort)
        {
            SerialPort = serialPort;
        }

        /// <summary>
        /// コマンド送信
        /// </summary>
        /// <param name="command">送信サブコマンド</param>
        /// <param name="parameter">パラメータ</param>
        /// <returns>送信結果</returns>
        public int Send(SubCommand command, byte[] parameter)
        {
            var sendData = CreateMessage(command, parameter);
            return WriteSerial(sendData);
        }

        /// <summary>
        /// シリアルデータ送信
        /// </summary>
        /// <param name="data">送信バイト列</param>
        /// <returns>送信結果</returns>
        public int WriteSerial(byte[] data)
        {
            if (!SerialPort.IsOpen) return Define.ERROR;
            try
            {
                SerialPort.Write(data, 0, data.Length);
                return Define.OK;
            }
            catch(Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// メッセージ構築
        /// </summary>
        /// <param name="command">送信サブコマンド</param>
        /// <param name="messageBody">コマンドコード、サブコマンドコードを除いたパラメータ</param>
        /// <returns>送信データ</returns>
        private byte[] CreateMessage(SubCommand command, byte[] messageBody)
        {
            var length = (byte)(messageBody != null ? messageBody.Length + 2 : 2);
            var parameterLength = length - 2;
            var frameBytes = 0;
            if(length <= 255)
            {
                frameBytes = 7;
            }
            else
            {
                frameBytes = 8;
            }
            var buf = new byte[length + frameBytes];
            buf[0] = 0x00;  // Preamble 00固定
            buf[1] = 0x00;  // Start Of Packet 00ff固定
            buf[2] = 0xff;
            if (length <= 255)
            {
                // normal frame
                buf[3] = length;  // データ部のバイト数（最大255バイト）
                buf[4] = (byte)-length;
                buf[5] = Define.COMMAND_CODE;
                buf[6] = (byte)command;
                // メッセージボディを転記する。
                if (messageBody != null)
                {
                    Array.Copy(messageBody, 0, buf, 7, parameterLength);
                }
                buf[7 + parameterLength] = CalculateDCS(buf.Skip(5).Take(length).ToArray());    // dcs
                buf[8 + parameterLength] = 0x00;    // Postamble 00固定
            }
            else
            {
                // extended frame
                buf[3] = 0xff;  // 2バイトフレーム識別用コード
                buf[4] = 0xff;
                buf[5] = (byte)((length >> 8) & 0xff);  // LENEx
                buf[6] = (byte)((length >> 0) & 0xff);
                buf[7] = (byte)-(buf[5] + buf[6]);      // LCSEx
                buf[8] = Define.COMMAND_CODE;
                buf[9] = (byte)command;
                // メッセージボディを転記する。
                if (messageBody != null)
                {
                    Array.Copy(messageBody, 0, buf, 10, parameterLength);
                }
                buf[10 + parameterLength] = CalculateDCS(buf.Skip(8).Take(length).ToArray());    // dcs
                buf[11 + parameterLength] = 0x00;    // Postamble 00固定
            }
            return buf;
        }

        private byte CalculateDCS(byte[] messageBody)
        {
            var bodySum = messageBody != null ? (byte)messageBody.Sum(value => (int)value) : 0;
            var ret = (byte)-(bodySum & 0xff);
            return ret;
        }
    }
}
