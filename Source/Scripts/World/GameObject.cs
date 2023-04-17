// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Serilog;

namespace Scripts.World.GameObjects;

internal struct SpellIds
{
    //CatFigurine
    public const uint SUMMON_GHOST_SABER = 5968;

    //EthereumPrison
    public const uint REP_LC = 39456;
    public const uint REP_SHAT = 39457;
    public const uint REP_CE = 39460;
    public const uint REP_CON = 39474;
    public const uint REP_KT = 39475;
    public const uint REP_SPOR = 39476;

    //Southfury
    public const uint BLACKJACK = 39865; //Stuns Player
    public const uint SUMMON_RIZZLE = 39866;

    //Felcrystalforge
    public const uint CREATE1_FLASK_OF_BEAST = 40964;
    public const uint CREATE5_FLASK_OF_BEAST = 40965;

    //Bashircrystalforge
    public const uint CREATE1_FLASK_OF_SORCERER = 40968;
    public const uint CREATE5_FLASK_OF_SORCERER = 40970;

    //Jotunheimcage
    public const uint SUMMON_BLADE_KNIGHT_H = 56207;
    public const uint SUMMON_BLADE_KNIGHT_NE = 56209;
    public const uint SUMMON_BLADE_KNIGHT_ORC = 56212;
    public const uint SUMMON_BLADE_KNIGHT_TROLL = 56214;

    //Amberpineouthouse
    public const uint INDISPOSED = 53017;
    public const uint INDISPOSED_III = 48341;
    public const uint CREATE_AMBERSEEDS = 48330;

    //Thecleansing
    public const uint CLEANSING_SOUL = 43351;
    public const uint RECENT_MEDITATION = 61720;

    //Midsummerbonfire
    public const uint STAMP_OUT_BONFIRE_QUEST_COMPLETE = 45458;

    //MidsummerPoleRibbon
    public static uint[] RibbonPoleSpells =
    {
        29705, 29726, 29727
    };

    //Toy Train Set
    public const uint TOY_TRAIN_PULSE = 61551;
}

internal struct CreatureIds
{
    //GildedBrazier
    public const uint STILLBLADE = 17716;

    //EthereumPrison
    public static uint[] PrisonEntry =
    {
        22810, 22811, 22812, 22813, 22814, 22815,       //Good Guys
        20783, 20784, 20785, 20786, 20788, 20789, 20790 //Bad Guys
    };

    //Ethereum Stasis
    public static uint[] StasisEntry =
    {
        22825, 20888, 22827, 22826, 22828
    };

    //ResoniteCask
    public const uint GOGGEROC = 11920;

    //Sacredfireoflife
    public const uint ARIKARA = 10882;

    //Southfury
    public const uint RIZZLE = 23002;

    //Bloodfilledorb
    public const uint ZELEMAR = 17830;

    //Jotunheimcage
    public const uint EBON_BLADE_PRISONER_HUMAN = 30186;
    public const uint EBON_BLADE_PRISONER_NE = 30194;
    public const uint EBON_BLADE_PRISONER_TROLL = 30196;
    public const uint EBON_BLADE_PRISONER_ORC = 30195;

    //Tadpoles
    public const uint WINTERFIN_TADPOLE = 25201;

    //Amberpineouthouse
    public const uint OUTHOUSE_BUNNY = 27326;

    //Missingfriends
    public const uint CAPTIVE_CHILD = 22314;

    //MidsummerPoleRibbon
    public const uint POLE_RIBBON_BUNNY = 17066;
}

internal struct GameObjectIds
{
    //Bellhourlyobjects
    public const uint HORDE_BELL = 175885;
    public const uint ALLIANCE_BELL = 176573;
    public const uint KHARAZHAN_BELL = 182064;
}

internal struct ItemIds
{
    //Amberpineouthouse
    public const uint ANDERHOLS_SLIDER_CIDER = 37247;
}

internal struct QuestIds
{
    //GildedBrazier
    public const uint THE_FIRST_TRIAL = 9678;

    //Dalarancrystal
    public const uint LEARN_LEAVE_RETURN = 12790;
    public const uint TELE_CRYSTAL_FLAG = 12845;

    //Tadpoles
    public const uint OH_NOES_THE_TADPOLES = 11560;

    //Amberpineouthouse
    public const uint DOING_YOUR_DUTY = 12227;

    //Missingfriends
    public const uint MISSING_FRIENDS = 10852;

    //Thecleansing
    public const uint THE_CLEANSING_HORDE = 11317;
    public const uint THE_CLEANSING_ALLIANCE = 11322;
}

internal struct TextIds
{
    //Missingfriends
    public const uint SAY_FREE0 = 0;
}

internal struct GossipConst
{
    //Dalarancrystal
    public const string GO_TELE_TO_DALARAN_CRYSTAL_FAILED = "This Teleport Crystal Cannot Be Used Until The Teleport Crystal In Dalaran Has Been Used At Least Once.";

    //Felcrystalforge
    public const uint GOSSIP_FEL_CRYSTALFORGE_TEXT = 31000;
    public const uint GOSSIP_FEL_CRYSTALFORGE_ITEM_TEXT_RETURN = 31001;
    public const string GOSSIP_FEL_CRYSTALFORGE_ITEM1 = "Purchase 1 Unstable Flask Of The Beast For The Cost Of 10 Apexis Shards";
    public const string GOSSIP_FEL_CRYSTALFORGE_ITEM5 = "Purchase 5 Unstable Flask Of The Beast For The Cost Of 50 Apexis Shards";
    public const string GOSSIP_FEL_CRYSTALFORGE_ITEM_RETURN = "Use The Fel Crystalforge To Make Another Purchase.";

    //Bashircrystalforge
    public const uint GOSSIP_BASHIR_CRYSTALFORGE_TEXT = 31100;
    public const uint GOSSIP_BASHIR_CRYSTALFORGE_ITEM_TEXT_RETURN = 31101;
    public const string GOSSIP_BASHIR_CRYSTALFORGE_ITEM1 = "Purchase 1 Unstable Flask Of The Sorcerer For The Cost Of 10 Apexis Shards";
    public const string GOSSIP_BASHIR_CRYSTALFORGE_ITEM5 = "Purchase 5 Unstable Flask Of The Sorcerer For The Cost Of 50 Apexis Shards";
    public const string GOSSIP_BASHIR_CRYSTALFORGE_ITEM_RETURN = "Use The Bashir Crystalforge To Make Another Purchase.";

    //Amberpineouthouse
    public const uint GOSSIP_OUTHOUSE_INUSE = 12775;
    public const uint GOSSIP_OUTHOUSE_VACANT = 12779;

    public const string GOSSIP_USE_OUTHOUSE = "Use The Outhouse.";
    public const string ANDERHOLS_SLIDER_CIDER_NOT_FOUND = "Quest Item Anderhol'S Slider Cider Not Found.";
}

internal struct SoundIds
{
    //BrewfestMusic
    public const uint EVENT_BREWFESTDWARF01 = 11810;  // 1.35 Min
    public const uint EVENT_BREWFESTDWARF02 = 11812;  // 1.55 Min 
    public const uint EVENT_BREWFESTDWARF03 = 11813;  // 0.23 Min
    public const uint EVENT_BREWFESTGOBLIN01 = 11811; // 1.08 Min
    public const uint EVENT_BREWFESTGOBLIN02 = 11814; // 1.33 Min
    public const uint EVENT_BREWFESTGOBLIN03 = 11815; // 0.28 Min

    //Brewfestmusicevents
    public const uint EVENT_BM_SELECT_MUSIC = 1;
    public const uint EVENT_BM_START_MUSIC = 2;

    //Bells
    //BellHourlySoundFX
    public const uint BELL_TOLL_HORDE = 6595; // Horde
    public const uint BELL_TOLL_TRIBAL = 6675;
    public const uint BELL_TOLL_ALLIANCE = 6594; // Alliance
    public const uint BELL_TOLL_NIGHTELF = 6674;
    public const uint BELL_TOLLDWARFGNOME = 7234;
    public const uint BELL_TOLL_KHARAZHAN = 9154; // Kharazhan
}

internal struct AreaIds
{
    public const uint SILVERMOON = 3430; // Horde
    public const uint UNDERCITY = 1497;
    public const uint ORGRIMMAR1 = 1296;
    public const uint ORGRIMMAR2 = 14;
    public const uint THUNDERBLUFF = 1638;
    public const uint IRONFORGE1 = 809; // Alliance
    public const uint IRONFORGE2 = 1;
    public const uint STORMWIND = 12;
    public const uint EXODAR = 3557;
    public const uint DARNASSUS = 1657;
    public const uint SHATTRATH = 3703; // General
    public const uint TELDRASSIL_ZONE = 141;
    public const uint KHARAZHAN_MAPID = 532;
}

internal struct ZoneIds
{
    public const uint TIRISFAL = 85;
    public const uint UNDERCITY = 1497;
    public const uint DUN_MOROGH = 1;
    public const uint IRONFORGE = 1537;
    public const uint TELDRASSIL = 141;
    public const uint DARNASSUS = 1657;
    public const uint ASHENVALE = 331;
    public const uint HILLSBRAD_FOOTHILLS = 267;
    public const uint DUSKWOOD = 10;
}

internal struct Misc
{
    // These Are In Seconds
    //Brewfestmusictime
    public static TimeSpan EventBrewfestdwarf01Time = TimeSpan.FromSeconds(95);
    public static TimeSpan EventBrewfestdwarf02Time = TimeSpan.FromSeconds(155);
    public static TimeSpan EventBrewfestdwarf03Time = TimeSpan.FromSeconds(23);
    public static TimeSpan EventBrewfestgoblin01Time = TimeSpan.FromSeconds(68);
    public static TimeSpan EventBrewfestgoblin02Time = TimeSpan.FromSeconds(93);
    public static TimeSpan EventBrewfestgoblin03Time = TimeSpan.FromSeconds(28);

    //Bellhourlymisc
    public const uint GAME_EVENT_HOURLY_BELLS = 73;
}

[Script]
internal class GOGildedBrazier : GameObjectAI
{
    public GOGildedBrazier(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        if (Me.GoType == GameObjectTypes.Goober)
            if (player.GetQuestStatus(QuestIds.THE_FIRST_TRIAL) == QuestStatus.Incomplete)
            {
                Creature stillblade = player.SummonCreature(CreatureIds.STILLBLADE, 8106.11f, -7542.06f, 151.775f, 3.02598f, TempSummonType.DeadDespawn, TimeSpan.FromMinutes(1));

                if (stillblade)
                    stillblade.AI.AttackStart(player);
            }

        return true;
    }
}

[Script]
internal class GOTabletOfTheSeven : GameObjectAI
{
    public GOTabletOfTheSeven(GameObject go) : base(go) { }

    /// @todo use gossip option ("Transcript the Tablet") instead, if Trinity adds support.
    public override bool OnGossipHello(Player player)
    {
        if (Me.GoType != GameObjectTypes.QuestGiver)
            return true;

        if (player.GetQuestStatus(4296) == QuestStatus.Incomplete)
            player.SpellFactory.CastSpell(player, 15065, false);

        return true;
    }
}

[Script]
internal class GOEthereumPrison : GameObjectAI
{
    public GOEthereumPrison(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        Me.UseDoorOrButton();
        var random = (int)(RandomHelper.Rand32() % (CreatureIds.PrisonEntry.Length / sizeof(uint)));

        Creature creature = player.SummonCreature(CreatureIds.PrisonEntry[random], Me.Location.X, Me.Location.Y, Me.Location.Z, Me.Location.GetAbsoluteAngle(player.Location), TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));

        if (creature)
            if (!creature.IsHostileTo(player))
            {
                var pFaction = creature.GetFactionTemplateEntry();

                if (pFaction != null)
                {
                    uint spellId = 0;

                    switch (pFaction.Faction)
                    {
                        case 1011:
                            spellId = SpellIds.REP_LC;

                            break;
                        case 935:
                            spellId = SpellIds.REP_SHAT;

                            break;
                        case 942:
                            spellId = SpellIds.REP_CE;

                            break;
                        case 933:
                            spellId = SpellIds.REP_CON;

                            break;
                        case 989:
                            spellId = SpellIds.REP_KT;

                            break;
                        case 970:
                            spellId = SpellIds.REP_SPOR;

                            break;
                    }

                    if (spellId != 0)
                        creature.SpellFactory.CastSpell(player, spellId, false);
                    else
                        Log.Logger.Error($"go_ethereum_prison summoned Creature (entry {creature.Entry}) but faction ({creature.Faction}) are not expected by script.");
                }
            }

        return false;
    }
}

[Script]
internal class GOEthereumStasis : GameObjectAI
{
    public GOEthereumStasis(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        Me.UseDoorOrButton();
        var random = (int)(RandomHelper.Rand32() % CreatureIds.StasisEntry.Length / sizeof(uint));

        player.SummonCreature(CreatureIds.StasisEntry[random], Me.Location.X, Me.Location.Y, Me.Location.Z, Me.Location.GetAbsoluteAngle(player.Location), TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));

        return false;
    }
}

[Script]
internal class GOResoniteCask : GameObjectAI
{
    public GOResoniteCask(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        if (Me.GoType == GameObjectTypes.Goober)
            Me.SummonCreature(CreatureIds.GOGGEROC, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromMinutes(5));

        return false;
    }
}

[Script]
internal class GOSouthfuryMoonstone : GameObjectAI
{
    public GOSouthfuryMoonstone(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        //implicitTarget=48 not implemented as of writing this code, and manual summon may be just ok for our purpose
        //player.SpellFactory.CastSpell(player, SpellSummonRizzle, false);

        Creature creature = player.SummonCreature(CreatureIds.RIZZLE, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.DeadDespawn);

        if (creature)
            creature.SpellFactory.CastSpell(player, SpellIds.BLACKJACK, false);

        return false;
    }
}

[Script]
internal class GOTeleToDalaranCrystal : GameObjectAI
{
    public GOTeleToDalaranCrystal(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        if (player.GetQuestRewardStatus(QuestIds.TELE_CRYSTAL_FLAG))
            return false;

        player.Session.SendNotification(GossipConst.GO_TELE_TO_DALARAN_CRYSTAL_FAILED);

        return true;
    }
}

[Script]
internal class GOTeleToVioletStand : GameObjectAI
{
    public GOTeleToVioletStand(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        if (player.GetQuestRewardStatus(QuestIds.LEARN_LEAVE_RETURN) ||
            player.GetQuestStatus(QuestIds.LEARN_LEAVE_RETURN) == QuestStatus.Incomplete)
            return false;

        return true;
    }
}

[Script]
internal class GOBloodFilledOrb : GameObjectAI
{
    public GOBloodFilledOrb(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        if (Me.GoType == GameObjectTypes.Goober)
            player.SummonCreature(CreatureIds.ZELEMAR, -369.746f, 166.759f, -21.50f, 5.235f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));

        return true;
    }
}

[Script]
internal class GOSoulwell : GameObjectAI
{
    public GOSoulwell(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        var owner = Me.OwnerUnit;

        if (!owner ||
            !owner.IsTypeId(TypeId.Player) ||
            !player.IsInSameRaidWith(owner.AsPlayer))
            return true;

        return false;
    }
}

[Script]
internal class GOAmberpineOuthouse : GameObjectAI
{
    public GOAmberpineOuthouse(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        var status = player.GetQuestStatus(QuestIds.DOING_YOUR_DUTY);

        if (status == QuestStatus.Incomplete ||
            status == QuestStatus.Complete ||
            status == QuestStatus.Rewarded)
        {
            player.AddGossipItem(GossipOptionNpc.None, GossipConst.GOSSIP_USE_OUTHOUSE, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);
            player.SendGossipMenu(GossipConst.GOSSIP_OUTHOUSE_VACANT, Me.GUID);
        }
        else
        {
            player.SendGossipMenu(GossipConst.GOSSIP_OUTHOUSE_INUSE, Me.GUID);
        }

        return true;
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
        player.ClearGossipMenu();

        if (action == GossipAction.GOSSIP_ACTION_INFO_DEF + 1)
        {
            player.CloseGossipMenu();
            var target = ScriptedAI.GetClosestCreatureWithEntry(player, CreatureIds.OUTHOUSE_BUNNY, 3.0f);

            if (target)
            {
                target.AI.SetData(1, (uint)player.NativeGender);
                Me.SpellFactory.CastSpell(target, SpellIds.INDISPOSED_III);
            }

            Me.SpellFactory.CastSpell(player, SpellIds.INDISPOSED);

            if (player.HasItemCount(ItemIds.ANDERHOLS_SLIDER_CIDER))
                Me.SpellFactory.CastSpell(player, SpellIds.CREATE_AMBERSEEDS);

            return true;
        }
        else
        {
            player.CloseGossipMenu();
            player.Session.SendNotification(GossipConst.ANDERHOLS_SLIDER_CIDER_NOT_FOUND);

            return false;
        }
    }
}

[Script]
internal class GOMassiveSeaforiumCharge : GameObjectAI
{
    public GOMassiveSeaforiumCharge(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        Me.SetLootState(LootState.JustDeactivated);

        return true;
    }
}

[Script]
internal class GOVeilSkithCage : GameObjectAI
{
    public GOVeilSkithCage(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        Me.UseDoorOrButton();

        if (player.GetQuestStatus(QuestIds.MISSING_FRIENDS) == QuestStatus.Incomplete)
        {
            var childrenList = Me.GetCreatureListWithEntryInGrid(CreatureIds.CAPTIVE_CHILD, SharedConst.InteractionDistance);

            foreach (var creature in childrenList)
            {
                player.KilledMonsterCredit(CreatureIds.CAPTIVE_CHILD, creature.GUID);
                creature.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                creature.MotionMaster.MovePoint(1, Me.Location.X + 5, Me.Location.Y, Me.Location.Z);
                creature.AI.Talk(TextIds.SAY_FREE0);
                creature.MotionMaster.Clear();
            }
        }

        return false;
    }
}

[Script]
internal class GOMidsummerBonfire : GameObjectAI
{
    public GOMidsummerBonfire(GameObject go) : base(go) { }

    public override bool OnGossipSelect(Player player, uint menuId, uint ssipListId)
    {
        player.SpellFactory.CastSpell(player, SpellIds.STAMP_OUT_BONFIRE_QUEST_COMPLETE, true);
        player.CloseGossipMenu();

        return false;
    }
}

[Script]
internal class GOMidsummerRibbonPole : GameObjectAI
{
    public GOMidsummerRibbonPole(GameObject go) : base(go) { }

    public override bool OnGossipHello(Player player)
    {
        var creature = Me.FindNearestCreature(CreatureIds.POLE_RIBBON_BUNNY, 10.0f);

        if (creature)
        {
            creature.AI.DoAction(0);
            player.SpellFactory.CastSpell(player, SpellIds.RibbonPoleSpells[RandomHelper.IRand(0, 2)], true);
        }

        return true;
    }
}

[Script]
internal class GOBrewfestMusic : GameObjectAI
{
    private TimeSpan _musicTime = TimeSpan.FromSeconds(1);
    private uint _rnd = 0;

    public GOBrewfestMusic(GameObject go) : base(go)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               if (Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                               {
                                   _rnd = RandomHelper.URand(0, 2); // Select random music sample
                                   task.Repeat(_musicTime);         // Select new song music after play Time is over
                               }
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               if (Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                               {
                                   switch (Me.Area)
                                   {
                                       case AreaIds.SILVERMOON:
                                       case AreaIds.UNDERCITY:
                                       case AreaIds.ORGRIMMAR1:
                                       case AreaIds.ORGRIMMAR2:
                                       case AreaIds.THUNDERBLUFF:
                                           switch (_rnd)
                                           {
                                               case 0:
                                                   Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTGOBLIN01);
                                                   _musicTime = Misc.EventBrewfestgoblin01Time;

                                                   break;
                                               case 1:
                                                   Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTGOBLIN02);
                                                   _musicTime = Misc.EventBrewfestgoblin02Time;

                                                   break;
                                               default:
                                                   Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTGOBLIN03);
                                                   _musicTime = Misc.EventBrewfestgoblin03Time;

                                                   break;
                                           }

                                           break;
                                       case AreaIds.IRONFORGE1:
                                       case AreaIds.IRONFORGE2:
                                       case AreaIds.STORMWIND:
                                       case AreaIds.EXODAR:
                                       case AreaIds.DARNASSUS:
                                           switch (_rnd)
                                           {
                                               case 0:
                                                   Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTDWARF01);
                                                   _musicTime = Misc.EventBrewfestdwarf01Time;

                                                   break;
                                               case 1:
                                                   Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTDWARF02);
                                                   _musicTime = Misc.EventBrewfestdwarf02Time;

                                                   break;
                                               default:
                                                   Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTDWARF03);
                                                   _musicTime = Misc.EventBrewfestdwarf03Time;

                                                   break;
                                           }

                                           break;
                                       case AreaIds.SHATTRATH:
                                           var playersNearby = Me.GetPlayerListInGrid(Me.VisibilityRange);

                                           foreach (Player player in playersNearby)
                                               if (player.TeamId == TeamIds.Horde)
                                                   switch (_rnd)
                                                   {
                                                       case 0:
                                                           Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTGOBLIN01);
                                                           _musicTime = Misc.EventBrewfestgoblin01Time;

                                                           break;
                                                       case 1:
                                                           Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTGOBLIN02);
                                                           _musicTime = Misc.EventBrewfestgoblin02Time;

                                                           break;
                                                       default:
                                                           Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTGOBLIN03);
                                                           _musicTime = Misc.EventBrewfestgoblin03Time;

                                                           break;
                                                   }
                                               else
                                                   switch (_rnd)
                                                   {
                                                       case 0:
                                                           Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTDWARF01);
                                                           _musicTime = Misc.EventBrewfestdwarf01Time;

                                                           break;
                                                       case 1:
                                                           Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTDWARF02);
                                                           _musicTime = Misc.EventBrewfestdwarf02Time;

                                                           break;
                                                       default:
                                                           Me.PlayDirectMusic(SoundIds.EVENT_BREWFESTDWARF03);
                                                           _musicTime = Misc.EventBrewfestdwarf03Time;

                                                           break;
                                                   }

                                           break;
                                   }

                                   task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client
                               }
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

[Script]
internal class GOMidsummerMusic : GameObjectAI
{
    public GOMidsummerMusic(GameObject go) : base(go)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.MidsummerFireFestival))
                                   return;

                               var playersNearby = Me.GetPlayerListInGrid(Me.Map.VisibilityRange);

                               foreach (Player player in playersNearby)
                                   if (player.Team == TeamFaction.Horde)
                                       Me.PlayDirectMusic(12325, player);
                                   else
                                       Me.PlayDirectMusic(12319, player);

                               task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client (sniffed value)
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

[Script]
internal class GODarkmoonFaireMusic : GameObjectAI
{
    public GODarkmoonFaireMusic(GameObject go) : base(go)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               if (Global.GameEventMgr.IsHolidayActive(HolidayIds.DarkmoonFaire))
                               {
                                   Me.PlayDirectMusic(8440);
                                   task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client (sniffed value)
                               }
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

[Script]
internal class GOPirateDayMusic : GameObjectAI
{
    public GOPirateDayMusic(GameObject go) : base(go)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.PiratesDay))
                                   return;

                               Me.PlayDirectMusic(12845);
                               task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client (sniffed value)
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

[Script]
internal class GOBells : GameObjectAI
{
    private uint _soundId;

    public GOBells(GameObject go) : base(go) { }

    public override void InitializeAI()
    {
        var zoneId = Me.Zone;

        switch (Me.Entry)
        {
            case GameObjectIds.HORDE_BELL:
            {
                switch (zoneId)
                {
                    case ZoneIds.TIRISFAL:
                    case ZoneIds.UNDERCITY:
                    case ZoneIds.HILLSBRAD_FOOTHILLS:
                    case ZoneIds.DUSKWOOD:
                        _soundId = SoundIds.BELL_TOLL_HORDE; // undead bell sound

                        break;
                    default:
                        _soundId = SoundIds.BELL_TOLL_TRIBAL; // orc drum sound 

                        break;
                }

                break;
            }
            case GameObjectIds.ALLIANCE_BELL:
            {
                switch (zoneId)
                {
                    case ZoneIds.IRONFORGE:
                    case ZoneIds.DUN_MOROGH:
                        _soundId = SoundIds.BELL_TOLLDWARFGNOME; // horn sound

                        break;
                    case ZoneIds.DARNASSUS:
                    case ZoneIds.TELDRASSIL:
                    case ZoneIds.ASHENVALE:
                        _soundId = SoundIds.BELL_TOLL_NIGHTELF; // nightelf bell sound

                        break;
                    default:
                        _soundId = SoundIds.BELL_TOLL_ALLIANCE; // human bell sound

                        break;
                }

                break;
            }
            case GameObjectIds.KHARAZHAN_BELL:
                _soundId = SoundIds.BELL_TOLL_KHARAZHAN;

                break;
        }
    }

    public override void OnGameEvent(bool start, ushort eventId)
    {
        if (eventId == Misc.GAME_EVENT_HOURLY_BELLS && start)
        {
            var localTm = Time.UnixTimeToDateTime(GameTime.GetGameTime()).ToLocalTime();
            var rings = localTm.Hour % 12;

            if (rings == 0) // 00:00 and 12:00
                rings = 12;

            // Dwarf hourly horn should only play a single Time, each Time the next hour begins.
            if (_soundId == SoundIds.BELL_TOLLDWARFGNOME)
                rings = 1;

            for (var i = 0; i < rings; ++i)
                Scheduler.Schedule(TimeSpan.FromSeconds(i * 4 + 1), task => Me.PlayDirectSound(_soundId));
        }
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}