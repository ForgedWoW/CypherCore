// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.AI.SmartScripts;

[StructLayout(LayoutKind.Explicit)]
public struct SmartAction
{
    [FieldOffset(0)] public SmartActions type;

    [FieldOffset(4)] public Talk talk;

    [FieldOffset(4)] public SimpleTalk simpleTalk;

    [FieldOffset(4)] public Faction faction;

    [FieldOffset(4)] public MorphOrMount morphOrMount;

    [FieldOffset(4)] public Sound sound;

    [FieldOffset(4)] public Emote emote;

    [FieldOffset(4)] public Quest quest;

    [FieldOffset(4)] public QuestOffer questOffer;

    [FieldOffset(4)] public React react;

    [FieldOffset(4)] public RandomEmote randomEmote;

    [FieldOffset(4)] public Cast cast;

    [FieldOffset(4)] public CrossCast crossCast;

    [FieldOffset(4)] public SummonCreature summonCreature;

    [FieldOffset(4)] public ThreatPct threatPCT;

    [FieldOffset(4)] public Threat threat;

    [FieldOffset(4)] public CastCreatureOrGO castCreatureOrGO;

    [FieldOffset(4)] public AutoAttack autoAttack;

    [FieldOffset(4)] public CombatMove combatMove;

    [FieldOffset(4)] public SetEventPhase setEventPhase;

    [FieldOffset(4)] public IncEventPhase incEventPhase;

    [FieldOffset(4)] public CastedCreatureOrGO castedCreatureOrGO;

    [FieldOffset(4)] public RemoveAura removeAura;

    [FieldOffset(4)] public Follow follow;

    [FieldOffset(4)] public RandomPhase randomPhase;

    [FieldOffset(4)] public RandomPhaseRange randomPhaseRange;

    [FieldOffset(4)] public KilledMonster killedMonster;

    [FieldOffset(4)] public SetInstanceData setInstanceData;

    [FieldOffset(4)] public SetInstanceData64 setInstanceData64;

    [FieldOffset(4)] public UpdateTemplate updateTemplate;

    [FieldOffset(4)] public CallHelp callHelp;

    [FieldOffset(4)] public SetSheath setSheath;

    [FieldOffset(4)] public ForceDespawn forceDespawn;

    [FieldOffset(4)] public InvincHp invincHP;

    [FieldOffset(4)] public IngamePhaseId ingamePhaseId;

    [FieldOffset(4)] public IngamePhaseGroup ingamePhaseGroup;

    [FieldOffset(4)] public SetData setData;

    [FieldOffset(4)] public MoveRandom moveRandom;

    [FieldOffset(4)] public Visibility visibility;

    [FieldOffset(4)] public SummonGO summonGO;

    [FieldOffset(4)] public Active active;

    [FieldOffset(4)] public Taxi taxi;

    [FieldOffset(4)] public WpStart wpStart;

    [FieldOffset(4)] public WpPause wpPause;

    [FieldOffset(4)] public WpStop wpStop;

    [FieldOffset(4)] public Item item;

    [FieldOffset(4)] public SetRun setRun;

    [FieldOffset(4)] public SetDisableGravity setDisableGravity;

    [FieldOffset(4)] public Teleport teleport;

    [FieldOffset(4)] public SetCounter setCounter;

    [FieldOffset(4)] public StoreTargets storeTargets;

    [FieldOffset(4)] public TimeEvent timeEvent;

    [FieldOffset(4)] public Movie movie;

    [FieldOffset(4)] public Equip equip;

    [FieldOffset(4)] public Flag flag;

    [FieldOffset(4)] public SetunitByte setunitByte;

    [FieldOffset(4)] public DelunitByte delunitByte;

    [FieldOffset(4)] public TimedActionList timedActionList;

    [FieldOffset(4)] public RandTimedActionList randTimedActionList;

    [FieldOffset(4)] public RandRangeTimedActionList randRangeTimedActionList;

    [FieldOffset(4)] public InterruptSpellCasting interruptSpellCasting;

    [FieldOffset(4)] public Jump jump;

    [FieldOffset(4)] public FleeAssist fleeAssist;

    [FieldOffset(4)] public EnableTempGO enableTempGO;

    [FieldOffset(4)] public MoveToPos moveToPos;

    [FieldOffset(4)] public SendGossipMenu sendGossipMenu;

    [FieldOffset(4)] public SetGoLootState setGoLootState;

    [FieldOffset(4)] public SendTargetToTarget sendTargetToTarget;

    [FieldOffset(4)] public SetRangedMovement setRangedMovement;

    [FieldOffset(4)] public SetHealthRegen setHealthRegen;

    [FieldOffset(4)] public SetRoot setRoot;

    [FieldOffset(4)] public GoState goState;

    [FieldOffset(4)] public CreatureGroup creatureGroup;

    [FieldOffset(4)] public Power power;

    [FieldOffset(4)] public GameEventStop gameEventStop;

    [FieldOffset(4)] public GameEventStart gameEventStart;

    [FieldOffset(4)] public ClosestWaypointFromList closestWaypointFromList;

    [FieldOffset(4)] public MoveOffset moveOffset;

    [FieldOffset(4)] public RandomSound randomSound;

    [FieldOffset(4)] public CorpseDelay corpseDelay;

    [FieldOffset(4)] public DisableEvade disableEvade;

    [FieldOffset(4)] public GroupSpawn groupSpawn;

    [FieldOffset(4)] public AuraType auraType;

    [FieldOffset(4)] public LoadEquipment loadEquipment;

    [FieldOffset(4)] public RandomTimedEvent randomTimedEvent;

    [FieldOffset(4)] public PauseMovement pauseMovement;

    [FieldOffset(4)] public RespawnData respawnData;

    [FieldOffset(4)] public AnimKit animKit;

    [FieldOffset(4)] public Scene scene;

    [FieldOffset(4)] public Cinematic cinematic;

    [FieldOffset(4)] public MovementSpeed movementSpeed;

    [FieldOffset(4)] public SpellVisualKit spellVisualKit;

    [FieldOffset(4)] public OverrideLight overrideLight;

    [FieldOffset(4)] public OverrideWeather overrideWeather;

    [FieldOffset(4)] public SetHover setHover;

    [FieldOffset(4)] public Evade evade;

    [FieldOffset(4)] public SetHealthPct setHealthPct;

    [FieldOffset(4)] public Conversation conversation;

    [FieldOffset(4)] public SetImmunePc setImmunePC;

    [FieldOffset(4)] public SetImmuneNPC setImmuneNPC;

    [FieldOffset(4)] public SetUninteractible setUninteractible;

    [FieldOffset(4)] public ActivateGameObject activateGameObject;

    [FieldOffset(4)] public AddToStoredTargets addToStoredTargets;

    [FieldOffset(4)] public BecomePersonalClone becomePersonalClone;

    [FieldOffset(4)] public TriggerGameEvent triggerGameEvent;

    [FieldOffset(4)] public DoAction doAction;

    [FieldOffset(4)] public Raw raw;

    #region Stucts

    public struct Talk
    {
        public uint TextGroupId;
        public uint Duration;
        public uint UseTalkTarget;
    }

    public struct SimpleTalk
    {
        public uint TextGroupId;
        public uint Duration;
    }

    public struct Faction
    {
        public uint FactionId;
    }

    public struct MorphOrMount
    {
        public uint Creature;
        public uint Model;
    }

    public struct Sound
    {
        public uint SoundId;
        public uint OnlySelf;
        public uint Distance;
        public uint KeyBroadcastTextId;
    }

    public struct Emote
    {
        public uint EmoteId;
    }

    public struct Quest
    {
        public uint QuestId;
    }

    public struct QuestOffer
    {
        public uint QuestId;
        public uint DirectAdd;
    }

    public struct React
    {
        public uint State;
    }

    public struct RandomEmote
    {
        public uint Emote1;
        public uint Emote2;
        public uint Emote3;
        public uint Emote4;
        public uint Emote5;
        public uint Emote6;
    }

    public struct Cast
    {
        public uint Spell;
        public uint CastFlags;
        public uint TriggerFlags;
        public uint TargetsLimit;
    }

    public struct CrossCast
    {
        public uint Spell;
        public uint CastFlags;
        public uint TargetType;
        public uint TargetParam1;
        public uint TargetParam2;
        public uint TargetParam3;
    }

    public struct SummonCreature
    {
        public uint Creature;
        public uint Type;
        public uint Duration;
        public uint StorageID;
        public uint AttackInvoker;
        public uint Flags; // SmartActionSummonCreatureFlags
        public uint Count;
    }

    public struct ThreatPct
    {
        public uint ThreatInc;
        public uint ThreatDec;
    }

    public struct CastCreatureOrGO
    {
        public uint QuestId;
        public uint Spell;
    }

    public struct Threat
    {
        public uint ThreatInc;
        public uint ThreatDec;
    }

    public struct AutoAttack
    {
        public uint Attack;
    }

    public struct CombatMove
    {
        public uint Move;
    }

    public struct SetEventPhase
    {
        public uint Phase;
    }

    public struct IncEventPhase
    {
        public uint Inc;
        public uint Dec;
    }

    public struct CastedCreatureOrGO
    {
        public uint Creature;
        public uint Spell;
    }

    public struct RemoveAura
    {
        public uint Spell;
        public uint Charges;
        public uint OnlyOwnedAuras;
    }

    public struct Follow
    {
        public uint Dist;
        public uint Angle;
        public uint Entry;
        public uint Credit;
        public uint CreditType;
    }

    public struct RandomPhase
    {
        public uint Phase1;
        public uint Phase2;
        public uint Phase3;
        public uint Phase4;
        public uint Phase5;
        public uint Phase6;
    }

    public struct RandomPhaseRange
    {
        public uint PhaseMin;
        public uint PhaseMax;
    }

    public struct KilledMonster
    {
        public uint Creature;
    }

    public struct SetInstanceData
    {
        public uint Field;
        public uint Data;
        public uint Type;
    }

    public struct SetInstanceData64
    {
        public uint Field;
    }

    public struct UpdateTemplate
    {
        public uint Creature;
        public uint UpdateLevel;
    }

    public struct CallHelp
    {
        public uint Range;
        public uint WithEmote;
    }

    public struct SetSheath
    {
        public uint Sheath;
    }

    public struct ForceDespawn
    {
        public uint Delay;
        public uint ForceRespawnTimer;
    }

    public struct InvincHp
    {
        public uint MinHp;
        public uint Percent;
    }

    public struct IngamePhaseId
    {
        public uint ID;
        public uint Apply;
    }

    public struct IngamePhaseGroup
    {
        public uint GroupId;
        public uint Apply;
    }

    public struct SetData
    {
        public uint Field;
        public uint Data;
    }

    public struct MoveRandom
    {
        public uint Distance;
    }

    public struct Visibility
    {
        public uint State;
    }

    public struct SummonGO
    {
        public uint Entry;
        public uint DespawnTime;
        public uint SummonType;
    }

    public struct Active
    {
        public uint State;
    }

    public struct Taxi
    {
        public uint ID;
    }

    public struct WpStart
    {
        public uint Run;
        public uint PathID;
        public uint Repeat;
        public uint QuestId;

        public uint DespawnTime;
        //public uint reactState; DO NOT REUSE
    }

    public struct WpPause
    {
        public uint Delay;
    }

    public struct WpStop
    {
        public uint DespawnTime;
        public uint QuestId;
        public uint Fail;
    }

    public struct Item
    {
        public uint Entry;
        public uint Count;
    }

    public struct SetRun
    {
        public uint Run;
    }

    public struct SetDisableGravity
    {
        public uint Disable;
    }

    public struct Teleport
    {
        public uint MapID;
    }

    public struct SetCounter
    {
        public uint CounterId;
        public uint Value;
        public uint Reset;
    }

    public struct StoreTargets
    {
        public uint ID;
    }

    public struct TimeEvent
    {
        public uint ID;
        public uint Min;
        public uint Max;
        public uint RepeatMin;
        public uint RepeatMax;
        public uint Chance;
    }

    public struct Movie
    {
        public uint Entry;
    }

    public struct Equip
    {
        public uint Entry;
        public uint Mask;
        public uint Slot1;
        public uint Slot2;
        public uint Slot3;
    }

    public struct Flag
    {
        public uint Id;
    }

    public struct SetunitByte
    {
        public uint Byte1;
        public uint Type;
    }

    public struct DelunitByte
    {
        public uint Byte1;
        public uint Type;
    }

    public struct TimedActionList
    {
        public uint ID;
        public uint TimerType;
        public uint AllowOverride;
    }

    public struct RandTimedActionList
    {
        public uint ActionList1;
        public uint ActionList2;
        public uint ActionList3;
        public uint ActionList4;
        public uint ActionList5;
        public uint ActionList6;
    }

    public struct RandRangeTimedActionList
    {
        public uint IDMin;
        public uint IDMax;
    }

    public struct InterruptSpellCasting
    {
        public uint WithDelayed;
        public uint SpellID;
        public uint WithInstant;
    }

    public struct Jump
    {
        public uint SpeedXy;
        public uint SpeedZ;
        public uint Gravity;
        public uint UseDefaultGravity;
        public uint PointId;
        public uint ContactDistance;
    }

    public struct FleeAssist
    {
        public uint WithEmote;
    }

    public struct EnableTempGO
    {
        public uint Duration;
    }

    public struct MoveToPos
    {
        public uint PointId;
        public uint Transport;
        public uint DisablePathfinding;
        public uint ContactDistance;
    }

    public struct SendGossipMenu
    {
        public uint GossipMenuId;
        public uint GossipNpcTextId;
    }

    public struct SetGoLootState
    {
        public uint State;
    }

    public struct SendTargetToTarget
    {
        public uint ID;
    }

    public struct SetRangedMovement
    {
        public uint Distance;
        public uint Angle;
    }

    public struct SetHealthRegen
    {
        public uint RegenHealth;
    }

    public struct SetRoot
    {
        public uint Root;
    }

    public struct GoState
    {
        public uint State;
    }

    public struct CreatureGroup
    {
        public uint Group;
        public uint AttackInvoker;
    }

    public struct Power
    {
        public uint PowerType;
        public uint NewPower;
    }

    public struct GameEventStop
    {
        public uint ID;
    }

    public struct GameEventStart
    {
        public uint ID;
    }

    public struct ClosestWaypointFromList
    {
        public uint Wp1;
        public uint Wp2;
        public uint Wp3;
        public uint Wp4;
        public uint Wp5;
        public uint Wp6;
    }

    public struct MoveOffset
    {
        public uint PointId;
    }

    public struct RandomSound
    {
        public uint Sound1;
        public uint Sound2;
        public uint Sound3;
        public uint Sound4;
        public uint OnlySelf;
        public uint Distance;
    }

    public struct CorpseDelay
    {
        public uint Timer;
        public uint IncludeDecayRatio;
    }

    public struct DisableEvade
    {
        public uint Disable;
    }

    public struct GroupSpawn
    {
        public uint GroupId;
        public uint MinDelay;
        public uint MaxDelay;
        public uint Spawnflags;
    }

    public struct LoadEquipment
    {
        public uint ID;
        public uint Force;
    }

    public struct RandomTimedEvent
    {
        public uint MinId;
        public uint MaxId;
    }

    public struct PauseMovement
    {
        public uint MovementSlot;
        public uint PauseTimer;
        public uint Force;
    }

    public struct RespawnData
    {
        public uint SpawnType;
        public uint SpawnId;
    }

    public struct AnimKit
    {
        public uint Kit;
        public uint Type;
    }

    public struct Scene
    {
        public uint SceneId;
    }

    public struct Cinematic
    {
        public uint Entry;
    }

    public struct MovementSpeed
    {
        public uint MovementType;
        public uint SpeedInteger;
        public uint SpeedFraction;
    }

    public struct SpellVisualKit
    {
        public uint SpellVisualKitId;
        public uint KitType;
        public uint Duration;
    }

    public struct OverrideLight
    {
        public uint ZoneId;
        public uint AreaLightId;
        public uint OverrideLightId;
        public uint TransitionMilliseconds;
    }

    public struct OverrideWeather
    {
        public uint ZoneId;
        public uint WeatherId;
        public uint Intensity;
    }

    public struct SetHover
    {
        public uint Enable;
    }

    public struct Evade
    {
        public uint ToRespawnPosition;
    }

    public struct SetHealthPct
    {
        public uint Percent;
    }

    public struct Conversation
    {
        public uint ID;
    }

    public struct SetImmunePc
    {
        public uint ImmunePc;
    }

    public struct SetImmuneNPC
    {
        public uint ImmuneNPC;
    }

    public struct SetUninteractible
    {
        public uint Uninteractible;
    }

    public struct ActivateGameObject
    {
        public uint GameObjectAction;
        public uint Param;
    }

    public struct AddToStoredTargets
    {
        public uint ID;
    }

    public struct BecomePersonalClone
    {
        public uint Type;
        public uint Duration;
    }

    public struct TriggerGameEvent
    {
        public uint EventId;
        public uint UseSaiTargetAsGameEventSource;
    }

    public struct DoAction
    {
        public uint ActionId;
    }

    public struct Raw
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