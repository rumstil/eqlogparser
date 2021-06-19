using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    /// <summary>
    /// Parses raid roster and guild roster files and returns LogWhoEvents that can be fed to a tracker.
    /// "/output guild" saves guild rosters. e.g. Derelict Space Toilet_erollisi-20201020-210532.txt
    /// "/output raid" saves raid rosters. e.g. RaidRoster_erollisi-20200802-190436.txt
    /// </summary>
    public class RosterParser
    {
        /// <summary>
        /// Check if the filename is a standard roster file name.
        /// Players can customize names to be anything so this doesn't cover those cases.
        /// </summary>
        public static bool IsValidFileName(string path)
        {
            return Regex.IsMatch(path, @"-20\d{6}-\d{6}\.txt$", RegexOptions.RightToLeft);
        }

        public static IEnumerable<LogWhoEvent> Load(string path)
        {
            //if (!File.Exists(path))
            //    throw new FileNotFoundException();

            if (!File.Exists(path))
                yield break;

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
                        yield return who;
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
                        yield return who;
                    }
                }
            }

        }

        public static IEnumerable<LogWhoEvent> Load(IEnumerable<FileInfo> files)
        {
            foreach (var f in files)
                foreach (var who in Load(f.FullName))
                    yield return who;
        }

    }
}
