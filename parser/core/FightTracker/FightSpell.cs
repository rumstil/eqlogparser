using System.Collections.Generic;


namespace EQLogParser
{
    /// <summary>
    /// An aggregate of any spell that landed or was cast during a fight.
    /// </summary>
    public class FightSpell
    {
        /// <summary>
        /// Can be "hit", "heal", or "buff"
        /// </summary>
        public string Type { get; set; }
        public string Name { get; set; }
        public int ResistCount { get; set; }
        public int HitCount { get; set; }
        public int HitSum { get; set; }
        //public int HitMin;
        //public int HitMax;
        public int CritCount { get; set; }
        public int CritSum { get; set; }
        public int TwinCount { get; set; }
        public int HealGross { get; set; }

        /// <summary>
        /// Each time the spell is cast an entry is added with the # seconds from start of fight
        /// </summary>
        public List<int> Times = new List<int>();

        public void Merge(FightSpell x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
            CritSum += x.CritSum;
            CritCount += x.CritCount;
            TwinCount += x.TwinCount;
            HealGross += x.HealGross;
            //HealSum += x.HealSum;
            //HealCount += x.HealCount;
            //if (HitMin > x.HitMin || HitMin == 0)
            //    HitMin = x.HitMin;
            //if (HitMax < x.HitMax)
            //    HitMax = x.HitMax;
            //if (HealMax < x.HealMax)
            //    HealMax = x.HealMax;
        }

    }
}
