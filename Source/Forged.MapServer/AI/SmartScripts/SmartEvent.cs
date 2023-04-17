// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

[StructLayout(LayoutKind.Explicit)]
public struct SmartEvent
{
    [FieldOffset(0)] public SmartEvents Type;

    [FieldOffset(4)] public uint EventPhaseMask;

    [FieldOffset(8)] public uint EventChance;

    [FieldOffset(12)] public SmartEventFlags EventFlags;

    [FieldOffset(16)] public SmartEventMinMaxRepeat MinMaxRepeat;

    [FieldOffset(16)] public SmartEventKill Kill;

    [FieldOffset(16)] public SmartEventSpellHit SpellHit;

    [FieldOffset(16)] public SmartEventLos Los;

    [FieldOffset(16)] public SmartEventRespawn Respawn;

    [FieldOffset(16)] public SmartEventMinMax MinMax;

    [FieldOffset(16)] public SmartEventTargetCasting TargetCasting;

    [FieldOffset(16)] public SmartEventFriendlyCc FriendlyCc;

    [FieldOffset(16)] public SmartEventMissingBuff MissingBuff;

    [FieldOffset(16)] public SmartEventSummoned Summoned;

    [FieldOffset(16)] public SmartEventQuest Quest;

    [FieldOffset(16)] public SmartEventQuestObjective QuestObjective;

    [FieldOffset(16)] public SmartEventEmote Emote;

    [FieldOffset(16)] public SmartEventAura Aura;

    [FieldOffset(16)] public SmartEventCharm Charm;

    [FieldOffset(16)] public SmartEventMovementInform MovementInform;

    [FieldOffset(16)] public SmartEventDataSet DataSet;

    [FieldOffset(16)] public SmartEventWaypoint Waypoint;

    [FieldOffset(16)] public SmartEventTransportAddCreature TransportAddCreature;

    [FieldOffset(16)] public SmartEventTransportRelocate TransportRelocate;

    [FieldOffset(16)] public SmartEventInstancePlayerEnter InstancePlayerEnter;

    [FieldOffset(16)] public SmartEventAreatrigger Areatrigger;

    [FieldOffset(16)] public SmartEventTextOver TextOver;

    [FieldOffset(16)] public SmartEventTimedEvent TimedEvent;

    [FieldOffset(16)] public SmartEventGossipHello GossipHello;

    [FieldOffset(16)] public SmartEventGossip Gossip;

    [FieldOffset(16)] public SmartEventGameEvent GameEvent;

    [FieldOffset(16)] public SmartEventGoLootStateChanged GoLootStateChanged;

    [FieldOffset(16)] public SmartEventEventInform EventInform;

    [FieldOffset(16)] public SmartEventDoAction DoAction;

    [FieldOffset(16)] public SmartEventFriendlyHealthPct FriendlyHealthPct;

    [FieldOffset(16)] public SmartEventDistance Distance;

    [FieldOffset(16)] public SmartEventCounter Counter;

    [FieldOffset(16)] public SmartEventSpellCast SpellCast;

    [FieldOffset(16)] public SmartEventSpell Spell;

    [FieldOffset(16)] public SmartEventRaw Raw;

    [FieldOffset(40)] public string ParamString;

    #region Structs

    public struct SmartEventMinMaxRepeat
    {
        public uint Min;
        public uint Max;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct SmartEventKill
    {
        public uint CooldownMin;
        public uint CooldownMax;
        public uint PlayerOnly;
        public uint Creature;
    }

    public struct SmartEventSpellHit
    {
        public uint Spell;
        public uint School;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventLos
    {
        public uint HostilityMode;
        public uint MaxDist;
        public uint CooldownMin;
        public uint CooldownMax;
        public uint PlayerOnly;
    }

    public struct SmartEventRespawn
    {
        public uint Type;
        public uint Map;
        public uint Area;
    }

    public struct SmartEventMinMax
    {
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct SmartEventTargetCasting
    {
        public uint RepeatMin;
        public uint RepeatMax;
        public uint SpellId;
    }

    public struct SmartEventFriendlyCc
    {
        public uint Radius;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct SmartEventMissingBuff
    {
        public uint Spell;
        public uint Radius;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct SmartEventSummoned
    {
        public uint Creature;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventQuest
    {
        public uint QuestId;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventQuestObjective
    {
        public uint ID;
    }

    public struct SmartEventEmote
    {
        public uint EmoteId;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventAura
    {
        public uint Spell;
        public uint Count;
        public uint RepeatMin;
        public uint RepeatMax;
    }

    public struct SmartEventCharm
    {
        public uint OnRemove;
    }

    public struct SmartEventMovementInform
    {
        public uint Type;
        public uint ID;
    }

    public struct SmartEventDataSet
    {
        public uint ID;
        public uint Value;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventWaypoint
    {
        public uint PointID;
        public uint PathID;
    }

    public struct SmartEventTransportAddCreature
    {
        public uint Creature;
    }

    public struct SmartEventTransportRelocate
    {
        public uint PointID;
    }

    public struct SmartEventInstancePlayerEnter
    {
        public uint Team;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventAreatrigger
    {
        public uint ID;
    }

    public struct SmartEventTextOver
    {
        public uint TextGroupID;
        public uint CreatureEntry;
    }

    public struct SmartEventTimedEvent
    {
        public uint ID;
    }

    public struct SmartEventGossipHello
    {
        public uint Filter;
    }

    public struct SmartEventGossip
    {
        public uint Sender;
        public uint Action;
    }

    public struct SmartEventGameEvent
    {
        public uint GameEventId;
    }

    public struct SmartEventGoLootStateChanged
    {
        public uint LootState;
    }

    public struct SmartEventEventInform
    {
        public uint EventId;
    }

    public struct SmartEventDoAction
    {
        public uint EventId;
    }

    public struct SmartEventFriendlyHealthPct
    {
        public uint MinHpPct;
        public uint MaxHpPct;
        public uint RepeatMin;
        public uint RepeatMax;
        public uint Radius;
    }

    public struct SmartEventDistance
    {
        public uint GUID;
        public uint Entry;
        public uint Dist;
        public uint Repeat;
    }

    public struct SmartEventCounter
    {
        public uint ID;
        public uint Value;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventSpellCast
    {
        public uint Spell;
        public uint CooldownMin;
        public uint CooldownMax;
    }

    public struct SmartEventSpell
    {
        public uint EffIndex;
    }

    public struct SmartEventRaw
    {
        public uint Param1;
        public uint Param2;
        public uint Param3;
        public uint Param4;
        public uint Param5;
    }

    #endregion
}