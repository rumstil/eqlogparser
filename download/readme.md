# Prerequisite

In order to run the parser you will need to have the Microsoft .NET Core 3.1 Runtime installed on your computer. You can download it from Microsoft here:

https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.10-windows-x86-installer

# Download

Once you've updated your system with the .NET Core runtime, you can download the parser from here:

https://s3.amazonaws.com/raidloot/logsync.exe

This is just a portable executuable file. There is no installer for the app yet. I recommend you save it to a folder somewhere on your desktop.

The first time you run the parser will you get a "Windows protected your PC" alert. This is called SmartScreen and protects your PC from running programs from unrecognized publishers - which is what I am. If you want to run the parser you will need to click the "More Info" link on the popup and then "Run anyway".

You may also get a Windows Firewall popup asking if the app can access the internet. You will need to allow this since part of the parser's job is to send data over the internet.


# How to enable logging in EQ

By default EQ doesn't have any logging enabled. Prior to using the parser, you must enable logging in EQ:

1. Use the `/log on` command.

2. Use the `/loginterval 1` command. This will make EQ update your log file every second.

3. Then open the options window (Ctrl+O), select the Chat tab, and set the following:

-   `Other damage other` to `Show`
-   `Spell Damage` to `Show`
-   `PC Spells` to `Show`
-   `Pet Spells` to `Show`
-   `Combat Abilities / Disciplines (Others)` to `Show`
-   `Combat Abilities / Disciplines (You)` to `Show`

**This will produce a lot of spam in your chat window** but it's the only way to get full and accurate information. I personally keep this spam in a second chat window so that it can scroll by without affecting my normal player chat.

Once your logging is enabled you can start up the parser during or after play.

The app will scan your EQ log file, generate summaries of all mobs and events you have fought, and upload them to a private channel on the raidloot.com website. To give people access to your channel you just need to share the link with them. Your channel will keep track of your parses for about 48 hours -- after that they get removed.

In addition to fights, the parser will also upload loot drops -- this information is only used to update the item drop database. Your looted items are not saved to your channel.
