using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when player, or NPC invokes a chat command.
    /// </summary>
    public class LogChatEvent : LogEvent
    {
        public string Source;
        public string Channel;
        public string Message;

        public override string ToString()
        {
            return String.Format("Chat: {0} tells {1} - {2}", Source, Channel, Message);
        }

        // [Tue Nov 03 21:52:01 2015] Dude tells the guild, 'hello?'
        // [Sun May 08 20:33:17 2016] You say to your guild, 'congrats'
        // using \w+ is a lot faster than .+? but will miss messages from NPCs with spaces in their names
        private static readonly Regex PrivateChatRegex = new Regex(@"^(.+?) (?:say to your|told|tell your|tells the|tells?) (.+?),\s(?:in .+, )?\s?'(.+)'$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PublicChatRegex = new Regex(@"^(.+?) (says? out of channel|says?|shouts?|auctions?),\s(?:in .+, )?'(.+)'$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogChatEvent Parse(LogRawEvent e)
        {
            var m = PrivateChatRegex.Match(e.Text);
            if (m.Success)
            {
                var source = e.FixName(m.Groups[1].Value);
                var channel = m.Groups[2].Value;
                if (channel == "you" || e.Text.StartsWith("You told"))
                    channel = "tell";
                if (channel == "party")
                    channel = "group";
                channel = Regex.Replace(channel, @":\d+$", "");

                return new LogChatEvent
                {
                    Timestamp = e.Timestamp,
                    Source = source,
                    Channel = channel,
                    Message = m.Groups[3].Value
                };
            }

            m = PublicChatRegex.Match(e.Text);
            if (m.Success)
            {
                var source = e.FixName(m.Groups[1].Value);
                var channel = m.Groups[2].Value.TrimEnd('s');
                if (channel == "says out of channel" || channel == "say out of channel")
                    channel = "ooc";

                return new LogChatEvent
                {
                    Timestamp = e.Timestamp,
                    Source = source,
                    Channel = channel,
                    Message = m.Groups[3].Value
                };
            }

            return null;
        }

    }
}
