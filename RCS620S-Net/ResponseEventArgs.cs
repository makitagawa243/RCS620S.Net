using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCS620S_Net
{
    public class ResponseEventArgs : EventArgs
    {
        public SubResponse ArrivedCode { get; private set; }
        public ResponseEventArgs(SubResponse code)
        {
            ArrivedCode = code;
        }
    }
}
