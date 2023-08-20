namespace EQLogParser
{
    /// <summary>
    /// An aggregate of single damage type that occurs in a fight: melee hit, ranged hit, dd, dot, etc...
    /// </summary>
    public class FightHit
    {
        public string Type { get; set; }
        public int HitCount { get; set; }
        public long HitSum { get; set; }
        public int CritCount { get; set; }
        public long CritSum { get; set; }
        public int HitMax { get; set; }

        public void Merge(FightHit x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
            CritSum += x.CritSum;
            CritCount += x.CritCount;
            if (HitMax < x.HitMax)
                HitMax = x.HitMax;
        }
    }
}
