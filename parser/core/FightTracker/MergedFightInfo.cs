﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace EQLogParser
{
    /// <summary>
    /// This is an extension of the FightInfo class with some extra state tracking used in merging fights together.
    /// </summary>
    public class MergedFightInfo : FightInfo
    {
        // keep a map of which tick each interval contains
        private List<int> intervals = new List<int>() { 0 };

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
        /// </summary>
        public void Merge(FightInfo f)
        {
            if (Target == null)
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
            }

            MobCount += 1;

            if (Zone != f.Zone)
                Zone = "Multiple Zones";

            Target.Merge(f.Target, 0);

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

            foreach (var _p in f.Participants)
            {
                var p = AddParticipant(_p.Name);
                p.Class = _p.Class;
                p.PetOwner = _p.PetOwner;
                p.Level = _p.Level;
                p.Duration += _p.Duration; // this doesn't handle overlaps well
                //p.Duration = (int)(UpdatedOn - StartedOn).TotalSeconds; // this is just the same as fight.duration and pointless
                p.Merge(_p, interval);
            }

        }

        public override void Finish()
        {
            base.Finish();

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
        }

    }
}