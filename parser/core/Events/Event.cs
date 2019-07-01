using System;
using System.Text.RegularExpressions;

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
        //Double_Bow_Shot = 128,
        Rampage = 256,
        Wild_Rampage = 512, // will also identify as Rampage
        Special = 1024
        //Headshot = 1024,
        //Assassinate = 2048,
        //Decapitate = 4096,
        //Slay_Undead = 8192
    }

    /// <summary>
    /// Root class for all log events.
    /// </summary>
    public abstract class LogEvent
    {
        /// <summary>
        /// Timestamp as it appears in the log file. Converted to UTC.
        /// </summary>
        public DateTime Timestamp;

        protected static LogEventMod ParseMod(string text)
        {
            LogEventMod mod = 0;
            var parts = text.ToLower().Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "critical":
                    case "crippling":
                        mod |= LogEventMod.Critical;
                        break;
                    case "twincast":
                    //case "twinstrike": 
                        mod |= LogEventMod.Twincast;
                        break;
                    case "lucky":
                        mod |= LogEventMod.Lucky;
                        break;
                    case "riposte":
                        mod |= LogEventMod.Riposte;
                        break;
                    case "strikethrough":
                        mod |= LogEventMod.Strikethrough;
                        break;
                    case "flurry":
                        mod |= LogEventMod.Flurry;
                        break;
                    case "finishing":
                        mod |= LogEventMod.Finishing_Blow;
                        break;
                    case "headshot":
                    case "assassinate":
                    case "decapitate":
                        mod |= LogEventMod.Special;
                        break;
                    //case "headshot":
                    //    mod |= LogEventMod.Headshot;
                    //    break;
                    //case "assassinate":
                    //    mod |= LogEventMod.Assassinate;
                    //    break;
                    case "rampage":
                        mod |= LogEventMod.Rampage;
                        break;
                    case "wild":
                        mod |= LogEventMod.Wild_Rampage;
                        break;
                    default:
                        break;
                }
            }
            return mod;
        }

    }

}
