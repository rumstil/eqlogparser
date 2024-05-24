using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when /random dice roll is generated.
    /// </summary>
    public class LogDiceRollEvent : LogEvent
    {
        public string Source;
        public int Min;
        public int Max;
        public int Roll;
        public string Item;

        public override string ToString()
        {
            return String.Format("Rolled: {0} => {1} ({2}..{3})", Source, Roll, Min, Max);
        }

        private static readonly Regex RandomRegex = new Regex(@"^\*\*A Magic Die is rolled by (\w+). It could have been any number from (\d+) to (\d+), but this time it turned up a (\d+).$", RegexOptions.Compiled);

        private static readonly Regex AutoLootRegex = new Regex(@"^(\w+) rolled a (\d+) on (.+).$", RegexOptions.Compiled);

        public static LogDiceRollEvent Parse(LogRawEvent e)
        {
            var m = RandomRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogDiceRollEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Min = Int32.Parse(m.Groups[2].Value),
                    Max = Int32.Parse(m.Groups[3].Value),
                    Roll = Int32.Parse(m.Groups[4].Value),
                };
            }

            m = AutoLootRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogDiceRollEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Min = 1,
                    Max = 1000,
                    Roll = Int32.Parse(m.Groups[2].Value),
                    Item = m.Groups[3].Value,
                };
            }

            return null;
        }

    }
}
