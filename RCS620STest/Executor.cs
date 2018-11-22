using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using RCS620S_Net;

namespace RCS620STest
{
    class Executor
    {
        private SerialPort sp;
        private RCS620S rcs;
        
        public void Execute()
        {
            //sp = new SerialPort("/dev/ttyAMA0", 115200, Parity.None, 8, StopBits.One);
            sp = new SerialPort("COM9", 115200, Parity.None, 8, StopBits.One);
            rcs = new RCS620S(sp);

            var ret = rcs.GetFirmWareVersion();
            //Console.WriteLine($"ret:{ret} send.");

            var hoge = rcs.Initialize();
            //Console.WriteLine($"hoge:{hoge} initialize end.");

            while (true) {
                //if(rcs.ReadFelica() == Define.OK)
                if (rcs.ReadMIFARE() == Define.OK)
                {
                    Console.WriteLine("MIFARE");

                    //if(rcs.IDm != string.Empty)
                    Console.WriteLine(rcs.IDm);
                    // TODO 同じカードを置きっぱなしにすると、1回検出無しを挟む。Felicaでは連続検知できる。また、2枚置きすると連続して交互に検知する。この挙動の原因を調査する。
                }
                else if(rcs.ReadFelica() == Define.OK)
                {
                    Console.WriteLine("Felica");
                    Console.WriteLine(rcs.IDm);
                }
                else
                {
                    Console.WriteLine("no exist");
                }
                
                System.Threading.Thread.Sleep(50);
            };
        }

    }
}
