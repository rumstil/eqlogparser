using EQLogParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EQLogParserTests.Tracker
{
    public class FakeSpellParser : ISpellLookup
    {
        public List<SpellInfo> Spells { get; set; } = new List<SpellInfo>();

        public SpellInfo GetSpell(string name)
        {
            return Spells.FirstOrDefault(x => x.Name == name);
        }
    }
}
