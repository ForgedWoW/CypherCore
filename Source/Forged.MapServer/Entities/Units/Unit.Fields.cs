// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Cache;
using Forged.MapServer.Combat;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Movement;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public partial class Unit
{
    public static TimeSpan MaxDamageHistoryDuration = TimeSpan.FromSeconds(20);
    public bool CanDualWield;
    public object SendLock = new();
    private static readonly TimeSpan DespawnTime = TimeSpan.FromSeconds(2);
    protected float[] CreateStats = new float[(int)Stats.Max];
    private readonly AuraApplicationCollection _appliedAuras = new();
    private readonly List<AreaTrigger> _areaTrigger = new();
    private readonly MultiMap<AuraStateType, AuraApplication> _auraStateAuras = new();
    private readonly uint[] _baseAttackSpeed = new uint[(int)WeaponAttackType.Max];

    // Threat+combat management
    private readonly CombatManager _combatManager;

    private readonly DiminishingReturn[] _diminishing = new DiminishingReturn[(int)DiminishingGroup.Max];
    private readonly Dictionary<ObjectGuid, uint> _extraAttacksTargets = new();
    private readonly double[] _floatStatNegBuff = new double[(int)Stats.Max];
    private readonly double[] _floatStatPosBuff = new double[(int)Stats.Max];
    private readonly List<AbstractFollower> _followingMe = new();

    private readonly object _healthLock = new();
    private readonly List<AuraApplication> _interruptableAuras = new();

    //Auras
    private readonly ConcurrentMultiMap<AuraType, AuraEffect> _modAuras = new();

    private readonly AuraCollection _ownedAuras = new();
    private readonly Dictionary<ReactiveType, uint> _reactiveTimer = new();
    private readonly List<Player> _sharedVision = new();
    private readonly Dictionary<SpellImmunity, MultiMap<uint, uint>> _spellImmune = new();
    private readonly TimeTracker _splineSyncTimer;
    private readonly ThreatManager _threatManager;

    // auras which have interrupt mask applied on unit
    // Used for improve performance of aura state checks on aura apply/remove
    private readonly SortedSet<AuraApplication> _visibleAuras = new(new VisibleAuraSlotCompare());

    private readonly SortedSet<AuraApplication> _visibleAurasToUpdate = new(new VisibleAuraSlotCompare());
    private ushort _aiAnimKitId;
    private bool _canModifyStats;
    private CharmInfo _charmInfo;
    private SpellAuraInterruptFlags _interruptMask;
    private SpellAuraInterruptFlags2 _interruptMask2;
    private bool _isWalkingBeforeCharm;
    private uint _lastExtraAttackSpell;
    private ushort _meleeAnimKitId;
    private ushort _movementAnimKitId;
    private uint _oldFactionId;
    private PositionUpdateInfo _positionUpdateInfo;

    // faction before charm
    // Are we walking before we were charmed?
    private UnitState _state;

    public virtual IUnitAI AI
    {
        get => Ai;
        set
        {
            PushAI(value);
            RefreshAI();
        }
    }

    public override ushort AIAnimKitId => _aiAnimKitId;
    public AnimTier AnimTier => (AnimTier)(byte)UnitData.AnimTier;
    public HashSet<AuraApplication> AppliedAuras => _appliedAuras.AuraApplications;
    public int AppliedAurasCount => _appliedAuras.Count;
    public Pet AsPet => this as Pet;
    public List<Unit> Attackers => AttackerList;
    public IUnitAI BaseAI => Ai;
    public double BaseSpellCritChance { get; set; }

    public uint BattlePetCompanionExperience
    {
        get => UnitData.BattlePetCompanionExperience;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionExperience), value);
    }

    public ObjectGuid BattlePetCompanionGUID
    {
        get => UnitData.BattlePetCompanionGUID;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionGUID), value);
    }

    public uint BattlePetCompanionNameTimestamp
    {
        get => UnitData.BattlePetCompanionNameTimestamp;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BattlePetCompanionNameTimestamp), value);
    }

    public float BoundingRadius
    {
        get => UnitData.BoundingRadius;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BoundingRadius), value);
    }

    public virtual bool CanEnterWater => false;
    public virtual bool CanFly => false;
    public bool CanHaveThreatList => _threatManager.CanHaveThreatList;
    public bool CanInstantCast { get; private set; }
    public bool CanProc => ProcDeep == 0;

    public virtual bool CanSwim
    {
        get
        {
            // Mirror client behavior, if this method returns false then client will not use swimming animation and for players will apply gravity as if there was no water
            if (HasUnitFlag(UnitFlags.CantSwim))
                return false;

            if (HasUnitFlag(UnitFlags.PlayerControlled)) // is player
                return true;

            if (HasUnitFlag2((UnitFlags2)0x1000000))
                return false;

            return HasUnitFlag(UnitFlags.PetInCombat) || HasUnitFlag(UnitFlags.Rename | UnitFlags.CanSwim);
        }
    }

    public uint ChannelScriptVisualId => UnitData.ChannelData.Value.SpellVisual.ScriptVisualID;

    public uint ChannelSpellId
    {
        get => ((UnitChannel)UnitData.ChannelData).SpellID;
        set => SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.ChannelData).Value.SpellID, value);
    }

    public uint ChannelSpellXSpellVisualId => UnitData.ChannelData.Value.SpellVisual.SpellXSpellVisualID;
    public CharacterCache CharacterCache { get; }
    public Unit Charmed { get; private set; }
    public ObjectGuid CharmedGUID => UnitData.Charm;
    public Unit Charmer { get; private set; }
    public ObjectGuid CharmerGUID => UnitData.CharmedBy;
    public override Unit CharmerOrOwner => IsCharmed ? Charmer : OwnerUnit;
    public override ObjectGuid CharmerOrOwnerGUID => IsCharmed ? CharmerGUID : OwnerGUID;

    public PlayerClass Class
    {
        get => (PlayerClass)(byte)UnitData.ClassId;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ClassId), (byte)value);
    }

    public uint ClassMask => (uint)(1 << ((int)Class - 1));

    public float CollisionHeight
    {
        get
        {
            var scaleMod = ObjectScale; // 99% sure about this

            if (IsMounted)
            {
                var mountDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(MountDisplayId);

                if (mountDisplayInfo != null)
                {
                    var mountModelData = CliDB.CreatureModelDataStorage.LookupByKey(mountDisplayInfo.ModelID);

                    if (mountModelData != null)
                    {
                        var displayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(NativeDisplayId);
                        var modelData = CliDB.CreatureModelDataStorage.LookupByKey(displayInfo.ModelID);
                        var collisionHeight = scaleMod * ((mountModelData.MountHeight * mountDisplayInfo.CreatureModelScale) + (modelData.CollisionHeight * modelData.ModelScale * displayInfo.CreatureModelScale * 0.5f));

                        return collisionHeight == 0.0f ? MapConst.DefaultCollesionHeight : collisionHeight;
                    }
                }
            }

            //! Dismounting case - use basic default model data
            var defaultDisplayInfo = CliDB.CreatureDisplayInfoStorage.LookupByKey(NativeDisplayId);
            var defaultModelData = CliDB.CreatureModelDataStorage.LookupByKey(defaultDisplayInfo.ModelID);

            var collisionHeight1 = scaleMod * defaultModelData.CollisionHeight * defaultModelData.ModelScale * defaultDisplayInfo.CreatureModelScale;

            return collisionHeight1 == 0.0f ? MapConst.DefaultCollesionHeight : collisionHeight1;
        }
    }

    public override float CombatReach => (float)UnitData.CombatReach;

    //Charm
    public List<Unit> Controlled { get; set; } = new();

    public bool ControlledByPlayer { get; protected set; }
    public ObjectGuid CreatorGUID => UnitData.CreatedBy;

    public CreatureType CreatureType
    {
        get
        {
            if (IsTypeId(TypeId.Player))
            {
                var form = ShapeshiftForm;
                var ssEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)form);

                if (ssEntry is { CreatureType: > 0 })
                    return (CreatureType)ssEntry.CreatureType;

                var raceEntry = CliDB.ChrRacesStorage.LookupByKey((uint)Race);

                return (CreatureType)raceEntry.CreatureType;
            }

            return AsCreature.Template.CreatureType;
        }
    }

    public uint CreatureTypeMask
    {
        get
        {
            var creatureType = (uint)CreatureType;

            return (uint)(creatureType >= 1 ? (1 << (int)(creatureType - 1)) : 0);
        }
    }

    public ObjectGuid CritterGUID
    {
        get => UnitData.Critter;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Critter), value);
    }

    public uint DamageImmunityMask
    {
        get { return _spellImmune[SpellImmunity.Damage].KeyValueList.Aggregate<KeyValuePair<uint, uint>, uint>(0, (current, pair) => current | pair.Key); }
    }

    public LoopSafeSortedDictionary<DateTime, double> DamageTakenHistory { get; set; } = new();
    public DB2Manager DB2Manager { get; }
    public DeathState DeathState { get; protected set; }

    public ObjectGuid DemonCreatorGUID
    {
        get => UnitData.DemonCreator;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.DemonCreator), value);
    }

    public ITransport DirectTransport => Vehicle ?? Transport;
    public uint DisplayId => UnitData.DisplayID;
    public UnitDynFlags DynamicFlags => (UnitDynFlags)(uint)ObjectData.DynamicFlags;

    public Emote EmoteState
    {
        get => (Emote)(int)UnitData.EmoteState;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.EmoteState), (int)value);
    }

    public override uint Faction
    {
        get => UnitData.FactionTemplate;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.FactionTemplate), value);
    }

    public virtual float FollowAngle => MathFunctions.PI_OVER2;

    public Gender Gender
    {
        get => (Gender)(byte)UnitData.Sex;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Sex), (byte)value);
    }

    public bool HasInvisibilityAura => HasAuraType(AuraType.ModInvisibility);
    public bool HasRootAura => HasAuraType(AuraType.ModRoot) || HasAuraType(AuraType.ModRoot2) || HasAuraType(AuraType.ModRootDisableGravity);

    //SharedVision
    public bool HasSharedVision => !_sharedVision.Empty();

    public bool HasStealthAura => HasAuraType(AuraType.ModStealth);
    public float HoverOffset => HasUnitMovementFlag(MovementFlag.Hover) ? UnitData.HoverHeight : 0.0f;
    public virtual bool IsAffectedByDiminishingReturns => (CharmerOrOwnerPlayerOrPlayerItself != null);
    public bool IsAIEnabled => Ai != null;
    public bool IsAlive => DeathState == DeathState.Alive;
    public bool IsArmorer => HasNpcFlag(NPCFlags.Repair);
    public bool IsAuctioner => HasNpcFlag(NPCFlags.Auctioneer);
    public bool IsBanker => HasNpcFlag(NPCFlags.Banker);
    public bool IsBattleMaster => HasNpcFlag(NPCFlags.BattleMaster);
    public bool IsCharmed => !CharmerGUID.IsEmpty;
    public bool IsCharmedOwnedByPlayerOrPlayer => CharmerOrOwnerOrOwnGUID.IsPlayer;

    /// <summary>
    ///     returns if the unit can't enter combat
    /// </summary>
    public bool IsCombatDisallowed { get; private set; }

    public bool IsCritter => CreatureType == CreatureType.Critter;
    public bool IsDead => DeathState is DeathState.Dead or DeathState.Corpse;
    public bool IsDuringRemoveFromWorld { get; private set; }
    public bool IsDying => DeathState == DeathState.JustDied;

    // This value can be different from IsInCombat, for example:
    // - when a projectile spell is midair against a creature (combat on launch - threat+aggro on impact)
    // - when the creature has no targets left, but the AI has not yet ceased engaged logic
    public virtual bool IsEngaged => IsInCombat;

    public bool IsFalling => MovementInfo.HasMovementFlag(MovementFlag.Falling | MovementFlag.FallingFar) || MoveSpline.IsFalling();
    public bool IsFeared => HasAuraType(AuraType.ModFear);
    public bool IsFFAPvP => HasPvpFlag(UnitPVPStateFlags.FFAPvp);
    public bool IsFlying => MovementInfo.HasMovementFlag(MovementFlag.Flying | MovementFlag.DisableGravity);
    public bool IsFrozen => HasAuraState(AuraStateType.Frozen);
    public bool IsGossip => HasNpcFlag(NPCFlags.Gossip);
    public bool IsGravityDisabled => MovementInfo.HasMovementFlag(MovementFlag.DisableGravity);
    public bool IsGuardian => UnitTypeMask.HasAnyFlag(UnitTypeMask.Guardian);
    public bool IsGuildMaster => HasNpcFlag(NPCFlags.Petitioner);
    public bool IsHovering => MovementInfo.HasMovementFlag(MovementFlag.Hover);
    public bool IsHunterPet => UnitTypeMask.HasAnyFlag(UnitTypeMask.HunterPet);
    public bool IsInCombat => HasUnitFlag(UnitFlags.InCombat);
    public bool IsInDisallowedMountForm => IsDisallowedMountForm(TransformSpell, ShapeshiftForm, DisplayId);

    public bool IsInFeralForm
    {
        get
        {
            var form = ShapeshiftForm;

            return form is ShapeShiftForm.CatForm or
                           ShapeShiftForm.BearForm or
                           ShapeShiftForm.DireBearForm or
                           ShapeShiftForm.GhostWolf;
        }
    }

    public bool IsInFlight => HasUnitState(UnitState.InFlight);
    public bool IsInnkeeper => HasNpcFlag(NPCFlags.Innkeeper);
    public bool IsInSanctuary => HasPvpFlag(UnitPVPStateFlags.Sanctuary);
    public virtual bool IsLoading => false;

    public bool IsMagnet
    {
        get
        {
            // Grounding Totem
            if (UnitData.CreatedBySpell == 8177) // @todo: find a more generic solution
                return true;

            return false;
        }
    }

    public bool IsMounted => HasUnitFlag(UnitFlags.Mount);
    public bool IsMoving => MovementInfo.HasMovementFlag(MovementFlag.MaskMoving);
    public bool IsPet => UnitTypeMask.HasAnyFlag(UnitTypeMask.Pet);
    public bool IsPetInCombat => HasUnitFlag(UnitFlags.PetInCombat);
    public bool IsPlayingHoverAnim { get; set; }

    public bool IsPolymorphed
    {
        get
        {
            var transformId = TransformSpell;

            if (transformId == 0)
                return false;

            var spellInfo = SpellManager.GetSpellInfo(transformId, Location.Map.DifficultyID);

            if (spellInfo == null)
                return false;

            return spellInfo.GetSpellSpecific() == SpellSpecificType.MagePolymorph;
        }
    }

    public bool IsPossessed => HasUnitState(UnitState.Possessed);
    public bool IsPossessedByPlayer => HasUnitState(UnitState.Possessed) && CharmerGUID.IsPlayer;

    public bool IsPossessing
    {
        get
        {
            var u = Charmed;

            if (u != null)
                return u.IsPossessed;
            else
                return false;
        }
    }

    public bool IsPvP => HasPvpFlag(UnitPVPStateFlags.PvP);
    public bool IsQuestGiver => HasNpcFlag(NPCFlags.QuestGiver);

    public bool IsServiceProvider => HasNpcFlag(NPCFlags.Vendor |
                                                NPCFlags.Trainer |
                                                NPCFlags.FlightMaster |
                                                NPCFlags.Petitioner |
                                                NPCFlags.BattleMaster |
                                                NPCFlags.Banker |
                                                NPCFlags.Innkeeper |
                                                NPCFlags.SpiritHealer |
                                                NPCFlags.SpiritGuide |
                                                NPCFlags.TabardDesigner |
                                                NPCFlags.Auctioneer);

    public bool IsSitState
    {
        get
        {
            var s = StandState;

            return
                s is UnitStandStateType.SitChair or
                     UnitStandStateType.SitLowChair or
                     UnitStandStateType.SitMediumChair or
                     UnitStandStateType.SitHighChair or
                     UnitStandStateType.Sit;
        }
    }

    public bool IsSpiritGuide => HasNpcFlag(NPCFlags.SpiritGuide);
    public bool IsSpiritHealer => HasNpcFlag(NPCFlags.SpiritHealer);
    public bool IsSpiritService => HasNpcFlag(NPCFlags.SpiritHealer | NPCFlags.SpiritGuide);

    //Spline
    public bool IsSplineEnabled => MoveSpline.Initialized() && !MoveSpline.Finalized();

    public bool IsStandState => !IsSitState && StandState != UnitStandStateType.Sleep && StandState != UnitStandStateType.Kneel;
    public bool IsStopped => !HasUnitState(UnitState.Moving);
    public bool IsSummon => UnitTypeMask.HasAnyFlag(UnitTypeMask.Summon);
    public bool IsTabardDesigner => HasNpcFlag(NPCFlags.TabardDesigner);
    public bool IsTaxi => HasNpcFlag(NPCFlags.FlightMaster);
    public bool IsThreatened => !_threatManager.IsThreatListEmpty();
    public bool IsTotem => UnitTypeMask.HasAnyFlag(UnitTypeMask.Totem);
    public bool IsTrainer => HasNpcFlag(NPCFlags.Trainer);
    public bool IsTurning => MovementInfo.HasMovementFlag(MovementFlag.MaskTurning);
    public bool IsVehicle => UnitTypeMask.HasAnyFlag(UnitTypeMask.Vehicle);
    public bool IsVendor => HasNpcFlag(NPCFlags.Vendor);
    public bool IsWalking => MovementInfo.HasMovementFlag(MovementFlag.Walking);
    public bool IsWildBattlePet => HasNpcFlag(NPCFlags.WildBattlePet);
    public ObjectGuid LastCharmerGuid { get; set; }
    public ObjectGuid LastDamagedTargetGuid { get; set; }
    public uint LastSanctuaryTime { get; set; }
    public uint Level => UnitData.Level;
    public LootManager LootManager { get; }
    public LootStoreBox LootStorage { get; }

    public ulong MechanicImmunityMask
    {
        get
        {
            ulong mask = 0;
            var mechanicList = _spellImmune[SpellImmunity.Mechanic];

            foreach (var pair in mechanicList.KeyValueList)
                mask |= (1ul << (int)pair.Value);

            return mask;
        }
    }

    public override ushort MeleeAnimKitId => _meleeAnimKitId;

    public ObjectGuid MinionGUID
    {
        get => UnitData.Summon;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Summon), value);
    }

    public double ModMeleeHitChance { get; set; }
    public double ModRangedHitChance { get; set; }
    public double ModSpellHitChance { get; set; }
    public MotionMaster MotionMaster { get; }

    public uint MountDisplayId
    {
        get => UnitData.MountDisplayID;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.MountDisplayID), value);
    }

    public override ushort MovementAnimKitId => _movementAnimKitId;
    public uint MovementCounter { get; set; }
    public MovementForces MovementForces { get; private set; }
    public MoveSpline MoveSpline { get; set; }
    public uint NativeDisplayId => UnitData.NativeDisplayID;
    public float NativeDisplayScale => UnitData.NativeXDisplayScale;

    public virtual Gender NativeGender
    {
        get => Gender;
        set => Gender = value;
    }

    public virtual float NativeObjectScale => 1.0f;
    public NPCFlags NpcFlags => (NPCFlags)UnitData.NpcFlags[0];
    public NPCFlags2 NpcFlags2 => (NPCFlags2)UnitData.NpcFlags[1];

    public override float ObjectScale
    {
        get => base.ObjectScale;
        set
        {
            var minfo = ObjectManager.GetCreatureModelInfo(DisplayId);

            if (minfo != null)
            {
                BoundingRadius = (IsPet ? 1.0f : minfo.BoundingRadius) * ObjectScale;
                SetCombatReach((IsPet ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * ObjectScale);
            }

            base.ObjectScale = value;
        }
    }

    public ObjectGuid[] ObjectSlot { get; set; } = new ObjectGuid[4];
    public List<Aura> OwnedAurasList => _ownedAuras.Auras;
    public override ObjectGuid OwnerGUID => UnitData.SummonedBy;
    public UnitPetFlags PetFlags => (UnitPetFlags)(byte)UnitData.PetFlags;

    public ObjectGuid PetGUID
    {
        get => SummonSlot[0];
        set => SummonSlot[0] = value;
    }

    public Player PlayerMovingMe1 => PlayerMovingMe;
    public UnitPVPStateFlags PvpFlags => (UnitPVPStateFlags)(byte)UnitData.PvpFlags;

    public Race Race
    {
        get => (Race)(byte)UnitData.Race;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Race), (byte)value);
    }

    public uint RegenTimer { get; set; }

    public uint SchoolImmunityMask
    {
        get
        {
            uint mask = 0;
            var schoolList = _spellImmune[SpellImmunity.School];

            foreach (var pair in schoolList.KeyValueList)
                mask |= pair.Key;

            return mask;
        }
    }

    public ScriptManager ScriptManager { get; }

    public ShapeShiftForm ShapeshiftForm
    {
        get => (ShapeShiftForm)(byte)UnitData.ShapeshiftForm;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ShapeshiftForm), (byte)value);
    }

    public SheathState Sheath
    {
        get => (SheathState)(byte)UnitData.SheatheState;
        set
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SheatheState), (byte)value);

            if (value == SheathState.Unarmed)
                RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Sheathing);
        }
    }

    // Auras
    public List<Aura> SingleCastAuras { get; } = new();

    public SpellHistory SpellHistory { get; private set; }
    public UnitStandStateType StandState => (UnitStandStateType)(byte)UnitData.StandState;
    public ObjectGuid[] SummonSlot { get; set; } = new ObjectGuid[7];
    public ObjectGuid Target => UnitData.Target;
    public uint TransformSpell { get; set; }
    public Unit UnitBeingMoved => UnitMovedByMe;
    public UnitCombatHelpers UnitCombatHelpers { get; }

    //General
    public UnitData UnitData { get; set; }

    public UnitTypeMask UnitTypeMask { get; set; }
    public Vehicle Vehicle { get; set; }
    public Unit VehicleBase => Vehicle?.GetBase();
    public Creature VehicleCreatureBase => VehicleBase?.AsCreature;
    public Vehicle VehicleKit { get; set; }
    public Unit Victim => Attacking;

    public List<AuraApplication> VisibleAuras
    {
        get
        {
            lock (_visibleAurasToUpdate)
            {
                return _visibleAuras.ToList();
            }
        }
    }

    public uint WildBattlePetLevel
    {
        get => UnitData.WildBattlePetLevel;
        set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.WildBattlePetLevel), value);
    }

    internal double[] ModAttackSpeedPct { get; set; } = new double[(int)WeaponAttackType.Max];
    internal uint RemovedAurasCount { get; private set; }

    protected IUnitAI Ai { get; set; }

    //Combat
    protected List<Unit> AttackerList { get; set; } = new();

    protected Unit Attacking { get; set; }

    protected uint[] AttackTimer { get; set; } = new uint[(int)WeaponAttackType.Max];

    protected double[][] AuraFlatModifiersGroup { get; set; } = new double[(int)UnitMods.End][];

    protected double[][] AuraPctModifiersGroup { get; set; } = new double[(int)UnitMods.End][];

    //Spells
    protected Dictionary<CurrentSpellTypes, Spell> CurrentSpells { get; set; } = new((int)CurrentSpellTypes.Max);

    protected List<DynamicObject> DynamicObjects { get; set; } = new();

    protected List<GameObject> GameObjects { get; set; } = new();

    protected LiquidTypeRecord LastLiquid { get; set; }

    protected Player PlayerMovingMe { get; set; }

    protected int ProcDeep { get; set; }

    //Movement
    protected float[] SpeedRate { get; set; } = new float[(int)UnitMoveType.Max];

    //AI
    protected Stack<IUnitAI> UnitAis { get; set; } = new();

    //< Incrementing counter used in movement packets
    protected Unit UnitMovedByMe { get; set; } // only ever set for players, and only for direct client control

    // only set for direct client control (possess effects, vehicles and similar)
    protected double[][] WeaponDamage { get; set; } = new double[(int)WeaponAttackType.Max][];
}