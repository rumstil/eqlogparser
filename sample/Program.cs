using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var timer = Stopwatch.StartNew();
            Console.Error.WriteLine("Loading spells...");
            var spells = new SpellParser();
            spells.Load("d:/games/everquest/spells_us.txt");
            Console.Error.WriteLine("Spells loaded in {0}", timer.Elapsed);
            timer.Restart();
                
            var file = new LogReader("d:/games/everquest/logs/eqlog_Rumstil_erollisi.txt");

            var parser = new LogParser();
            parser.Player = LogOpenEvent.GetPlayerFromFileName(file.Path);
            //parser.MinDate = DateTime.MinValue;
            //parser.MinDate = DateTime.Today.AddDays(-1).ToUniversalTime();
            //parser.MaxDate = DateTime.Parse("7/13/2020 11:00 PM").ToUniversalTime();

            var fights = new FightTracker(spells);
            fights.HandleEvent(LogOpenEvent.FromFileName(file.Path));
            fights.OnFightStarted += f =>
            {
                ShowFight(f);
            };
            fights.OnFightFinished += f =>
            {
                ShowFight(f);
            };

            while (true)
            {
                var s = file.ReadLine();
                if (s == null)
                    break;

                // pass text to the parser and convert to an event
                var e = parser.ParseLine(s);

                // pass event to the fight tracker
                if (e != null)
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
