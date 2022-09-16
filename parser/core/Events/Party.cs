using System;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public enum PartyStatus
    {
        None,
        GroupJoined,
        GroupLeft,
        RaidJoined,
        RaidLeft,
        ChannelJoined,
        ChannelLeft,
    }

    /// <summary>
    /// Generated when a player changes party status.
    /// </summary>
    public class LogPartyEvent : LogEvent
    {
        public string Name;
        public PartyStatus Status;

        public override string ToString()
        {
            return String.Format("Party: {0} {1}", Name, Status);
        }

        // [Thu May 19 10:02:40 2016] You have joined the group.
        // [Thu May 19 10:02:40 2016] Rumstil has joined the group.
        // [Sat Mar 19 20:48:12 2016] You have been removed from the group.
        // [Sat May 07 20:44:12 2016] You remove Fourier from the party.
        // [Thu May 19 10:20:40 2016] You notify Rumstil that you agree to join the group.
        //private static readonly Regex PartyRegex = new Regex(@"^(.+?) (?:have|has) (joined|left|been removed from) the (group|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyJoinedRegex = new Regex(@"^(.+?)(?: have| has)? joined the (group|raid)\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PartyJoinedInviteeRegex = new Regex(@"^You notify (\w+) that you agree to join the (group|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyLeftRegex = new Regex(@"^(.+?) (?:have been|has been|has|were) (?:left|removed from) the (group|raid)\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PartyKickRegex = new Regex(@"^You remove (.+?) from the (group|party|raid)\.$", RegexOptions.Compiled);

        // [Tue Jan 12 13:12:52 2021] * Rumstil has left channel openraids:1
        // [Tue Jan 12 14:06:52 2021] * Rumstil has entered channel openraids:1
        private static readonly Regex ChannelRegex = new Regex(@"^\* (.+?) has (left|entered) channel (\w+):\d+$", RegexOptions.Compiled);

        // can we log shared task add/removal?
        // [Thu Mar 25 00:35:54 2010] You have been assigned the task 'Showdown at the Crystal Core - The Hard Way'.
        // [Wed Mar 31 07:03:30 2010] Rumstil has been removed from your shared task, 'The Lost Gnomes'.
        // [Tue Oct 28 22:20:10 2014] Fred has been removed from your shared task.
        // [Tue Oct 28 21:50:19 2014] Fred has been added to your shared task.
        // [Wed Mar 31 08:58:21 2010] Your shared task, 'To Serve Sporali', has ended.

        private static readonly Regex XPRegex = new Regex(@"^You gaine?d? (experience|party|raid)", RegexOptions.Compiled);


        public static LogPartyEvent Parse(LogRawEvent e)
        {
            var m = PartyJoinedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = m.Groups[2].Value == "raid" ? PartyStatus.RaidJoined : PartyStatus.GroupJoined
                };
            }

            m = PartyLeftRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = m.Groups[2].Value == "raid" ? PartyStatus.RaidLeft : PartyStatus.GroupLeft
                };
            }

            m = PartyKickRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = m.Groups[2].Value == "raid" ? PartyStatus.RaidLeft : PartyStatus.GroupLeft
                };
            }

            m = ChannelRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = m.Groups[2].Value == "left" ? PartyStatus.ChannelLeft : PartyStatus.ChannelJoined
                };
            }

            return null;
        }

    }
}
