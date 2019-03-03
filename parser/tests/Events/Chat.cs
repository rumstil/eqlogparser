using Xunit;

namespace EQLogParser
{
    public class LogChatEventTests
    {
        const string PLAYER = "Bob";

        private LogChatEvent Parse(string text)
        {
            return LogChatEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_All()
        {
            var chat = Parse("Rumstil says, 'adventure'");
            Assert.NotNull(chat);
            Assert.Equal("Rumstil", chat.Source);
            Assert.Equal("say", chat.Channel);
            Assert.Equal("adventure", chat.Message);

            chat = Parse("You say, 'fish'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("say", chat.Channel);
            Assert.Equal("fish", chat.Message);

            chat = Parse("Fred tells you, 'hola!'");
            Assert.NotNull(chat);
            Assert.Equal("Fred", chat.Source);
            Assert.Equal("tell", chat.Channel);
            Assert.Equal("hola!", chat.Message);
            
            chat = Parse("You told Fred, 'hi'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("tell", chat.Channel);
            Assert.Equal("hi", chat.Message);

            chat = Parse("Dude tells the guild, 'k thx bye'");
            Assert.NotNull(chat);
            Assert.Equal("Dude", chat.Source);
            Assert.Equal("guild", chat.Channel);
            Assert.Equal("k thx bye", chat.Message);

            chat = Parse("You say to your guild, 'rofl'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("guild", chat.Channel);
            Assert.Equal("rofl", chat.Message);

            chat = Parse("Dude tells the group, 'lol'");
            Assert.NotNull(chat);
            Assert.Equal("Dude", chat.Source);
            Assert.Equal("group", chat.Channel);
            Assert.Equal("lol", chat.Message);

            chat = Parse("You tell your party, 'omg'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("group", chat.Channel);
            Assert.Equal("omg", chat.Message);

            chat = null;
            chat = Parse("You tell your raid, 'afk 2 hours'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("raid", chat.Channel);
            Assert.Equal("afk 2 hours", chat.Message);

            // there is a double space in raid tells
            chat = Parse("Leader tells the raid,  'begin zerg'");
            Assert.NotNull(chat);
            Assert.Equal("Leader", chat.Source);
            Assert.Equal("raid", chat.Channel);
            Assert.Equal("begin zerg", chat.Message);

            chat = Parse("You tell testing:4, 'talking to myself again'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("testing", chat.Channel);
            Assert.Equal("talking to myself again", chat.Message);

            chat = Parse("Buymystuff tells General:1, 'can ne1 buy my stuff plz'");
            Assert.NotNull(chat);
            Assert.Equal("Buymystuff", chat.Source);
            Assert.Equal("General", chat.Channel);
            Assert.Equal("can ne1 buy my stuff plz", chat.Message);

            chat = Parse("Buymystuff shouts, 'wts fine steel sword'");
            Assert.NotNull(chat);
            Assert.Equal("Buymystuff", chat.Source);
            Assert.Equal("shout", chat.Channel);
            Assert.Equal("wts fine steel sword", chat.Message);

            // public in other language
            chat = Parse("Rumstil says, in an unknown tongue, 'blearg!'");
            Assert.NotNull(chat);
            Assert.Equal("Rumstil", chat.Source);
            Assert.Equal("say", chat.Channel);
            Assert.Equal("blearg!", chat.Message);

            // private in other language
            chat = Parse("Rumstil tells the group, in Elvish, 'QQ'");
            Assert.NotNull(chat);
            Assert.Equal("Rumstil", chat.Source);
            Assert.Equal("group", chat.Channel);
            Assert.Equal("QQ", chat.Message);
        }

    }
}
