using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace EQLogParser
{
    /* All tests below uppercase the first letter of a mobs name in case we want to make the parser normalize names (and not have it break tests).
     */

    public class ParserTests
    {
        const string PLAYER = "FakeName";

        [Fact]
        public void PlayerFound_Who()
        {
            var parser = new LogParser(PLAYER);
            PlayerFoundEvent player = null;
            parser.OnPlayerFound += (args) => player = args;

            // anon/role
            player = null;
            parser.ParseLine("[Thu May 19 13:37:35 2016] [ANONYMOUS] Rumstil");
            Assert.NotNull(player);
            Assert.Equal("Rumstil", player.Name);
            Assert.Null(player.Class);

            // level based class name (basic class name)
            player = null;
            parser.ParseLine("[Thu May 19 13:39:00 2016] [105 Huntmaster (Ranger)] Rumstil (Halfling)  ZONE: kattacastrumb  ");
            Assert.NotNull(player);
            Assert.Equal("Rumstil", player.Name);
            Assert.Equal("Ranger", player.Class);
            Assert.Equal(105, player.Level);

            // basic class name
            player = null;
            parser.ParseLine("[Thu May 19 13:55:55 2016] [1 Shadow Knight] Scary (Froglok)  ZONE: bazaar  ");
            Assert.NotNull(player);
            Assert.Equal("Scary", player.Name);
            Assert.Equal("Shadow Knight", player.Class);
            Assert.Equal(1, player.Level);
        }

        [Fact]
        public void PlayerFound_PartyChanged()
        {
            var parser = new LogParser(PLAYER);
            PlayerFoundEvent player = null;
            parser.OnPlayerFound += (args) => player = args;

            player = null;
            parser.ParseLine("[Thu May 19 14:54:55 2016] A lizard hireling has joined the group.");
            Assert.NotNull(player);
            Assert.Equal("A lizard hireling", player.Name);
            Assert.Equal(PlayerPartyStatus.JoinedGroup, player.Status);

            player = null;
            parser.ParseLine("[Sat Mar 19 20:47:57 2016] Fourier has left the group.");
            Assert.NotNull(player);
            Assert.Equal("Fourier", player.Name);
            Assert.Equal(PlayerPartyStatus.LeftGroup, player.Status);

            player = null;
            parser.ParseLine("[Mon Apr 04 22:33:18 2016] You remove Rumstil from the party.");
            Assert.NotNull(player);
            Assert.Equal("Rumstil", player.Name);
            Assert.Equal(PlayerPartyStatus.LeftGroup, player.Status);

            player = null;
            parser.ParseLine("[Sat Mar 19 20:48:12 2016] You have been removed from the group.");
            Assert.NotNull(player);
            Assert.Equal(PLAYER, player.Name);
            Assert.Equal(PlayerPartyStatus.LeftGroup, player.Status);

            player = null;
            parser.ParseLine("[Sat Mar 19 20:48:15 2016] You have joined the raid.");
            Assert.NotNull(player);
            Assert.Equal(PLAYER, player.Name);
            Assert.Equal(PlayerPartyStatus.JoinedRaid, player.Status);

            player = null;
            parser.ParseLine("[Sat Mar 19 20:51:09 2016] You were removed from the raid.");
            Assert.NotNull(player);
            Assert.Equal(PLAYER, player.Name);
            Assert.Equal(PlayerPartyStatus.LeftRaid, player.Status);
        }

        [Fact]
        public void PetFound()
        {
            PetFoundEvent pet = null;
            var parser = new LogParser(PLAYER);
            parser.OnPetFound += (args) => pet = args;

            parser.ParseLine("[Tue Mar 22 20:43:25 2016] Kibann says 'My leader is Fourier.'");
            Assert.NotNull(pet);
            Assert.Equal("Kibann", pet.Name);
            Assert.Equal("Fourier", pet.Owner);
        }

        [Fact]
        public void Zone()
        {
            var parser = new LogParser(PLAYER);
            ZoneEvent zone = null;
            parser.OnZone += (args) => zone = args;

            parser.ParseLine("[Tue Nov 03 21:41:54 2015] You have entered Plane of Knowledge.");
            Assert.NotNull(zone);
            Assert.Equal("Plane of Knowledge", zone.Name);

            // ignore special messages that look like zoning
            zone = null;
            parser.ParseLine("[Wed Nov 04 22:04:45 2015] You have entered an area where levitation effects do not function.");
            Assert.Null(zone);

            zone = null;
            parser.ParseLine("[Wed Jun 01 19:53:34 2016] You have entered an Arena (PvP) area.");
            Assert.Null(zone);
        }

        [Fact]
        public void Location()
        {
            LocationEvent loc = null;
            var parser = new LogParser(PLAYER);
            parser.OnLocation += (args) => loc = args;

            parser.ParseLine("[Mon Mar 21 21:44:57 2016] Your Location is 1131.16, -1089.94, 162.74");
            Assert.NotNull(loc);
            Assert.Equal(1131, loc.Y);
            Assert.Equal(-1089, loc.X);
            Assert.Equal(162, loc.Z);
        }

        [Fact]
        public void FightCrit()
        {
            FightCritEvent crit = null;
            var parser = new LogParser(PLAYER);
            parser.OnFightCrit += (args) => crit = args;
            
            crit = null;
            parser.ParseLine("[Sun Nov 08 20:03:14 2015] Rumstil scores a critical hit! (208)");
            Assert.NotNull(crit);
            Assert.Equal("Rumstil", crit.Source);
            Assert.Equal(208, crit.Amount);
            Assert.Equal(FightCritEventSequence.BeforeHit, crit.Sequence);

            crit = null;
            parser.ParseLine("[Wed Aug 27 13:26:56 2003] Rumstil lands a Crippling Blow!(272)");
            Assert.NotNull(crit);
            Assert.Equal("Rumstil", crit.Source);
            Assert.Equal(272, crit.Amount);
            Assert.Equal(FightCritEventSequence.BeforeHit, crit.Sequence);

            crit = null;
            parser.ParseLine("[Wed Aug 27 13:26:56 2003] Rumstil scores a Deadly Strike!(172)");
            Assert.NotNull(crit);
            Assert.Equal("Rumstil", crit.Source);
            Assert.Equal(172, crit.Amount);
            Assert.Equal(FightCritEventSequence.BeforeHit, crit.Sequence);

            crit = null;
            parser.ParseLine("[Wed Jul 07 23:14:17 2004] Rumstil's holy blade cleanses his target!(2987)");
            Assert.NotNull(crit);
            Assert.Equal("Rumstil", crit.Source);
            Assert.Equal(2987, crit.Amount);
            Assert.Equal(FightCritEventSequence.BeforeHit, crit.Sequence);

            crit = null;
            parser.ParseLine("[Sun Nov 08 20:04:07 2015] You deliver a critical blast! (19589)");
            Assert.NotNull(crit);
            Assert.Equal(PLAYER, crit.Source);
            Assert.Equal(19589, crit.Amount);
            Assert.Equal(FightCritEventSequence.AfterHit, crit.Sequence);

            // ignore duplicate 3rd party version of own nuke critical
            crit = null;
            parser.ParseLine("[Sun Nov 08 20:04:07 2015] " + PLAYER + " delivers a critical blast! (19589)");
            Assert.Null(crit);

        }

        [Fact]
        public void FightHit_Melee()
        {
            FightHitEvent hit = null;
            var parser = new LogParser(PLAYER);
            parser.OnFightHit += (args) => hit = args;

            // personal hit
            hit = null;
            parser.ParseLine("[Wed Apr 20 18:36:59 2016] You pierce A skeletal minion for 1424 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(1424, hit.Amount);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A skeletal minion", hit.Target);
            Assert.Equal("pierce", hit.Type);

            // 3rd party hit
            hit = null;
            parser.ParseLine("[Sun May 08 20:13:09 2016] Jonekab frenzies on An aggressive corpse for 429 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(429, hit.Amount);
            Assert.Equal("Jonekab", hit.Source);
            Assert.Equal("An aggressive corpse", hit.Target);
            Assert.Equal("frenzy", hit.Type);

            // incoming hit
            hit = null;
            parser.ParseLine("[Wed Apr 27 09:46:20 2016] A ghoul hits YOU for 3551 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(3551, hit.Amount);
            Assert.Equal("A ghoul", hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal("hit", hit.Type);

            // rampage
            hit = null;
            parser.ParseLine("[Wed Apr 27 09:46:20 2016] Praetor Ledalus Thaddaeus slashes Rumstil for 9456 points of damage. (Rampage)");
            Assert.NotNull(hit);
            Assert.Equal(9456, hit.Amount);
            Assert.Equal("Praetor Ledalus Thaddaeus", hit.Source);
            Assert.Equal("Rumstil", hit.Target);
            Assert.Equal("rampage", hit.Type);

            // frenzy on - 2 word attack skill
            hit = null;
            parser.ParseLine("[Wed Apr 27 09:46:20 2016] A ghoul frenzies on YOU for 3551 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(3551, hit.Amount);
            Assert.Equal("A ghoul", hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal("frenzy", hit.Type);

            // make sure the extra non-melee message from archery and other skill attacks is not processed twice
            // this was also the format used by old damage shield messages
            // [Thu May 19 10:37:29 2016] a fright funnel was hit by non-melee for 186844 points of damage.
            // [Thu May 19 10:37:29 2016] You gain party experience!!
            // [Thu May 19 10:37:29 2016] You hit a fright funnel for 186844 points of damage.
            hit = null;
            parser.ParseLine("[Thu May 12 17:11:55 2016] A singedbones skeleton was hit by non-melee for 246657 points of damage.");
            Assert.Null(hit);
        }

        /*
        [Fact]
        public void FightHit_Misc()
        {
            FightHit hit = null;
            var parser = new Parser(PLAYER);
            parser.OnFightHit += (args) => hit = args;

            // archery hit (this is also received as a regular hit)
            parser.ParseLine("[Thu May 12 17:11:55 2016] a singedbones skeleton was hit by non-melee for 246657 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(246657, hit.Amount);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("a singedbones skeleton", hit.Target);
            Assert.Equal("hit", hit.Type);
        }
        */

        [Fact]
        public void FightHit_DoT()
        {
            FightHitEvent hit = null;
            var parser = new LogParser(PLAYER);
            parser.OnFightHit += (args) => hit = args;

            // personal DoT
            hit = null;
            parser.ParseLine("[Sun Nov 08 19:41:40 2015] A corricaux echo has taken 3936 damage from your Glistenwing Swarm.");
            Assert.NotNull(hit);
            Assert.Equal(3936, hit.Amount);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A corricaux echo", hit.Target);
            Assert.Equal("Glistenwing Swarm", hit.Spell);
            Assert.Equal("dot", hit.Type);

            // 3rd party DoT
            hit = null;
            parser.ParseLine("[Wed Nov 04 20:26:51 2015] Warpriest Poxxil has taken 14841 damage from Fourier by Mental Contortion V.");
            Assert.NotNull(hit);
            Assert.Equal(14841, hit.Amount);
            Assert.Equal("Fourier", hit.Source);
            Assert.Equal("Warpriest Poxxil", hit.Target);
            Assert.Equal("Mental Contortion V", hit.Spell);
            Assert.Equal("dot", hit.Type);

            // 3rd party DoT from dead caster or trap
            hit = null;
            parser.ParseLine("[Wed Nov 04 20:26:51 2015] Warpriest Poxxil has taken 14841 damage by Mental Contortion V.");
            Assert.NotNull(hit);
            Assert.Equal(14841, hit.Amount);
            Assert.Null(hit.Source);
            Assert.Equal("Warpriest Poxxil", hit.Target);
            Assert.Equal("Mental Contortion V", hit.Spell);
            Assert.Equal("dot", hit.Type);

            // incoming DoT 
            hit = null;
            parser.ParseLine("[Wed Nov 04 20:26:51 2015] You have taken 3251 damage from Deadly Screech by The Cliknar Queen");
            Assert.NotNull(hit);
            Assert.Equal(3251, hit.Amount);
            Assert.Equal("The Cliknar Queen", hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal("Deadly Screech", hit.Spell);
            Assert.Equal("dot", hit.Type);
            
            // incoming DoT from dead caster or trap
            hit = null;
            parser.ParseLine("[Wed Nov 04 20:26:51 2015] You have taken 3251 damage from Deadly Screech");
            Assert.NotNull(hit);
            Assert.Equal(3251, hit.Amount);
            Assert.Null(hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal("Deadly Screech", hit.Spell);
            Assert.Equal("dot", hit.Type);
        }

        [Fact]
        public void FightHit_Nuke()
        {
            FightHitEvent hit = null;
            var parser = new LogParser(PLAYER);
            parser.OnFightHit += (args) => hit = args;

            // outgoing (personal name is always shown like a 3rd party)
            parser.ParseLine("[Thu May 12 17:11:52 2016] Rumstil hit A singedbones skeleton for 37469 points of non-melee damage.");
            //parser.ParseLine("[Thu May 12 17:11:52 2016] You deliver a critical blast! (37469)");
            //parser.ParseLine("[Thu May 12 17:11:52 2016] A singedbones skeleton is caught in a hot summer's storm.");
            Assert.NotNull(hit);
            Assert.Equal(37469, hit.Amount);
            Assert.Equal("Rumstil", hit.Source);
            Assert.Equal("A singedbones skeleton", hit.Target);
            Assert.Equal("nuke", hit.Type);

            // incoming
            hit = null;
            parser.ParseLine("[Sun Nov 08 21:50:16 2015] A vicious shadow bites at your soul.  You have taken 1138 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(1138, hit.Amount);
            Assert.Null(hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal("A vicious shadow bites at your soul.", hit.Spell);
            Assert.Equal("nuke", hit.Type);

            // todo: incoming 3rd party
        }

        [Fact]
        public void FightHit_DS()
        {
            FightHitEvent hit = null;
            var parser = new LogParser(PLAYER);
            parser.OnFightHit += (args) => hit = args;

            parser.ParseLine("[Thu May 12 15:49:46 2016] A soldier is pierced by YOUR thorns for 703 points of non-melee damage.");
            Assert.NotNull(hit);
            Assert.Equal(703, hit.Amount);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A soldier", hit.Target);
            Assert.Equal("dmgshield", hit.Type);
        }

        [Fact]
        public void FightMiss()
        {
            FightMissEvent miss = null;
            var parser = new LogParser(PLAYER);
            parser.OnFightMiss += (args) => miss = args;


            // [Thu May 19 10:30:15 2016] A darkmud watcher tries to hit Rumstil, but misses!

            // incoming miss
            miss = null;
            parser.ParseLine("[Thu Apr 21 20:56:17 2016] Commander Alast Degmar tries to punch YOU, but misses!");
            Assert.NotNull(miss);
            Assert.Equal("Commander Alast Degmar", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("miss", miss.Type);

            // outgoing miss
            miss = null;
            parser.ParseLine("[Thu May 19 15:32:30 2016] You try to pierce An ocean serpent, but miss!");
            Assert.NotNull(miss);
            Assert.Equal(PLAYER, miss.Source);
            Assert.Equal("An ocean serpent", miss.Target);
            Assert.Equal("miss", miss.Type);

            // incoming defense
            miss = null;
            parser.ParseLine("[Thu May 19 15:32:23 2016] An ocean serpent tries to hit YOU, but YOU parry!");
            Assert.NotNull(miss);
            Assert.Equal("An ocean serpent", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("parry", miss.Type);

            // outgoing defense
            // ...

            // 3rd party defense
            miss = null;
            parser.ParseLine("[Thu May 19 10:46:27 2016] Xenann tries to crush A fright funnel, but A fright funnel blocks!");
            Assert.NotNull(miss);
            Assert.Equal("Xenann", miss.Source);
            Assert.Equal("A fright funnel", miss.Target);
            Assert.Equal("block", miss.Type);

            // incoming rune
            miss = null;
            parser.ParseLine("[Thu May 19 10:30:26 2016] A darkmud watcher tries to hit YOU, but YOUR magical skin absorbs the blow!");
            Assert.NotNull(miss);
            Assert.Equal("A darkmud watcher", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("rune", miss.Type);

            // 3rd party rune
            miss = null;
            parser.ParseLine("[Thu May 19 15:33:13 2016] A coral serpent tries to hit Fourier, but Fourier's magical skin absorbs the blow!");
            Assert.NotNull(miss);
            Assert.Equal("A coral serpent", miss.Source);
            Assert.Equal("Fourier", miss.Target);
            Assert.Equal("rune", miss.Type);

            // frenzy on - 2 word attack skill
            miss = null;
            parser.ParseLine("[Thu May 19 10:46:27 2016] Xenann tries to frenzy on A fright funnel, but A fright funnel blocks!");
            Assert.NotNull(miss);
            Assert.Equal("Xenann", miss.Source);
            Assert.Equal("A fright funnel", miss.Target);
            Assert.Equal("block", miss.Type);

            // invulnerable
            miss = null;
            parser.ParseLine("[Thu May 26 09:28:23 2016] You try to pierce High Infectioner, but High Infectioner is INVULNERABLE!");
            Assert.NotNull(miss);
            Assert.Equal(PLAYER, miss.Source);
            Assert.Equal("High Infectioner", miss.Target);
            Assert.Equal("invul", miss.Type);

        }

        [Fact]
        public void HealCrit()
        {
            HealCritEvent crit = null;
            var parser = new LogParser(PLAYER);
            parser.OnHealCrit += (args) => crit = args;

            crit = null;
            parser.ParseLine("[Fri May 20 09:34:13 2016] Rumstil performs an exceptional heal! (1914)");
            Assert.NotNull(crit);
            Assert.Equal("Rumstil", crit.Source);
            Assert.Equal(1914, crit.Amount);

            crit = null;
            parser.ParseLine("[Fri May 20 09:34:13 2016] You perform an exceptional heal! (1914)");
            Assert.NotNull(crit);
            Assert.Equal(PLAYER, crit.Source);
            Assert.Equal(1914, crit.Amount);
        }

        [Fact]
        public void Heal()
        {
            HealEvent heal = null;
            var parser = new LogParser(PLAYER);
            parser.OnHeal += (args) => heal = args;

            // you healing others
            heal = null;
            parser.ParseLine("[Sun May 22 11:02:53 2016] You have healed A lizard hireling for 15802 points.");
            Assert.NotNull(heal);
            Assert.Equal(PLAYER, heal.Source);
            Assert.Equal("A lizard hireling", heal.Target);
            Assert.Equal(15802, heal.Amount);

            // others healing you
            heal = null;
            parser.ParseLine("[Thu May 19 10:37:34 2016] Cleric has healed you for 61780 points.");
            Assert.NotNull(heal);
            Assert.Equal("Cleric", heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(61780, heal.Amount);

            // self healing with a HoT
            heal = null;
            parser.ParseLine("[Sun May 22 11:40:18 2016] You have been healed for 10000 hit points by your Nature's Reprieve III.");
            Assert.NotNull(heal);
            Assert.Equal(PLAYER, heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(10000, heal.Amount);

            // others healing you with a HoT
            heal = null;
            parser.ParseLine("[Sun May 22 11:04:19 2016] Cleric healed you for 7853 hit points by Ardent Elixir Rk. II.");
            Assert.NotNull(heal);
            Assert.Equal("Cleric", heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(7853, heal.Amount);
            //Assert.Equal("Ardent Elixir Rk. II", heal.Spell);

            // dead cleric healing you with a HoT
            heal = null;
            parser.ParseLine("[Sun May 22 11:04:19 2016] Cleric's corpse healed you for 7853 hit points by Ardent Elixir Rk. II.");
            Assert.NotNull(heal);
            Assert.Equal("Cleric", heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(7853, heal.Amount);

            // you healing others with a HoT
            heal = null;
            parser.ParseLine("[Thu Jun 16 16:09:25 2016] You have healed Rumstil for 1170 hit points with your Pious Elixir.");
            Assert.NotNull(heal);
            Assert.Equal(PLAYER, heal.Source);
            Assert.Equal("Rumstil", heal.Target);
            Assert.Equal(1170, heal.Amount);
            //Assert.Equal("Pious Elixir", heal.Spell);

            // others healing others (not shown in logs)
            heal = null;
            
            // attempted healing messages - the amount shown is the potential maximum rather than the actual healing
            heal = null;
            parser.ParseLine("[Sun May 22 11:04:37 2016] The promise of divine reformation is fulfilled. You have been healed for 34982 points.");
            Assert.Null(heal);
        }

        [Fact]
        public void Death()
        {
            DeathEvent dead = null;
            var parser = new LogParser(PLAYER);
            parser.OnDeath += (args) => dead = args;

            dead = null;
            parser.ParseLine("[Thu May 12 17:16:25 2016] You have slain A slag golem!");
            Assert.NotNull(dead);
            Assert.Equal(PLAYER, dead.KillShot);
            Assert.Equal("A slag golem", dead.Name);

            dead = null;
            parser.ParseLine("[Tue Nov 03 22:34:34 2015] You have been slain by A sneaky escort!");
            Assert.NotNull(dead);
            Assert.Equal("A sneaky escort", dead.KillShot);
            Assert.Equal(PLAYER, dead.Name);

            dead = null;
            parser.ParseLine("[Tue Nov 03 22:34:38 2015] Rumstil has been slain by A supply guardian!");
            Assert.NotNull(dead);
            Assert.Equal("A supply guardian", dead.KillShot);
            Assert.Equal("Rumstil", dead.Name);

            dead = null;
            parser.ParseLine("[Thu May 26 14:09:39 2016] A loyal reaver died.");
            Assert.NotNull(dead);
            Assert.Null(dead.KillShot);
            Assert.Equal("A loyal reaver", dead.Name);
        }

        [Fact]
        public void SpellCasting()
        {
            SpellCastingEvent cast = null;
            var parser = new LogParser(PLAYER);
            parser.OnSpellCasting += (args) => cast = args;

            cast = null;
            parser.ParseLine("[Sun May 01 08:44:56 2016] A woundhealer goblin begins to cast a spell. <Inner Fire>");
            Assert.NotNull(cast);
            Assert.Equal("A woundhealer goblin", cast.Source);
            Assert.Equal("Inner Fire", cast.Spell);

            cast = null;
            parser.ParseLine("[Tue Nov 03 22:38:46 2015] You begin casting Ro's Burning Cloak Rk. III.");
            Assert.NotNull(cast);
            Assert.Equal(PLAYER, cast.Source);
            Assert.Equal("Ro's Burning Cloak Rk. III", cast.Spell);
        }

        [Fact]
        public void SpellFade()
        {
            SpellFadeEvent fade = null;
            var parser = new LogParser(PLAYER);
            parser.OnSpellFade += (args) => fade = args;

            fade = null;
            parser.ParseLine("[Sat Dec 03 06:27:20 2011] Your Vinelash Cascade Rk. II spell has worn off of A tower sentry.");
            Assert.NotNull(fade);
            Assert.Equal("A tower sentry", fade.Target);
            Assert.Equal("Vinelash Cascade Rk. II", fade.Spell);
        }

        [Fact]
        public void Chat()
        {
            ChatEvent chat = null;
            var parser = new LogParser(PLAYER);
            parser.OnChat += (args) => chat = args;

            // NPCs tend to use "say" without the coma but those can be ignored
            // [Fri May 20 17:28:40 2016] a crazed digger says 'No. We can't stop the digging.'

            chat = null;
            parser.ParseLine("[Fri May 20 17:18:54 2016] Rumstil says, 'adventure'");
            Assert.NotNull(chat);
            Assert.Equal("Rumstil", chat.Source);
            Assert.Equal("say", chat.Channel);
            Assert.Equal("adventure", chat.Message);

            chat = null;
            parser.ParseLine("[Fri May 20 09:27:46 2016] You say, 'fish'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("say", chat.Channel);
            Assert.Equal("fish", chat.Message);

            chat = null;
            parser.ParseLine("[Sat Mar 19 21:12:37 2016] Fred tells you, 'hola!'");
            Assert.NotNull(chat);
            Assert.Equal("Fred", chat.Source);
            Assert.Equal("tell", chat.Channel);
            Assert.Equal("hola!", chat.Message);
            
            chat = null;
            parser.ParseLine("[Sat Mar 19 21:12:48 2016] You told Fred, 'hi'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("tell", chat.Channel);
            Assert.Equal("hi", chat.Message);

            chat = null;
            parser.ParseLine("[Tue Nov 03 21:52:01 2015] Dude tells the guild, 'k thx bye'");
            Assert.NotNull(chat);
            Assert.Equal("Dude", chat.Source);
            Assert.Equal("guild", chat.Channel);
            Assert.Equal("k thx bye", chat.Message);

            chat = null;
            parser.ParseLine("[Sun May 08 20:33:17 2016] You say to your guild, 'rofl'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("guild", chat.Channel);
            Assert.Equal("rofl", chat.Message);

            chat = null;
            parser.ParseLine("[Sat Aug 28 23:39:13 2010] Dude tells the group, 'lol'");
            Assert.NotNull(chat);
            Assert.Equal("Dude", chat.Source);
            Assert.Equal("group", chat.Channel);
            Assert.Equal("lol", chat.Message);

            chat = null;
            parser.ParseLine("[Fri May 06 12:21:25 2016] You tell your party, 'omg'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("group", chat.Channel);
            Assert.Equal("omg", chat.Message);

            chat = null;
            parser.ParseLine("[Sat Aug 28 23:16:46 2010] You tell your raid, 'afk 2 hours'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("raid", chat.Channel);
            Assert.Equal("afk 2 hours", chat.Message);

            // there is a double space in raid tells
            chat = null;
            parser.ParseLine("[Sat Aug 28 23:16:46 2010] Leader tells the raid,  'begin zerg'");
            Assert.NotNull(chat);
            Assert.Equal("Leader", chat.Source);
            Assert.Equal("raid", chat.Channel);
            Assert.Equal("begin zerg", chat.Message);

            chat = null;
            parser.ParseLine("[Tue May 24 13:06:43 2016] You tell testing:4, 'talking to myself again'");
            Assert.NotNull(chat);
            Assert.Equal(PLAYER, chat.Source);
            Assert.Equal("testing", chat.Channel);
            Assert.Equal("talking to myself again", chat.Message);

            chat = null;
            parser.ParseLine("[Tue May 24 13:25:06 2016] Buymystuff tells General:1, 'can ne1 buy my stuff plz'");
            Assert.NotNull(chat);
            Assert.Equal("Buymystuff", chat.Source);
            Assert.Equal("General", chat.Channel);
            Assert.Equal("can ne1 buy my stuff plz", chat.Message);

            chat = null;
            parser.ParseLine("[Tue May 24 13:25:06 2016] Buymystuff shouts, 'wts fine steel sword'");
            Assert.NotNull(chat);
            Assert.Equal("Buymystuff", chat.Source);
            Assert.Equal("shout", chat.Channel);
            Assert.Equal("wts fine steel sword", chat.Message);

            // public in other language
            chat = null;
            parser.ParseLine("[Fri May 20 17:18:54 2016] Rumstil says, in an unknown tongue, 'blearg!'");
            Assert.NotNull(chat);
            Assert.Equal("Rumstil", chat.Source);
            Assert.Equal("say", chat.Channel);
            Assert.Equal("blearg!", chat.Message);

            // private in other language
            chat = null;
            parser.ParseLine("[Fri May 20 17:18:54 2016] Rumstil tells the group, in Elvish, 'QQ'");
            Assert.NotNull(chat);
            Assert.Equal("Rumstil", chat.Source);
            Assert.Equal("group", chat.Channel);
            Assert.Equal("QQ", chat.Message);
        }

        [Fact]
        public void ItemLooted()
        {
            ItemLootedEvent drop = null;
            var parser = new LogParser(PLAYER);
            parser.OnItemLooted += (args) => drop = args;

            drop = null;
            parser.ParseLine("[Tue Apr 26 20:26:20 2016] --You have looted a Bixie Chitin Sword.--");
            Assert.NotNull(drop);
            Assert.Equal(PLAYER, drop.Looter);
            Assert.Equal("a Bixie Chitin Sword", drop.Item);

            drop = null;
            parser.ParseLine("[Tue Apr 26 20:24:39 2016] --Rumstil has looted a Alluring Flower.--");
            Assert.NotNull(drop);
            Assert.Equal("Rumstil", drop.Looter);
            Assert.Equal("a Alluring Flower", drop.Item);
        }

        [Fact]
        public void ItemCrafted()
        {
            ItemCraftedEvent craft = null;
            var parser = new LogParser(PLAYER);
            parser.OnItemCrafted += (args) => craft = args;

            craft = null;
            parser.ParseLine("[Fri Jun 10 08:39:54 2016] You have fashioned the items together to create something new: Magi-potent Crystal.");
            Assert.NotNull(craft);
            Assert.Equal(PLAYER, craft.Crafter);
            Assert.Equal("Magi-potent Crystal", craft.Item);

            craft = null;
            parser.ParseLine("[Fri Jun 10 08:39:54 2016] You have fashioned the items together to create an alternate product: Magi-potent Crystal.");
            Assert.NotNull(craft);
            Assert.Equal(PLAYER, craft.Crafter);
            Assert.Equal("Magi-potent Crystal", craft.Item);
        }

        [Fact]
        public void Skill()
        {
            SkillEvent skill = null;
            var parser = new LogParser(PLAYER);
            parser.OnSkill += (args) => skill = args;

            skill = null;
            parser.ParseLine("[Sat Dec 03 06:25:38 2011] You have become better at Archery! (401)");
            Assert.NotNull(skill);
            Assert.Equal("Archery", skill.Name);
            Assert.Equal(401, skill.Level);
        }

        [Fact]
        public void Faction()
        {
            FactionEvent fact = null;
            var parser = new LogParser(PLAYER);
            parser.OnFaction += (args) => fact = args;

            fact = null;
            parser.ParseLine("[Tue Nov 03 22:09:49 2015] Your faction standing with Stone Hive Bixies has been adjusted by -2.");
            Assert.NotNull(fact);
            Assert.Equal("Stone Hive Bixies", fact.Name);
            Assert.Equal(-2, fact.Change);

            fact = null;
            parser.ParseLine("[Sat Mar 19 11:17:05 2016] Your faction standing with Apparitions of Fear could not possibly get any worse.");
            Assert.NotNull(fact);
            Assert.Equal("Apparitions of Fear", fact.Name);
            Assert.Equal(0, fact.Change);

            fact = null;
            parser.ParseLine("[Sat Mar 19 11:17:05 2016] Your faction standing with Iceshard Manor could not possibly get any better.");
            Assert.NotNull(fact);
            Assert.Equal("Iceshard Manor", fact.Name);
            Assert.Equal(0, fact.Change);
        }



    }
}
