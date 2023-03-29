// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Combat;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Movement;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public partial class Unit
{
    public static TimeSpan MAX_DAMAGE_HISTORY_DURATION = TimeSpan.FromSeconds(20);
    public bool CanDualWield;
    protected float[] CreateStats = new float[(int)Stats.Max];
    private readonly List<AbstractFollower> _followingMe = new();

    private readonly MotionMaster _motionMaster;
    private readonly TimeTracker _splineSyncTimer;
    private readonly Dictionary<ReactiveType, uint> _reactiveTimer = new();
    private readonly uint[] _baseAttackSpeed = new uint[(int)WeaponAttackType.Max];

    // Threat+combat management
    private readonly CombatManager _combatManager;
    private readonly ThreatManager _threatManager;
    private readonly Dictionary<ObjectGuid, uint> _extraAttacksTargets = new();
    private readonly List<Player> _sharedVision = new();
    private readonly MultiMap<uint, uint>[] _spellImmune = new MultiMap<uint, uint>[(int)SpellImmunity.Max];

    //Auras
    private readonly ConcurrentMultiMap<AuraType, AuraEffect> _modAuras = new();
    private readonly List<Aura> _removedAuras = new();
    private readonly List<AuraApplication> _interruptableAuras = new();                // auras which have interrupt mask applied on unit
    private readonly MultiMap<AuraStateType, AuraApplication> _auraStateAuras = new(); // Used for improve performance of aura state checks on aura apply/remove
    private readonly SortedSet<AuraApplication> _visibleAuras = new(new VisibleAuraSlotCompare());
    private readonly SortedSet<AuraApplication> _visibleAurasToUpdate = new(new VisibleAuraSlotCompare());
    private readonly AuraApplicationCollection _appliedAuras = new();
    private readonly AuraCollection _ownedAuras = new();
    private readonly List<Aura> _scAuras = new();
    private readonly DiminishingReturn[] _diminishing = new DiminishingReturn[(int)DiminishingGroup.Max];
    private readonly List<AreaTrigger> _areaTrigger = new();
    private readonly double[] _floatStatPosBuff = new double[(int)Stats.Max];
    private readonly double[] _floatStatNegBuff = new double[(int)Stats.Max];
    private MovementForces _movementForces;
    private PositionUpdateInfo _positionUpdateInfo;
    private bool _isCombatDisallowed;

    private uint _lastExtraAttackSpell;
    private ObjectGuid _lastDamagedTargetGuid;
    private Unit _charmer; // Unit that is charming ME
    private Unit _charmed; // Unit that is being charmed BY ME
    private CharmInfo _charmInfo;

    private uint _oldFactionId;         // faction before charm
    private bool _isWalkingBeforeCharm; // Are we walking before we were charmed?
    private SpellAuraInterruptFlags _interruptMask;
    private SpellAuraInterruptFlags2 _interruptMask2;
    private SpellHistory _spellHistory;
    private uint _removedAurasCount;
    private UnitState _state;
    private bool _canModifyStats;
    private uint _transformSpell;
    private bool _cleanupDone;           // lock made to not add stuff after cleanup before delete
    private bool _duringRemoveFromWorld; // lock made to not add stuff after begining removing from world
    private bool _instantCast;

    private bool _playHoverAnim;

    private ushort _aiAnimKitId;
    private ushort _movementAnimKitId;

    private ushort _meleeAnimKitId;

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

    private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
    {
        private readonly Unit _owner;
        private readonly ObjectFieldData _objectMask = new();
        private readonly UnitData _unitMask = new();

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