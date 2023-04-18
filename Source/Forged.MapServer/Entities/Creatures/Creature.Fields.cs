// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Events;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Movement;
using Forged.MapServer.Pools;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities.Creatures;

public partial class Creature
{
    private readonly Position _homePosition;
    private readonly MultiMap<byte, byte> _textRepeat = new();
    private readonly Position _transportHomePosition = new();

    // vendor items
    private readonly Dictionary<uint, VendorItemCount> _vendorItemCounts = new();

    private bool _alreadyCallAssistance;
    private uint _boundaryCheckTime;
    private uint _cannotReachTimer;
    private uint _combatPulseDelay;

    // (msecs) remaining time for next evade boundary check
    private uint _combatPulseTime;

    // (msecs) remaining time for next zone-in-combat pulse
    // (secs) how often the creature puts the entire zone in combat (only works in dungeons)
    private uint? _gossipMenuId;

    private bool _ignoreCorpseDecayRatio;
    private bool _isMissingCanSwimFlagOutOfCombat;
    private uint? _lootid;
    private LootModes _lootMode;
    private SpellSchoolMask _meleeDamageSchoolMask;

    // Timers
    private long _pickpocketLootRestore;

    // Regenerate health
    private bool _regenerateHealth;

    private SpellFocusInfo _spellFocusInfo;

    // Set on creation
    // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable
    private bool _triggerJustAppeared;

    public override CreatureAI AI => Ai as CreatureAI;
    public BattlegroundManager BattlegroundManager { get; }
    public override bool CanEnterWater => CanSwim || MovementTemplate.Swim;
    public override bool CanFly => MovementTemplate.IsFlightAllowed || IsFlying;
    public bool CanGeneratePickPocketLoot => _pickpocketLootRestore <= GameTime.CurrentTime;
    public bool CanGiveExperience => !StaticFlags.HasFlag(CreatureStaticFlags.NO_XP);

    public bool CanHaveLoot
    {
        get => !StaticFlags.HasFlag(CreatureStaticFlags.NO_LOOT);
        set => StaticFlags.ModifyFlag(CreatureStaticFlags.NO_LOOT, !value);
    }

    public bool CanHover => MovementTemplate.Ground == CreatureGroundMovementType.Hover || IsHovering;
    public bool CanIgnoreFeignDeath => Template.FlagsExtra.HasFlag(CreatureFlagsExtra.IgnoreFeighDeath);

    public bool CanMelee
    {
        get => !StaticFlags.HasFlag(CreatureStaticFlags.NO_MELEE);
        set => StaticFlags.ModifyFlag(CreatureStaticFlags.NO_MELEE, !value);
    }

    public bool CanRegenerateHealth => !StaticFlags.HasFlag(CreatureStaticFlags5.NO_HEALTH_REGEN) && _regenerateHealth;
    public override bool CanSwim => base.CanSwim || IsPet;
    public bool CanWalk => MovementTemplate.IsGroundAllowed;
    public float CombatDistance { get; set; }

    // (secs) interval at which the creature pulses the entire zone into combat (only works in dungeons)
    public uint CombatPulseDelay
    {
        get => _combatPulseDelay;
        set
        {
            _combatPulseDelay = value;

            if (_combatPulseTime == 0 || _combatPulseTime > value)
                _combatPulseTime = value;
        }
    }

    public uint CorpseDelay { get; private set; }
    public long CorpseRemoveTime { get; set; }
    public CreatureData CreatureData { get; private set; }
    public CreatureFactory CreatureFactory { get; }
    public CreatureTextManager CreatureTextManager { get; }
    public byte CurrentEquipmentId { get; set; }
    public (uint nodeId, uint pathId) CurrentWaypointInfo { get; private set; }
    public MovementGeneratorType DefaultMovementType { get; set; }
    public CreatureGroup Formation { get; set; }
    public FormationMgr FormationManager { get; }
    public GameEventManager GameEventManager { get; }

    public uint GossipMenuId
    {
        get => _gossipMenuId ?? Template.GossipMenuId;
        set => _gossipMenuId = value;
    }

    public bool HasCanSwimFlagOutOfCombat => !_isMissingCanSwimFlagOutOfCombat;
    public bool HasLootRecipient => !TapList.Empty();
    public bool HasScalableLevels => UnitData.ContentTuningID != 0;
    public bool HasSearchedAssistance { get; private set; }

    public Position HomePosition
    {
        get => _homePosition;
        set => _homePosition.Relocate(value);
    }

    public override bool IsAffectedByDiminishingReturns => base.IsAffectedByDiminishingReturns || Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish);
    public bool IsCivilian => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Civilian);
    public bool IsDungeonBoss => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss);

    public bool IsElite
    {
        get
        {
            if (IsPet)
                return false;

            return Template.Rank != CreatureEliteType.Elite && Template.Rank != CreatureEliteType.RareElite;
        }
    }

    public override bool IsEngaged => AI is { IsEngaged: true };
    public bool IsEscorted => AI != null && AI.IsEscorted();
    public bool IsEvadingAttacks => IsInEvadeMode || CannotReachTarget;
    public bool IsFormationLeader => Formation != null && Formation.IsLeader(this);
    public bool IsFormationLeaderMoveAllowed => Formation != null && Formation.CanLeaderStartMoving();

    public bool IsFullyLooted
    {
        get
        {
            if (Loot != null && !Loot.IsLooted())
                return false;

            foreach (var (_, loot) in PersonalLoot)
                if (loot != null && !loot.IsLooted())
                    return false;

            return true;
        }
    }

    public bool IsGuard => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Guard);
    public bool IsInEvadeMode => HasUnitState(UnitState.Evade);
    public bool IsRacialLeader => Template.RacialLeader;
    public bool IsReputationGainDisabled { get; set; }
    public bool IsReturningHome => MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Home;
    public bool IsTempWorldObject { get; set; }
    public bool IsTrigger => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger);
    public bool IsWorldBoss => !IsPet && Convert.ToBoolean(Template.TypeFlags & CreatureTypeFlags.BossMob);

    // Part of Evade mechanics
    public long LastDamagedTime { get; set; }

    // (msecs)timer for death or corpse disappearance
    public Loot Loot { get; set; }

    public uint LootId
    {
        get => _lootid ?? Template.LootId;
        set => _lootid = value;
    }

    public CreatureMovementData MovementTemplate => GameObjectManager.TryGetGetCreatureMovementOverride(SpawnId, out var movementOverride) ? movementOverride : Template.Movement;
    public override float NativeObjectScale => Template.Scale;

    //true when possessed
    public uint OriginalEntry { get; set; }

    public sbyte OriginalEquipmentId { get; private set; }
    public virtual byte PetAutoSpellSize => 4;
    public ulong PlayerDamageReq { get; set; }
    public PoolManager PoolManager { get; }
    public ReactStates ReactState { get; set; }

    // There's many places not ready for dynamic spawns. This allows them to live on for now.
    public bool RespawnCompatibilityMode { get; private set; }

    public uint RespawnDelay { get; set; }
    public Position RespawnPosition => GetRespawnPosition(out _);
    public long RespawnTime { get; private set; }
    public long RespawnTimeEx => RespawnTime > GameTime.CurrentTime ? RespawnTime : GameTime.CurrentTime;
    public float SightDistance { get; set; }
    public ulong SpawnId { get; set; }
    public uint[] Spells { get; set; } = new uint[SharedConst.MaxCreatureSpells];
    public StaticCreatureFlags StaticFlags { get; set; } = new();
    public string[] StringIds { get; } = new string[3];
    public HashSet<ObjectGuid> TapList { get; private set; } = new();
    public CreatureTemplate Template { get; private set; }

    public Position TransportHomePosition
    {
        get => _transportHomePosition;
        set => _transportHomePosition.Relocate(value);
    }

    public VendorItemData VendorItems => GameObjectManager.GetNpcVendorItemList(Entry);
    public float WanderDistance { get; set; }
    public WaypointManager WaypointManager { get; }
    public uint WaypointPath { get; private set; }
    public WorldDatabase WorldDatabase { get; }
    public WorldManager WorldManager { get; }
    internal Dictionary<ObjectGuid, Loot> PersonalLoot { get; set; } = new();
    private bool CannotReachTarget { get; set; }

    private CreatureAddon CreatureAddon
    {
        get
        {
            if (SpawnId == 0)
                return GameObjectManager.GetCreatureTemplateAddon(Template.Entry);

            // dependent from difficulty mode entry
            return GameObjectManager.GetCreatureAddon(SpawnId) ?? GameObjectManager.GetCreatureTemplateAddon(Template.Entry);
        }
    }

    private bool IsSpawnedOnTransport => CreatureData != null && CreatureData.MapId != Location.MapId;
}