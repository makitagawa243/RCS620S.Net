using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCS620S_Net
{
    public static class Define
    {
        public static int RCS620S_MAX_RW_RESPONSE_LEN = 265;
        public static int RCS620S_MAX_CARD_RESPONSE_LEN = 254;

        public static int ERROR = -1;
        public static int OK = 0;

        public static byte COMMAND_CODE = 0xd4;
        public static byte RESPONSE_CODE = 0xd5;

        public static int ACK_LENGTH = 6;
        public static byte[] ACK_DATA = { 0x00, 0x00, 0xff, 0x00, 0xff, 0x00 };
    }
}
