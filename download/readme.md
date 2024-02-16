# Prerequisite

In order to run the parser you will need to have the Microsoft .NET 8.0 Runtime installed on your computer. (This requirement was updated on to Feb 16, 2024)

You can download it from Microsoft here:

https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.2-windows-x64-installer

# Download

Once you've updated your system with the .NET Core runtime, you can download the parser from here:

https://s3.amazonaws.com/raidloot/logsync.exe

This is just a portable executuable file. There is no installer for the app yet. I recommend you save it to a folder somewhere on your desktop.

The first time you run the parser will you get a "Windows protected your PC" alert. This is called SmartScreen and protects your PC from running programs from unrecognized publishers - which is what I am. If you want to run the parser you will need to click the "More Info" link on the popup and then "Run anyway".

You may also get a Windows Firewall popup asking if the app can access the internet. You will need to allow this since part of the parser's job is to send data over the internet.

# How it works

When you start the app, you will need to select a log file from your EQ folder. The logs folder is usually:

```
C:\Users\Public\Daybreak Game Company\Installed Games\EverQuest\Logs
or
C:\Users\Public\Sony Online Entertainment\Installed Games\EverQuest\Logs
```

The app will scan your log file, generate summaries of all mobs and events you have fought, and upload them to a private channel on the raidloot.com website. You can either upload fights individually by double-clicking specific fights, or select the option for auto uploads.

To view your channel you just need to click the "View Channel" button. To give other people access to your channel you need to share the link with them. Your channel will keep track of your parses for about 48 hours -- after that they get removed.

In addition to fights, the parser will also upload loot drops -- this information is only used to update the item drop database. Your looted items are not saved to your channel.

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

4. Once your logging is enabled, you will have to keep in mind that damage reporting ranges vary based on damage type and source. I believe melee and direct damage spells are reported by mobs, while DoT spells are reported by caster. There are additonal quirks, but that best advice is probably to be close to the mob in order to log as much information as possible.

Once your logging is enabled you can start up the parser during or after play.
