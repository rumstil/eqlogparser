using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EQLogParser;

/*

This is a sample console app to demonstrate usage of the parser in it's most minimalist form.
I also use this for debugging so it has some hardcoded paths from my PC.

If you want to see an app with an actual user interface, try the LogSync project.

*/

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // load spells to give the trackers more context when processing log files
            // this is optional and can be skipped
            Console.Error.WriteLine("Loading spells...");
            var spells = new SpellParser();
            spells.Load("d:/games/everquest/spells_us.txt");
                
            // generate an open event that stores the player name, server name, and file path
            // all trackers should receive this as their first event to signal that a new log file is being processed
            var open = LogOpenEvent.FromFileName("d:/games/everquest/logs/eqlog_Rumstil_erollisi.txt");

            // create a CharTracker to help the FightTracker determine friends/foes
            var chars = new CharTracker(spells);
            chars.HandleEvent(open);

            // create a FightTracker to build fight summaries from various combat events
            var fights = new FightTracker(spells, chars);
            fights.HandleEvent(open);
            fights.OnFightStarted += ShowFight;
            fights.OnFightFinished += ShowFight;

            // create a log parser for converting log lines into events that can be passed to the trackers
            var parser = new LogParser();
            parser.Player = open.Player;
            //parser.MinDate = DateTime.MinValue;
            //parser.MinDate = DateTime.Today.AddDays(-1).ToUniversalTime();

            var timer = Stopwatch.StartNew();
            var reader = File.OpenText(open.Path);
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;

                // pass line to the parser and convert to an event
                // lines that cannot be parsed will be returned as nulls
                var e = parser.ParseLine(line);
                if (e == null)
                    continue;

                // pass event to the trackers
                chars.HandleEvent(e);
                fights.HandleEvent(e);

                //if (e is LogRawEvent) Console.WriteLine(e);
            };

            fights.ForceFightTimeouts();
            Console.Error.WriteLine("Parse completed in {0}", timer.Elapsed);
        }

        /// <summary>
        /// Dump a fight summary to the console.
        /// </summary>
        static void ShowFight(FightInfo f)
        {
            if (f.Status == FightStatus.Active)
            {
                Console.WriteLine("\n--- {0} --- started at {1}", f.Target.Name, f.StartedOn.ToLocalTime());
                return;
            }

            Console.Out.WriteFightDetails(f);
        }

    }
}
