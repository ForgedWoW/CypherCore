// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Game.Common.DataStorage.Structs.L;
using Game.Common.Entities.AreaTriggers;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Players;

namespace Game.Common.Entities.Units;

public partial class Unit
{
	public static TimeSpan MAX_DAMAGE_HISTORY_DURATION = TimeSpan.FromSeconds(20);
	public bool CanDualWield;
	protected float[] CreateStats = new float[(int)Stats.Max];
	readonly List<AbstractFollower> _followingMe = new();

	readonly MotionMaster _motionMaster;
	readonly TimeTracker _splineSyncTimer;
	readonly Dictionary<ReactiveType, uint> _reactiveTimer = new();
	readonly uint[] _baseAttackSpeed = new uint[(int)WeaponAttackType.Max];

	// Threat+combat management
	readonly CombatManager _combatManager;
	readonly ThreatManager _threatManager;
	readonly Dictionary<ObjectGuid, uint> _extraAttacksTargets = new();
	readonly List<Player> _sharedVision = new();
	readonly MultiMap<uint, uint>[] _spellImmune = new MultiMap<uint, uint>[(int)SpellImmunity.Max];

	//Auras
	readonly ConcurrentMultiMap<AuraType, AuraEffect> _modAuras = new();
	readonly List<Aura> _removedAuras = new();
	readonly List<AuraApplication> _interruptableAuras = new();                // auras which have interrupt mask applied on unit
	readonly MultiMap<AuraStateType, AuraApplication> _auraStateAuras = new(); // Used for improve performance of aura state checks on aura apply/remove
	readonly SortedSet<AuraApplication> _visibleAuras = new(new VisibleAuraSlotCompare());
	readonly SortedSet<AuraApplication> _visibleAurasToUpdate = new(new VisibleAuraSlotCompare());
	readonly AuraApplicationCollection _appliedAuras = new();
	readonly AuraCollection _ownedAuras = new();
	readonly List<Aura> _scAuras = new();
	readonly DiminishingReturn[] _diminishing = new DiminishingReturn[(int)DiminishingGroup.Max];
	readonly List<AreaTrigger> _areaTrigger = new();
	readonly double[] _floatStatPosBuff = new double[(int)Stats.Max];
	readonly double[] _floatStatNegBuff = new double[(int)Stats.Max];
	MovementForces _movementForces;
	PositionUpdateInfo _positionUpdateInfo;
	bool _isCombatDisallowed;

	uint _lastExtraAttackSpell;
	ObjectGuid _lastDamagedTargetGuid;
	Unit _charmer; // Unit that is charming ME
	Unit _charmed; // Unit that is being charmed BY ME
	CharmInfo _charmInfo;

	uint _oldFactionId;         // faction before charm
	bool _isWalkingBeforeCharm; // Are we walking before we were charmed?
	SpellAuraInterruptFlags _interruptMask;
	SpellAuraInterruptFlags2 _interruptMask2;
	SpellHistory _spellHistory;
	uint _removedAurasCount;
	UnitState _state;
	bool _canModifyStats;
	uint _transformSpell;
	bool _cleanupDone;           // lock made to not add stuff after cleanup before delete
	bool _duringRemoveFromWorld; // lock made to not add stuff after begining removing from world
	bool _instantCast;

	bool _playHoverAnim;

	ushort _aiAnimKitId;
	ushort _movementAnimKitId;

	ushort _meleeAnimKitId;

	//AI
	protected Stack<IUnitAI> UnitAis { get; set; } = new();
	protected IUnitAI Ai { get; set; }

	//Movement
	protected float[] SpeedRate { get; set; } = new float[(int)UnitMoveType.Max];
	public MoveSpline MoveSpline { get; set; }
	public uint MovementCounter { get; set; }     //< Incrementing counter used in movement packets
	protected Unit UnitMovedByMe { get; set; }    // only ever set for players, and only for direct client control
	protected Player PlayerMovingMe { get; set; } // only set for direct client control (possess effects, vehicles and similar)

	//Combat
	protected List<Unit> AttackerList { get; set; } = new();
	protected double[][] WeaponDamage { get; set; } = new double[(int)WeaponAttackType.Max][];
	internal double[] ModAttackSpeedPct { get; set; } = new double[(int)WeaponAttackType.Max];
	protected uint[] AttackTimer { get; set; } = new uint[(int)WeaponAttackType.Max];

	protected Unit Attacking { get; set; }

	public double ModMeleeHitChance { get; set; }
	public double ModRangedHitChance { get; set; }
	public double ModSpellHitChance { get; set; }
	public double BaseSpellCritChance { get; set; }
	public uint RegenTimer { get; set; }

	//Charm
	public List<Unit> Controlled { get; set; } = new();
	protected bool ControlledByPlayer { get; set; }
	public ObjectGuid LastCharmerGuid { get; set; }

	//Spells 
	protected Dictionary<CurrentSpellTypes, Spell> CurrentSpells { get; set; } = new((int)CurrentSpellTypes.Max);
	protected int ProcDeep { get; set; }
	protected double[][] AuraFlatModifiersGroup { get; set; } = new double[(int)UnitMods.End][];
	protected double[][] AuraPctModifiersGroup { get; set; } = new double[(int)UnitMods.End][];

	//General  
	public UnitData UnitData { get; set; }
	protected List<GameObject> GameObjects { get; set; } = new();
	protected List<DynamicObject> DynamicObjects { get; set; } = new();
	public ObjectGuid[] SummonSlot { get; set; } = new ObjectGuid[7];
	public ObjectGuid[] ObjectSlot { get; set; } = new ObjectGuid[4];
	public UnitTypeMask UnitTypeMask { get; set; }
	protected LiquidTypeRecord LastLiquid { get; set; }
	public DeathState DeathState { get; protected set; }
	public Vehicle Vehicle { get; set; }
	public Vehicle VehicleKit { get; set; }
	public uint LastSanctuaryTime { get; set; }
	public LoopSafeSortedDictionary<DateTime, double> DamageTakenHistory { get; set; } = new();

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly Unit _owner;
		readonly ObjectFieldData _objectMask = new();
		readonly UnitData _unitMask = new();

		public ValuesUpdateForPlayerWithMaskSender(Unit owner)
		{
			_owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(_owner.Location.MapId);

			_owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _unitMask.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}
