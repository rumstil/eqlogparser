using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;




namespace EQLogParser
{
    public delegate LogEvent LogEventParser(LogRawEvent e);

    public delegate void LogEventHandler(LogEvent e);

    /// <summary>
    /// An event-based parser for converting EQ log wierdness into well defined structures. 
    /// This class maintains a collection of parsers that implement the LogEventParser delegate pattern.
    /// Each of those parsers returns a LogEvent descendant if succesful which is then returned by this
    /// class through the OnEvent callback.
    /// </summary>
    public class LogParser
    {
        public List<string> Ignore = new List<string>();
        public List<LogEventParser> Parsers = new List<LogEventParser>();

        public event LogEventHandler OnEvent;

        // 2018-12-18 TBL expansion changed log format significantly and most the hit parsers will not 
        // work for earlier log files
        public DateTime MinDate = DateTime.Parse("2018-12-18"); 
        public DateTime MaxDate = DateTime.MaxValue;

        public string Server;
        public string Player;

        private int Count;

        public LogParser()
        {
            // spammy message are ignored rather than returned as LogRawEvent
            Ignore.Add("Your target is too far away, get closer!");
            Ignore.Add("Your target is too close to use a ranged weapon!");
            Ignore.Add("You cannot see your target.");
            Ignore.Add("You can't use that command right now...");
            //Ignore.Add("Try attacking someone other than yourself. It's more productive.");

            // register all parsers that are marked with LogRawEventParserAttribute
            // downside of this method is we can't optimize the order
            //var methods = typeof(Parser).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            //var parsers = methods.Where(x => x.GetCustomAttributes(false).OfType<LogRawEventParserAttribute>().Any());

            // add obsolete parsers first (these should check the date)
            //Parsers.Add(LogHitCritEvent.Parse);

            // parsers can be placed in any sequence as long as there is no overlap in matching 
            // the order below has been chosen to place the most frequent event types first which should speed up overall parsing
            Parsers.Add(LogHitEvent.Parse);
            Parsers.Add(LogMissEvent.Parse);
            Parsers.Add(LogHealEvent.Parse);
            Parsers.Add(LogDeathEvent.Parse);
            Parsers.Add(LogPetChatEvent.Parse); // add before LogChatEvent to capture pet chat
            Parsers.Add(LogChatEvent.Parse);
            Parsers.Add(LogCastingEvent.Parse);
            Parsers.Add(LogZoneEvent.Parse);
            Parsers.Add(LogPartyEvent.Parse);
            Parsers.Add(LogWhoEvent.Parse);
            Parsers.Add(LogConEvent.Parse);
            Parsers.Add(LogLootEvent.Parse);
            //Parsers.Add(LogAAXPEvent.Parse);
            //Parsers.Add(LogSkillEvent.Parse);

        }

        public LogParser(LogReader file) : this()
        {
            // eqlog_Rumstil_erollisi.txt
            var m = Regex.Match(file.Path, @"eqlog_(\w+)_(\w+)\.txt$");
            if (m.Success)
            {
                Player = m.Groups[1].Value;
                Player = Char.ToUpper(Player[0]) + Player.Substring(1).ToLower();
                Server = m.Groups[2].Value;
            }
            file.OnRead += line => ParseLine(line);
        }

        public void Subscribe(LogEventHandler handler)
        {
            OnEvent += handler;
        }

        public void Unsubscribe(LogEventHandler handler)
        {
            OnEvent -= handler;
        }

        /// <summary>
        /// Process a full log line and trigger OnEvent delegate if successful.
        /// </summary>
        public void ParseLine(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            // first stage parser accepts a string and convert to a LogRawEvent
            var raw = LogRawEvent.Parse(text);
            if (raw == null)
                return;

            raw.Id = Count++;
            raw.Player = Player;

            // ignore if timestamp out of range
            if (raw.Timestamp < MinDate || raw.Timestamp > MaxDate)
                return;

            // ignore spam
            if (Ignore.Contains(raw.Text))
                return;

            // second stage parsers accept a LogRawEvent and convert to a LogEvent descendant
            // call each custom parser until one returns a non null result
            for (int i = 0; i < Parsers.Count; i++)
            {
                var result = Parsers[i](raw);
                if (result != null)
                {
                    result.Id = raw.Id;
                    OnEvent(result);
                    return;
                }
            }

            // if no match was found then just return the raw event
            // this is useful for catching parsing left-overs that slipped through regex checks
            OnEvent(raw);
        }

        /// <summary>
        /// Emit log owner as event to help ID player/mob names.
        /// </summary>
        //public void GetOwner()
        //{
        //    OnEvent(new LogWhoEvent() { Name = Player });
        //}
    }
}
