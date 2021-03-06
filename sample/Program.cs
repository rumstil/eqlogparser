﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using EQLogParser;


namespace logdump
{

    class Program
    {
        static string LogPath = "d:/games/everquest/logs/";

        static string JsonPath = "c:/Proj/eq/raidloot2/web/wwwroot/fights/";

        static void Main(string[] args)
        {
            //var serviceProvider = new ServiceCollection();
            //DeleteJsonFiles();

            var timer = Stopwatch.StartNew();
            Console.Error.WriteLine("Loading spells...");
            var spells = new SpellParser();
            spells.Load("d:/games/everquest/spells_us.txt");
            Console.Error.WriteLine("Spells loaded in {0}", timer.Elapsed);
            timer.Restart();

            //var file = new LogReader("d:/games/everquest/logs/backup/sample.txt");
            var file = new LogReader(LogPath + "eqlog_Rumstil_erollisi.txt");
            //var file = new LogReader(LogPath + "eqlog_Rumstil_test.txt");
            //var file = new LogReader(LogPath + "eqlog_Fourier_erollisi.txt");


            var parser = new LogParser();
            parser.Player = LogParser.GetPlayerFromFileName(file.Path);
            //parser.MinDate = DateTime.MinValue;
            //parser.MinDate = DateTime.Today.AddDays(-1).ToUniversalTime();
            //parser.MinDate = DateTime.Parse("7/13/2020").ToUniversalTime();
            //parser.MaxDate = DateTime.Parse("7/13/2020 11:00 PM").ToUniversalTime();
            //parser.OnEvent += ShowLog;

            var completed = new List<FightInfo>();
            var fights = new FightTracker(spells);
            fights.OnFightStarted += f =>
            {
                ShowFight(f);
            };
            fights.OnFightFinished += f =>
            {
                ShowFight(f);
                completed.Add(f);
            };


            while (true)
            {
                var s = file.ReadLine();
                if (s == null)
                    break;
                var e = parser.ParseLine(s);
                if (e != null)
                    fights.HandleEvent(e);
            };


            //TimeParsers(file);

            fights.ForceFightTimeouts();
            Console.Error.WriteLine("Parse completed in {0}", timer.Elapsed);

            //var total = new MergedFightInfo();
            //var temp = completed;
            //foreach (var f in temp)
            //    total.Merge(f);
            //total.Finish();
            //ShowFight(total);

            //Console.Error.WriteLine("Fights: {0}", list.Count);

            // keep reading log file
            //Console.Error.WriteLine("Watching log file... press any key to quit");
            //Console.ReadKey();
            //file.StopWatcherThread();
        }

        static void TimeParsers(LogReader file)
        {
            var parser = new LogParser();
            parser.Player = LogParser.GetPlayerFromFileName(file.Path);

            // load raw log lines once 
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
            Console.Error.WriteLine("Loaded {0} events", events.Count);

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
        /// Dump a log event to the console. All log events override the ToString() method to generate a nicer log message.
        /// </summary>
        static void ShowLog(LogEvent log)
        {
            Console.WriteLine(log);

            //if (log is LogRawEvent)
            //    Console.WriteLine(log);

            //if (log is LogHitEvent)
            //    Console.WriteLine(log);

            //if (log is LogDeathEvent)
            //    Console.WriteLine(log);

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

            //if (f.HP < 1000000)
            //    return;

            //f.Anonymize();
            //var duration = (f.Finished.Value - f.Started).TotalSeconds + 1;

            //f.WriteAll(Console.Out);
            f.WriteNotes(Console.Out);
        }

        static void SaveFight(FightInfo f)
        { 
            //var json = JsonSerializer.Serialize(f);
            //var path = JsonPath + f.ID + ".json";
            //Console.WriteLine(path);
            //File.WriteAllText(path, json);
        }

        static void DeleteJsonFiles()
        {
            var dir = new DirectoryInfo(JsonPath);
            foreach (var f in dir.GetFiles("*.json"))
                f.Delete();
        }
    }
}
