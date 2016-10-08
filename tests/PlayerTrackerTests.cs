using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace EQLogParser
{
    public class PlayerTrackerTests
    {
        [Fact]
        public void GetOwner()
        {
            var pt = new PlayerTracker();
            Assert.Null(pt.GetOwner("Rumstil"));
            Assert.Equal("Rumstil", pt.GetOwner("Rumstil`s pet"));
            Assert.Equal("Rumstil", pt.GetOwner("Rumstil`s warder"));
        }
    }
}
