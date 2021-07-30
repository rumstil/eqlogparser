using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EQLogParser
{
    /// <summary>
    /// Extensions for writing fight information out to a console.
    /// </summary>
    public static class FightUtils
    {
        /// <summary>
        /// Output a detailed summary of each players damage, tanking, and spells to a TextWriter.
        /// </summary>
        public static void WriteFightDetails(this TextWriter writer, FightInfo fight)
        {
            writer.WriteLine();
            writer.WriteLine("**{0}** {1:N0} HP in {2}s at {3}", fight.Name, fight.Target.InboundHitSum, fight.Duration, fight.StartedOn.ToLocalTime(), fight.Zone);
            if (fight.Participants.Count == 0)
                return;

            var top = fight.Participants.Max(x => x.OutboundHitSum);
            foreach (var p in fight.Participants)
            {
                var pct = (float)p.OutboundHitSum / top;

                writer.WriteLine(" {0} {1:P0} {2} to {3}", p, pct, p.FirstAction?.ToLocalTime().ToString("T"), p.LastAction?.ToLocalTime().ToString("T"));

                writer.WriteLine("   {0,-10} {1,11:N0} / {2,6:N0} DPS", "total", p.OutboundHitSum, p.OutboundHitSum / fight.Duration);
                foreach (var ht in p.AttackTypes)
                    writer.WriteLine("   {0,-10} {1,11:N0} / {2,6:N0} DPS", ht.Type, ht.HitSum, ht.HitSum / fight.Duration);

                foreach (var d in p.DefenseTypes)
                    writer.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*" + d.Type + "*", d.Count, d.Attempts, (double)d.Count / d.Attempts);

                writer.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*hit*", p.InboundHitCount, p.InboundHitCount + p.InboundMissCount, (double)p.InboundHitCount / (p.InboundHitCount + p.InboundMissCount));

                foreach (var s in p.Spells)
                {
                    writer.WriteLine("   spell {0} {1}: {2:N0}", s.Type, s.Name, s.HitSum);
                }

                foreach (var h in p.Heals)
                {
                    writer.WriteLine("   healed {0}: {1:N0}", h.Target, h.HitSum);
                }

            }
        }

        /// <summary>
        /// Output a short summary of each players damage to a TextWriter.
        /// </summary>
        public static void WriteFightSummary(this TextWriter writer, FightInfo fight)
        {
            writer.WriteLine();
            writer.WriteLine("** {0} ** {1} HP in {2}s at {3}", fight.Name, FormatNum(fight.Target.InboundHitSum), fight.Duration, fight.StartedOn.ToLocalTime().ToShortTimeString(), fight.Zone);
            if (fight.Participants.Count == 0)
                return;

            var top = fight.Participants.Max(x => x.OutboundHitSum);
            foreach (var p in fight.Participants.Take(10).Where(x => x.OutboundHitSum > 0))
            {
                // percent = share of damage vs mob
                //var pct = (float)p.OutboundHitSum / Target.InboundHitSum;
                // percent = damage relative to top player
                var pct = (float)p.OutboundHitSum / top;
                writer.WriteLine(" {0,-20} --- {1,4:P0} {2,8} / {3,5} DPS", p, pct, FormatNum(p.OutboundHitSum), FormatNum(p.OutboundHitSum / fight.Duration));
            }
        }

        public static string FormatNum(long n)
        {
            if (n >= 1_000_000_000)
                return (n / 1_000_000_000F).ToString("F2") + 'B';
            if (n >= 1_000_000)
                return (n / 1_000_000F).ToString("F2") + 'M';
            if (n >= 10_000)
                return (n / 1000F).ToString("F0") + 'K';
            if (n > 1000)
                return (n / 1000F).ToString("F1") + 'K';
            return n.ToString();
        }

    }
}
