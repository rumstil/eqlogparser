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
            Console.Error.WriteLine("Loading spells...");
            var spells = new SpellParser();
            spells.Load("d:/games/everquest/spells_us.txt");
                
            var open = LogOpenEvent.FromFileName("d:/games/everquest/logs/eqlog_Rumstil_erollisi.txt");

            var parser = new LogParser();
            parser.Player = open.Player;
            //parser.MinDate = DateTime.MinValue;
            //parser.MinDate = DateTime.Today.AddDays(-1).ToUniversalTime();

            var fights = new FightTracker(spells);
            fights.HandleEvent(open);
            fights.OnFightStarted += ShowFight;
            fights.OnFightFinished += ShowFight;

            var timer = Stopwatch.StartNew();
            var reader = File.OpenText(open.Path);
            while (true)
            {
                var s = reader.ReadLine();
                if (s == null)
                    break;

                // pass text to the parser and convert to an event
                var e = parser.ParseLine(s);
                if (e == null)
                    continue;

                // pass event to the fight tracker
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

            f.WriteAll(Console.Out);
        }

    }
}
