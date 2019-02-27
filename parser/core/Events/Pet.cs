using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a pet chat is shown.
    /// </summary>
    public class LogPetChatEvent : LogEvent
    {
        public string Name;
        public string Owner;

        public override string ToString()
        {
            return String.Format("Pet: {0} belongs to {1}", Name, Owner);
        }

        // [Wed Apr 27 09:46:18 2016] Goner says, 'My leader is Fourier.'
        // [Tue Jan 01 12:24:26 2019] Xebn tells you, 'Attacking a gehein pillager Master.'
        private static readonly Regex PetOwnerRegex = new Regex(@"^(.+) says, 'My leader is (\w+)\.'$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PetTellOwnerRegex = new Regex(@"^(.+) tells you, '(Attacking .+? Master|Sorry, Master\.\.\. calming down|Following you, Master|By your command, master|I live again\.\.)\.'$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogPetChatEvent Parse(LogRawEvent e)
        {
            var m = PetOwnerRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogPetChatEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Owner = m.Groups[2].Value
                };
            }

            m = PetTellOwnerRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogPetChatEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Owner = e.Player
                };
            }

            return null;
        }

    }
}
