namespace EQLogParser
{
    /// <summary>
    /// An aggregate of heals that landed on a player.
    /// </summary>
    public class FightHeal
    {
        public string Target { get; set; }
        public int HitCount { get; set; }
        public int HitSum { get; set; }

        public void Merge(FightHeal x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
        }
    }
}
