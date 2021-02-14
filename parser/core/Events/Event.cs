using System;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Base class for all log events.
    /// </summary>
    public abstract class LogEvent
    {
        /// <summary>
        /// Timestamp as it appears in the log file. Converted to UTC.
        /// </summary>
        public DateTime Timestamp;

        public static LogEventMod ParseMod(string text)
        {
            LogEventMod mod = 0;
            text = text.ToLower();
            text = text.Replace("double bow shot", "doublebow"); // multi word mod won't split properly
            var parts = text.Split(' ');
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
                    case "doublebow":
                        mod |= LogEventMod.Double_Bow_Shot;
                        break;
                    //case "headshot":
                    //case "assassinate":
                    //case "decapitate":
                    //case "slay":
                    //    mod |= LogEventMod.Special;
                    //    break;
                    case "headshot":
                        mod |= LogEventMod.Headshot;
                        break;
                    case "assassinate":
                        mod |= LogEventMod.Assassinate;
                        break;
                    case "decapitate":
                        mod |= LogEventMod.Decapitate;
                        break;
                    case "slay":
                        mod |= LogEventMod.Slay_Undead;
                        mod |= LogEventMod.Critical;
                        break;
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
