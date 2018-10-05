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
            sp = new SerialPort("COM8", 115200, Parity.None, 8, StopBits.One);
            rcs = new RCS620S(sp);

            var ret = rcs.GetFirmWareVersion();
            Console.WriteLine($"ret:{ret} send.");

            var hoge = rcs.Initialize();
            Console.WriteLine($"hoge:{hoge} initialize end.");

            while (true) {
                //if(rcs.ReadFelica() == Define.OK)
                if (rcs.ReadMIFARE() == Define.OK)
                {
                    if(rcs.IDm != string.Empty) Console.WriteLine(rcs.IDm);
                }
                System.Threading.Thread.Sleep(50);
            };
        }

    }
}
