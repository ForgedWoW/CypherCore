// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

[StructLayout(LayoutKind.Explicit)]
public struct SmartAction
{
    [FieldOffset(0)] public SmartActions Type;

    [FieldOffset(4)] public AiTalk Talk;

    [FieldOffset(4)] public AiSimpleTalk SimpleTalk;

    [FieldOffset(4)] public AiFaction Faction;

    [FieldOffset(4)] public AiMorphOrMount MorphOrMount;

    [FieldOffset(4)] public AiSound Sound;

    [FieldOffset(4)] public AiEmote Emote;

    [FieldOffset(4)] public AiQuest Quest;

    [FieldOffset(4)] public AiQuestOffer QuestOffer;

    [FieldOffset(4)] public AiReact React;

    [FieldOffset(4)] public AiRandomEmote RandomEmote;

    [FieldOffset(4)] public AiCast Cast;

    [FieldOffset(4)] public AiCrossCast CrossCast;

    [FieldOffset(4)] public AiSummonCreature SummonCreature;

    [FieldOffset(4)] public AiThreatPct ThreatPct;

    [FieldOffset(4)] public AiThreat Threat;

    [FieldOffset(4)] public AiCastCreatureOrGO CastCreatureOrGO;

    [FieldOffset(4)] public AiAutoAttack AutoAttack;

    [FieldOffset(4)] public AiCombatMove CombatMove;

    [FieldOffset(4)] public AiSetEventPhase SetEventPhase;

    [FieldOffset(4)] public AiIncEventPhase IncEventPhase;

    [FieldOffset(4)] public AiCastedCreatureOrGO CastedCreatureOrGO;

    [FieldOffset(4)] public AiRemoveAura RemoveAura;

    [FieldOffset(4)] public AiFollow Follow;

    [FieldOffset(4)] public AiRandomPhase RandomPhase;

    [FieldOffset(4)] public AiRandomPhaseRange RandomPhaseRange;

    [FieldOffset(4)] public AiKilledMonster KilledMonster;

    [FieldOffset(4)] public AiSetInstanceData SetInstanceData;

    [FieldOffset(4)] public AiSetInstanceData64 SetInstanceData64;

    [FieldOffset(4)] public AiUpdateTemplate UpdateTemplate;

    [FieldOffset(4)] public AiCallHelp CallHelp;

    [FieldOffset(4)] public AiSetSheath SetSheath;

    [FieldOffset(4)] public AiForceDespawn ForceDespawn;

    [FieldOffset(4)] public AiInvincHp InvincHp;

    [FieldOffset(4)] public AiIngamePhaseId IngamePhaseId;

    [FieldOffset(4)] public AiIngamePhaseGroup IngamePhaseGroup;

    [FieldOffset(4)] public AiSetData SetData;

    [FieldOffset(4)] public AiMoveRandom MoveRandom;

    [FieldOffset(4)] public AiVisibility Visibility;

    [FieldOffset(4)] public AiSummonGO SummonGO;

    [FieldOffset(4)] public AiActive Active;

    [FieldOffset(4)] public AiTaxi Taxi;

    [FieldOffset(4)] public AiWpStart WpStart;

    [FieldOffset(4)] public AiWpPause WpPause;

    [FieldOffset(4)] public AiWpStop WpStop;

    [FieldOffset(4)] public AiItem Item;

    [FieldOffset(4)] public AiSetRun SetRun;

    [FieldOffset(4)] public AiSetDisableGravity SetDisableGravity;

    [FieldOffset(4)] public AiTeleport Teleport;

    [FieldOffset(4)] public AiSetCounter SetCounter;

    [FieldOffset(4)] public AiStoreTargets StoreTargets;

    [FieldOffset(4)] public AiTimeEvent TimeEvent;

    [FieldOffset(4)] public AiMovie Movie;

    [FieldOffset(4)] public AiEquip Equip;

    [FieldOffset(4)] public AiFlag Flag;

    [FieldOffset(4)] public AiSetunitByte SetunitByte;

    [FieldOffset(4)] public AiDelunitByte DelunitByte;

    [FieldOffset(4)] public AiTimedActionList TimedActionList;

    [FieldOffset(4)] public AiRandTimedActionList RandTimedActionList;

    [FieldOffset(4)] public AiRandRangeTimedActionList RandRangeTimedActionList;

    [FieldOffset(4)] public AiInterruptSpellCasting InterruptSpellCasting;

    [FieldOffset(4)] public AiJump Jump;

    [FieldOffset(4)] public AiFleeAssist FleeAssist;

    [FieldOffset(4)] public AiEnableTempGO EnableTempGO;

    [FieldOffset(4)] public AiMoveToPos MoveToPos;

    [FieldOffset(4)] public AiSendGossipMenu SendGossipMenu;

    [FieldOffset(4)] public AiSetGoLootState SetGoLootState;

    [FieldOffset(4)] public AiSendTargetToTarget SendTargetToTarget;

    [FieldOffset(4)] public AiSetRangedMovement SetRangedMovement;

    [FieldOffset(4)] public AiSetHealthRegen SetHealthRegen;

    [FieldOffset(4)] public AiSetRoot SetRoot;

    [FieldOffset(4)] public AiGoState GoState;

    [FieldOffset(4)] public AiCreatureGroup CreatureGroup;

    [FieldOffset(4)] public AiPower Power;

    [FieldOffset(4)] public AiGameEventStop GameEventStop;

    [FieldOffset(4)] public AiGameEventStart GameEventStart;

    [FieldOffset(4)] public AiClosestWaypointFromList ClosestWaypointFromList;

    [FieldOffset(4)] public AiMoveOffset MoveOffset;

    [FieldOffset(4)] public AiRandomSound RandomSound;

    [FieldOffset(4)] public AiCorpseDelay CorpseDelay;

    [FieldOffset(4)] public AiDisableEvade DisableEvade;

    [FieldOffset(4)] public AiGroupSpawn GroupSpawn;

    [FieldOffset(4)] public AuraType auraType;

    [FieldOffset(4)] public AiLoadEquipment LoadEquipment;

    [FieldOffset(4)] public AiRandomTimedEvent RandomTimedEvent;

    [FieldOffset(4)] public AiPauseMovement PauseMovement;

    [FieldOffset(4)] public AiRespawnData RespawnData;

    [FieldOffset(4)] public AiAnimKit AnimKit;

    [FieldOffset(4)] public AiScene Scene;

    [FieldOffset(4)] public AiCinematic Cinematic;

    [FieldOffset(4)] public AiMovementSpeed MovementSpeed;

    [FieldOffset(4)] public AiSpellVisualKit SpellVisualKit;

    [FieldOffset(4)] public AiOverrideLight OverrideLight;

    [FieldOffset(4)] public AiOverrideWeather OverrideWeather;

    [FieldOffset(4)] public AiSetHover SetHover;

    [FieldOffset(4)] public AiEvade Evade;

    [FieldOffset(4)] public AiSetHealthPct SetHealthPct;

    [FieldOffset(4)] public AiConversation Conversation;

    [FieldOffset(4)] public AiSetImmunePc SetImmunePc;

    [FieldOffset(4)] public AiSetImmuneNPC SetImmuneNPC;

    [FieldOffset(4)] public AiSetUninteractible SetUninteractible;

    [FieldOffset(4)] public AiActivateGameObject ActivateGameObject;

    [FieldOffset(4)] public AiAddToStoredTargets AddToStoredTargets;

    [FieldOffset(4)] public AiBecomePersonalClone BecomePersonalClone;

    [FieldOffset(4)] public AiTriggerGameEvent TriggerGameEvent;

    [FieldOffset(4)] public AiDoAction doAction;

    [FieldOffset(4)] public AiRaw raw;

    #region Stucts

    public struct AiTalk
    {
        public uint TextGroupId;
        public uint Duration;
        public uint UseTalkTarget;
    }

    public struct AiSimpleTalk
    {
        public uint TextGroupId;
        public uint Duration;
    }

    public struct AiFaction
    {
        public uint FactionId;
    }

    public struct AiMorphOrMount
    {
        public uint Creature;
        public uint Model;
    }

    public struct AiSound
    {
        public uint SoundId;
        public uint OnlySelf;
        public uint Distance;
        public uint KeyBroadcastTextId;
    }

    public struct AiEmote
    {
        public uint EmoteId;
    }

    public struct AiQuest
    {
        public uint QuestId;
    }

    public struct AiQuestOffer
    {
        public uint QuestId;
        public uint DirectAdd;
    }

    public struct AiReact
    {
        public uint State;
    }

    public struct AiRandomEmote
    {
        public uint Emote1;
        public uint Emote2;
        public uint Emote3;
        public uint Emote4;
        public uint Emote5;
        public uint Emote6;
    }

    public struct AiCast
    {
        public uint Spell;
        public uint CastFlags;
        public uint TriggerFlags;
        public uint TargetsLimit;
    }

    public struct AiCrossCast
    {
        public uint Spell;
        public uint CastFlags;
        public uint TargetType;
        public uint TargetParam1;
        public uint TargetParam2;
        public uint TargetParam3;
    }

    public struct AiSummonCreature
    {
        public uint Creature;
        public uint Type;
        public uint Duration;
        public uint StorageID;
        public uint AttackInvoker;
        public uint Flags; // SmartActionSummonCreatureFlags
        public uint Count;
    }

    public struct AiThreatPct
    {
        public uint ThreatInc;
        public uint ThreatDec;
    }

    public struct AiCastCreatureOrGO
    {
        public uint QuestId;
        public uint Spell;
    }

    public struct AiThreat
    {
        public uint ThreatInc;
        public uint ThreatDec;
    }

    public struct AiAutoAttack
    {
        public uint Attack;
    }

    public struct AiCombatMove
    {
        public uint Move;
    }

    public struct AiSetEventPhase
    {
        public uint Phase;
    }

    public struct AiIncEventPhase
    {
        public uint Inc;
        public uint Dec;
    }

    public struct AiCastedCreatureOrGO
    {
        public uint Creature;
        public uint Spell;
    }

    public struct AiRemoveAura
    {
        public uint Spell;
        public uint Charges;
        public uint OnlyOwnedAuras;
    }

    public struct AiFollow
    {
        public uint Dist;
        public uint Angle;
        public uint Entry;
        public uint Credit;
        public uint CreditType;
    }

    public struct AiRandomPhase
    {
        public uint Phase1;
        public uint Phase2;
        public uint Phase3;
        public uint Phase4;
        public uint Phase5;
        public uint Phase6;
    }

    public struct AiRandomPhaseRange
    {
        public uint PhaseMin;
        public uint PhaseMax;
    }

    public struct AiKilledMonster
    {
        public uint Creature;
    }

    public struct AiSetInstanceData
    {
        public uint Field;
        public uint Data;
        public uint Type;
    }

    public struct AiSetInstanceData64
    {
        public uint Field;
    }

    public struct AiUpdateTemplate
    {
        public uint Creature;
        public uint UpdateLevel;
    }

    public struct AiCallHelp
    {
        public uint Range;
        public uint WithEmote;
    }

    public struct AiSetSheath
    {
        public uint Sheath;
    }

    public struct AiForceDespawn
    {
        public uint Delay;
        public uint ForceRespawnTimer;
    }

    public struct AiInvincHp
    {
        public uint MinHp;
        public uint Percent;
    }

    public struct AiIngamePhaseId
    {
        public uint ID;
        public uint Apply;
    }

    public struct AiIngamePhaseGroup
    {
        public uint GroupId;
        public uint Apply;
    }

    public struct AiSetData
    {
        public uint Field;
        public uint Data;
    }

    public struct AiMoveRandom
    {
        public uint Distance;
    }

    public struct AiVisibility
    {
        public uint State;
    }

    public struct AiSummonGO
    {
        public uint Entry;
        public uint DespawnTime;
        public uint SummonType;
    }

    public struct AiActive
    {
        public uint State;
    }

    public struct AiTaxi
    {
        public uint ID;
    }

    public struct AiWpStart
    {
        public uint Run;
        public uint PathID;
        public uint Repeat;
        public uint QuestId;

        public uint DespawnTime;
        //public uint reactState; DO NOT REUSE
    }

    public struct AiWpPause
    {
        public uint Delay;
    }

    public struct AiWpStop
    {
        public uint DespawnTime;
        public uint QuestId;
        public uint Fail;
    }

    public struct AiItem
    {
        public uint Entry;
        public uint Count;
    }

    public struct AiSetRun
    {
        public uint Run;
    }

    public struct AiSetDisableGravity
    {
        public uint Disable;
    }

    public struct AiTeleport
    {
        public uint MapID;
    }

    public struct AiSetCounter
    {
        public uint CounterId;
        public uint Value;
        public uint Reset;
    }

    public struct AiStoreTargets
    {
        public uint ID;
    }

    public struct AiTimeEvent
    {
        public uint ID;
        public uint Min;
        public uint Max;
        public uint RepeatMin;
        public uint RepeatMax;
        public uint Chance;
    }

    public struct AiMovie
    {
        public uint Entry;
    }

    public struct AiEquip
    {
        public uint Entry;
        public uint Mask;
        public uint Slot1;
        public uint Slot2;
        public uint Slot3;
    }

    public struct AiFlag
    {
        public uint Id;
    }

    public struct AiSetunitByte
    {
        public uint Byte1;
        public uint Type;
    }

    public struct AiDelunitByte
    {
        public uint Byte1;
        public uint Type;
    }

    public struct AiTimedActionList
    {
        public uint ID;
        public uint TimerType;
        public uint AllowOverride;
    }

    public struct AiRandTimedActionList
    {
        public uint ActionList1;
        public uint ActionList2;
        public uint ActionList3;
        public uint ActionList4;
        public uint ActionList5;
        public uint ActionList6;
    }

    public struct AiRandRangeTimedActionList
    {
        public uint IDMin;
        public uint IDMax;
    }

    public struct AiInterruptSpellCasting
    {
        public uint WithDelayed;
        public uint SpellID;
        public uint WithInstant;
    }

    public struct AiJump
    {
        public uint SpeedXy;
        public uint SpeedZ;
        public uint Gravity;
        public uint UseDefaultGravity;
        public uint PointId;
        public uint ContactDistance;
    }

    public struct AiFleeAssist
    {
        public uint WithEmote;
    }

    public struct AiEnableTempGO
    {
        public uint Duration;
    }

    public struct AiMoveToPos
    {
        public uint PointId;
        public uint Transport;
        public uint DisablePathfinding;
        public uint ContactDistance;
    }

    public struct AiSendGossipMenu
    {
        public uint GossipMenuId;
        public uint GossipNpcTextId;
    }

    public struct AiSetGoLootState
    {
        public uint State;
    }

    public struct AiSendTargetToTarget
    {
        public uint ID;
    }

    public struct AiSetRangedMovement
    {
        public uint Distance;
        public uint Angle;
    }

    public struct AiSetHealthRegen
    {
        public uint RegenHealth;
    }

    public struct AiSetRoot
    {
        public uint Root;
    }

    public struct AiGoState
    {
        public uint State;
    }

    public struct AiCreatureGroup
    {
        public uint Group;
        public uint AttackInvoker;
    }

    public struct AiPower
    {
        public uint PowerType;
        public uint NewPower;
    }

    public struct AiGameEventStop
    {
        public uint ID;
    }

    public struct AiGameEventStart
    {
        public uint ID;
    }

    public struct AiClosestWaypointFromList
    {
        public uint Wp1;
        public uint Wp2;
        public uint Wp3;
        public uint Wp4;
        public uint Wp5;
        public uint Wp6;
    }

    public struct AiMoveOffset
    {
        public uint PointId;
    }

    public struct AiRandomSound
    {
        public uint Sound1;
        public uint Sound2;
        public uint Sound3;
        public uint Sound4;
        public uint OnlySelf;
        public uint Distance;
    }

    public struct AiCorpseDelay
    {
        public uint Timer;
        public uint IncludeDecayRatio;
    }

    public struct AiDisableEvade
    {
        public uint Disable;
    }

    public struct AiGroupSpawn
    {
        public uint GroupId;
        public uint MinDelay;
        public uint MaxDelay;
        public uint Spawnflags;
    }

    public struct AiLoadEquipment
    {
        public uint ID;
        public uint Force;
    }

    public struct AiRandomTimedEvent
    {
        public uint MinId;
        public uint MaxId;
    }

    public struct AiPauseMovement
    {
        public uint MovementSlot;
        public uint PauseTimer;
        public uint Force;
    }

    public struct AiRespawnData
    {
        public uint SpawnType;
        public uint SpawnId;
    }

    public struct AiAnimKit
    {
        public uint Kit;
        public uint Type;
    }

    public struct AiScene
    {
        public uint SceneId;
    }

    public struct AiCinematic
    {
        public uint Entry;
    }

    public struct AiMovementSpeed
    {
        public uint MovementType;
        public uint SpeedInteger;
        public uint SpeedFraction;
    }

    public struct AiSpellVisualKit
    {
        public uint SpellVisualKitId;
        public uint KitType;
        public uint Duration;
    }

    public struct AiOverrideLight
    {
        public uint ZoneId;
        public uint AreaLightId;
        public uint OverrideLightId;
        public uint TransitionMilliseconds;
    }

    public struct AiOverrideWeather
    {
        public uint ZoneId;
        public uint WeatherId;
        public uint Intensity;
    }

    public struct AiSetHover
    {
        public uint Enable;
    }

    public struct AiEvade
    {
        public uint ToRespawnPosition;
    }

    public struct AiSetHealthPct
    {
        public uint Percent;
    }

    public struct AiConversation
    {
        public uint ID;
    }

    public struct AiSetImmunePc
    {
        public uint ImmunePc;
    }

    public struct AiSetImmuneNPC
    {
        public uint ImmuneNPC;
    }

    public struct AiSetUninteractible
    {
        public uint Uninteractible;
    }

    public struct AiActivateGameObject
    {
        public uint GameObjectAction;
        public uint Param;
    }

    public struct AiAddToStoredTargets
    {
        public uint ID;
    }

    public struct AiBecomePersonalClone
    {
        public uint Type;
        public uint Duration;
    }

    public struct AiTriggerGameEvent
    {
        public uint EventId;
        public uint UseSaiTargetAsGameEventSource;
    }

    public struct AiDoAction
    {
        public uint ActionId;
    }

    public struct AiRaw
    {
        public uint Param1;
        public uint Param2;
        public uint Param3;
        public uint Param4;
        public uint Param5;
        public uint Param6;
        public uint Param7;
    }

    #endregion Stucts
}