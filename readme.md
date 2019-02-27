

# Status #

This parser is an experiment in creating a headless event based log parser. It doesn't have an interface that would be useful to end users. In it's current state it will be helpful as a starting point for people wanting to write their own EQ log parsing application.

At this point, this project will probably just stay an experiment. I don't have much interest in developing it further. I had hoped to turn it into a collection app for a web based log aggregation service, but I don't think there would be much interest in that.

 

# Structure #

At the root of the parser is the LogParser class. This is an event based parser that converts log lines into events. This class doesn't do any sort of state tracking - it is limited to handling EQ log syntax wierdness and converting it to a more stable interface. The parser itself it fairly complete and handles most useful log messages.

Further down the chain there are "tracker" classes which then consume LogParser events to maintain state information.

FightTracker - This class consumes parser events and compiles them into fight summaries. The FightTracker still needs a bit of work to be useful.




# License #

This project is licensed under Apache License 2.0