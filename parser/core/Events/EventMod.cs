using System;
using System.Collections.Generic;
using System.Text;

namespace EQLogParser
{
    [Flags]
    public enum LogEventMod
    {
        None = 0,
        Critical        = 1,
        Twincast        = 1 << 1,
        Lucky           = 1 << 2,
        Flurry          = 1 << 3,
        Riposte         = 1 << 4,
        Strikethrough   = 1 << 5,
        Finishing_Blow  = 1 << 6,
        Double_Bow_Shot = 1 << 7,
        Rampage         = 1 << 8,
        Wild_Rampage    = 1 << 9, // will also identify as Rampage
        Headshot        = 1 << 10,
        Assassinate     = 1 << 11,
        Decapitate      = 1 << 12,
        Slay_Undead     = 1 << 13,
        Locked          = 1 << 14,
    }

}
