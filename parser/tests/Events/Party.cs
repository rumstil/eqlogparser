using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogPartyEventTests
    {
        const string PLAYER = "Bob";

        private LogPartyEvent Parse(string text)
        {
            return LogPartyEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Party()
        {
            var party = Parse("A lizard hireling has joined the group.");
            Assert.NotNull(party);
            Assert.Equal("A lizard hireling", party.Name);
            Assert.Equal(PartyStatus.GroupJoined, party.Status);

            party = Parse("Fourier has left the group.");
            Assert.NotNull(party);
            Assert.Equal("Fourier", party.Name);
            Assert.Equal(PartyStatus.GroupLeft, party.Status);

            party = Parse("You remove Rumstil from the party.");
            Assert.NotNull(party);
            Assert.Equal("Rumstil", party.Name);
            Assert.Equal(PartyStatus.GroupLeft, party.Status);

            party = Parse("You have been removed from the group.");
            Assert.NotNull(party);
            Assert.Equal(PLAYER, party.Name);
            Assert.Equal(PartyStatus.GroupLeft, party.Status);

            party = Parse("You have joined the raid.");
            Assert.NotNull(party);
            Assert.Equal(PLAYER, party.Name);
            Assert.Equal(PartyStatus.RaidJoined, party.Status);

            party = Parse("You were removed from the raid.");
            Assert.NotNull(party);
            Assert.Equal(PLAYER, party.Name);
            Assert.Equal(PartyStatus.RaidLeft, party.Status);
        }

    }
}
