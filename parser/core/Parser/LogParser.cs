﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public delegate LogEvent LogEventParser(LogRawEvent e);

    public delegate void LogEventHandler(LogEvent e);

    /// <summary>
    /// This is the main interface for log parsing. 
    /// You feed it log lines via the ParseLine() function and it returns them as events via the OnEvent delegate.
    /// All of the parsing is done outside of this class in the individual event parsers.
    /// This class is mostly stateless and doesn't do much more than parsing of events.
    /// </summary>
    public class LogParser
    {
        public List<string> Ignore = new List<string>();
        public List<LogEventParser> Parsers = new List<LogEventParser>();

        public event LogEventHandler OnEvent;

        // 2018-12-18 TBL expansion changed log format significantly and most of the hit parsing will not 
        // work for earlier log files
        public DateTime MinDate = DateTime.Parse("2018-12-18");
        public DateTime MaxDate = DateTime.MaxValue;

        public string Server;
        public string Player;

        public LogParser()
        {
            // spammy message are ignored rather than returned as LogRawEvent
            Ignore.Add("Your target is too far away, get closer!");
            Ignore.Add("Your target is too close to use a ranged weapon!");
            Ignore.Add("You cannot see your target.");
            Ignore.Add("You can't use that command right now...");
            //Ignore.Add("Try attacking someone other than yourself. It's more productive.");

            // obsolete parsers (these should check the date)
            //Parsers.Add(LogCritEvent.Parse);

            // parsers can be placed in any sequence as long as there is no overlap in matching 
            // the order below has been chosen to place the most frequent event types first which should speed up overall parsing
            Parsers.Add(LogHitEvent.Parse);
            Parsers.Add(LogMissEvent.Parse);
            Parsers.Add(LogHealEvent.Parse);
            Parsers.Add(LogDeathEvent.Parse);
            Parsers.Add(LogChatEvent.Parse);
            Parsers.Add(LogCastingEvent.Parse);
            //Parsers.Add(LogTwinEvent.Parse);
            Parsers.Add(LogZoneEvent.Parse);
            Parsers.Add(LogPartyEvent.Parse);
            Parsers.Add(LogWhoEvent.Parse);
            Parsers.Add(LogConEvent.Parse);
            Parsers.Add(LogLootEvent.Parse);
            Parsers.Add(LogAAXPEvent.Parse);
            Parsers.Add(LogSkillEvent.Parse);
        }

        /// <summary>
        /// Get player name from the filename. This is important for converting the "you" messages
        /// in the log to an actual name.
        /// </summary>
        public static string GetPlayerFromFileName(string path)
        {
            var m = Regex.Match(path, @"eqlog_(\w+)_(\w+)\.txt$");
            if (m.Success)
            {
                var name = m.Groups[1].Value;
                name = Char.ToUpper(name[0]) + name.Substring(1).ToLower();
                return name;
            }
            return null;
        }

        //public void Subscribe(LogEventHandler handler)
        //{
        //    OnEvent += handler;
        //    if (Player != null)
        //        handler(new LogWhoEvent() { Name = Player });
        //}

        //public void Unsubscribe(LogEventHandler handler)
        //{
        //    OnEvent -= handler;
        //}

        /// <summary>
        /// Process a single log file line and return the result if successful.
        /// </summary>
        public LogEvent ParseLine(string text)
        {
            if (String.IsNullOrEmpty(text))
                return null;

            // first stage parser accepts a string and convert to a LogRawEvent
            var raw = LogRawEvent.Parse(text);
            if (raw == null)
                return null;

            raw.Player = Player;

            // ignore if timestamp out of range
            if (raw.Timestamp < MinDate || raw.Timestamp > MaxDate)
                return null;

            // ignore spam
            if (Ignore.Contains(raw.Text, StringComparer.Ordinal))
                return null;

            // second stage parsers accept a LogRawEvent and convert to a LogEvent descendant
            // call each custom parser until one returns a non null result
            for (int i = 0; i < Parsers.Count; i++)
            {
                var result = Parsers[i](raw);
                if (result != null)
                {
                    if (OnEvent != null)
                        OnEvent(result);
                    return result; 
                }
            }

            // if no match was found then just return the raw event
            // this is useful for catching parsing left-overs that slipped through regex checks
            if (OnEvent != null)
                OnEvent(raw);
            return raw;
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
