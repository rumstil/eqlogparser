using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    /// <summary>
    /// Parses raid roster and guild roster files and stores them as LogWhoEvents that can be fed to a tracker.
    /// </summary>
    public class RosterParser
    {
        public readonly List<LogWhoEvent> Chars = new List<LogWhoEvent>();

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            using (var f = File.OpenText(path))
            {
                var m = Regex.Match(path, @"(20\d{6}-\d{6})");
                if (m.Success && DateTime.TryParseExact(m.Groups[1].Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime ts))
                    ts = ts.ToUniversalTime();
                else
                    ts = DateTime.UtcNow;

                while (true)
                {
                    var line = f.ReadLine();
                    if (line == null)
                        break;

                    var parts = line.Split('\t');

                    // guild format:
                    // Rumstil	115	Ranger	Member		08/02/20	The Overthere	Inactive		off	off	1954229	03/21/20	Inactive	
                    if (parts.Length >= 3 && Regex.IsMatch(line, @"^\w+\t\d+\w+\t"))
                    {
                        var who = new LogWhoEvent()
                        {
                            Timestamp = ts,
                            Name = parts[0],
                            Level = Int32.Parse(parts[1]),
                            Class = LogWhoEvent.ParseClass(parts[2])
                        };
                        Chars.Add(who);
                    }

                    // raid format:
                    // 0   Rumstil 115 Ranger Raid Leader
                    if (parts.Length >= 4 && Regex.IsMatch(line, @"^\d+\t\w+\t\d+\t\w+\t"))
                    {
                        var who = new LogWhoEvent()
                        {
                            Timestamp = ts,
                            Name = parts[1],
                            Level = Int32.Parse(parts[2]),
                            Class = LogWhoEvent.ParseClass(parts[3])
                        };
                        Chars.Add(who);
                    }

                }
            }

        }
    }
}
