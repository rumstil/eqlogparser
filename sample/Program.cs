using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EQLogParser;

namespace logdump
{

    class Program
    {
        static Dictionary<string, int> MostCommon = new Dictionary<string, int>();

        static FightTracker fights;

        static void Main(string[] args)
        {
            var timer = Stopwatch.StartNew();

            var spells = new SpellParser();
            spells.Load("d:/games/everquest/spells_us.txt");
            Console.Error.WriteLine("Spells loaded in {0}", timer.Elapsed);
            timer.Restart();

            var file = new LogReader("d:/games/everquest/logs/eqlog_Rumstil_erollisi.txt");
            //var file = new LogReader("d:/games/everquest/logs/eqlog_Rumstil_test.txt");
            //var file = new LogReader("d:/games/everquest/logs/eqlog_Fourier_erollisi.txt");
            //var file = new LogReader("d:/games/everquest/logs/eqlog_Fourier_test.txt");

            var parser = new LogParser(file);
            parser.MinDate = DateTime.MinValue;
            //parser.MinDate = DateTime.Today.AddDays(-14);
            //parser.MinDate = DateTime.Parse("2/25/2019 10:06:44 PM").ToUniversalTime();
            parser.OnEvent += ShowLog;

            fights = new FightTracker();
            fights.Chars.GetSpellClass = spells.GetClass;
            //fights.OnFightStarted += f => Console.WriteLine("\n--- Started {0}", f.Target.Name);
            fights.OnFightFinished += ShowFight;
            parser.OnEvent += fights.HandleEvent;


            file.ReadAllLines();
            fights.ForceFightTimeouts();

            Console.Error.WriteLine("Parse completed in {0}", timer.Elapsed);
            Console.Error.WriteLine("Fights: {0}", fights.Fights.Count);

            // keep reading log file
            //file.StartWatcherThread();
            //Console.Error.WriteLine("Watching log file... press any key to quit");
            //Console.ReadKey();
            //file.StopWatcherThread();


            //var common = MostCommon.OrderByDescending(x => x.Value).Take(20);
            //foreach (var item in common)
            //    Console.Error.WriteLine("{0} {1}", item.Key, item.Value);

        }

        /// <summary>
        /// Dump a log event to the console. All log events override the ToString() method to generate a nicer log message.
        /// </summary>
        static void ShowLog(LogEvent log)
        {
            //if (log is LogRawEvent)
            //    Console.WriteLine(log);

            //if (log is LogHitEvent)
            //    Console.WriteLine(log);

            //if (log is LogDeathEvent)
            //    Console.WriteLine(log);

            //if (log is LogCastingEvent cast && cast.Source == "Annjule")
            //    Console.WriteLine(log);

        }

        /// <summary>
        /// Dump a fight summary to the console.
        /// </summary>
        static void ShowFight(Fight f)
        {
            //var duration = (f.Finished.Value - f.Started).TotalSeconds + 1;

            Console.WriteLine();
            Console.WriteLine("=== {0} - {1:N0} HP - {2}s at {3}", f.Name, f.Target.InboundHitSum, f.Duration, f.Started.ToLocalTime(), f.Zone);
            foreach (var p in f.Participants)
            {
                var pct = (float)p.OutboundHitSum / f.Target.InboundHitSum;

                Console.WriteLine(" {0} {1:P0} {2}-{3}", p, pct, p.FirstAction, p.LastAction);
                //Console.WriteLine(" {0,-25} {1,12:N0} {2,8:N0} DPS  {3:P0} {4}", p.FullName, p.SourceHitSum, p.SourceHitSum / duration, pct, p.SourceHitCount);

                Console.WriteLine("   {0,-10} {1,10:N0} / {2,6:N0} DPS", "total",  p.OutboundHitSum, p.OutboundHitSum / f.Duration);
                foreach (var ht in p.AttackTypes)
                    Console.WriteLine("   {0,-10} {1,10:N0} / {2,6:N0} DPS", ht.Type, ht.HitSum, ht.HitSum / f.Duration);

                //if (p.TargetMissCount > 0)
                //    Console.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*total*", p.TargetMissCount, p.TargetHitCount + p.TargetMissCount, (double)p.TargetMissCount / (p.TargetHitCount + p.TargetMissCount));
                foreach (var d in p.DefenseTypes)
                    Console.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*" + d.Type + "*", d.Count, d.Attempts, (double)d.Count / d.Attempts);

                Console.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*hit*", p.InboundHitCount, p.InboundHitCount + p.InboundMissCount, (double)p.InboundHitCount / (p.InboundHitCount + p.InboundMissCount));

                foreach (var s in p.Spells)
                {
                    //Console.WriteLine("   cast {0}: {1} for {2:N0} damage", s.Name, String.Join(", ", s.Times.Select(x => x.ToString())), s.Damage);
                    Console.WriteLine("   cast {0}: {1:N0} damage, {2:N0} healed", s.Name, s.HitSum, s.HealSum);
                }

                if (p.OutboundHealSum > 0)
                    Console.WriteLine("   healed     {0,10:N0}", p.OutboundHealSum);

            }
            Console.WriteLine();

            var json = JsonConvert.SerializeObject(f, Formatting.Indented);
            //var json = JsonConvert.SerializeObject(f);
            //File.WriteAllText("c:/proj/eq/logparser/server/static/json/" + f.ID + ".json", json);
            File.WriteAllText("/Proj/eq/logparser/localhost/wwwroot/json/" + f.ID + ".json", json);
            
            var web = new WebClient();
            // write to realtime database
            //web.UploadString("https://eqlogdb.firebaseio.com/fights.json", json);
            // write to cloud firestore (not working)
            //web.UploadString("https://firestore.googleapis.com/v1/projects/eqlogdb/databases/(default)/documents/fights", json);
        }


    }
}
