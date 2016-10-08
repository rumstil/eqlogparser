using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EQLogParser;



namespace logdump
{

    class Program
    {
        static Dictionary<string, int> MostCommon = new Dictionary<string, int>();

        static FightTracker fights;

        static void Main(string[] args)
        {
            Stopwatch timer = Stopwatch.StartNew();

            var file = File.OpenText("d:/games/everquest/logs/eqlog_Rumstil_erollisi.txt");
            //file.BaseStream.Position = file.BaseStream.Length - 5000000;


            var parser = new LogParser("Rumstil");
            //parser.MinDate = DateTime.Today.AddDays(-14);
            //parser.OnEvent += LogHandler;
            parser.OnZone += LogHandler;
            parser.OnPlayerFound += LogHandler;
            //parser.OnPetOwner += LogHandler;
            //parser.OnFightHit += LogHandler;
            //parser.OnDeath += LogHandler;
            //parser.OnChat += LogHandler;
            //parser.OnHeal += LogHandler;
            parser.OnItemLooted += LogHandler;


            fights = new FightTracker(parser);
            fights.OnFightStarted += f => Console.WriteLine("--- {0}", f.Opponent.Name);
            fights.OnFightFinished += FightHandler;

            //var faction = new FactionTracker(parser);

            //parser.OnBeforeEvent += CheckMostCommon
            //parser.MaxDate = DateTime.Parse("5/19/2016 10:35:43");
            //parser.OnAfterEvent += DebugCheck;

            parser.LoadFromFile(file);
            Console.Error.WriteLine("Parse completed in {0} - {1:N0} bytes ", timer.Elapsed, file.BaseStream.Length);
            Console.Error.WriteLine("Fights: {0}", fights.Fights.Count);

            //foreach (var f in faction)
            //{
            //    Console.Error.WriteLine("{0}", f);
            //}

            //var common = MostCommon.OrderByDescending(x => x.Value).Take(20);
            //foreach (var item in common)
            //    Console.Error.WriteLine("{0} {1}", item.Key, item.Value);


        }

        static void DebugCheck(RawLogEvent log)
        {
            if (log.RawText.Contains("Lebekn"))
            {
                var p = fights.Fights[0].Participants.FirstOrDefault(x => x.Name == "Lebekn");
                if (p == null)
                    p = new Combatant("---");
                Console.Error.WriteLine("{0} {1}", p.SourceHitSum, log.RawText);
            }
        }

        static void CheckMostCommon(RawLogEvent log)
        {
            if (!MostCommon.ContainsKey(log.RawText))
                MostCommon[log.RawText] = 0;

            MostCommon[log.RawText] = MostCommon[log.RawText] + 1;
        }

        /// <summary>
        /// Dump a log event to the console. All log events override the ToString() method to generate a nicer log message.
        /// </summary>
        static void LogHandler(LogEvent log)
        {
            Console.WriteLine(log);
        }

        /// <summary>
        /// Dump a fight summary to the console.
        /// </summary>
        static void FightHandler(Fight f)
        {
            var duration = (f.Finished.Value - f.Started).TotalSeconds + 1;

            Console.WriteLine();
            Console.WriteLine("=== {0} - {1:N0} HP - {2}s at {3}", f.Opponent.Name, f.Opponent.TargetHitSum, duration, f.Started, f.Zone);
            foreach (var p in f.Participants)
            {
                var pct = (float)p.SourceHitSum / f.Opponent.TargetHitSum;
                if (p == f.Opponent)
                    pct = 0;
                Console.WriteLine(" {0,-25} {1,12:N0} {2,8:N0} DPS  {3:P0} {4}", p.Name, p.SourceHitSum, p.SourceHitSum / duration, pct, p.SourceHitCount);
                //foreach (var c in p.Casting)
                //    Console.WriteLine("    {0} {1}", c.Timestamp, c.Spell);
                foreach (var ht in p.AttackTypes)
                    Console.WriteLine("   {0,-10} {2,10:N0} / {1,3:N0} [T]    {4,10:N0} / {3,3:N0} [N]    {6,10:N0} / {5,3:N0} [C] ", ht.Type, ht.NormalHitCount + ht.CritHitCount, ht.NormalHitSum + ht.CritHitSum, ht.NormalHitCount, ht.NormalHitSum, ht.CritHitCount, ht.CritHitSum);
            }
            Console.WriteLine();
        }


    }
}
