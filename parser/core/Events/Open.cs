using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// This should be sent to the trackers as the first HandleEvent() call after a log file is opened to ensure proper initialization.
    /// Unlike other events which are all returned from the parser, it should fall to the log reader to generate this event.
    /// </summary>
    public class LogOpenEvent : LogEvent
    {
        public string Path;

        /// <summary>
        /// Player name as it appear in log filename.
        /// </summary>
        public string Player;

        /// <summary>
        /// Server name as it appear in log filename.
        /// </summary>
        public string Server;

        /// <summary>
        /// Get player name from the filename. This is important for converting the "you" messages
        /// in the log to an actual name.
        /// </summary>
        public static string GetPlayerFromFileName(string path)
        {
            //var m = Regex.Match(path, @"eqlog_(\w+)_(\w+)\.txt$"); // official format
            var m = Regex.Match(path, @"eqlog_([A-Za-z]+)"); // permissive format
            if (m.Success)
            {
                var name = m.Groups[1].Value;
                name = Char.ToUpper(name[0]) + name.Substring(1).ToLower();
                return name;
            }
            return null;
        }

        /// <summary>
        /// Get server name from filename. 
        /// </summary>
        public static string GetServerFromFileName(string path)
        {
            // this may be incorrect if the log file has been renamed.
            //var m = Regex.Match(path, @"eqlog_\w+_([A-Za-z]+)"); // permissive format

            // hardcoding server names to guard against files that are renamed being misattributed
            // some of these servers may be defunct but I'll leave them in so old files can be parsed
            var m = Regex.Match(path, @"eqlog_\w+_(test|beta|agnarr|aradune|bertox|bristle|cazic|coirnav|drinal|erollisi|firiona|luclin|mangler|miragul|mischief|phinigel|povar|ragefire|rizlona|selo|thornblade|rathe|tunare|vox|xegony|zek)");
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
        public static LogOpenEvent FromFileName(string path)
        {
            return new LogOpenEvent()
            {
                Path = path,
                Player = GetPlayerFromFileName(path),
                Server = GetServerFromFileName(path)
            };
        }
    }
}
