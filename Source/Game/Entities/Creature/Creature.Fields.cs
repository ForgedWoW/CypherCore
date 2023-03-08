// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Loots;

namespace Game.Entities;

public partial class Creature
{
	readonly string[] _stringIds = new string[3];
	readonly MultiMap<byte, byte> _textRepeat = new();
	readonly Position _homePosition;
	readonly Position _transportHomePosition = new();

	// vendor items
	readonly List<VendorItemCount> _vendorItemCounts = new();
	CreatureTemplate _creatureInfo;
	CreatureData _creatureData;
	string _scriptStringId;

	SpellFocusInfo _spellFocusInfo;

	long _lastDamagedTime; // Part of Evade mechanics

	// Regenerate health
	bool _regenerateHealth;     // Set on creation
	bool _regenerateHealthLock; // Dynamically set

	bool _isMissingCanSwimFlagOutOfCombat;

	ReactStates _reactState; // for AI, not charmInfo
	byte _equipmentId;
	sbyte _originalEquipmentId; // can be -1

	bool _alreadyCallAssistance;
	bool _alreadySearchedAssistance;
	bool _cannotReachTarget;
	uint _cannotReachTimer;

	SpellSchoolMask _meleeDamageSchoolMask;

	bool _reputationGain;

	LootModes _lootMode; // Bitmask (default: LOOT_MODE_DEFAULT) that determines what loot will be lootable

	// Waypoint path
	uint _waypointPathId;
	(uint nodeId, uint pathId) _currentWaypointNodeInfo;

	//Formation var
	CreatureGroup _creatureGroup;
	bool _triggerJustAppeared;
	bool _respawnCompatibilityMode;

	// Timers
	long _pickpocketLootRestore;
	long _respawnTime;  // (secs) time of next respawn
	uint _respawnDelay; // (secs) delay between corpse disappearance and respawning
	uint _corpseDelay;  // (secs) delay between death and corpse disappearance
	bool _ignoreCorpseDecayRatio;
	float _wanderDistance;
	uint _boundaryCheckTime; // (msecs) remaining time for next evade boundary check
	uint _combatPulseTime;   // (msecs) remaining time for next zone-in-combat pulse
	uint _combatPulseDelay;  // (secs) how often the creature puts the entire zone in combat (only works in dungeons)
	HashSet<ObjectGuid> _tapList = new();
	public ulong PlayerDamageReq { get; set; }
	public float SightDistance { get; set; }
	public float CombatDistance { get; set; }
	public bool IsTempWorldObject { get; set; } //true when possessed
	public uint OriginalEntry { get; set; }

	internal Dictionary<ObjectGuid, Loot> PersonalLoot { get; set; } = new();
	public MovementGeneratorType DefaultMovementType { get; set; }
	public ulong SpawnId { get; set; }

	public uint[] Spells { get; set; } = new uint[SharedConst.MaxCreatureSpells];
	public long CorpseRemoveTime { get; set; } // (msecs)timer for death or corpse disappearance
	public Loot Loot { get; set; }
}