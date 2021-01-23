using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    /// <summary>
    /// Configuration for a single raid event that can be tracked.
    /// </summary>
    public class RaidTemplate
    {
        public string Zone { get; set; }
        public string Name { get; set; }
        public string[] Mobs { get; set; }
        public string[] EndsOnDeath { get; set; }
        //public string[] StartsOnText { get; set; }
        //public string[] EndsOnText { get; set; }
        //public int TimeOut { get; set; }
    }

    /// <summary>
    /// An extension of the MergedFightInfo class with the ability to merge fights in any order.
    /// </summary>
    public class RaidFightInfo : MergedFightInfo
    {
        private List<FightInfo> fights = new List<FightInfo>();

        public RaidTemplate Template;

        public override void Merge(FightInfo f)
        {
            // add to internal list and delay actual merge until the Finish is called
            fights.Add(f);

            // raid needs a timestamp to keep from getting timed out
            if (StartedOn == DateTime.MinValue || StartedOn > f.StartedOn)
                StartedOn = f.StartedOn;
        }

        public override void Finish()       
        {
            // merge algorithm only works when fights are sorted in starting order
            foreach (var f in fights.OrderBy(x => x.StartedOn))
                base.Merge(f);

            base.Finish();

            Name = Template.Name;
            Zone = Template.Zone;
            CohortCount = MobCount;

            // i'm not sure if we only want to count killed mobs
            //MobCount = fights.Where(x => x.Status == FightStatus.Killed).Count();
        }
    }
}
