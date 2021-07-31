using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using EQLogParser;
using EQLogParser.Helpers;

namespace Sample
{
    public static class Utils
    {
        /// <summary>
        /// Perform timing tests on all parsers.
        /// </summary>
        public static void TimeParsers(LogReader file)
        {
            var parser = new LogParser();
            parser.Player = LogOpenEvent.GetPlayerFromFileName(file.Path);

            // load raw log lines once 
            var etimer = Stopwatch.StartNew();
            var events = new List<LogRawEvent>();
            while (true)
            {
                var s = file.ReadLine();
                if (s == null)
                    break;
                var e = LogRawEvent.Parse(s);
                if (e != null)
                    events.Add(e);
            };
            Console.Error.WriteLine("Loaded {0} events in {1}", events.Count, etimer.Elapsed);

            // time individual parsers
            foreach (var p in parser.Parsers)
            {
                var ptimer = Stopwatch.StartNew();
                var pcount = 0;
                foreach (var e in events)
                {
                    var result = p.Invoke(e);
                    if (result != null && !(result is LogRawEvent))
                        pcount++;
                }
                Console.Error.WriteLine("{0,-20} {1,10} in {2}", p.Method.DeclaringType.Name, pcount, ptimer.Elapsed);
            }

            Console.WriteLine("***");
        }

        /// <summary>
        /// Perform timing tests on all trackers.
        /// </summary>
        public static void TimeTrackers(LogReader file, SpellParser spells)
        {
            var parser = new LogParser();
            parser.Player = LogOpenEvent.GetPlayerFromFileName(file.Path);

            // load raw log lines once 
            var events = new List<LogEvent>();
            while (true)
            {
                var s = file.ReadLine();
                if (s == null)
                    break;
                var e = parser.ParseLine(s);
                if (e != null)
                    events.Add(e);
            };
            Console.Error.WriteLine("Loaded {0} events", events.Count);

            // timer trackers
            var trackers = new List<Action<LogEvent>>();
            var chars = new CharTracker(spells);
            trackers.Add(chars.HandleEvent);
            var fights = new FightTracker(spells, chars);
            trackers.Add(fights.HandleEvent);
            var buffs = new BuffTracker(spells, chars);
            trackers.Add(buffs.HandleEvent);
            var loot = new LootTracker();
            trackers.Add(loot.HandleEvent);

            // time individual trackers
            foreach (var t in trackers)
            {
                var ptimer = Stopwatch.StartNew();
                var pcount = 0;
                foreach (var e in events)
                {
                    t.Invoke(e);
                }
                Console.Error.WriteLine("{0,-20} {1,10} in {2}", t.Method.DeclaringType.Name, pcount, ptimer.Elapsed);
            }

            Console.WriteLine("***");
        }

    }
}
