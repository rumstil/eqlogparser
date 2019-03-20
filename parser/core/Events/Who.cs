﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a /who response is shown.
    /// </summary>
    public class LogWhoEvent : LogEvent
    {
        public string Name;
        public string Class;
        public int Level;

        public override string ToString()
        {
            return String.Format("Player: {0}", Name);
        }

        // [Thu May 19 13:37:35 2016] [ANONYMOUS] Rumstil 
        // [Thu May 19 13:39:00 2016] [105 Huntmaster (Ranger)] Rumstil (Halfling) ZONE: kattacastrumb  
        // [Thu May 19 13:55:55 2016] [1 Cleric] Test (Froglok)  ZONE: bazaar  
        // [Thu May 19 13:57:50 2016] OFFLINE MODE[1 Shadow Knight] Test (Dark Elf) ZONE: bazaar  
        private static readonly Regex WhoRegex = new Regex(@"^[A-Z\s]*\[(?:(ANONYMOUS)|(?<2>\d+) (?<3>[\w\s]+)|(?<2>\d+) .+? \((?<3>[\w\s]+)\))\] (?<1>\w+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public static LogWhoEvent Parse(LogRawEvent e)
        {
            var m = WhoRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogWhoEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Class = ParseClass(m.Groups[3].Success ? m.Groups[3].Value : null),
                    Level = m.Groups[2].Success ? Int32.Parse(m.Groups[2].Value) : 0
                };
            }

            return null;
        }

        public static string ParseClass(string name)
        {
            //if (Enum.TryParse(typeof(ClassesMaskLong), name, out object value))
            //    return ((ClassesMaskShort)(int)value).ToString();

            switch (name)
            {
                case "Warrior": return "WAR";
                case "Cleric": return "CLR";
                case "Paladin": return "PAL";
                case "Ranger": return "RNG";
                case "Shadow Knight": return "SHD";
                case "Druid": return "DRU";
                case "Monk": return "MNK";
                case "Bard": return "BRD";
                case "Rogue": return "ROG";
                case "Shaman": return "SHM";
                case "Necromancer": return "NEC";
                case "Wizard": return "WIZ";
                case "Magician": return "MAG";
                case "Enchanter": return "ENC";
                case "Beastlor": return "BST";
                case "Berserker": return "BER";
                default: return null;
            }
        }
    }
}
