using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
2022-9-14
- Experience gain messages will now also display the percentage gained. This change currently only applies to regular experience (not AA).

*/

namespace EQLogParser
{
    public enum XPType
    {
        None,
        SoloXP,
        GroupXP,
        RaidXP,
    }

    /// <summary>
    /// Generated when you earn regular experience.
    /// </summary>
    public class LogXPEvent : LogEvent
    {
        public XPType Type;
        public decimal? Amount;

        public override string ToString()
        {
            return String.Format("XP: {0}", Type, Amount);
        }
        
        private static readonly Regex XPRegex = new Regex(@"^You gaine?d? (experience|party|raid)", RegexOptions.Compiled);
        private static readonly Regex AmountRegex = new Regex(@"\d+\.\d+", RegexOptions.Compiled);

        public static LogXPEvent Parse(LogRawEvent e)
        {
            var m = XPRegex.Match(e.Text);
            if (m.Success)
            {
                var type = XPType.SoloXP;
                if (m.Groups[1].Value == "party")
                    type = XPType.GroupXP;
                if (m.Groups[1].Value == "raid")
                    type = XPType.RaidXP;

                var amount = AmountRegex.Match(e.Text);

                return new LogXPEvent
                {
                    Timestamp = e.Timestamp,
                    Type = type,
                    Amount = amount.Success ? Decimal.Parse(amount.Value) : null,
                };
            }

            return null;
        }

    }
}
