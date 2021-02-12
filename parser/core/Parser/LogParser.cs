﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    public delegate LogEvent LogEventParser(LogRawEvent e);

    //public delegate void LogEventHandler(LogEvent e);

    /// <summary>
    /// This is the main interface for log parsing. 
    /// You feed it log lines via the ParseLine() function and it returns them as events.
    /// All of the parsing is done outside of this class in the individual event parsers.
    /// </summary>
    public class LogParser
    {
        public List<string> Ignore = new List<string>();
        public List<LogEventParser> Parsers = new List<LogEventParser>();

        //public event LogEventHandler OnEvent;

        // 2018-12-18 TBL expansion changed log format significantly and most of the hit parsing will not 
        // work for earlier log files
        public DateTime MinDate = DateTime.Parse("2018-12-18");
        public DateTime MaxDate = DateTime.MaxValue;

        //public string Server;
        public string Player;

        public LogParser()
        {
            // spammy message are ignored rather than returned as LogRawEvent
            Ignore.Add("Your target is too far away, get closer!");
            Ignore.Add("Your target is too close to use a ranged weapon!");
            Ignore.Add("You cannot see your target.");
            Ignore.Add("You can't reach that, get closer.");
            Ignore.Add("You can't use that command right now...");
            //Ignore.Add("Try attacking someone other than yourself. It's more productive.");

            // obsolete parsers (these should check the date)
            //Parsers.Add(LogCritEvent.Parse);
            //Parsers.Add(LogTwinEvent.Parse);

            // parsers can be placed in any sequence as long as there is no overlap in matching 
            // the order below has been chosen to place the most frequent event types first which should speed up overall parsing
            Parsers.Add(LogMissEvent.Parse);
            Parsers.Add(LogHitEvent.Parse);
            Parsers.Add(LogHealEvent.Parse);
            Parsers.Add(LogCastingEvent.Parse);
            Parsers.Add(LogDeathEvent.Parse);
            Parsers.Add(LogChatEvent.Parse);
            Parsers.Add(LogZoneEvent.Parse);
            Parsers.Add(LogPartyEvent.Parse);
            Parsers.Add(LogWhoEvent.Parse);
            Parsers.Add(LogConEvent.Parse);
            Parsers.Add(LogLootEvent.Parse);
            Parsers.Add(LogRotEvent.Parse);
            Parsers.Add(LogCraftEvent.Parse);
            Parsers.Add(LogAAXPEvent.Parse);
            Parsers.Add(LogSkillEvent.Parse);
        }

        /// <summary>
        /// Get player name from the filename. This is important for converting the "you" messages
        /// in the log to an actual name.
        /// </summary>
        public static string GetPlayerFromFileName(string path)
        {
            //var m = Regex.Match(path, @"eqlog_(\w+)_(\w+)\.txt$"); // official format
            var m = Regex.Match(path, @"eqlog_([A-Za-z]+).*\.txt(\.gz)?$"); // permissive format
            if (m.Success)
            {
                var name = m.Groups[1].Value;
                name = Char.ToUpper(name[0]) + name.Substring(1).ToLower();
                return name;
            }
            return null;
        }

        /// <summary>
        /// Get server name from filename. This may be incorrect if the log file has been renamed.
        /// I should probably check again list of real server names -- at least test/beta since those are those 
        /// ones where we can expect different log formats or data.
        /// </summary>
        public static string GetServerFromFileName(string path)
        {
            var m = Regex.Match(path, @"eqlog_\w+_([A-Za-z]+).*\.txt(\.gz)?$"); // permissive format
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        /// Generate an open event based on info from the log file name.
        /// This is used by trackers to tell them who the "you" player is.
        /// </summary>
        public static LogOpenEvent GetOpenEvent(string path)
        {
            return new LogOpenEvent() 
            { 
                Path = path,
                Player = GetPlayerFromFileName(path), 
                Server = GetServerFromFileName(path) 
            };
        }

        /// <summary>
        /// Process a single line from the log file and return an event that describes the line.
        /// If none of the event parsers can handle the line then a LogRawEvent will be returned.
        /// If the line is blank or the date is malformed then a null will be returned.
        /// </summary>
        public LogEvent ParseLine(string text)
        {
            if (String.IsNullOrEmpty(Player))
                throw new InvalidOperationException("Log owner player name must be set prior to parsing.");

            if (String.IsNullOrEmpty(text))
                return null;

            // convert a raw log message line to a LogRawEvent (this can return a null)
            var raw = LogRawEvent.Parse(text);
            if (raw == null)
                return null;

            raw.Player = Player;
            return ParseEvent(raw);
        }

        /// <summary>
        /// Try to convert a LogRawEvent to a more specific type of event.
        /// </summary>
        private LogEvent ParseEvent(LogRawEvent raw)
        {
            // GamParse exports the first line in single fight log files as 
            // [Mon Jan 01 01:01:01 1990] 
            if (raw.Timestamp.Year == 1990)
                raw.Timestamp = MinDate;

            // ignore if timestamp out of range
            if (raw.Timestamp < MinDate || raw.Timestamp > MaxDate)
                return null;

            // ignore spam
            if (Ignore.Contains(raw.Text, StringComparer.Ordinal))
                return null;

            // call each custom parser until one returns a non null result
            for (int i = 0; i < Parsers.Count; i++)
            {
                var result = Parsers[i](raw);
                if (result != null)
                {
                    //OnEvent?.Invoke(result);
                    return result;
                }
            }

            // if no match was found then just return the raw event
            // this is useful for catching parsing left-overs that slipped through regex checks
            //OnEvent?.Invoke(raw);
            return raw;
        }

    }
}
