using EQLogParser;
using Xunit;


namespace EQLogParserTests.Event
{
    public class LogCoinEventTests
    {
        const string PLAYER = "Bob";

        private LogCoinEvent Parse(string text)
        {
            return LogCoinEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Corpse_Loot()
        {
            var coin = Parse("You receive 15 platinum and 7 gold from the corpse.");
            Assert.NotNull(coin);
            Assert.Equal(15, coin.Platinum);
            Assert.Equal(7, coin.Gold);
            Assert.Equal(0, coin.Silver);
            Assert.Equal(0, coin.Copper);
        }

        [Fact]
        public void Parse_Auto_Split()
        {
            var coin = Parse("You receive 9 gold as your split.");
            Assert.NotNull(coin);
            Assert.Equal(0, coin.Platinum);
            Assert.Equal(9, coin.Gold);
            Assert.Equal(0, coin.Silver);
            Assert.Equal(0, coin.Copper);
        }

        [Fact]
        public void Parse_Auto_Lucky_Split()
        {
            var coin = Parse("You receive 78 platinum, 1 gold, 7 silver and 1 copper as your split (with a lucky bonus).");
            Assert.NotNull(coin);
            Assert.Equal(78, coin.Platinum);
            Assert.Equal(1, coin.Gold);
            Assert.Equal(7, coin.Silver);
            Assert.Equal(1, coin.Copper);
        }

        [Fact]
        public void Parse_Dead_Split()
        {
            var coin = Parse("Alive group members received 144 platinum and 4 silver as their share of the split from the corpse.");
            Assert.NotNull(coin);
            Assert.Equal(144, coin.Platinum);
            Assert.Equal(0, coin.Gold);
            Assert.Equal(4, coin.Silver);
            Assert.Equal(0, coin.Copper);
        }


    }
}
