using System.Collections.Generic;


namespace EQLogParser
{
    /// <summary>
    /// An aggregate of any spell that landed or was cast during a fight.
    /// </summary>
    public class FightSpell
    {
        /// <summary>
        /// Can be "hit", "heal"
        /// </summary>
        public string Type { get; set; }
        public string Name { get; set; }
        public int ResistCount { get; set; }
        public int HitCount { get; set; }
        public int HitSum { get; set; }
        public int HitMax { get; set; }
        public int CritCount { get; set; }
        public int CritSum { get; set; }
        public int TwinCount { get; set; }
        public int FullHitSum { get; set; } // currently only used for heals

        /// <summary>
        /// Each time the spell is cast an entry is added with the # seconds from start of fight
        /// </summary>
        //public List<int> Times = new List<int>();

        public void Merge(FightSpell x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
            CritSum += x.CritSum;
            CritCount += x.CritCount;
            TwinCount += x.TwinCount;
            FullHitSum += x.FullHitSum;
            if (HitMax < x.HitMax)
                HitMax = x.HitMax;
        }

    }
}
