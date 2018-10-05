using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCS620S_Net
{
    public enum NFCType
    {
        ISOIEC14443_TypeA_MIFARE = 0x01,
        ISOIEC14443_TypeA_MIFARE_UL = 0x02,
        ISOIEC14443_TypeB = 0x04,
        FeliCa = 0x08,
    }
}
