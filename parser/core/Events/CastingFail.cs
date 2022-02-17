using System;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a casting failure occurs (fizzle, interruption, reflection).
    /// Resists, and "did not take hold" are only reported to caster and not currently processed.
    /// </summary>
    public class LogCastingFailEvent : LogEvent
    {
        public string Source;
        public string Target;
        public string Type;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Failed: {0} => {1} {2}", Source, Target, Type);
        }

        // [Wed Jan 19 20:56:13 2022] a clockwork defender CXCIX's Night's Perpetual Darkness spell fizzles!
        // [Wed Jan 19 21:33:12 2022] Your Shield of Shadethorns spell fizzles!
        private static readonly Regex FizzleRegex = new Regex(@"^(\w+)'s (.+?) spell fizzles!$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex SelfFizzleRegex = new Regex(@"^Your (.+?) spell fizzles!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Wed Jan 19 21:42:10 2022] Fourier's Mind Coil spell is interrupted.
        // [Wed Jan 19 22:53:38 2022] Your Claimed Shots spell is interrupted.
        // [Tue Jan 25 23:57:12 2022] Fourier's casting is interrupted!
        private static readonly Regex InterruptedRegex = new Regex(@"^(\w+)'s (.+?) spell is interrupted.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex SelfInterruptedRegex = new Regex(@"^Your (.+?) spell is interrupted.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu Jan 27 18:28:31 2022] Rumstil's Clinging Darkness spell has been reflected by Laskuth the Colossus.


        // resists are only reported to the caster -- which makes them somewhat useless to capture
        // [Wed Feb 13 21:25:32 2019] A mist wolf resisted your Undermining Helix Rk. II!
        //private static readonly Regex ResistRegex = new Regex(@"^(.+) resisted your (.+?)!$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        //private static readonly Regex SelfResistRegex = new Regex(@"^You resist (.+?)'s (.+)!$", RegexOptions.Compiled);


        public static LogCastingFailEvent Parse(LogRawEvent e)
        {
            var m = InterruptedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingFailEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[2].Value,
                    Type = "interrupt",
                };
            }

            m = SelfInterruptedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingFailEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Spell = m.Groups[1].Value,
                    Type = "interrupt",
                };
            }

            m = FizzleRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingFailEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[2].Value,
                    Type = "fizzle",
                };
            }

            m = SelfFizzleRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingFailEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Spell = m.Groups[1].Value,
                    Type = "fizzle",
                };
            }

            /*
            var m = ResistRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingFailEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Target = e.FixName(m.Groups[1].Value),
                    Type = "resist",
                    Spell = m.Groups[2].Value
                };
            }

            m = SelfResistRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingFailEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.Player,
                    Type = "resist",
                    Spell = m.Groups[2].Value
                };
            }
            */

            return null;
        }

    }
}
