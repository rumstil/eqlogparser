using System;
using System.Collections.Generic;
using System.Text;

namespace EQLogParser
{
    [Flags]
    public enum LogEventMod
    {
        None = 0,
        Critical = 1,
        Twincast = 2,
        Lucky = 4,
        Flurry = 8,
        Riposte = 16,
        Strikethrough = 32,
        Finishing_Blow = 64,
        Double_Bow_Shot = 128,
        Rampage = 256,
        Wild_Rampage = 512, // will also identify as Rampage
        //Special = 1024
        Headshot = 1024,
        Assassinate = 2048,
        Decapitate = 4096,
        Slay_Undead = 8192,
    }

}
