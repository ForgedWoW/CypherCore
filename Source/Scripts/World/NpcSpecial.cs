// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Movement;
using Forged.MapServer.Quest;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.Text;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Scripts.World.NpcSpecial;

internal enum SpawnType
{
    Tripwire, // no warning, summon Creature at smaller range
    AlarmBot  // cast guards mark and summon npc - if player shows up with that buff duration < 5 seconds attack
}

internal class AirForceSpawn
{
    public uint MyEntry;
    public uint OtherEntry;
    public SpawnType SpawnType;

    public AirForceSpawn(uint myEntry, uint otherEntry, SpawnType spawnType)
    {
        MyEntry = myEntry;
        OtherEntry = otherEntry;
        SpawnType = spawnType;
    }
}

internal struct CreatureIds
{
    //Torchtossingtarget
    public const uint TORCH_TOSSING_TARGET_BUNNY = 25535;

    //Garments
    public const uint SHAYA = 12429;
    public const uint ROBERTS = 12423;
    public const uint DOLF = 12427;
    public const uint KORJA = 12430;
    public const uint DG_KEL = 12428;

    //Doctor
    public const uint DOCTOR_ALLIANCE = 12939;
    public const uint DOCTOR_HORDE = 12920;

    //Fireworks
    public const uint OMEN = 15467;
    public const uint MINION_OF_OMEN = 15466;
    public const uint FIREWORK_BLUE = 15879;
    public const uint FIREWORK_GREEN = 15880;
    public const uint FIREWORK_PURPLE = 15881;
    public const uint FIREWORK_RED = 15882;
    public const uint FIREWORK_YELLOW = 15883;
    public const uint FIREWORK_WHITE = 15884;
    public const uint FIREWORK_BIG_BLUE = 15885;
    public const uint FIREWORK_BIG_GREEN = 15886;
    public const uint FIREWORK_BIG_PURPLE = 15887;
    public const uint FIREWORK_BIG_RED = 15888;
    public const uint FIREWORK_BIG_YELLOW = 15889;
    public const uint FIREWORK_BIG_WHITE = 15890;

    public const uint CLUSTER_BLUE = 15872;
    public const uint CLUSTER_RED = 15873;
    public const uint CLUSTER_GREEN = 15874;
    public const uint CLUSTER_PURPLE = 15875;
    public const uint CLUSTER_WHITE = 15876;
    public const uint CLUSTER_YELLOW = 15877;
    public const uint CLUSTER_BIG_BLUE = 15911;
    public const uint CLUSTER_BIG_GREEN = 15912;
    public const uint CLUSTER_BIG_PURPLE = 15913;
    public const uint CLUSTER_BIG_RED = 15914;
    public const uint CLUSTER_BIG_WHITE = 15915;
    public const uint CLUSTER_BIG_YELLOW = 15916;
    public const uint CLUSTER_ELUNE = 15918;

    // Rabbitspells
    public const uint SPRING_RABBIT = 32791;

    // TrainWrecker
    public const uint EXULTING_WIND_UP_TRAIN_WRECKER = 81071;

    // Argent squire/gruntling
    public const uint ARGENT_SQUIRE = 33238;

    // BountifulTable
    public const uint THE_TURKEY_CHAIR = 34812;
    public const uint THE_CRANBERRY_CHAIR = 34823;
    public const uint THE_STUFFING_CHAIR = 34819;
    public const uint THE_SWEET_POTATO_CHAIR = 34824;
    public const uint THE_PIE_CHAIR = 34822;

    // TravelerTundraMammothNPCs
    public const uint HAKMUD_OF_ARGUS = 32638;
    public const uint GNIMO = 32639;
    public const uint DRIX_BLACKWRENCH = 32641;
    public const uint MOJODISHU = 32642;

    // BrewfestReveler2
    public const uint BREWFEST_REVELER = 24484;
}

internal struct GameobjectIds
{
    //Fireworks
    public const uint FIREWORK_LAUNCHER1 = 180771;
    public const uint FIREWORK_LAUNCHER2 = 180868;
    public const uint FIREWORK_LAUNCHER3 = 180850;
    public const uint CLUSTER_LAUNCHER1 = 180772;
    public const uint CLUSTER_LAUNCHER2 = 180859;
    public const uint CLUSTER_LAUNCHER3 = 180869;
    public const uint CLUSTER_LAUNCHER4 = 180874;

    //TrainWrecker
    public const uint TOY_TRAIN = 193963;

    //RibbonPole
    public const uint RIBBON_POLE = 181605;
}

internal struct SpellIds
{
    public const uint GUARDS_MARK = 38067;

    //Dancingflames
    public const uint SUMMON_BRAZIER = 45423;
    public const uint BRAZIER_DANCE = 45427;
    public const uint FIERY_SEDUCTION = 47057;

    //RibbonPole
    public const uint RIBBON_DANCE_COSMETIC = 29726;
    public const uint RED_FIRE_RING = 46836;
    public const uint BLUE_FIRE_RING = 46842;

    //Torchtossingtarget
    public const uint TARGET_INDICATOR = 45723;

    //Garments    
    public const uint LESSER_HEAL_R2 = 2052;
    public const uint FORTITUDE_R1 = 1243;

    //Guardianspells
    public const uint DEATHTOUCH = 5;

    //Brewfestreveler
    public const uint BREWFEST_TOAST = 41586;

    //Wormholespells
    public const uint BOREAN_TUNDRA = 67834;
    public const uint SHOLAZAR_BASIN = 67835;
    public const uint ICECROWN = 67836;
    public const uint STORM_PEAKS = 67837;
    public const uint HOWLING_FJORD = 67838;
    public const uint UNDERGROUND = 68081;

    //Rabbitspells
    public const uint SPRING_FLING = 61875;
    public const uint SPRING_RABBIT_JUMP = 61724;
    public const uint SPRING_RABBIT_WANDER = 61726;
    public const uint SUMMON_BABY_BUNNY = 61727;
    public const uint SPRING_RABBIT_IN_LOVE = 61728;

    //TrainWrecker
    public const uint TOY_TRAIN_PULSE = 61551;
    public const uint WRECK_TRAIN = 62943;

    //Argent squire/gruntling
    public const uint DARNASSUS_PENNANT = 63443;
    public const uint EXODAR_PENNANT = 63439;
    public const uint GNOMEREGAN_PENNANT = 63442;
    public const uint IRONFORGE_PENNANT = 63440;
    public const uint STORMWIND_PENNANT = 62727;
    public const uint SENJIN_PENNANT = 63446;
    public const uint UNDERCITY_PENNANT = 63441;
    public const uint ORGRIMMAR_PENNANT = 63444;
    public const uint SILVERMOON_PENNANT = 63438;
    public const uint THUNDERBLUFF_PENNANT = 63445;
    public const uint AURA_POSTMAN_S = 67376;
    public const uint AURA_SHOP_S = 67377;
    public const uint AURA_BANK_S = 67368;
    public const uint AURA_TIRED_S = 67401;
    public const uint AURA_BANK_G = 68849;
    public const uint AURA_POSTMAN_G = 68850;
    public const uint AURA_SHOP_G = 68851;
    public const uint AURA_TIRED_G = 68852;
    public const uint TIRED_PLAYER = 67334;

    //BountifulTable
    public const uint CRANBERRY_SERVER = 61793;
    public const uint PIE_SERVER = 61794;
    public const uint STUFFING_SERVER = 61795;
    public const uint TURKEY_SERVER = 61796;
    public const uint SWEET_POTATOES_SERVER = 61797;

    //VoidZone
    public const uint CONSUMPTION = 28874;
}

internal struct QuestConst
{
    //Lunaclawspirit
    public const uint BODY_HEART_A = 6001;
    public const uint BODY_HEART_H = 6002;

    //ChickenCluck
    public const uint CLUCK = 3861;

    //Garments
    public const uint MOON = 5621;
    public const uint LIGHT1 = 5624;
    public const uint LIGHT2 = 5625;
    public const uint SPIRIT = 5648;
    public const uint DARKNESS = 5650;
}

internal struct TextIds
{
    //Lunaclawspirit
    public const uint TEXT_ID_DEFAULT = 4714;
    public const uint TEXT_ID_PROGRESS = 4715;

    //Chickencluck
    public const uint EMOTE_HELLO_A = 0;
    public const uint EMOTE_HELLO_H = 1;
    public const uint EMOTE_CLUCK = 2;

    //Doctor
    public const uint SAY_DOC = 0;

    //    Garments
    // Used By 12429; 12423; 12427; 12430; 12428; But Signed For 12429
    public const uint SAY_THANKS = 0;
    public const uint SAY_GOODBYE = 1;
    public const uint SAY_HEALED = 2;

    //Wormholespells
    public const uint WORMHOLE = 14785;

    //NpcExperience
    public const uint XP_ON_OFF = 14736;
}

internal struct GossipMenus
{
    //Wormhole
    public const int MENU_ID_WORMHOLE = 10668; // "This tear in the fabric of Time and space looks ominous."
    public const int OPTION_ID_WORMHOLE1 = 0;  // "Borean Tundra"
    public const int OPTION_ID_WORMHOLE2 = 1;  // "Howling Fjord"
    public const int OPTION_ID_WORMHOLE3 = 2;  // "Sholazar Basin"
    public const int OPTION_ID_WORMHOLE4 = 3;  // "Icecrown"
    public const int OPTION_ID_WORMHOLE5 = 4;  // "Storm Peaks"
    public const int OPTION_ID_WORMHOLE6 = 5;  // "Underground..."

    //Lunaclawspirit
    public const string ITEM_GRANT = "You Have Thought Well; Spirit. I Ask You To Grant Me The Strength Of Your Body And The Strength Of Your Heart.";

    //Pettrainer
    public const uint MENU_ID_PET_UNLEARN = 6520;
    public const uint OPTION_ID_PLEASE_DO = 0;

    //NpcExperience
    public const uint MENU_ID_XP_ON_OFF = 10638;
    public const uint OPTION_ID_XP_OFF = 0;
    public const uint OPTION_ID_XP_ON = 1;

    //Argent squire/gruntling
    public const uint OPTION_ID_BANK = 0;
    public const uint OPTION_ID_SHOP = 1;
    public const uint OPTION_ID_MAIL = 2;
    public const uint OPTION_ID_DARNASSUS_SENJIN_PENNANT = 3;
    public const uint OPTION_ID_EXODAR_UNDERCITY_PENNANT = 4;
    public const uint OPTION_ID_GNOMEREGAN_ORGRIMMAR_PENNANT = 5;
    public const uint OPTION_ID_IRONFORGE_SILVERMOON_PENNANT = 6;
    public const uint OPTION_ID_STORMWIND_THUNDERBLUFF_PENNANT = 7;
}

internal enum SeatIds
{
    //BountifulTable
    TurkeyChair = 0,
    CranberryChair = 1,
    StuffingChair = 2,
    SweetPotatoChair = 3,
    PieChair = 4,
    FoodHolder = 5,
    PlateHolder = 6
}

internal struct Misc
{
    public static AirForceSpawn[] AirforceSpawns =
    {
        new(2614, 15241, SpawnType.AlarmBot),  //Air Force Alarm Bot (Alliance)
        new(2615, 15242, SpawnType.AlarmBot),  //Air Force Alarm Bot (Horde)
        new(21974, 21976, SpawnType.AlarmBot), //Air Force Alarm Bot (Area 52)
        new(21993, 15242, SpawnType.AlarmBot), //Air Force Guard Post (Horde - Bat Rider)
        new(21996, 15241, SpawnType.AlarmBot), //Air Force Guard Post (Alliance - Gryphon)
        new(21997, 21976, SpawnType.AlarmBot), //Air Force Guard Post (Goblin - Area 52 - Zeppelin)
        new(21999, 15241, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Alliance)
        new(22001, 15242, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Horde)
        new(22002, 15242, SpawnType.Tripwire), //Air Force Trip Wire - Ground (Horde)
        new(22003, 15241, SpawnType.Tripwire), //Air Force Trip Wire - Ground (Alliance)
        new(22063, 21976, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Goblin - Area 52)
        new(22065, 22064, SpawnType.AlarmBot), //Air Force Guard Post (Ethereal - Stormspire)
        new(22066, 22067, SpawnType.AlarmBot), //Air Force Guard Post (Scryer - Dragonhawk)
        new(22068, 22064, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Ethereal - Stormspire)
        new(22069, 22064, SpawnType.AlarmBot), //Air Force Alarm Bot (Stormspire)
        new(22070, 22067, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Scryer)
        new(22071, 22067, SpawnType.AlarmBot), //Air Force Alarm Bot (Scryer)
        new(22078, 22077, SpawnType.AlarmBot), //Air Force Alarm Bot (Aldor)
        new(22079, 22077, SpawnType.AlarmBot), //Air Force Guard Post (Aldor - Gryphon)
        new(22080, 22077, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Aldor)
        new(22086, 22085, SpawnType.AlarmBot), //Air Force Alarm Bot (Sporeggar)
        new(22087, 22085, SpawnType.AlarmBot), //Air Force Guard Post (Sporeggar - Spore Bat)
        new(22088, 22085, SpawnType.Tripwire), //Air Force Trip Wire - Rooftop (Sporeggar)
        new(22090, 22089, SpawnType.AlarmBot), //Air Force Guard Post (Toshley's Station - Flying Machine)
        new(22124, 22122, SpawnType.AlarmBot), //Air Force Alarm Bot (Cenarion)
        new(22125, 22122, SpawnType.AlarmBot), //Air Force Guard Post (Cenarion - Stormcrow)
        new(22126, 22122, SpawnType.AlarmBot)  //Air Force Trip Wire - Rooftop (Cenarion Expedition)
    };

    public const float RANGE_TRIPWIRE = 15.0f;
    public const float RANGE_ALARMBOT = 100.0f;

    //ChickenCluck
    public const uint FACTION_FRIENDLY = 35;
    public const uint FACTION_CHICKEN = 31;

    //Doctor
    public static Position[] DoctorAllianceCoords =
    {
        new(-3757.38f, -4533.05f, 14.16f, 3.62f), // Top-far-right bunk as seen from entrance
        new(-3754.36f, -4539.13f, 14.16f, 5.13f), // Top-far-left bunk
        new(-3749.54f, -4540.25f, 14.28f, 3.34f), // Far-right bunk
        new(-3742.10f, -4536.85f, 14.28f, 3.64f), // Right bunk near entrance
        new(-3755.89f, -4529.07f, 14.05f, 0.57f), // Far-left bunk
        new(-3749.51f, -4527.08f, 14.07f, 5.26f), // Mid-left bunk
        new(-3746.37f, -4525.35f, 14.16f, 5.22f)  // Left bunk near entrance
    };

    //alliance run to where
    public static Position DoctorAllianceRunTo = new(-3742.96f, -4531.52f, 11.91f);

    public static Position[] DoctorHordeCoords =
    {
        new(-1013.75f, -3492.59f, 62.62f, 4.34f), // Left, Behind
        new(-1017.72f, -3490.92f, 62.62f, 4.34f), // Right, Behind
        new(-1015.77f, -3497.15f, 62.82f, 4.34f), // Left, Mid
        new(-1019.51f, -3495.49f, 62.82f, 4.34f), // Right, Mid
        new(-1017.25f, -3500.85f, 62.98f, 4.34f), // Left, front
        new(-1020.95f, -3499.21f, 62.98f, 4.34f)  // Right, Front
    };

    //horde run to where
    public static Position DoctorHordeRunTo = new(-1016.44f, -3508.48f, 62.96f);

    public static uint[] AllianceSoldierId =
    {
        12938, // 12938 Injured Alliance Soldier
        12936, // 12936 Badly injured Alliance Soldier
        12937  // 12937 Critically injured Alliance Soldier
    };

    public static uint[] HordeSoldierId =
    {
        12923, //12923 Injured Soldier
        12924, //12924 Badly injured Soldier
        12925  //12925 Critically injured Soldier
    };

    //    WormholeSpells
    public const uint DATA_SHOW_UNDERGROUND = 1;

    //Fireworks
    public const uint ANIM_GO_LAUNCH_FIREWORK = 3;
    public const uint ZONE_MOONGLADE = 493;

    public static Position OmenSummonPos = new(7558.993f, -2839.999f, 450.0214f, 4.46f);

    public const uint AURA_DURATION_TIME_LEFT = 30000;

    //Argent squire/gruntling
    public const uint ACHIEVEMENT_PONY_UP = 3736;

    public static Tuple<uint, uint>[] BannerSpells =
    {
        Tuple.Create(SpellIds.DARNASSUS_PENNANT, SpellIds.SENJIN_PENNANT), Tuple.Create(SpellIds.EXODAR_PENNANT, SpellIds.UNDERCITY_PENNANT), Tuple.Create(SpellIds.GNOMEREGAN_PENNANT, SpellIds.ORGRIMMAR_PENNANT), Tuple.Create(SpellIds.IRONFORGE_PENNANT, SpellIds.SILVERMOON_PENNANT), Tuple.Create(SpellIds.STORMWIND_PENNANT, SpellIds.THUNDERBLUFF_PENNANT)
    };
}

[Script]
internal class NPCAirForceBots : NullCreatureAI
{
    private readonly AirForceSpawn _spawn;
    private readonly List<ObjectGuid> _toAttack = new();
    private ObjectGuid _myGuard;

    public NPCAirForceBots(Creature creature) : base(creature)
    {
        _spawn = FindSpawnFor(creature.Entry);
    }

    public override void UpdateAI(uint diff)
    {
        if (_toAttack.Empty())
            return;

        var guard = GetOrSummonGuard();

        if (guard == null)
            return;

        // Keep the list of targets for later on when the guards will be alive
        if (!guard.IsAlive)
            return;

        for (var i = 0; i < _toAttack.Count; ++i)
        {
            var guid = _toAttack[i];

            var target = Global.ObjAccessor.GetUnit(Me, guid);

            if (!target)
                continue;

            if (guard.IsEngagedBy(target))
                continue;

            guard.EngageWithTarget(target);

            if (_spawn.SpawnType == SpawnType.AlarmBot)
                guard.SpellFactory.CastSpell(target, SpellIds.GUARDS_MARK, true);
        }

        _toAttack.Clear();
    }

    public override void MoveInLineOfSight(Unit who)
    {
        // guards are only spawned against players
        if (!who.IsPlayer)
            return;

        // we're already scheduled to attack this player on our next tick, don't bother checking
        if (_toAttack.Contains(who.GUID))
            return;

        // check if they're in range
        if (!who.IsWithinDistInMap(Me, (_spawn.SpawnType == SpawnType.AlarmBot) ? Misc.RANGE_ALARMBOT : Misc.RANGE_TRIPWIRE))
            return;

        // check if they're hostile
        if (!(Me.IsHostileTo(who) || who.IsHostileTo(Me)))
            return;

        // check if they're a valid attack Target
        if (!Me.IsValidAttackTarget(who))
            return;

        if ((_spawn.SpawnType == SpawnType.Tripwire) &&
            who.IsFlying)
            return;

        _toAttack.Add(who.GUID);
    }

    private static AirForceSpawn FindSpawnFor(uint entry)
    {
        foreach (var spawn in Misc.AirforceSpawns)
            if (spawn.MyEntry == entry)
                return spawn;

        return null;
    }

    private Creature GetOrSummonGuard()
    {
        var guard = ObjectAccessor.GetCreature(Me, _myGuard);

        if (guard == null &&
            (guard = Me.SummonCreature(_spawn.OtherEntry, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromMinutes(5))))
            _myGuard = guard.GUID;

        return guard;
    }
}

[Script]
internal class NPCChickenCluck : ScriptedAI
{
    private uint _resetFlagTimer;

    public NPCChickenCluck(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        Me.Faction = Misc.FACTION_CHICKEN;
        Me.RemoveNpcFlag(NPCFlags.QuestGiver);
    }

    public override void JustEngagedWith(Unit who) { }

    public override void UpdateAI(uint diff)
    {
        // Reset flags after a certain Time has passed so that the next player has to start the 'event' again
        if (Me.HasNpcFlag(NPCFlags.QuestGiver))
        {
            if (_resetFlagTimer <= diff)
            {
                EnterEvadeMode();

                return;
            }
            else
                _resetFlagTimer -= diff;
        }

        if (UpdateVictim())
            DoMeleeAttackIfReady();
    }

    public override void ReceiveEmote(Player player, TextEmotes emote)
    {
        switch (emote)
        {
            case TextEmotes.Chicken:
                if (player.GetQuestStatus(QuestConst.CLUCK) == QuestStatus.None &&
                    RandomHelper.Rand32() % 30 == 1)
                {
                    Me.SetNpcFlag(NPCFlags.QuestGiver);
                    Me.Faction = Misc.FACTION_FRIENDLY;
                    Talk(player.Team == TeamFaction.Horde ? TextIds.EMOTE_HELLO_H : TextIds.EMOTE_HELLO_A);
                }

                break;
            case TextEmotes.Cheer:
                if (player.GetQuestStatus(QuestConst.CLUCK) == QuestStatus.Complete)
                {
                    Me.SetNpcFlag(NPCFlags.QuestGiver);
                    Me.Faction = Misc.FACTION_FRIENDLY;
                    Talk(TextIds.EMOTE_CLUCK);
                }

                break;
        }
    }

    public override void OnQuestAccept(Player player, Quest quest)
    {
        if (quest.Id == QuestConst.CLUCK)
            Reset();
    }

    public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
    {
        if (quest.Id == QuestConst.CLUCK)
            Reset();
    }

    private void Initialize()
    {
        _resetFlagTimer = 120000;
    }
}

[Script]
internal class NPCDancingFlames : ScriptedAI
{
    public NPCDancingFlames(Creature creature) : base(creature) { }

    public override void Reset()
    {
        DoCastSelf(SpellIds.SUMMON_BRAZIER, new CastSpellExtraArgs(true));
        DoCastSelf(SpellIds.BRAZIER_DANCE, new CastSpellExtraArgs(false));
        Me.EmoteState = Emote.StateDance;
        Me.Location.Relocate(Me.Location.X, Me.Location.Y, Me.Location.Z + 1.05f);
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }

    public override void ReceiveEmote(Player player, TextEmotes emote)
    {
        if (Me.IsWithinLOS(player.Location.X, player.Location.Y, player.Location.Z) &&
            Me.IsWithinDistInMap(player, 30.0f))
        {
            // She responds to emotes not instantly but ~1500ms later
            // If you first /bow, then /wave before dancing flames bow back, it doesnt bow at all and only does wave
            // If you're performing emotes too fast, she will not respond to them
            // Means she just replaces currently scheduled event with new after receiving new Emote
            Scheduler.CancelAll();

            switch (emote)
            {
                case TextEmotes.Kiss:
                    Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotShy));

                    break;
                case TextEmotes.Wave:
                    Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotWave));

                    break;
                case TextEmotes.Bow:
                    Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotBow));

                    break;
                case TextEmotes.Joke:
                    Scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => Me.HandleEmoteCommand(Emote.OneshotLaugh));

                    break;
                case TextEmotes.Dance:
                    if (!player.HasAura(SpellIds.FIERY_SEDUCTION))
                    {
                        DoCast(player, SpellIds.FIERY_SEDUCTION, new CastSpellExtraArgs(true));
                        Me.SetFacingTo(Me.Location.GetAbsoluteAngle(player.Location));
                    }

                    break;
            }
        }
    }
}

[Script]
internal class NPCTorchTossingTargetBunnyController : ScriptedAI
{
    private ObjectGuid _lastTargetGUID;

    private uint _targetTimer;

    public NPCTorchTossingTargetBunnyController(Creature creature) : base(creature)
    {
        _targetTimer = 3000;
    }

    public override void UpdateAI(uint diff)
    {
        if (_targetTimer < diff)
        {
            var target = Global.ObjAccessor.GetUnit(Me, DoSearchForTargets(_lastTargetGUID));

            if (target)
                target.SpellFactory.CastSpell(target, SpellIds.TARGET_INDICATOR, true);

            _targetTimer = 3000;
        }
        else
            _targetTimer -= diff;
    }

    private ObjectGuid DoSearchForTargets(ObjectGuid lastTargetGUID)
    {
        var targets = Me.GetCreatureListWithEntryInGrid(CreatureIds.TORCH_TOSSING_TARGET_BUNNY, 60.0f);
        targets.RemoveAll(creature => creature.GUID == lastTargetGUID);

        if (!targets.Empty())
        {
            _lastTargetGUID = targets.SelectRandom().GUID;

            return _lastTargetGUID;
        }

        return ObjectGuid.Empty;
    }
}

[Script]
internal class NPCMidsummerBunnyPole : ScriptedAI
{
    private bool _running;

    public NPCMidsummerBunnyPole(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        Scheduler.SetValidator(() => _running);

        Scheduler.Schedule(TimeSpan.FromMilliseconds(1),
                           task =>
                           {
                               if (CheckNearbyPlayers())
                               {
                                   Reset();

                                   return;
                               }

                               var go = Me.FindNearestGameObject(GameobjectIds.RIBBON_POLE, 10.0f);

                               if (go)
                                   Me.SpellFactory.CastSpell(go, SpellIds.RED_FIRE_RING, true);

                               task.Schedule(TimeSpan.FromSeconds(5),
                                             task1 =>
                                             {
                                                 if (CheckNearbyPlayers())
                                                 {
                                                     Reset();

                                                     return;
                                                 }

                                                 go = Me.FindNearestGameObject(GameobjectIds.RIBBON_POLE, 10.0f);

                                                 if (go)
                                                     Me.SpellFactory.CastSpell(go, SpellIds.BLUE_FIRE_RING, true);

                                                 task.Repeat(TimeSpan.FromSeconds(5));
                                             });
                           });
    }

    public override void DoAction(int action)
    {
        // Don't start event if it's already running.
        if (_running)
            return;

        _running = true;
        //events.ScheduleEvent(EVENT_CAST_RED_FIRE_RING, 1);
    }

    public override void UpdateAI(uint diff)
    {
        if (!_running)
            return;

        Scheduler.Update(diff);
    }

    private void Initialize()
    {
        Scheduler.CancelAll();
        _running = false;
    }

    private bool CheckNearbyPlayers()
    {
        // Returns true if no nearby player has aura "Test Ribbon Pole Channel".
        List<Unit> players = new();
        var check = new UnitAuraCheck<Player>(true, SpellIds.RIBBON_DANCE_COSMETIC);
        var searcher = new PlayerListSearcher(Me, players, check);
        Cell.VisitGrid(Me, searcher, 10.0f);

        return players.Empty();
    }
}

[Script]
internal class NPCDoctor : ScriptedAI
{
    private readonly List<Position> _coordinates = new();

    private readonly List<ObjectGuid> _patients = new();

    private bool _event;
    private uint _patientDiedCount;
    private uint _patientSavedCount;

    private ObjectGuid _playerGUID;
    private uint _summonPatientCount;

    private uint _summonPatientTimer;

    public NPCDoctor(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        Me.RemoveUnitFlag(UnitFlags.Uninteractible);
    }

    public void BeginEvent(Player player)
    {
        _playerGUID = player.GUID;

        _summonPatientTimer = 10000;
        _summonPatientCount = 0;
        _patientDiedCount = 0;
        _patientSavedCount = 0;

        switch (Me.Entry)
        {
            case CreatureIds.DOCTOR_ALLIANCE:
                foreach (var coord in Misc.DoctorAllianceCoords)
                    _coordinates.Add(coord);

                break;
            case CreatureIds.DOCTOR_HORDE:
                foreach (var coord in Misc.DoctorHordeCoords)
                    _coordinates.Add(coord);

                break;
        }

        _event = true;
        Me.SetUnitFlag(UnitFlags.Uninteractible);
    }

    public void PatientDied(Position point)
    {
        var player = Global.ObjAccessor.GetPlayer(Me, _playerGUID);

        if (player && ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) || (player.GetQuestStatus(6622) == QuestStatus.Incomplete)))
        {
            ++_patientDiedCount;

            if (_patientDiedCount > 5 && _event)
            {
                if (player.GetQuestStatus(6624) == QuestStatus.Incomplete)
                    player.FailQuest(6624);
                else if (player.GetQuestStatus(6622) == QuestStatus.Incomplete)
                    player.FailQuest(6622);

                Reset();

                return;
            }

            _coordinates.Add(point);
        }
        else
            // If no player or player abandon quest in progress
            Reset();
    }

    public void PatientSaved(Creature soldier, Player player, Position point)
    {
        if (player && _playerGUID == player.GUID)
            if ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) ||
                (player.GetQuestStatus(6622) == QuestStatus.Incomplete))
            {
                ++_patientSavedCount;

                if (_patientSavedCount == 15)
                {
                    if (!_patients.Empty())
                        foreach (var guid in _patients)
                        {
                            var patient = ObjectAccessor.GetCreature(Me, guid);

                            if (patient)
                                patient.SetDeathState(DeathState.JustDied);
                        }

                    if (player.GetQuestStatus(6624) == QuestStatus.Incomplete)
                        player.AreaExploredOrEventHappens(6624);
                    else if (player.GetQuestStatus(6622) == QuestStatus.Incomplete)
                        player.AreaExploredOrEventHappens(6622);

                    Reset();

                    return;
                }

                _coordinates.Add(point);
            }
    }

    public override void UpdateAI(uint diff)
    {
        if (_event && _summonPatientCount >= 20)
        {
            Reset();

            return;
        }

        if (_event)
        {
            if (_summonPatientTimer <= diff)
            {
                if (_coordinates.Empty())
                    return;

                uint patientEntry;

                switch (Me.Entry)
                {
                    case CreatureIds.DOCTOR_ALLIANCE:
                        patientEntry = Misc.AllianceSoldierId[RandomHelper.Rand32() % 3];

                        break;
                    case CreatureIds.DOCTOR_HORDE:
                        patientEntry = Misc.HordeSoldierId[RandomHelper.Rand32() % 3];

                        break;
                    default:
                        Log.Logger.Error("Invalid entry for Triage doctor. Please check your database");

                        return;
                }

                var index = RandomHelper.IRand(0, _coordinates.Count - 1);

                Creature patient = Me.SummonCreature(patientEntry, _coordinates[index], TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(5));

                if (patient)
                {
                    //303, this flag appear to be required for client side Item.spell to work (TARGET_SINGLE_FRIEND)
                    patient.SetUnitFlag(UnitFlags.PlayerControlled);

                    _patients.Add(patient.GUID);
                    ((NPCInjuredPatient)patient.AI).DoctorGUID = Me.GUID;
                    ((NPCInjuredPatient)patient.AI).Coord = _coordinates[index];

                    _coordinates.RemoveAt(index);
                }

                _summonPatientTimer = 10000;
                ++_summonPatientCount;
            }
            else
                _summonPatientTimer -= diff;
        }
    }

    public override void JustEngagedWith(Unit who) { }

    public override void OnQuestAccept(Player player, Quest quest)
    {
        if ((quest.Id == 6624) ||
            (quest.Id == 6622))
            BeginEvent(player);
    }

    private void Initialize()
    {
        _playerGUID.Clear();

        _summonPatientTimer = 10000;
        _summonPatientCount = 0;
        _patientDiedCount = 0;
        _patientSavedCount = 0;

        _patients.Clear();
        _coordinates.Clear();

        _event = false;
    }
}

[Script]
public class NPCInjuredPatient : ScriptedAI
{
    public Position Coord;

    public ObjectGuid DoctorGUID;

    public NPCInjuredPatient(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();

        //no select
        Me.RemoveUnitFlag(UnitFlags.Uninteractible);

        //no regen health
        Me.SetUnitFlag(UnitFlags.InCombat);

        //to make them lay with face down
        Me.SetStandState(UnitStandStateType.Dead);

        var mobId = Me.Entry;

        switch (mobId)
        {
            //lower max health
            case 12923:
            case 12938: //Injured Soldier
                Me.SetHealth(Me.CountPctFromMaxHealth(75));

                break;
            case 12924:
            case 12936: //Badly injured Soldier
                Me.SetHealth(Me.CountPctFromMaxHealth(50));

                break;
            case 12925:
            case 12937: //Critically injured Soldier
                Me.SetHealth(Me.CountPctFromMaxHealth(25));

                break;
        }
    }

    public override void JustEngagedWith(Unit who) { }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        var player = caster.AsPlayer;

        if (!player ||
            !Me.IsAlive ||
            spellInfo.Id != 20804)
            return;

        if (player.GetQuestStatus(6624) == QuestStatus.Incomplete ||
            player.GetQuestStatus(6622) == QuestStatus.Incomplete)
            if (!DoctorGUID.IsEmpty)
            {
                var doctor = ObjectAccessor.GetCreature(Me, DoctorGUID);

                if (doctor)
                    ((NPCDoctor)doctor.AI).PatientSaved(Me, player, Coord);
            }

        //make not selectable
        Me.SetUnitFlag(UnitFlags.Uninteractible);

        //regen health
        Me.RemoveUnitFlag(UnitFlags.InCombat);

        //stand up
        Me.SetStandState(UnitStandStateType.Stand);

        Talk(TextIds.SAY_DOC);

        var mobId = Me.Entry;
        Me.SetWalk(false);

        switch (mobId)
        {
            case 12923:
            case 12924:
            case 12925:
                Me.MotionMaster.MovePoint(0, Misc.DoctorHordeRunTo);

                break;
            case 12936:
            case 12937:
            case 12938:
                Me.MotionMaster.MovePoint(0, Misc.DoctorAllianceRunTo);

                break;
        }
    }

    public override void UpdateAI(uint diff)
    {
        //lower HP on every world tick makes it a useful counter, not officlone though
        if (Me.IsAlive &&
            Me.Health > 6)
            Me.ModifyHealth(-5);

        if (Me.IsAlive &&
            Me.Health <= 6)
        {
            Me.RemoveUnitFlag(UnitFlags.InCombat);
            Me.SetUnitFlag(UnitFlags.Uninteractible);
            Me.SetDeathState(DeathState.JustDied);
            Me.SetUnitFlag3(UnitFlags3.FakeDead);

            if (!DoctorGUID.IsEmpty)
            {
                var doctor = ObjectAccessor.GetCreature((Me), DoctorGUID);

                if (doctor)
                    ((NPCDoctor)doctor.AI).PatientDied(Coord);
            }
        }
    }

    private void Initialize()
    {
        DoctorGUID.Clear();
        Coord = null;
    }
}

[Script]
internal class NPCGarmentsOfQuests : EscortAI
{
    private readonly uint _quest;
    private bool _canRun;
    private ObjectGuid _casterGUID;

    private bool _isHealed;

    private uint _runAwayTimer;

    public NPCGarmentsOfQuests(Creature creature) : base(creature)
    {
        switch (Me.Entry)
        {
            case CreatureIds.SHAYA:
                _quest = QuestConst.MOON;

                break;
            case CreatureIds.ROBERTS:
                _quest = QuestConst.LIGHT1;

                break;
            case CreatureIds.DOLF:
                _quest = QuestConst.LIGHT2;

                break;
            case CreatureIds.KORJA:
                _quest = QuestConst.SPIRIT;

                break;
            case CreatureIds.DG_KEL:
                _quest = QuestConst.DARKNESS;

                break;
            default:
                _quest = 0;

                break;
        }

        Initialize();
    }

    public override void Reset()
    {
        _casterGUID.Clear();

        Initialize();

        Me.SetStandState(UnitStandStateType.Kneel);
        // expect database to have RegenHealth=0
        Me.SetHealth(Me.CountPctFromMaxHealth(70));
    }

    public override void JustEngagedWith(Unit who) { }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.LESSER_HEAL_R2 ||
            spellInfo.Id == SpellIds.FORTITUDE_R1)
        {
            //not while in combat
            if (Me.IsInCombat)
                return;

            //nothing to be done now
            if (_isHealed && _canRun)
                return;

            var player = caster.AsPlayer;

            if (player)
            {
                if (_quest != 0 &&
                    player.GetQuestStatus(_quest) == QuestStatus.Incomplete)
                {
                    if (_isHealed &&
                        !_canRun &&
                        spellInfo.Id == SpellIds.FORTITUDE_R1)
                    {
                        Talk(TextIds.SAY_THANKS, player);
                        _canRun = true;
                    }
                    else if (!_isHealed &&
                             spellInfo.Id == SpellIds.LESSER_HEAL_R2)
                    {
                        _casterGUID = player.GUID;
                        Me.SetStandState(UnitStandStateType.Stand);
                        Talk(TextIds.SAY_HEALED, player);
                        _isHealed = true;
                    }
                }

                // give quest credit, not expect any special quest objectives
                if (_canRun)
                    player.TalkedToCreature(Me.Entry, Me.GUID);
            }
        }
    }

    public override void WaypointReached(uint waypointId, uint pathId) { }

    public override void UpdateAI(uint diff)
    {
        if (_canRun && !Me.IsInCombat)
        {
            if (_runAwayTimer <= diff)
            {
                var unit = Global.ObjAccessor.GetUnit(Me, _casterGUID);

                if (unit)
                {
                    switch (Me.Entry)
                    {
                        case CreatureIds.SHAYA:
                        case CreatureIds.ROBERTS:
                        case CreatureIds.DOLF:
                        case CreatureIds.KORJA:
                        case CreatureIds.DG_KEL:
                            Talk(TextIds.SAY_GOODBYE, unit);

                            break;
                    }

                    Start(false, true);
                }
                else
                    EnterEvadeMode(); //something went wrong

                _runAwayTimer = 30000;
            }
            else
                _runAwayTimer -= diff;
        }

        base.UpdateAI(diff);
    }

    private void Initialize()
    {
        _isHealed = false;
        _canRun = false;

        _runAwayTimer = 5000;
    }
}

[Script]
internal class NPCGuardian : ScriptedAI
{
    public NPCGuardian(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Me.SetUnitFlag(UnitFlags.NonAttackable);
    }

    public override void JustEngagedWith(Unit who) { }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (Me.IsAttackReady())
        {
            DoCastVictim(SpellIds.DEATHTOUCH, new CastSpellExtraArgs(true));
            Me.ResetAttackTimer();
        }
    }
}

[Script]
internal class NPCSteamTonk : ScriptedAI
{
    public NPCSteamTonk(Creature creature) : base(creature) { }

    public override void Reset() { }

    public override void JustEngagedWith(Unit who) { }

    public void OnPossess(bool apply)
    {
        if (apply)
        {
            // Initialize the Action bar without the melee attack command
            Me.InitCharmInfo();
            Me.GetCharmInfo().InitEmptyActionBar(false);

            Me.ReactState = ReactStates.Passive;
        }
        else
            Me.ReactState = ReactStates.Aggressive;
    }
}

[Script]
internal class NPCBrewfestReveler : ScriptedAI
{
    public NPCBrewfestReveler(Creature creature) : base(creature) { }

    public override void ReceiveEmote(Player player, TextEmotes emote)
    {
        if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
            return;

        if (emote == TextEmotes.Dance)
            Me.SpellFactory.CastSpell(player, SpellIds.BREWFEST_TOAST, false);
    }
}

[Script]
internal class NPCBrewfestReveler2 : ScriptedAI
{
    private readonly List<ObjectGuid> _revelerGuids = new();

    private readonly Emote[] _brewfestRandomEmote =
    {
        Emote.OneshotQuestion, Emote.OneshotApplaud, Emote.OneshotShout, Emote.OneshotEatNoSheathe, Emote.OneshotLaughNoSheathe
    };

    public NPCBrewfestReveler2(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           TimeSpan.FromSeconds(2),
                           fillListTask =>
                           {
                               var creatureList = Me.GetCreatureListWithEntryInGrid(CreatureIds.BREWFEST_REVELER, 5.0f);

                               foreach (var creature in creatureList)
                                   if (creature != Me)
                                       _revelerGuids.Add(creature.GUID);

                               fillListTask.Schedule(TimeSpan.FromSeconds(1),
                                                     TimeSpan.FromSeconds(2),
                                                     faceToTask =>
                                                     {
                                                         // Turn to random brewfest reveler within set range
                                                         if (!_revelerGuids.Empty())
                                                         {
                                                             var creature = ObjectAccessor.GetCreature(Me, _revelerGuids.SelectRandom());

                                                             if (creature != null)
                                                                 Me.SetFacingToObject(creature);
                                                         }

                                                         Scheduler.Schedule(TimeSpan.FromSeconds(2),
                                                                            TimeSpan.FromSeconds(6),
                                                                            emoteTask =>
                                                                            {
                                                                                var nextTask = (TaskContext task) =>
                                                                                {
                                                                                    // If dancing stop before next random State
                                                                                    if (Me.EmoteState == Emote.StateDance)
                                                                                        Me.EmoteState = Emote.OneshotNone;

                                                                                    // Random EVENT_EMOTE or EVENT_FACETO
                                                                                    if (RandomHelper.randChance(50))
                                                                                        faceToTask.Repeat(TimeSpan.FromSeconds(1));
                                                                                    else
                                                                                        emoteTask.Repeat(TimeSpan.FromSeconds(1));
                                                                                };

                                                                                // Play random Emote or dance
                                                                                if (RandomHelper.randChance(50))
                                                                                {
                                                                                    Me.HandleEmoteCommand(_brewfestRandomEmote.SelectRandom());
                                                                                    Scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), nextTask);
                                                                                }
                                                                                else
                                                                                {
                                                                                    Me.EmoteState = Emote.StateDance;
                                                                                    Scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), nextTask);
                                                                                }
                                                                            });
                                                     });
                           });
    }

    // Copied from old script. I don't know if this is 100% correct.
    public override void ReceiveEmote(Player player, TextEmotes emote)
    {
        if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
            return;

        if (emote == TextEmotes.Dance)
            Me.SpellFactory.CastSpell(player, SpellIds.BREWFEST_TOAST, false);
    }

    public override void UpdateAI(uint diff)
    {
        UpdateVictim();

        Scheduler.Update(diff);
    }
}

[Script]
internal class NPCWormhole : PassiveAI
{
    private bool _showUnderground;

    public NPCWormhole(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void InitializeAI()
    {
        Initialize();
    }

    public override bool OnGossipHello(Player player)
    {
        player.InitGossipMenu(GossipMenus.MENU_ID_WORMHOLE);

        if (Me.IsSummon)
            if (player == Me.ToTempSummon().Summoner)
            {
                player.AddGossipItem(GossipMenus.MENU_ID_WORMHOLE, GossipMenus.OPTION_ID_WORMHOLE1, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);
                player.AddGossipItem(GossipMenus.MENU_ID_WORMHOLE, GossipMenus.OPTION_ID_WORMHOLE2, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);
                player.AddGossipItem(GossipMenus.MENU_ID_WORMHOLE, GossipMenus.OPTION_ID_WORMHOLE3, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 3);
                player.AddGossipItem(GossipMenus.MENU_ID_WORMHOLE, GossipMenus.OPTION_ID_WORMHOLE4, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 4);
                player.AddGossipItem(GossipMenus.MENU_ID_WORMHOLE, GossipMenus.OPTION_ID_WORMHOLE5, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 5);

                if (_showUnderground)
                    player.AddGossipItem(GossipMenus.MENU_ID_WORMHOLE, GossipMenus.OPTION_ID_WORMHOLE6, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 6);

                player.SendGossipMenu(TextIds.WORMHOLE, Me.GUID);
            }

        return true;
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
        player.PlayerTalkClass.ClearMenus();

        switch (action)
        {
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 1: // Borean Tundra
                player.CloseGossipMenu();
                DoCast(player, SpellIds.BOREAN_TUNDRA, new CastSpellExtraArgs(false));

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 2: // Howling Fjord
                player.CloseGossipMenu();
                DoCast(player, SpellIds.HOWLING_FJORD, new CastSpellExtraArgs(false));

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 3: // Sholazar Basin
                player.CloseGossipMenu();
                DoCast(player, SpellIds.SHOLAZAR_BASIN, new CastSpellExtraArgs(false));

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 4: // Icecrown
                player.CloseGossipMenu();
                DoCast(player, SpellIds.ICECROWN, new CastSpellExtraArgs(false));

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 5: // Storm peaks
                player.CloseGossipMenu();
                DoCast(player, SpellIds.STORM_PEAKS, new CastSpellExtraArgs(false));

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 6: // Underground
                player.CloseGossipMenu();
                DoCast(player, SpellIds.UNDERGROUND, new CastSpellExtraArgs(false));

                break;
        }

        return true;
    }

    private void Initialize()
    {
        _showUnderground = RandomHelper.URand(0, 100) == 0; // Guessed value, it is really rare though
    }
}

[Script]
internal class NPCSpringRabbit : ScriptedAI
{
    private uint _bunnyTimer;

    private bool _inLove;
    private uint _jumpTimer;
    private ObjectGuid _rabbitGUID;
    private uint _searchTimer;

    public NPCSpringRabbit(Creature creature) : base(creature)
    {
        Initialize();
    }

    public override void Reset()
    {
        Initialize();
        var owner = Me.OwnerUnit;

        if (owner)
            Me.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
    }

    public override void JustEngagedWith(Unit who) { }

    public override void DoAction(int param)
    {
        _inLove = true;
        var owner = Me.OwnerUnit;

        if (owner)
            owner.SpellFactory.CastSpell(owner, SpellIds.SPRING_FLING, true);
    }

    public override void UpdateAI(uint diff)
    {
        if (_inLove)
        {
            if (_jumpTimer <= diff)
            {
                var rabbit = Global.ObjAccessor.GetUnit(Me, _rabbitGUID);

                if (rabbit)
                    DoCast(rabbit, SpellIds.SPRING_RABBIT_JUMP);

                _jumpTimer = RandomHelper.URand(5000, 10000);
            }
            else
                _jumpTimer -= diff;

            if (_bunnyTimer <= diff)
            {
                DoCast(SpellIds.SUMMON_BABY_BUNNY);
                _bunnyTimer = RandomHelper.URand(20000, 40000);
            }
            else
                _bunnyTimer -= diff;
        }
        else
        {
            if (_searchTimer <= diff)
            {
                var rabbit = Me.FindNearestCreature(CreatureIds.SPRING_RABBIT, 10.0f);

                if (rabbit)
                {
                    if (rabbit == Me ||
                        rabbit.HasAura(SpellIds.SPRING_RABBIT_IN_LOVE))
                        return;

                    Me.AddAura(SpellIds.SPRING_RABBIT_IN_LOVE, Me);
                    DoAction(1);
                    rabbit.AddAura(SpellIds.SPRING_RABBIT_IN_LOVE, rabbit);
                    rabbit.AI.DoAction(1);
                    rabbit.SpellFactory.CastSpell(rabbit, SpellIds.SPRING_RABBIT_JUMP, true);
                    _rabbitGUID = rabbit.GUID;
                }

                _searchTimer = RandomHelper.URand(5000, 10000);
            }
            else
                _searchTimer -= diff;
        }
    }

    private void Initialize()
    {
        _inLove = false;
        _rabbitGUID.Clear();
        _jumpTimer = RandomHelper.URand(5000, 10000);
        _bunnyTimer = RandomHelper.URand(10000, 20000);
        _searchTimer = RandomHelper.URand(5000, 10000);
    }
}

[Script]
internal class NPCImpInABall : ScriptedAI
{
    private ObjectGuid _summonerGUID;

    public NPCImpInABall(Creature creature) : base(creature)
    {
        _summonerGUID.Clear();
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        if (summoner.IsTypeId(TypeId.Player))
        {
            _summonerGUID = summoner.GUID;

            Scheduler.Schedule(TimeSpan.FromSeconds(3),
                               task =>
                               {
                                   var owner = Global.ObjAccessor.GetPlayer(Me, _summonerGUID);

                                   if (owner)
                                       Global.CreatureTextMgr.SendChat(Me, 0, owner, owner.Group ? ChatMsg.MonsterParty : ChatMsg.MonsterWhisper, Language.Addon, CreatureTextRange.Normal);
                               });
        }
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

internal struct TrainWrecker
{
    public const int EVENT_DO_JUMP = 1;
    public const int EVENT_DO_FACING = 2;
    public const int EVENT_DO_WRECK = 3;
    public const int EVENT_DO_DANCE = 4;
    public const uint MOVEID_CHASE = 1;
    public const uint MOVEID_JUMP = 2;
}

[Script]
internal class NPCTrainWrecker : NullCreatureAI
{
    private bool _isSearching;
    private byte _nextAction;
    private ObjectGuid _target;
    private uint _timer;

    public NPCTrainWrecker(Creature creature) : base(creature)
    {
        _isSearching = true;
        _nextAction = 0;
        _timer = 1 * Time.IN_MILLISECONDS;
    }

    public override void UpdateAI(uint diff)
    {
        if (_isSearching)
        {
            if (diff < _timer)
                _timer -= diff;
            else
            {
                var target = Me.FindNearestGameObject(GameobjectIds.TOY_TRAIN, 15.0f);

                if (target)
                {
                    _isSearching = false;
                    _target = target.GUID;
                    Me.SetWalk(true);
                    Me.MotionMaster.MovePoint(TrainWrecker.MOVEID_CHASE, target.GetNearPosition(3.0f, target.Location.GetAbsoluteAngle(Me.Location)));
                }
                else
                    _timer = 3 * Time.IN_MILLISECONDS;
            }
        }
        else
            switch (_nextAction)
            {
                case TrainWrecker.EVENT_DO_JUMP:
                {
                    var target = VerifyTarget();

                    if (target)
                        Me.MotionMaster.MoveJump(target.Location, 5.0f, 10.0f, TrainWrecker.MOVEID_JUMP);

                    _nextAction = 0;
                }

                    break;
                case TrainWrecker.EVENT_DO_FACING:
                {
                    var target = VerifyTarget();

                    if (target)
                    {
                        Me.SetFacingTo(target.Location.Orientation);
                        Me.HandleEmoteCommand(Emote.OneshotAttack1h);
                        _timer = (uint)(1.5 * Time.IN_MILLISECONDS);
                        _nextAction = TrainWrecker.EVENT_DO_WRECK;
                    }
                    else
                        _nextAction = 0;
                }

                    break;
                case TrainWrecker.EVENT_DO_WRECK:
                {
                    if (diff < _timer)
                    {
                        _timer -= diff;

                        break;
                    }

                    var target = VerifyTarget();

                    if (target)
                    {
                        Me.SpellFactory.CastSpell(target, SpellIds.WRECK_TRAIN, false);
                        _timer = 2 * Time.IN_MILLISECONDS;
                        _nextAction = TrainWrecker.EVENT_DO_DANCE;
                    }
                    else
                        _nextAction = 0;
                }

                    break;
                case TrainWrecker.EVENT_DO_DANCE:
                    if (diff < _timer)
                    {
                        _timer -= diff;

                        break;
                    }

                    Me.UpdateEntry(CreatureIds.EXULTING_WIND_UP_TRAIN_WRECKER);
                    Me.EmoteState = Emote.OneshotDance;
                    Me.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                    _nextAction = 0;

                    break;
            }
    }

    public override void MovementInform(MovementGeneratorType type, uint id)
    {
        if (id == TrainWrecker.MOVEID_CHASE)
            _nextAction = TrainWrecker.EVENT_DO_JUMP;
        else if (id == TrainWrecker.MOVEID_JUMP)
            _nextAction = TrainWrecker.EVENT_DO_FACING;
    }

    private GameObject VerifyTarget()
    {
        var target = ObjectAccessor.GetGameObject(Me, _target);

        if (target)
            return target;

        Me.HandleEmoteCommand(Emote.OneshotRude);
        Me.DespawnOrUnsummon(TimeSpan.FromSeconds(3));

        return null;
    }
}

[Script]
internal class NPCArgentSquireGruntling : ScriptedAI
{
    public NPCArgentSquireGruntling(Creature creature) : base(creature) { }

    public override void Reset()
    {
        var owner = Me.OwnerUnit?.AsPlayer;

        if (owner != null)
        {
            var ownerTired = owner.GetAura(SpellIds.TIRED_PLAYER);

            if (ownerTired != null)
            {
                var squireTired = Me.AddAura(IsArgentSquire() ? SpellIds.AURA_TIRED_S : SpellIds.AURA_TIRED_G, Me);

                squireTired?.SetDuration(ownerTired.Duration);
            }

            if (owner.HasAchieved(Misc.ACHIEVEMENT_PONY_UP) &&
                !Me.HasAura(SpellIds.AURA_TIRED_S) &&
                !Me.HasAura(SpellIds.AURA_TIRED_G))
            {
                Me.SetNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);

                return;
            }
        }

        Me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        switch (gossipListId)
        {
            case GossipMenus.OPTION_ID_BANK:
            {
                Me.RemoveNpcFlag(NPCFlags.Mailbox | NPCFlags.Vendor);
                var bankAura = IsArgentSquire() ? SpellIds.AURA_BANK_S : SpellIds.AURA_BANK_G;

                if (!Me.HasAura(bankAura))
                    DoCastSelf(bankAura);

                if (!player.HasAura(SpellIds.TIRED_PLAYER))
                    player.SpellFactory.CastSpell(player, SpellIds.TIRED_PLAYER, true);

                break;
            }
            case GossipMenus.OPTION_ID_SHOP:
            {
                Me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox);
                var shopAura = IsArgentSquire() ? SpellIds.AURA_SHOP_S : SpellIds.AURA_SHOP_G;

                if (!Me.HasAura(shopAura))
                    DoCastSelf(shopAura);

                if (!player.HasAura(SpellIds.TIRED_PLAYER))
                    player.SpellFactory.CastSpell(player, SpellIds.TIRED_PLAYER, true);

                break;
            }
            case GossipMenus.OPTION_ID_MAIL:
            {
                Me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Vendor);

                var mailAura = IsArgentSquire() ? SpellIds.AURA_POSTMAN_S : SpellIds.AURA_POSTMAN_G;

                if (!Me.HasAura(mailAura))
                    DoCastSelf(mailAura);

                if (!player.HasAura(SpellIds.TIRED_PLAYER))
                    player.SpellFactory.CastSpell(player, SpellIds.TIRED_PLAYER, true);

                break;
            }
            case GossipMenus.OPTION_ID_DARNASSUS_SENJIN_PENNANT:
            case GossipMenus.OPTION_ID_EXODAR_UNDERCITY_PENNANT:
            case GossipMenus.OPTION_ID_GNOMEREGAN_ORGRIMMAR_PENNANT:
            case GossipMenus.OPTION_ID_IRONFORGE_SILVERMOON_PENNANT:
            case GossipMenus.OPTION_ID_STORMWIND_THUNDERBLUFF_PENNANT:
                if (IsArgentSquire())
                    DoCastSelf(Misc.BannerSpells[gossipListId - 3].Item1, new CastSpellExtraArgs(true));
                else
                    DoCastSelf(Misc.BannerSpells[gossipListId - 3].Item2, new CastSpellExtraArgs(true));

                player.PlayerTalkClass.SendCloseGossip();

                break;
        }

        return false;
    }

    private bool IsArgentSquire()
    {
        return Me.Entry == CreatureIds.ARGENT_SQUIRE;
    }
}

[Script]
internal class NPCBountifulTable : PassiveAI
{
    private readonly Dictionary<uint, uint> _chairSpells = new()
    {
        {
            CreatureIds.THE_CRANBERRY_CHAIR, SpellIds.CRANBERRY_SERVER
        },
        {
            CreatureIds.THE_PIE_CHAIR, SpellIds.PIE_SERVER
        },
        {
            CreatureIds.THE_STUFFING_CHAIR, SpellIds.STUFFING_SERVER
        },
        {
            CreatureIds.THE_TURKEY_CHAIR, SpellIds.TURKEY_SERVER
        },
        {
            CreatureIds.THE_SWEET_POTATO_CHAIR, SpellIds.SWEET_POTATOES_SERVER
        }
    };

    public NPCBountifulTable(Creature creature) : base(creature) { }

    public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
    {
        var x = 0.0f;
        var y = 0.0f;
        var z = 0.0f;
        var o = 0.0f;

        switch ((SeatIds)seatId)
        {
            case SeatIds.TurkeyChair:
                x = 3.87f;
                y = 2.07f;
                o = 3.700098f;

                break;
            case SeatIds.CranberryChair:
                x = 3.87f;
                y = -2.07f;
                o = 2.460914f;

                break;
            case SeatIds.StuffingChair:
                x = -2.52f;

                break;
            case SeatIds.SweetPotatoChair:
                x = -0.09f;
                y = -3.24f;
                o = 1.186824f;

                break;
            case SeatIds.PieChair:
                x = -0.18f;
                y = 3.24f;
                o = 5.009095f;

                break;
            case SeatIds.FoodHolder:
            case SeatIds.PlateHolder:
                var holders = who.VehicleKit1;

                if (holders)
                    holders.InstallAllAccessories(true);

                return;
        }

        var initializer = (MoveSplineInit init) =>
        {
            init.DisableTransportPathTransformations();
            init.MoveTo(x, y, z, false);
            init.SetFacing(o);
        };

        who.MotionMaster.LaunchMoveSpline(initializer, EventId.VehicleBoard, MovementGeneratorPriority.Highest);
        who.Events.AddEvent(new CastFoodSpell(who, _chairSpells[who.Entry]), who.Events.CalculateTime(TimeSpan.FromSeconds(1)));
        var creature = who.AsCreature;

        if (creature)
            creature.SetDisplayFromModel(0);
    }
}

[Script]
internal class NPCGenVoidZone : ScriptedAI
{
    public NPCGenVoidZone(Creature creature) : base(creature) { }

    public override void InitializeAI()
    {
        Me.ReactState = ReactStates.Passive;
    }

    public override void JustAppeared()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(2), task => { DoCastSelf(SpellIds.CONSUMPTION); });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

internal class CastFoodSpell : BasicEvent
{
    private readonly Unit _owner;
    private readonly uint _spellId;

    public CastFoodSpell(Unit owner, uint spellId)
    {
        _owner = owner;
        _spellId = spellId;
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        _owner.SpellFactory.CastSpell(_owner, _spellId, true);

        return true;
    }
}