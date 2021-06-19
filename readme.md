# What is this?

This is an EverQuest log file parser application that can scan an Everquest log file and generate fight summaries to help players analyze their gameplay.

There are 3 projects in this repository. All require the .net core 3.1 SDK to compile:

The **LogSync** project is a windows app that is used to collect and send log parses to the www.raidloot.com/logs website for sharing with group and guild members. It has just enough of a user interface to let you see basic fight info, but it's meant to be used alongside the website.

The **Sample** project is a console app to demonstrate some minimalist usage of the parser without any UI getting in the way.

The **EQLogParser** project is a .NET library containing all the parsing code without any UI. It can be used in a console app, WinForms/WPF app or even a web app by anyone else that wants to build their own parser with a nicer UI.


# Web Based Parser

If you would prefer not to install the **LogSync** application, you can use a web based version of this parser by uploading your logs directly to www.raidloot.com/logs.

Your logs will be published to your own private channel.

e.g. This is my channel: https://www.raidloot.com/channel/x


# Limitations

The parser currently only handles The Burning Lands (Dec 12, 2018) and newer log formats.


# Structure

At the root of the parser is the LogParser class. This class converts log lines into structured events. This class doesn't do any sort of state tracking - it is limited to handling EQ log syntax wierdness and converting it to a more useful data structure.

First it takes a log line like this:

```
[Fri Dec 28 16:30:41 2018] A tree snake bites Lenantik for 470 points of damage.
```

And converts it to a LogRawEvent like this:

```
public class LogRawEvent : LogEvent
{
    public string Text = "A tree snake bites Lenantik for 470 points of damage.";
    public DateTime Timestamp = DateTime.Parse("Fri Dec 28 16:30:41 2018").ToUniversalTime();
}
```

LogRawEvent represents the common traits every log line has: a date and a text string. This is not very useful yet but it serves as an input for all the other event parsers.

We then pass the LogRawEvent to a bunch of parsers to see if one of them recognizes it. In this case LogHitEvent parser will and returns a new LogHitEvent event:

```
public class LogHitEvent : LogEvent
{
    public string Source = "A tree snake";
    public string Target = "Lenatik";
    public int Amount = 470;
    public string Type = "bites";
    public DateTime Timestamp = rawEvent.Timestamp;
}
```

This is better. At this point the LogHitEvent is useful for building some kind of fight information, but it we don't want to do it in the parser. That would be mixing multiple concerns in the parser class.

Instead, we hand this LogHitEvent off to the FightTracker class. The FightTracker consumes hit and other events to build out some stateful fight information. When the FightTracker detects a mob has died it emits a fight summary -- which is what most people want when they think of a log parser. These completed fight summaries can then finally be sent to some sort of UI or console output for display.

There are a few additonal tracker classes available:

The CharTracker class keeps track of who is a friend or foe. This functionality is mostly useful to the FightTracker because EQ logs don't include context to indicate if a third party is a friend or foe.

The LootTracker class keeps track of which mobs dropped what loot. This is used to collect data that can increase the accuracy of information on the raidloot.com website.

The BuffTracker class keeps track of which buffs players and pets have received.


# Copyright

Copyright 2020 Rumstil / raidloot.com


# License

This project is licensed under Apache License 2.0


# Contact

I can be reached at raidloot@gmail.com


