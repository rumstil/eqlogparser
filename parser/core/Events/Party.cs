﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public enum PartyStatus
    {
        None,
        SoloXP,
        GroupXP,
        GroupJoined,
        GroupLeft,
        RaidXP,
        RaidJoined,
        RaidLeft
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
        // [Sat May 21 19:42:50 2016] You will now auto-follow Rumstil.
        // [Thu May 19 10:36:24 2016] You are no longer auto-following Rumstil.
        //private static readonly Regex PartyRegex = new Regex(@"^(.+?) (?:have|has) (joined|left|been removed from) the (group|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyJoinedRegex = new Regex(@"^(.+?) (?:have|has) joined the (group|raid)\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PartyJoinedInviteeRegex = new Regex(@"^You notify (\w+) that you agree to join the (group|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyLeftRegex = new Regex(@"^(.+?) (?:have been|has been|has|were) (?:left|removed from) the (group|raid)\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PartyKickRegex = new Regex(@"^You remove (.+?) from the (group|party|raid)\.$", RegexOptions.Compiled);

        // can we log shared task add/removal?
        // [Thu Mar 25 00:35:54 2010] You have been assigned the task 'Showdown at the Crystal Core - The Hard Way'.
        // [Wed Mar 31 07:03:30 2010] Rumstil has been removed from your shared task, 'The Lost Gnomes'.
        // [Tue Oct 28 22:20:10 2014] Fred has been removed from your shared task.
        // [Tue Oct 28 21:50:19 2014] Fred has been added to your shared task.
        // [Wed Mar 31 08:58:21 2010] Your shared task, 'To Serve Sporali', has ended.

        public static LogPartyEvent Parse(LogRawEvent e)
        {
            var m = PartyJoinedRegex.Match(e.Text);
            if (m.Success)
            {
                var status = m.Groups[2].Value == "raid" ? PartyStatus.RaidJoined : PartyStatus.GroupJoined;
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = status
                };
            }

            m = PartyLeftRegex.Match(e.Text);
            if (m.Success)
            {
                var status = m.Groups[2].Value == "raid" ? PartyStatus.RaidLeft : PartyStatus.GroupLeft;
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = status
                };
            }

            m = PartyKickRegex.Match(e.Text);
            if (m.Success)
            {
                var status = m.Groups[2].Value == "raid" ? PartyStatus.RaidLeft : PartyStatus.GroupLeft;
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    Status = status
                };
            }

            if (e.Text.StartsWith("You gain experience"))
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.Player,
                    Status = PartyStatus.SoloXP
                };
            }

            if (e.Text.StartsWith("You gain party experience"))
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.Player,
                    Status = PartyStatus.GroupXP
                };
            }

            if (e.Text.StartsWith("You gained raid experience"))
            {
                return new LogPartyEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.Player,
                    Status = PartyStatus.RaidXP
                };
            }

            return null;
        }

    }
}
