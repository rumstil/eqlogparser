using System;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player twincasts or twinstrikes an spell/ability.
    /// This is only shown for the logging player so it isn't particularly useful for group/raid parses.
    /// </summary>
    public class LogTwinEvent : LogEvent
    {
        public string Source;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Twin: {0} casting {1}", Source, Spell);
        }

        // [Wed Feb 20 18:14:12 2019] You twincast Night's Endless Darkness. -- Obsolete as of March 2019 patch
        // [Sat Apr 13 19:46:09 2019] You twinstrike Strike of the Archer IV.
        private static readonly Regex TwincastRegex = new Regex(@"^You twin(?:strike|cast) (.+)\.$", RegexOptions.Compiled);

        public static LogTwinEvent Parse(LogRawEvent e)
        {
            var m = TwincastRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogTwinEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Spell = m.Groups[1].Value
                };
            }

            return null;
        }

    }

}
