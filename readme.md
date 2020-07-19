# Status

This parser is an experiment in creating a headless event based log parser that can be used in a console app, WinForms/WPF app or even a web app.

The **EQLogParser** project is the main parser library.

The **Sample** project is a console app. I mostly use this for debugging.

The **LogSync** project is a WinForms app that is used to collect and send log parses to the www.raidloot.com website for sharing with group and guild members.

All projects are .net core 3.1 projects. You will need the .net core 3.1 SDK and possibly VS 2019 to compile them.

# Web Parser

The LogSync app will publish your fights to your personal channel on raidloot.com.

e.g. This is my channel: https://www.raidloot.com/channel/x

# Limitations

The parser currently only handles The Burning Lands (Dec 12, 2018) and newer log formats.

# Structure

At the root of the parser is the LogParser class. This class converts log lines into structured events. This class doesn't do any sort of state tracking - it is limited to handling EQ log syntax wierdness and converting it to a more useful interface.

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

LogRawEvent represents the common traits every log line has: a date and a text string. This is not very useful yet, but it keeps us from spraying date parsing and substring copying code all over the place and does a very basic check that the line is even a proper EQ log line.

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

The CharTracker class keeps track of who is a friend or foe. This functionality is mostly useful to the FightTracker, but again we don't want to mix too many concerns in a single class so it's a separate class.

The LootTracker class keeps track of which mobs dropped what loot. This is used to collect data that can increase the accuracy of information on the raidloot.com website.

# Copyright

Copyright 2020 Rumstil / raidloot.com

# License

This project is licensed under Apache License 2.0

# Contact

I can be reached at raidloot@gmail.com

If you encounter log parsing bugs, sending me a log file would be helpful. There isn't much that I can do with just an error message.
