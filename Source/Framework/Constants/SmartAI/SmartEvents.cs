// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SmartEvents
{
    UpdateIc = 0,               // Initialmin, Initialmax, Repeatmin, Repeatmax
    UpdateOoc = 1,              // Initialmin, Initialmax, Repeatmin, Repeatmax
    HealthPct = 2,              // Hpmin%, Hpmax%,  Repeatmin, Repeatmax
    ManaPct = 3,                // Manamin%, Manamax%, Repeatmin, Repeatmax
    Aggro = 4,                  // None
    Kill = 5,                   // Cooldownmin0, Cooldownmax1, Playeronly2, Else Creature Entry3
    Death = 6,                  // None
    Evade = 7,                  // None
    SpellHit = 8,               // Spellid, School, Cooldownmin, Cooldownmax
    Range = 9,                  // Mindist, Maxdist, Repeatmin, Repeatmax
    OocLos = 10,                // HostilityMode, Maxrnage, Cooldownmin, Cooldownmax
    Respawn = 11,               // Type, Mapid, Zoneid
    TargetHealthPct = 12,       // UNUSED, DO NOT REUSE
    VictimCasting = 13,         // Repeatmin, Repeatmax
    FriendlyHealth = 14,        // UNUSED, DO NOT REUSE
    FriendlyIsCc = 15,          // Radius, Repeatmin, Repeatmax
    FriendlyMissingBuff = 16,   // Spellid, Radius, Repeatmin, Repeatmax
    SummonedUnit = 17,          // Creatureid(0 All), Cooldownmin, Cooldownmax
    TargetManaPct = 18,         // UNUSED, DO NOT REUSE
    AcceptedQuest = 19,         // QuestID (0 = any), CooldownMin, CooldownMax
    RewardQuest = 20,           // QuestID (0 = any), CooldownMin, CooldownMax
    ReachedHome = 21,           // None
    ReceiveEmote = 22,          // Emoteid, Cooldownmin, Cooldownmax, Condition, Val1, Val2, Val3
    HasAura = 23,               // Param1 = Spellid, Param2 = Stack Amount, Param3/4 Repeatmin, Repeatmax
    TargetBuffed = 24,          // Param1 = Spellid, Param2 = Stack Amount, Param3/4 Repeatmin, Repeatmax
    Reset = 25,                 // Called After Combat, When The Creature Respawn And Spawn.
    IcLos = 26,                 // HostilityMode, Maxrnage, Cooldownmin, Cooldownmax
    PassengerBoarded = 27,      // Cooldownmin, Cooldownmax
    PassengerRemoved = 28,      // Cooldownmin, Cooldownmax
    Charmed = 29,               // onRemove (0 - on apply, 1 - on remove)
    CharmedTarget = 30,         // UNUSED, DO NOT REUSE
    SpellHitTarget = 31,        // Spellid, School, Cooldownmin, Cooldownmax
    Damaged = 32,               // Mindmg, Maxdmg, Cooldownmin, Cooldownmax
    DamagedTarget = 33,         // Mindmg, Maxdmg, Cooldownmin, Cooldownmax
    Movementinform = 34,        // Movementtype(Any), Pointid
    SummonDespawned = 35,       // Entry, Cooldownmin, Cooldownmax
    CorpseRemoved = 36,         // None
    AiInit = 37,                // None
    DataSet = 38,               // Id, Value, Cooldownmin, Cooldownmax
    WaypointStart = 39,         // UNUSED, DO NOT REUSE
    WaypointReached = 40,       // Pointid(0any), Pathid(0any)
    TransportAddplayer = 41,    // None
    TransportAddcreature = 42,  // Entry (0 Any)
    TransportRemovePlayer = 43, // None
    TransportRelocate = 44,     // Pointid
    InstancePlayerEnter = 45,   // Team (0 Any), Cooldownmin, Cooldownmax
    AreatriggerOntrigger = 46,  // Triggerid(0 Any)
    QuestAccepted = 47,         // None
    QuestObjCompletion = 48,    // None
    QuestCompletion = 49,       // None
    QuestRewarded = 50,         // None
    QuestFail = 51,             // None
    TextOver = 52,              // Groupid From CreatureText,  Creature Entry Who Talks (0 Any)
    ReceiveHeal = 53,           // Minheal, Maxheal, Cooldownmin, Cooldownmax
    JustSummoned = 54,          // None
    WaypointPaused = 55,        // Pointid(0any), Pathid(0any)
    WaypointResumed = 56,       // Pointid(0any), Pathid(0any)
    WaypointStopped = 57,       // Pointid(0any), Pathid(0any)
    WaypointEnded = 58,         // Pointid(0any), Pathid(0any)
    TimedEventTriggered = 59,   // Id
    Update = 60,                // Initialmin, Initialmax, Repeatmin, Repeatmax
    Link = 61,                  // Internal Usage, No Params, Used To Link Together Multiple Events, Does Not Use Any Extra Resources To Iterate Event Lists Needlessly
    GossipSelect = 62,          // Menuid, Actionid
    JustCreated = 63,           // None
    GossipHello = 64,           // noReportUse (for GOs)
    FollowCompleted = 65,       // None
    PhaseChange = 66,           //UNUSED, DO NOT REUSE
    IsBehindTarget = 67,        // UNUSED, DO NOT REUSE
    GameEventStart = 68,        // GameEvent.Entry
    GameEventEnd = 69,          // GameEvent.Entry
    GoLootStateChanged = 70,    // Go State
    GoEventInform = 71,         // Eventid
    ActionDone = 72,            // Eventid (Shareddefines.Eventid)
    OnSpellclick = 73,          // Clicker (Unit)
    FriendlyHealthPCT = 74,     // minHpPct, maxHpPct, repeatMin, repeatMax
    DistanceCreature = 75,      // guid, entry, distance, repeat
    DistanceGameobject = 76,    // guid, entry, distance, repeat
    CounterSet = 77,            // id, value, cooldownMin, cooldownMax
    SceneStart = 78,            // none
    SceneTrigger = 79,          // param_string : triggerName
    SceneCancel = 80,           // none
    SceneComplete = 81,         // none
    SummonedUnitDies = 82,      // CreatureId(0 all), CooldownMin, CooldownMax
    OnSpellCast = 83,           // SpellID, CooldownMin, CooldownMax
    OnSpellFailed = 84,         // SpellID, CooldownMin, CooldownMax
    OnSpellStart = 85,          // SpellID, CooldownMin, CooldownMax
    OnDespawn = 86,             // NONE

    End
}