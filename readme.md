

#Status#

This parser is an experiment in creating a headless event based log parser. It doesn't have an interface that would be useful to end users. In it's current state it will be helpful as a starting point for people wanting to write their own EQ log parsing application.

At this point, this project will probably just stay an experiment. I don't have much interest in developing it further. I had hoped to turn it into a collection app for a web based log aggregation service, but I don't think there would be much interest in that.

 

#Structure#

At the root of the parser is the LogParser class. This is an event based parser that converts log lines into a set of events. This class doesn't do any sort of state tracking - it is limited to handling EQ log syntax wierdness and converting it to a more stable interface. The parser itself it fairly complete and handles most useful log messages.

Further down the chain there are "tracker" classes which then consume LogParser events to maintain state information.

FightTracker - This class consumes parser events and compiles them into fight summaries. The FightTracker still needs a bit of work to be useful.

PlayerTracker - This class consumes parser events to track player, merc and pet names. (i.e. Anyone who isn't a killable mob).

FactionTracker - This class consumes parser events to track faction changes.




#Tests#

Tests are implemented in xUnit. 

I've aimed for 100% coverage with LogParser class due to the potential quirkiness of it's regex based parsing - it would be easy to mess them up and potentially miss or misflag log entries.


#License#

This project is licensed under Apache License 2.0