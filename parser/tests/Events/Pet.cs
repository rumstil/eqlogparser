using Xunit;

namespace EQLogParser
{
    public class LogPetChatEventTests
    {
        private LogPetChatEvent Parse(string text)
        {
            return LogPetChatEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_Pet_Leader()
        {
            var pet = Parse("Xebn says, 'My leader is Fourier.'");
            Assert.NotNull(pet);
            Assert.Equal("Xebn", pet.Name);
            Assert.Equal("Fourier", pet.Owner);
        }

    }
}
