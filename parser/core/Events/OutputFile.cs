using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when /ouputfile command is used to save a file.
    /// usage: /outputfile [guild | raid | spellbook | inventory | guildbank | realestate | guildhall | missingspells | recipes [argument] | faction ] [optional filename]
    /// This event shouldn't be trusted in a server context where the file reference may be an exploit attempt.
    /// </summary>
    public class LogOutputFileEvent : LogEvent
    {
        public string FileName;

        public override string ToString()
        {
            return String.Format("File: {0}", FileName);
        }

        // [Sun Aug 02 19:04:36 2020] Outputfile Complete: RaidRoster_erollisi-20200802-190436.txt
        // [Sun Aug 02 19:04:41 2020] Outputfile Complete: Derelict Space Toilet-20200802-190441.txt
        private static readonly Regex OutputRegex = new Regex(@"^Outputfile Complete: (.*)$", RegexOptions.Compiled);


        public static LogOutputFileEvent Parse(LogRawEvent e)
        {
            var m = OutputRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogOutputFileEvent
                {
                    Timestamp = e.Timestamp,
                    FileName = e.FixName(m.Groups[1].Value),
                };
            }

            return null;
        }

    }
}
