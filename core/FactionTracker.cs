using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EQLogParser
{
    public class FactionInfo
    {
        public string Name;
        public int Count;
        public int Sum;

        public override string ToString()
        {
            return String.Format("{0}: {1}", Name, Sum);
        }
    }

    /// <summary>
    /// This class consumes parser events to track faction changes.
    /// </summary>
    public class FactionTracker : IEnumerable<FactionInfo>
    {
        private string Zone;
        private readonly List<FactionInfo> Factions = new List<FactionInfo>();

        public FactionTracker()
        {

        }

        public FactionTracker(LogParser parser) : base()
        {
            Subscribe(parser);
        }

        public void Subscribe(LogParser parser)
        {
            parser.OnFaction += TrackFaction;
            parser.OnZone += TrackZone;
        }

        public void Unsubscribe(LogParser parser)
        {
            parser.OnFaction -= TrackFaction;
            parser.OnZone -= TrackZone;
        }

        private void TrackFaction(FactionEvent faction)
        {
            var f = Factions.FirstOrDefault(x => x.Name == faction.Name);
            if (f == null)
            {
                f = new FactionInfo { Name = faction.Name };
                Factions.Add(f);
            }
            f.Count += 1;
            f.Sum += faction.Change;
        }

        private void TrackZone(ZoneEvent zone)
        {
            Zone = zone.Name;
        }

        public IEnumerator<FactionInfo> GetEnumerator()
        {
            return Factions.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
