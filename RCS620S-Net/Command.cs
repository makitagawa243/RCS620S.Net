using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCS620S_Net
{
    public enum SubCommand : byte
    {
        GetFirmwareVersion = 0x02,
        GetGeneralStatus = 0x04,
        SetParameters = 0x12,
        PowerDown = 0x16,
        RFConfiguration = 0x32,
        Reset = 0x18,
        InListPassiveTarget = 0x4a,
        TgInitTarget = 0x8c,
        TgSetGeneralBytes = 0x92,
        TgGetDEPData = 0x86,
        TgSetDEPData = 0x8e,
        CommunicateThruEX = 0xa0,
    }

    public enum SubResponse : byte
    {
        Undefine = 0x00,

        GetFirmwareVersion = 0x03,
        GetGeneralStatus = 0x05,
        SetParameters = 0x13,
        PowerDown = 0x17,
        RFConfiguration = 0x33,
        Reset = 0x19,
        InListPassiveTarget = 0x4b,
        TgInitTarget = 0x8d,
        TgSetGeneralBytes = 0x93,
        TgGetDEPData = 0x87,
        TgSetDEPData = 0x8f,
        CommunicateThruEX = 0xa1,

    }

    public enum RFConfigurationItem
    {
        Undefine = 0x00,
        RFField = 0x01,
        VariousTimings = 0x02,
        MaxRetries = 0x05,
        WaitTime = 0x81,
        DEPTimeout = 0x82,
    }
}
