using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace EQLogParser
{
    /// <summary>
    /// An extension of the FightInfo class with some extra state tracking used in merging fights together.
    /// </summary>
    public class MergedFightInfo : FightInfo
    {
        // keep a map of which tick each interval contains
        private List<int> intervals = new List<int>() { 0 };
        private List<FightInfo> fights = new List<FightInfo>();

        public MergedFightInfo()
        {

        }

        public MergedFightInfo(IEnumerable<FightInfo> fights) 
        {
            foreach (var f in fights)
                Merge(f);
        }

        /// <summary>
        /// Merge data from another fight into this fight.
        /// The supplied fight must have started after any previously merged fights.
        /// </summary>
        public virtual void Merge(FightInfo f)
        { 
            if (Target == null || String.IsNullOrEmpty(Target.Name))
            {
                Target = new FightParticipant() { Name = "X" };
                Name = "X";
                Zone = f.Zone;
                Party = f.Party;
                Player = f.Player;
                Server = f.Server;
                StartedOn = f.StartedOn;
                UpdatedOn = f.UpdatedOn;
                Status = FightStatus.Merged;
                Duration = 1;
            }

            MobCount += 1;
            fights.Add(f);

            if (Zone != f.Zone)
                Zone = "Multiple Zones";

            //Target.Merge(f.Target, 0);

            if (StartedOn > f.StartedOn)
                throw new Exception("Fights must be merged in order of start time.");
            //if (StartedOn > f.StartedOn)
            //    StartedOn = f.StartedOn;
            if (UpdatedOn < f.UpdatedOn)
                UpdatedOn = f.UpdatedOn;



            /*
            for single fights each tick has a 1 to 1 mapping to intervals.
            however when merging multiple fights together we have to deal with
            ticks that may overlap intervals (which we need to merge) and
            gaps between fights (which we want to remove).

            here are some scenarios that will occur:

                       1111
            12345678  90123   -- intervals we want to assign
            ---------------   -- the tick timeline
            1111              -- first fight
               22222          -- 2nd overlaps with 1st
                      33333   -- 3rd has gap after 2nd
                       44     -- 4th ends before 3rd

            fights must be sorted in starting order for this algorithm to work
            */

            // starting interval
            var interval = 0;
            // get tick relative to first tick of first fight
            var head = GetTick(f.StartedOn);
            var tail = GetTick(f.UpdatedOn);
            Debug.Assert(tail >= head);

            // has this tick already been seen?
            // if so backtrack so we can find where it belongs
            if (head <= intervals[^1])
            {
                interval = intervals.Count - 1;
                while (intervals[interval] > head)
                    interval--;
            }
            // if it hasn't been seen then add it as a new interval
            else
            {
                intervals.Add(head);
                interval = intervals.Count - 1;
            }

            // handle the tail
            var current = intervals[^1];
            while (intervals[^1] < tail)
                intervals.Add(++current);

            // get offset for shifting buff timeline
            var time = interval * 6 + (f.StartedOn.Second % 6) - (StartedOn.Second % 6);

            foreach (var p in f.Participants)
            {
                var _p = AddParticipant(p.Name);
                _p.Class = p.Class;
                _p.PetOwner = p.PetOwner;
                _p.Level = p.Level;
                _p.Duration += p.Duration; // this doesn't handle overlaps well but gets corrected in the finish() method
                //p.Duration = (int)(UpdatedOn - StartedOn).TotalSeconds; // this is just the same as fight.duration and pointless
                _p.Merge(p, interval, time);
            }

            Target.Merge(f.Target, interval, time);

        }

        public override void Finish()
        {
            base.Finish();

            MobCount = fights.Count;

            MobNotes = String.Join(", ", fights.GroupBy(x => x.Name)
                .OrderByDescending(x => x.Sum(y => y.HP))
                .Select(x => String.Format("{0} x {1}", x.Count(), x.Key)));

            Name = $"* {MobCount} combined mobs";

            // todo: maybe if the combined fight is really long then reduce the number of interals by using 1 minute intervals?

            foreach (var p in Participants)
            {
                //p.Duration = intervals.Count * 6;

                // count non AFK ticks
                var ticks = 0;
                for (var i = 0; i < intervals.Count; i++)
                {
                    if ((p.DPS != null && p.DPS.Count > i && p.DPS[i] != 0) || 
                        (p.HPS != null && p.HPS.Count > i && p.HPS[i] != 0) || 
                        (p.TankDPS != null && p.TankDPS.Count > i && p.TankDPS[i] != 0))
                    {
                        ticks += 1;
                        continue;
                    }
                }
                p.Duration = ticks * 6;
            }

            // this rounds the duration up to 6 second intervals for each fight that was merged
            Duration = Math.Max(Target.DPS.Count, Target.TankDPS.Count) * 6;
            if (Elapsed < Duration)
                Duration = Elapsed;
            if (Duration < 1)
                Duration = 1;

        }

    }
}
