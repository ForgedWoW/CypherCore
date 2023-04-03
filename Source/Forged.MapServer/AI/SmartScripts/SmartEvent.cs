// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

[StructLayout(LayoutKind.Explicit)]
public struct SmartEvent
{
    [FieldOffset(0)] public SmartEvents type;

    [FieldOffset(4)] public uint event_phase_mask;

    [FieldOffset(8)] public uint event_chance;

    [FieldOffset(12)] public SmartEventFlags event_flags;

    [FieldOffset(16)] public MinMaxRepeat minMaxRepeat;

    [FieldOffset(16)] public Kill kill;

    [FieldOffset(16)] public SpellHit spellHit;

    [FieldOffset(16)] public Los los;

    [FieldOffset(16)] public Respawn respawn;

    [FieldOffset(16)] public MinMax minMax;

    [FieldOffset(16)] public TargetCasting targetCasting;

    [FieldOffset(16)] public FriendlyCc friendlyCC;

    [FieldOffset(16)] public MissingBuff missingBuff;

    [FieldOffset(16)] public Summoned summoned;

    [FieldOffset(16)] public Quest quest;

    [FieldOffset(16)] public QuestObjective questObjective;

    [FieldOffset(16)] public Emote emote;

    [FieldOffset(16)] public Aura aura;

    [FieldOffset(16)] public Charm charm;

    [FieldOffset(16)] public MovementInform movementInform;

    [FieldOffset(16)] public DataSet dataSet;

    [FieldOffset(16)] public Waypoint waypoint;

    [FieldOffset(16)] public TransportAddCreature transportAddCreature;

    [FieldOffset(16)] public TransportRelocate transportRelocate;

    [FieldOffset(16)] public InstancePlayerEnter instancePlayerEnter;

    [FieldOffset(16)] public Areatrigger areatrigger;

    [FieldOffset(16)] public TextOver textOver;

    [FieldOffset(16)] public TimedEvent timedEvent;

    [FieldOffset(16)] public GossipHello gossipHello;

    [FieldOffset(16)] public Gossip gossip;

    [FieldOffset(16)] public GameEvent gameEvent;

    [FieldOffset(16)] public GoLootStateChanged goLootStateChanged;

    [FieldOffset(16)] public EventInform eventInform;

    [FieldOffset(16)] public DoAction doAction;

    [FieldOffset(16)] public FriendlyHealthPct friendlyHealthPct;

    [FieldOffset(16)] public Distance distance;

    [FieldOffset(16)] public Counter counter;

    [FieldOffset(16)] public SpellCast spellCast;

    [FieldOffset(16)] public Spell spell;

    [FieldOffset(16)] public Raw raw;

    [FieldOffset(40)] public string param_string;

    #region Structs

    public struct MinMaxRepeat
    {
        public uint Min;
        public uint Max;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct Kill
    {
        public uint CooldownMin;
        public uint CooldownMax;
        public uint PlayerOnly;
        public uint Creature;
    }

    public struct SpellHit
    {
        public uint Spell;
        public uint School;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct Los
    {
        public uint HostilityMode;
        public uint MaxDist;
        public uint CooldownMin;
        public uint CooldownMax;
        public uint PlayerOnly;
    }

    public struct Respawn
    {
        public uint Type;
        public uint Map;
        public uint Area;
    }

    public struct MinMax
    {
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct TargetCasting
    {
        public uint RepeatMin;
        public uint RepeatMax;
        public uint SpellId;
    }

    public struct FriendlyCc
    {
        public uint Radius;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct MissingBuff
    {
        public uint Spell;
        public uint Radius;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct Summoned
    {
        public uint Creature;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct Quest
    {
        public uint QuestId;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct QuestObjective
    {
        public uint ID;
    }

    public struct Emote
    {
        public uint EmoteId;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct Aura
    {
        public uint Spell;
        public uint Count;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct Charm
    {
        public uint OnRemove;
    }

    public struct MovementInform
    {
        public uint Type;
        public uint ID;
    }

    public struct DataSet
    {
        public uint ID;
        public uint Value;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct Waypoint
    {
        public uint PointID;
        public uint PathID;
    }

    public struct TransportAddCreature
    {
        public uint Creature;
    }

    public struct TransportRelocate
    {
        public uint PointID;
    }

    public struct InstancePlayerEnter
    {
        public uint Team;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct Areatrigger
    {
        public uint ID;
    }

    public struct TextOver
    {
        public uint TextGroupID;
        public uint CreatureEntry;
    }

    public struct TimedEvent
    {
        public uint ID;
    }

    public struct GossipHello
    {
        public uint Filter;
    }

    public struct Gossip
    {
        public uint Sender;
        public uint Action;
    }

    public struct GameEvent
    {
        public uint GameEventId;
    }

    public struct GoLootStateChanged
    {
        public uint LootState;
    }

    public struct EventInform
    {
        public uint EventId;
    }

    public struct DoAction
    {
        public uint EventId;
    }

    public struct FriendlyHealthPct
    {
        public uint MinHpPct;
        public uint MaxHpPct;
        public uint RepeatMin;
        public uint RepeatMax;
        public uint Radius;
    }

    public struct Distance
    {
        public uint GUID;
        public uint Entry;
        public uint Dist;
        public uint Repeat;
    }

    public struct Counter
    {
        public uint ID;
        public uint Value;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SpellCast
    {
        public uint Spell;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct Spell
    {
        public uint EffIndex;
    }

    public struct Raw
    {
        public uint Param1;
        public uint Param2;
        public uint Param3;
        public uint Param4;
        public uint Param5;
    }

    #endregion
}