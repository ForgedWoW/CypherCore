// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Loots;

namespace Forged.RealmServer.Entities;

public partial class Creature
{
	private readonly MultiMap<byte, byte> _textRepeat = new();
	private readonly Position _homePosition;
	private readonly Position _transportHomePosition = new();

	// vendor items
	private readonly List<VendorItemCount> _vendorItemCounts = new();
	private string _scriptStringId;
	private SpellFocusInfo _spellFocusInfo;

	// Regenerate health
	private bool _regenerateHealth; // Set on creation

	private bool _isMissingCanSwimFlagOutOfCombat;
	private bool _alreadyCallAssistance;
	private uint _cannotReachTimer;
	private SpellSchoolMask _meleeDamageSchoolMask;
	private LootModes _lootMode; // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable
	private (uint nodeId, uint pathId) _currentWaypointNodeInfo;
	private bool _triggerJustAppeared;

	// Timers
	private long _pickpocketLootRestore;
	private bool _ignoreCorpseDecayRatio;
	private uint _boundaryCheckTime; // (msecs) remaining time for next evade boundary check
	private uint _combatPulseTime;   // (msecs) remaining time for next zone-in-combat pulse
	private uint _combatPulseDelay;  // (secs) how often the creature puts the entire zone in combat (only works in dungeons)
	private uint? _gossipMenuId;
	private uint? _lootid;

	public ulong PlayerDamageReq { get; set; }
	public float SightDistance { get; set; }
	public float CombatDistance { get; set; }
	public bool IsTempWorldObject { get; set; } //true when possessed
	public uint OriginalEntry { get; set; }

	public uint LootId
	{
		get
		{
			if (_lootid.HasValue)
				return _lootid.Value;

			return Template.LootId;
		}
		set { _lootid = value; }
	}

	internal Dictionary<ObjectGuid, Loot> PersonalLoot { get; set; } = new();
	public MovementGeneratorType DefaultMovementType { get; set; }
	public ulong SpawnId { get; set; }

	public StaticCreatureFlags StaticFlags { get; set; } = new();
	public uint[] Spells { get; set; } = new uint[SharedConst.MaxCreatureSpells];
	public long CorpseRemoveTime { get; set; } // (msecs)timer for death or corpse disappearance
	public Loot Loot { get; set; }

	public bool CanHaveLoot
	{
		get { return !StaticFlags.HasFlag(CreatureStaticFlags.NO_LOOT); }
		set { StaticFlags.ModifyFlag(CreatureStaticFlags.NO_LOOT, !value); }
	}

	public bool CanMelee
	{
		get { return !StaticFlags.HasFlag(CreatureStaticFlags.NO_MELEE); }
		set { StaticFlags.ModifyFlag(CreatureStaticFlags.NO_MELEE, !value); }
	}

	public uint GossipMenuId
	{
		get
		{
			if (_gossipMenuId.HasValue)
				return _gossipMenuId.Value;

			return Template.GossipMenuId;
		}
		set { _gossipMenuId = value; }
	}

	public bool IsReturningHome
	{
		get
		{
			if (MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Home)
				return true;

			return false;
		}
	}

	public bool IsFormationLeader
	{
		get
		{
			if (Formation == null)
				return false;

			return Formation.IsLeader(this);
		}
	}

	public bool IsFormationLeaderMoveAllowed
	{
		get
		{
			if (Formation == null)
				return false;

			return Formation.CanLeaderStartMoving();
		}
	}

	public bool CanGiveExperience => !StaticFlags.HasFlag(CreatureStaticFlags.NO_XP);

	public override bool IsEngaged
	{
		get
		{
			var ai = AI;

			if (ai != null)
				return ai.IsEngaged;

			return false;
		}
	}

	public bool IsEscorted
	{
		get
		{
			var ai = AI;

			if (ai != null)
				return ai.IsEscorted();

			return false;
		}
	}

	public bool CanGeneratePickPocketLoot => _pickpocketLootRestore <= GameTime.GetGameTime();

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

	public HashSet<ObjectGuid> TapList { get; private set; } = new();

	public bool HasLootRecipient => !TapList.Empty();

	public bool IsElite
	{
		get
		{
			if (IsPet)
				return false;

			return Template.Rank != CreatureEliteType.Elite && Template.Rank != CreatureEliteType.RareElite;
		}
	}

	public bool IsWorldBoss
	{
		get
		{
			if (IsPet)
				return false;

			return Convert.ToBoolean(Template.TypeFlags & CreatureTypeFlags.BossMob);
		}
	}

	public long RespawnTimeEx
	{
		get
		{
			var now = GameTime.GetGameTime();

			if (RespawnTime > now)
				return RespawnTime;
			else
				return now;
		}
	}

	public Position RespawnPosition => GetRespawnPosition(out _);

	public CreatureMovementData MovementTemplate
	{
		get
		{
			if (Global.ObjectMgr.TryGetGetCreatureMovementOverride(SpawnId, out var movementOverride))
				return movementOverride;

			return Template.Movement;
		}
	}

	public override bool CanSwim
	{
		get
		{
			if (base.CanSwim)
				return true;

			if (IsPet)
				return true;

			return false;
		}
	}

	public override bool CanEnterWater
	{
		get
		{
			if (CanSwim)
				return true;

			return MovementTemplate.IsSwimAllowed();
		}
	}

	public bool HasCanSwimFlagOutOfCombat => !_isMissingCanSwimFlagOutOfCombat;

	public bool HasScalableLevels => UnitData.ContentTuningID != 0;

	public string[] StringIds { get; } = new string[3];

	public VendorItemData VendorItems => Global.ObjectMgr.GetNpcVendorItemList(Entry);

	public virtual byte PetAutoSpellSize => 4;

	public override float NativeObjectScale => Template.Scale;

	public uint CorpseDelay { get; private set; }

	public bool IsRacialLeader => Template.RacialLeader;

	public bool IsCivilian => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Civilian);

	public bool IsTrigger => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger);

	public bool IsGuard => Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Guard);

	public bool CanWalk => MovementTemplate.IsGroundAllowed();

	public override bool CanFly => MovementTemplate.IsFlightAllowed() || IsFlying;

	public bool IsDungeonBoss => (Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss));

	public override bool IsAffectedByDiminishingReturns => base.IsAffectedByDiminishingReturns || Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.AllDiminish);

	public ReactStates ReactState { get; set; }

	public bool IsInEvadeMode => HasUnitState(UnitState.Evade);

	public bool IsEvadingAttacks => IsInEvadeMode || CannotReachTarget;

	public sbyte OriginalEquipmentId { get; private set; }

	public byte CurrentEquipmentId { get; set; }

	public CreatureTemplate Template { get; private set; }

	public CreatureData CreatureData { get; private set; }

	public bool IsReputationGainDisabled { get; private set; }

	private CreatureAddon CreatureAddon
	{
		get
		{
			if (SpawnId != 0)
			{
				var addon = Global.ObjectMgr.GetCreatureAddon(SpawnId);

				if (addon != null)
					return addon;
			}

			// dependent from difficulty mode entry
			return Global.ObjectMgr.GetCreatureTemplateAddon(Template.Entry);
		}
	}

	private bool IsSpawnedOnTransport => CreatureData != null && CreatureData.MapId != Location.MapId;

	private bool CannotReachTarget { get; set; }

	public bool CanHover => MovementTemplate.Ground == CreatureGroundMovementType.Hover || IsHovering;

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

	// Part of Evade mechanics
	public long LastDamagedTime { get; set; }

	public bool HasSearchedAssistance { get; private set; }

	public bool CanIgnoreFeignDeath => Template.FlagsExtra.HasFlag(CreatureFlagsExtra.IgnoreFeighDeath);

	public long RespawnTime { get; private set; }

	public uint RespawnDelay { get; set; }

	public float WanderDistance { get; set; }

	public bool CanRegenerateHealth => !StaticFlags.HasFlag(CreatureStaticFlags5.NO_HEALTH_REGEN) && _regenerateHealth;

	public Position HomePosition
	{
		get => _homePosition;
		set => _homePosition.Relocate(value);
	}

	public Position TransportHomePosition
	{
		get => _transportHomePosition;
		set => _transportHomePosition.Relocate(value);
	}

	public uint WaypointPath { get; private set; }

	public (uint nodeId, uint pathId) CurrentWaypointInfo => _currentWaypointNodeInfo;

	public CreatureGroup Formation { get; set; }

	// There's many places not ready for dynamic spawns. This allows them to live on for now.
	public bool RespawnCompatibilityMode { get; private set; }
}