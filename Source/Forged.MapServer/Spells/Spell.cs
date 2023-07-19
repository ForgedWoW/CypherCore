// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.BattleFields;
using Forged.MapServer.Chat;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Forged.MapServer.Spells.Skills;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Dynamic;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Spells;

public partial class Spell : IDisposable
{
    public SpellCastVisual SpellVisual;

    // if need this can be replaced by Aura copy
    // we can't store original aura link to prevent access to deleted auras
    // and in same time need aura data and after aura deleting.
    public SpellInfo TriggeredByAuraSpell;

    // *****************************************
    // Spell target subsystem
    // *****************************************
    // Targets store structures and data
    public List<TargetInfo> UniqueTargetInfo = new();

    // Healing in effects count here
    public List<TargetInfo> UniqueTargetInfoOrgi = new();

    private static readonly List<ISpellScript> Dummy = new();
    private static readonly List<(ISpellScript, ISpellEffect)> DummySpellEffects = new();

    private readonly HashSet<int> _applyMultiplierMask = new();

    private readonly BattleFieldManager _battleFieldManager;
    private readonly BattlePetData _battlePetData;
    private readonly bool _canReflect;

    private readonly CellCalculator _cellCalculator;
    private readonly HashSet<int> _channelTargetEffectMask = new();
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDb;
    private readonly UnitCombatHelpers _combatHelpers;
    private readonly CreatureTextManager _creatureTextManager;
    // can reflect this spell?
    private readonly Dictionary<int, double> _damageMultipliers = new();

    private readonly DB2Manager _db2Manager;
    private readonly Dictionary<int, SpellDestination> _destTargets = new();
    private readonly Dictionary<int, Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>> _effectHandlers = new();
    private readonly Dictionary<byte, SpellEmpowerStageRecord> _empowerStages = new();
    private readonly Dictionary<SpellEffectName, SpellLogEffect> _executeLogEffects = new();
    private readonly GameObjectFactory _gameObjectFactory;
    private readonly GameObjectManager _gameObjectManager;
    private readonly GridDefines _gridDefines;
    private readonly GroupManager _groupManager;
    private readonly List<HitTriggerSpell> _hitTriggerSpells = new();
    private readonly InstanceLockManager _instanceLockManager;
    private readonly ItemEnchantmentManager _itemEnchantmentManager;
    private readonly LootFactory _lootFactory;
    private readonly LootStoreBox _lootStoreBox;
    private readonly ObjectAccessor _objectAccessor;
    private readonly OutdoorPvPManager _outdoorPvPManager;
    private readonly PhasingHandler _phasingHandler;
    private readonly PlayerComputators _playerComputators;
    private readonly ConversationFactory _conversationFactory;
    private readonly ItemFactory _itemFactory;
    private readonly SceneFactory _sceneFactory;
    private readonly ScriptManager _scriptManager;
    private readonly SkillExtraItems _skillExtraItems;
    private readonly SkillPerfectItems _skillPerfectItems;
    private readonly SpellFactory _spellFactory;
    private readonly SpellManager _spellManager;
    // Spell school (can be overwrite for some spells (wand shoot for example)
    // Victim   trigger flags
    private readonly Dictionary<Type, List<ISpellScript>> _spellScriptsByType = new();

    private readonly TraitMgr _traitMgr;
    private readonly TriggerCastFlags _triggeredCastFlags;
    private readonly List<CorpseTargetInfo> _uniqueCorpseTargetInfo = new();
    private readonly List<GOTargetInfo> _uniqueGoTargetInfo = new();
    private readonly List<ItemTargetInfo> _uniqueItemInfo = new();

    private readonly VMapManager _vMapManager;

    private readonly WorldManager _worldManager;
    // Mask req. alive targets

    private int _channeledDuration;
    private byte _delayAtDamageCount;
    private SpellEffectHandleMode _effectHandleMode;
    private uint _empoweredSpellDelta;

    private byte _empoweredSpellStage;

    // Empower spell meta
    private EmpowerState _empowerState = EmpowerState.None;

    private bool _executedCurrently;

    // -------------------------------------------
    private GameObject _focusObject;

    private bool _immediateHandled;

    private bool _isAutoRepeat;

    // Delayed spells system
    private bool _launchHandled;

    private List<SpellScript> _loadedScripts = new();
    private ObjectGuid _originalCasterGuid;

    private PathGenerator _preGeneratedPath;

    // These vars are used in both delayed spell system and modified immediate spell system
    private bool _referencedFromCurrentSpell;

    // Calculated channeled spell duration in order to calculate correct pushback.
    private byte _runesState;

    private SpellEvent _spellEvent;

    // were launch actions handled
    // were immediate actions handled? (used by delayed spells only)
    private int _timer;

    public Spell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, LootFactory lootFactory, ClassFactory classFactory, VMapManager vMapManager, DB2Manager db2Manager,
                 SkillPerfectItems skillPerfectItems, SkillExtraItems skillExtraItems, ItemEnchantmentManager itemEnchantmentManager, GameObjectManager gameObjectManager, InstanceLockManager instanceLockManager,
                 CliDB cliDb, BattleFieldManager battleFieldManager, UnitCombatHelpers combatHelpers, SpellManager spellManager, GroupManager groupManager, ScriptManager scriptManager, LootStoreBox lootStoreBox,
                 WorldManager worldManager, GridDefines gridDefines, CellCalculator cellCalculator, TraitMgr traitMgr, GameObjectFactory gameObjectFactory, PhasingHandler phasingHandler,
                 BattlePetData battlePetData, OutdoorPvPManager outdoorPvPManager, ObjectAccessor objectAccessor, CreatureTextManager creatureTextManager, PlayerComputators playerComputators, 
                 ConversationFactory conversationFactory, ItemFactory itemFactory, SceneFactory sceneFactory, 
                 ObjectGuid originalCasterGuid = default, ObjectGuid originalCastId = default, byte? empoweredStage = null)
    {
        SpellInfo = info;
        _vMapManager = vMapManager;
        _db2Manager = db2Manager;
        _skillPerfectItems = skillPerfectItems;
        _skillExtraItems = skillExtraItems;
        _itemEnchantmentManager = itemEnchantmentManager;
        _gameObjectManager = gameObjectManager;
        _instanceLockManager = instanceLockManager;
        _cliDb = cliDb;
        _battleFieldManager = battleFieldManager;
        _combatHelpers = combatHelpers;
        _spellManager = spellManager;
        _groupManager = groupManager;
        _scriptManager = scriptManager;
        _lootStoreBox = lootStoreBox;
        _worldManager = worldManager;
        _gridDefines = gridDefines;
        _cellCalculator = cellCalculator;
        _traitMgr = traitMgr;
        _gameObjectFactory = gameObjectFactory;
        _phasingHandler = phasingHandler;
        _battlePetData = battlePetData;
        _outdoorPvPManager = outdoorPvPManager;
        _objectAccessor = objectAccessor;
        _creatureTextManager = creatureTextManager;
        _playerComputators = playerComputators;
        _conversationFactory = conversationFactory;
        _itemFactory = itemFactory;
        _sceneFactory = sceneFactory;

        foreach (var stage in info.EmpowerStages)
            _empowerStages[stage.Key] = new SpellEmpowerStageRecord
            {
                Id = stage.Value.Id,
                DurationMs = stage.Value.DurationMs,
                SpellEmpowerID = stage.Value.SpellEmpowerID,
                Stage = stage.Value.Stage,
            };

        Caster = info.HasAttribute(SpellAttr6.OriginateFromController) && caster.CharmerOrOwner != null ? caster.CharmerOrOwner : caster;
        SpellValue = new SpellValue(SpellInfo, caster);
        NeedComboPoints = SpellInfo.NeedsComboPoints;

        // Get data for type of attack
        AttackType = info.GetAttackType();

        SpellSchoolMask = SpellInfo.SchoolMask; // Can be override for some spell (wand shoot for example)

        if (originalCasterGuid.IsEmpty)
            _originalCasterGuid = Caster.GUID;

        var playerCaster = Caster.AsPlayer;

        if (playerCaster != null)
            // wand case
            if (AttackType == WeaponAttackType.RangedAttack)
                if ((playerCaster.ClassMask & (uint)PlayerClass.ClassMaskWandUsers) != 0)
                {
                    var pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

                    if (pItem != null)
                        SpellSchoolMask = (SpellSchoolMask)(1 << (int)pItem.Template.DamageType);
                }

        var modOwner = caster.SpellModOwner;
        int stack = SpellValue.AuraStackAmount;
        modOwner?.ApplySpellMod(info, SpellModOp.Doses, ref stack, this);
        SpellValue.AuraStackAmount = stack;
        if (_originalCasterGuid == Caster.GUID)
            OriginalCaster = Caster.AsUnit;
        else
        {
            OriginalCaster = Caster.ObjectAccessor.GetUnit(Caster, _originalCasterGuid);
            OriginalCaster = OriginalCaster is { Location.IsInWorld: false } ? null : Caster.AsUnit;
        }

        _triggeredCastFlags = triggerFlags;
        _lootFactory = lootFactory;
        _classFactory = classFactory;

        if (info.HasAttribute(SpellAttr2.DoNotReportSpellFailure) || _triggeredCastFlags.HasFlag(TriggerCastFlags.TriggeredAllowProc))
            _triggeredCastFlags |= TriggerCastFlags.DontReportCastError;

        if (SpellInfo.HasAttribute(SpellAttr4.AllowCastWhileCasting) || _triggeredCastFlags.HasFlag(TriggerCastFlags.TriggeredAllowProc))
            _triggeredCastFlags |= TriggerCastFlags.IgnoreCastInProgress;

        CastItemLevel = -1;

        if (IsIgnoringCooldowns)
            CastFlagsEx |= SpellCastFlagsEx.IgnoreCooldown;

        CastId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, Caster.Location.MapId, SpellInfo.Id, Caster.Location.Map.GenerateLowGuid(HighGuid.Cast));
        OriginalCastId = originalCastId;
        SpellVisual.SpellXSpellVisualID = caster.GetCastSpellXSpellVisualId(SpellInfo);

        //Auto Shot & Shoot (wand)
        _isAutoRepeat = SpellInfo.IsAutoRepeatRangedSpell;

        // Determine if spell can be reflected back to the caster
        // Patch 1.2 notes: Spell Reflection no longer reflects abilities
        _canReflect = caster.IsUnit && SpellInfo.DmgClass == SpellDmgClass.Magic && !SpellInfo.HasAttribute(SpellAttr0.IsAbility) && !SpellInfo.HasAttribute(SpellAttr1.NoReflection) && !SpellInfo.HasAttribute(SpellAttr0.NoImmunities) && !SpellInfo.IsPassive;
        CleanupTargetList();

        foreach (var effect in SpellInfo.Effects)
            _destTargets[effect.EffectIndex] = new SpellDestination(Caster);

        Targets = new SpellCastTargets();
        AppliedMods = new List<Aura>();
        EmpoweredStage = empoweredStage;
    }

    public List<Aura> AppliedMods { get; set; }
    public Difficulty CastDifficulty => Caster.Location.Map.DifficultyID;
    public WorldObject Caster { get; }
    public SpellCastFlagsEx CastFlagsEx { get; set; }
    public ObjectGuid CastId { get; set; }
    public Item CastItem { get; set; }
    public uint CastItemEntry { get; set; }
    public ObjectGuid CastItemGuid { get; set; }
    public int CastItemLevel { get; set; }
    public int CastTime { get; private set; }
    public sbyte ComboPointGain { get; set; }
    public Corpse CorpseTarget { get; set; }

    public CurrentSpellTypes CurrentContainer
    {
        get
        {
            if (SpellInfo.IsNextMeleeSwingSpell)
                return CurrentSpellTypes.Melee;

            if (_isAutoRepeat)
                return CurrentSpellTypes.AutoRepeat;

            return SpellInfo.IsChanneled ? CurrentSpellTypes.Channeled : CurrentSpellTypes.Generic;
        }
    }

    public object CustomArg { get; set; }
    public SpellCustomErrors CustomErrors { get; set; }

    public double Damage { get; set; }

    // Damage and healing in effects need just calculate
    public double DamageInEffects { get; set; }

    public ulong DelayMoment { get; private set; }
    public ulong DelayStart { get; set; }
    public WorldLocation DestTarget { get; set; }
    public SpellEffectInfo EffectInfo { get; set; }
    public byte? EmpoweredStage { get; set; }
    public bool FromClient { get; set; }

    public GameObject GameObjTarget { get; set; }

    // Damge   in effects count here
    public double HealingInEffects { get; set; }

    public bool IsChannelActive => Caster.IsUnit && Caster.AsUnit.ChannelSpellId != 0;
    public bool IsDeletable => !_referencedFromCurrentSpell && !_executedCurrently;
    public bool IsEmpowered => SpellInfo.EmpowerStages.Count > 0 && Caster.IsPlayer;
    public bool IsFocusDisabled => _triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing) || (SpellInfo.IsChanneled && !SpellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel));
    public bool IsIgnoringCooldowns => _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSpellAndCategoryCD);
    public bool IsInterruptable => !_executedCurrently;
    public bool IsPositive => SpellInfo.IsPositive && (TriggeredByAuraSpell == null || TriggeredByAuraSpell.IsPositive);
    public bool IsProcDisabled => _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents);
    public bool IsTriggered => _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.FullMask);
    public Item ItemTarget { get; set; }
    public Unit OriginalCaster { get; private set; }
    public ObjectGuid OriginalCasterGuid => _originalCasterGuid;
    public ObjectGuid OriginalCastId { get; set; }
    public List<SpellPowerCost> PowerCost { get; private set; } = new();
    public Spell SelfContainer { get; set; }
    public SpellInfo SpellInfo { get; set; }
    public SpellMisc SpellMisc;
    public SpellValue SpellValue { get; set; }
    public SpellState State { get; set; }
    public SpellMissInfo TargetMissInfo { get; set; }
    public SpellCastTargets Targets { get; set; }

    public bool TriggeredAllowProc => _triggeredCastFlags.HasFlag(TriggerCastFlags.TriggeredAllowProc);

    public Unit UnitCasterForEffectHandlers => OriginalCaster ?? Caster.AsUnit;

    // Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
    public Unit UnitTarget { get; set; }

    public double Variance { get; set; }
    internal WeaponAttackType AttackType { get; set; }
    internal DynObjAura DynObjAura { get; set; }
    internal ProcFlagsHit HitMask { get; set; }

    internal bool NeedComboPoints { get; set; }

    // ******************************************
    // Spell trigger system
    // ******************************************
    internal ProcFlagsInit ProcAttacker { get; set; }

    // Attacker trigger flags
    internal ProcFlagsInit ProcVictim { get; set; }

    // For weapon based attack
    // used in effects handlers
    internal UnitAura SpellAura { get; set; }

    //Spell data
    internal SpellSchoolMask SpellSchoolMask { get; set; }

    public static Spell ExtractSpellFromEvent(BasicEvent basicEvent)
    {
        var spellEvent = (SpellEvent)basicEvent;

        return spellEvent?.Spell;
    }

    public static void SendCastResult(Player caster, SpellInfo spellInfo, SpellCastVisual spellVisual, ObjectGuid castCount, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, int? param1 = null, int? param2 = null)
    {
        if (result == SpellCastResult.SpellCastOk)
            return;

        CastFailed packet = new()
        {
            Visual = spellVisual
        };

        FillSpellCastFailedArgs(packet, castCount, spellInfo, result, customError, param1, param2, caster);
        caster.SendPacket(packet);
    }

    public double CalculateSpellDamage(Unit target, SpellEffectInfo spellEffectInfo, double? basePoints = null, uint castItemId = 0, int itemLevel = -1)
    {
        return CalculateSpellDamage(out _, target, spellEffectInfo, basePoints, castItemId, itemLevel);
    }

    // function uses real base points (typically value - 1)
    public double CalculateSpellDamage(out double variance, Unit target, SpellEffectInfo spellEffectInfo, double? basePoints = null, uint castItemId = 0, int itemLevel = -1)
    {
        variance = 0.0f;

        return spellEffectInfo != null ? spellEffectInfo.CalcValue(out variance, Caster, basePoints, target, castItemId, itemLevel) : 0;
    }

    public void CallScriptAfterHitHandlers()
    {
        foreach (var script in GetSpellScripts<ISpellAfterHit>())
        {
            script._PrepareScriptCall(SpellScriptHookType.AfterHit);
            ((ISpellAfterHit)script).AfterHit();
            script._FinishScriptCall();
        }
    }

    public void CallScriptBeforeHitHandlers(SpellMissInfo missInfo)
    {
        foreach (var script in GetSpellScripts<ISpellBeforeHit>())
        {
            script._InitHit();
            script._PrepareScriptCall(SpellScriptHookType.BeforeHit);
            ((ISpellBeforeHit)script).BeforeHit(missInfo);
            script._FinishScriptCall();
        }
    }

    public void CallScriptCalcCritChanceHandlers(Unit victim, ref double critChance)
    {
        foreach (var loadedScript in GetSpellScripts<ISpellCalcCritChance>())
        {
            loadedScript._PrepareScriptCall(SpellScriptHookType.CalcCritChance);

            ((ISpellCalcCritChance)loadedScript).CalcCritChance(victim, ref critChance);

            loadedScript._FinishScriptCall();
        }
    }

    public void CallScriptOnHitHandlers()
    {
        foreach (var script in GetSpellScripts<ISpellOnHit>())
        {
            script._PrepareScriptCall(SpellScriptHookType.Hit);
            ((ISpellOnHit)script).OnHit();
            script._FinishScriptCall();
        }
    }

    public void CallScriptOnResistAbsorbCalculateHandlers(DamageInfo damageInfo, ref double resistAmount, ref double absorbAmount)
    {
        foreach (var script in GetSpellScripts<ISpellCalculateResistAbsorb>())
        {
            script._PrepareScriptCall(SpellScriptHookType.OnResistAbsorbCalculation);

            ((ISpellCalculateResistAbsorb)script).CalculateResistAbsorb(damageInfo, ref resistAmount, ref absorbAmount);

            script._FinishScriptCall();
        }
    }

    public bool CanAutoCast(Unit target)
    {
        if (target == null)
            return CheckPetCast(null) == SpellCastResult.SpellCastOk;

        var targetguid = target.GUID;

        // check if target already has the same or a more powerful aura
        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            if (!spellEffectInfo.IsAura)
                continue;

            var auraType = spellEffectInfo.ApplyAuraName;
            var auras = target.GetAuraEffectsByType(auraType);

            foreach (var eff in auras)
            {
                if (SpellInfo.Id == eff.SpellInfo.Id)
                    return false;

                switch (_spellManager.CheckSpellGroupStackRules(SpellInfo, eff.SpellInfo))
                {
                    case SpellGroupStackRule.Exclusive:
                        return false;

                    case SpellGroupStackRule.ExclusiveFromSameCaster:
                        if (Caster == eff.Caster)
                            return false;

                        break;

                    case SpellGroupStackRule.ExclusiveSameEffect: // this one has further checks, but i don't think they're necessary for autocast logic
                    case SpellGroupStackRule.ExclusiveHighest:
                        if (Math.Abs(spellEffectInfo.BasePoints) <= Math.Abs(eff.Amount))
                            return false;

                        break;
                }
            }
        }

        var result = CheckPetCast(target);

        if (result is not (SpellCastResult.SpellCastOk or SpellCastResult.UnitNotInfront))
            return false;

        // do not check targets for ground-targeted spells (we target them on top of the intended target anyway)
        if (SpellInfo.ExplicitTargetMask.HasFlag(SpellCastTargetFlags.DestLocation))
            return true;

        SelectSpellTargets();

        //check if among target units, our WANTED target is as well (.only self cast spells return false)
        // either the cast failed or the intended target wouldn't be hit
        return UniqueTargetInfo.Any(ihit => ihit.TargetGuid == targetguid);
    }

    public void Cancel()
    {
        if (State == SpellState.Finished)
            return;

        var oldState = State;
        State = SpellState.Finished;

        _isAutoRepeat = false;

        switch (oldState)
        {
            case SpellState.Preparing:
                CancelGlobalCooldown();
                goto case SpellState.Delayed;
            case SpellState.Delayed:
                SendInterrupted(0);
                SendCastResult(SpellCastResult.Interrupted);

                break;

            case SpellState.Casting:
                foreach (var ihit in UniqueTargetInfo)
                    if (ihit.MissCondition == SpellMissInfo.None)
                    {
                        var unit = Caster.GUID == ihit.TargetGuid ? Caster.AsUnit : Caster.ObjectAccessor.GetUnit(Caster, ihit.TargetGuid);

                        unit?.RemoveOwnedAura(SpellInfo.Id, _originalCasterGuid, AuraRemoveMode.Cancel);
                    }

                EndEmpoweredSpell();
                SendChannelUpdate(0);
                SendInterrupted(0);
                SendCastResult(SpellCastResult.Interrupted);

                AppliedMods.Clear();

                break;
        }

        SetReferencedFromCurrent(false);

        if (SelfContainer != null && SelfContainer == this)
            SelfContainer = null;

        // originalcaster handles gameobjects/dynobjects for gob caster
        if (OriginalCaster != null)
        {
            OriginalCaster.RemoveDynObject(SpellInfo.Id);

            if (SpellInfo.IsChanneled) // if not channeled then the object for the current cast wasn't summoned yet
                OriginalCaster.RemoveGameObject(SpellInfo.Id, true);
        }

        //set state back so finish will be processed
        State = oldState;

        Finish(SpellCastResult.Interrupted);
    }

    public bool CanExecuteTriggersOnHit(Unit unit, SpellInfo triggeredByAura = null)
    {
        var onlyOnTarget = triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ClassTriggerOnlyOnTarget);

        if (!onlyOnTarget)
            return true;

        // If triggeredByAura has SPELL_ATTR4_CLASS_TRIGGER_ONLY_ON_TARGET then it can only proc on either noncaster units...
        if (unit != Caster)
            return true;

        // ... or caster if it is the only target
        return UniqueTargetInfo.Count == 1;
    }

    public void Cast(bool skipCheck = false)
    {
        var modOwner = Caster.SpellModOwner;
        Spell lastSpellMod = null;

        if (modOwner != null)
        {
            lastSpellMod = modOwner.SpellModTakingSpell;

            if (lastSpellMod != null)
                modOwner.SetSpellModTakingSpell(lastSpellMod, false);
        }

        _cast(skipCheck);

        if (lastSpellMod != null)
            modOwner.SetSpellModTakingSpell(lastSpellMod, true);
    }

    public SpellCastResult CheckCast(bool strict)
    {
        int param1 = 0, param2 = 0;

        return CheckCast(strict, ref param1, ref param2);
    }

    public SpellCastResult CheckCast(bool strict, ref int param1, ref int param2)
    {
        SpellCastResult castResult;

        // check death state
        if (Caster.AsUnit is { IsAlive: false } && !SpellInfo.IsPassive && !(SpellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead) || (IsTriggered && TriggeredByAuraSpell == null)))
            return SpellCastResult.CasterDead;

        // Prevent cheating in case the player has an immunity effect and tries to interact with a non-allowed gameobject. The error message is handled by the client so we don't report anything here
        if (Caster.IsPlayer && Targets.GOTarget != null)
            if (Targets.GOTarget.Template.GetNoDamageImmune() != 0 && Caster.AsUnit?.HasUnitFlag(UnitFlags.Immune) == true)
                return SpellCastResult.DontReport;

        // check cooldowns to prevent cheating
        if (!SpellInfo.IsPassive)
        {
            var playerCaster = Caster.AsPlayer;

            if (playerCaster != null)
            {
                //can cast triggered (by aura only?) spells while have this Id
                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAurastate))
                {
                    // These two auras check SpellFamilyName defined by db2 class data instead of current spell SpellFamilyName
                    if (playerCaster.HasAuraType(AuraType.DisableCastingExceptAbilities) &&
                        !SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) &&
                        !SpellInfo.HasEffect(SpellEffectName.Attack) &&
                        !SpellInfo.HasAttribute(SpellAttr12.IgnoreCastingDisabled) &&
                        !playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableCastingExceptAbilities, _cliDb.ChrClassesStorage.LookupByKey(playerCaster.Class).SpellClassSet, SpellInfo.SpellFamilyFlags))
                        return SpellCastResult.CantDoThatRightNow;

                    if (playerCaster.HasAuraType(AuraType.DisableAttackingExceptAbilities))
                        if (!playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableAttackingExceptAbilities, _cliDb.ChrClassesStorage.LookupByKey(playerCaster.Class).SpellClassSet, SpellInfo.SpellFamilyFlags))
                            if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || SpellInfo.IsNextMeleeSwingSpell || SpellInfo.HasAttribute(SpellAttr1.InitiatesCombatEnablesAutoAttack) || SpellInfo.HasAttribute(SpellAttr2.InitiateCombatPostCastEnablesAutoAttack) || SpellInfo.HasEffect(SpellEffectName.Attack) || SpellInfo.HasEffect(SpellEffectName.NormalizedWeaponDmg) || SpellInfo.HasEffect(SpellEffectName.WeaponDamageNoSchool) || SpellInfo.HasEffect(SpellEffectName.WeaponPercentDamage) || SpellInfo.HasEffect(SpellEffectName.WeaponDamage))
                                return SpellCastResult.CantDoThatRightNow;
                }

                // check if we are using a potion in combat for the 2nd+ time. Cooldown is added only after caster gets out of combat
                if (!IsIgnoringCooldowns && playerCaster.GetLastPotionId() != 0 && CastItem != null && (CastItem.IsPotion || SpellInfo.IsCooldownStartedOnEvent))
                    return SpellCastResult.NotReady;
            }

            if (!IsIgnoringCooldowns && Caster.AsUnit != null)
            {
                if (!Caster.AsUnit.SpellHistory.IsReady(SpellInfo, CastItemEntry))
                    return TriggeredByAuraSpell != null ? SpellCastResult.DontReport : SpellCastResult.NotReady;

                if ((_isAutoRepeat || SpellInfo.CategoryId == 76) && !Caster.AsUnit.IsAttackReady(WeaponAttackType.RangedAttack))
                    return SpellCastResult.DontReport;
            }
        }

        if (Caster.AsUnit != null && SpellInfo.HasAttribute(SpellAttr7.IsCheatSpell) && Caster.IsUnit && !Caster.AsUnit.HasUnitFlag2(UnitFlags2.AllowCheatSpells))
        {
            CustomErrors = SpellCustomErrors.GmOnly;

            return SpellCastResult.CustomError;
        }

        // Check global cooldown
        if (strict && !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreGCD) && HasGlobalCooldown())
            return !SpellInfo.HasAttribute(SpellAttr0.CooldownOnEvent) ? SpellCastResult.NotReady : SpellCastResult.DontReport;

        // only triggered spells can be processed an ended Battleground
        if (!IsTriggered && Caster.IsTypeId(TypeId.Player) && Caster.AsPlayer.Battleground is { Status: BattlegroundStatus.WaitLeave })
            return SpellCastResult.DontReport;

        if (Caster.IsTypeId(TypeId.Player) && _vMapManager.IsLineOfSightCalcEnabled)
        {
            if (SpellInfo.HasAttribute(SpellAttr0.OnlyOutdoors) && !Caster.Location.IsOutdoors)
                return SpellCastResult.OnlyOutdoors;

            if (SpellInfo.HasAttribute(SpellAttr0.OnlyIndoors) && Caster.Location.IsOutdoors)
                return SpellCastResult.OnlyIndoors;
        }

        var unitCaster = Caster.AsUnit;

        if (unitCaster != null)
        {
            if (SpellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed) && unitCaster.IsCharmed)
                return SpellCastResult.Charmed;

            // only check at first call, Stealth auras are already removed at second call
            // for now, ignore triggered spells
            if (strict && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreShapeshift))
            {
                // Ignore form req aura
                var ignore = unitCaster.GetAuraEffectsByType(AuraType.ModIgnoreShapeshift);

                var checkForm = ignore.All(aurEff => !aurEff.IsAffectingSpell(SpellInfo));

                if (checkForm)
                {
                    // Cannot be used in this stance/form
                    var shapeError = SpellInfo.CheckShapeshift(unitCaster.ShapeshiftForm);

                    if (shapeError != SpellCastResult.SpellCastOk)
                        return shapeError;

                    if (SpellInfo.HasAttribute(SpellAttr0.OnlyStealthed) && !unitCaster.HasStealthAura)
                        return SpellCastResult.OnlyStealthed;
                }
            }

            var reqCombat = true;
            var stateAuras = unitCaster.GetAuraEffectsByType(AuraType.AbilityIgnoreAurastate);

            foreach (var aura in stateAuras.Where(aura => aura.IsAffectingSpell(SpellInfo)))
            {
                NeedComboPoints = false;

                if (aura.MiscValue != 1)
                    continue;

                reqCombat = false;

                break;
            }

            // caster state requirements
            // not for triggered spells (needed by execute)
            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterAurastate))
            {
                if (SpellInfo.CasterAuraState != 0 && !unitCaster.HasAuraState(SpellInfo.CasterAuraState, SpellInfo, unitCaster))
                    return SpellCastResult.CasterAurastate;

                if (SpellInfo.ExcludeCasterAuraState != 0 && unitCaster.HasAuraState(SpellInfo.ExcludeCasterAuraState, SpellInfo, unitCaster))
                    return SpellCastResult.CasterAurastate;

                // Note: spell 62473 requres casterAuraSpell = triggering spell
                if (SpellInfo.CasterAuraSpell != 0 && !unitCaster.HasAura(SpellInfo.CasterAuraSpell))
                    return SpellCastResult.CasterAurastate;

                if (SpellInfo.ExcludeCasterAuraSpell != 0 && unitCaster.HasAura(SpellInfo.ExcludeCasterAuraSpell))
                    return SpellCastResult.CasterAurastate;

                if (SpellInfo.CasterAuraType != 0 && !unitCaster.HasAuraType(SpellInfo.CasterAuraType))
                    return SpellCastResult.CasterAurastate;

                if (SpellInfo.ExcludeCasterAuraType != 0 && unitCaster.HasAuraType(SpellInfo.ExcludeCasterAuraType))
                    return SpellCastResult.CasterAurastate;

                if (reqCombat && unitCaster.IsInCombat && !SpellInfo.CanBeUsedInCombat)
                    return SpellCastResult.AffectingCombat;
            }

            // Check vehicle flags
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
            {
                var vehicleCheck = SpellInfo.CheckVehicle(unitCaster);

                if (vehicleCheck != SpellCastResult.SpellCastOk)
                    return vehicleCheck;
            }
        }

        // check spell cast conditions from database
        {
            ConditionSourceInfo condInfo = new(Caster, Targets.ObjectTarget);

            if (!Caster.ConditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Spell, SpellInfo.Id, condInfo))
            {
                // mLastFailedCondition can be NULL if there was an error processing the condition in Condition.Meets (i.e. wrong data for ConditionTarget or others)
                if (condInfo.LastFailedCondition != null && condInfo.LastFailedCondition.ErrorType != 0)
                {
                    if (condInfo.LastFailedCondition.ErrorType == (uint)SpellCastResult.CustomError)
                        CustomErrors = (SpellCustomErrors)condInfo.LastFailedCondition.ErrorTextId;

                    return (SpellCastResult)condInfo.LastFailedCondition.ErrorType;
                }

                if (condInfo.LastFailedCondition == null || condInfo.LastFailedCondition.ConditionTarget == 0)
                    return SpellCastResult.CasterAurastate;

                return SpellCastResult.BadTargets;
            }
        }

        // Don't check explicit target for passive spells (workaround) (check should be skipped only for learn case)
        // those spells may have incorrect target entries or not filled at all (for example 15332)
        // such spells when learned are not targeting anyone using targeting system, they should apply directly to caster instead
        // also, such casts shouldn't be sent to client
        if (!(SpellInfo.IsPassive && (Targets.UnitTarget == null || Targets.UnitTarget == Caster)))
        {
            // Check explicit target for m_originalCaster - todo: get rid of such workarounds
            var caster = Caster;

            // in case of gameobjects like traps, we need the gameobject itself to check target validity
            // otherwise, if originalCaster is far away and cannot detect the target, the trap would not hit the target
            if (OriginalCaster != null && !caster.IsGameObject)
                caster = OriginalCaster;

            castResult = SpellInfo.CheckExplicitTarget(caster, Targets.ObjectTarget, Targets.ItemTarget);

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;
        }

        var unitTarget = Targets.UnitTarget;

        if (unitTarget != null)
        {
            castResult = SpellInfo.CheckTarget(Caster, unitTarget, Caster.IsGameObject); // skip stealth checks for GO casts

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;

            // If it's not a melee spell, check if vision is obscured by SPELL_AURA_INTERFERE_TARGETTING
            if (SpellInfo.DmgClass != SpellDmgClass.Melee)
            {
                var unitCaster1 = Caster.AsUnit;

                if (unitCaster1 != null)
                {
                    if (unitCaster1.GetAuraEffectsByType(AuraType.InterfereTargetting).Any(auraEffect => !unitCaster1.WorldObjectCombat.IsFriendlyTo(auraEffect.Caster) && !unitTarget.HasAura(auraEffect.Id, auraEffect.CasterGuid)))
                        return SpellCastResult.VisionObscured;

                    if (unitTarget.GetAuraEffectsByType(AuraType.InterfereTargetting).Any(auraEffect => !unitCaster1.WorldObjectCombat.IsFriendlyTo(auraEffect.Caster) && (!unitTarget.HasAura(auraEffect.Id, auraEffect.CasterGuid) || !unitCaster1.HasAura(auraEffect.Id, auraEffect.CasterGuid))))
                        return SpellCastResult.VisionObscured;
                }
            }

            if (unitTarget != Caster)
            {
                // Must be behind the target
                if (SpellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget) && unitTarget.Location.HasInArc(MathFunctions.PI, Caster.Location))
                    return SpellCastResult.NotBehind;

                // Target must be facing you
                if (SpellInfo.HasAttribute(SpellCustomAttributes.ReqTargetFacingCaster) && !unitTarget.Location.HasInArc(MathFunctions.PI, Caster.Location))
                    return SpellCastResult.NotInfront;

                // Ignore LOS for gameobjects casts
                if (!Caster.IsGameObject)
                {
                    var losTarget = Caster;

                    if (IsTriggered && TriggeredByAuraSpell != null)
                    {
                        var dynObj = Caster.AsUnit.GetDynObject(TriggeredByAuraSpell.Id);

                        if (dynObj != null)
                            losTarget = dynObj;
                    }

                    if (!SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) && !Caster.DisableManager.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS) && !unitTarget.Location.IsWithinLOSInMap(losTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        return SpellCastResult.LineOfSight;
                }
            }
        }

        // Check for line of sight for spells with dest
        if (Targets.HasDst)
            if (!SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) && !Caster.DisableManager.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS) && !Caster.Location.IsWithinLOS(Targets.DstPos, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                return SpellCastResult.LineOfSight;

        // check pet presence
        if (unitCaster != null)
        {
            if (SpellInfo.HasAttribute(SpellAttr2.NoActivePets))
                if (!unitCaster.PetGUID.IsEmpty)
                    return SpellCastResult.AlreadyHavePet;

            foreach (var spellEffectInfo in SpellInfo.Effects)
                if (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.UnitPet)
                {
                    if (unitCaster.GetGuardianPet() == null)
                        return TriggeredByAuraSpell != null
                                   ? // not report pet not existence for triggered spells
                                   SpellCastResult.DontReport
                                   : SpellCastResult.NoPet;

                    break;
                }
        }

        // Spell casted only on Battleground
        if (SpellInfo.HasAttribute(SpellAttr3.OnlyBattlegrounds))
            if (!Caster.Location.Map.IsBattleground)
                return SpellCastResult.OnlyBattlegrounds;

        // do not allow spells to be cast in arenas or rated Battlegrounds
        var player = Caster.AsPlayer;

        if (player is { InArena: true })
        /* || player.InRatedBattleground() NYI*/
        {
            castResult = CheckArenaAndRatedBattlegroundCastRules();

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;
        }

        // zone check
        if (!Caster.IsPlayer || !Caster.AsPlayer.IsGameMaster)
        {
            var locRes = SpellInfo.CheckLocation(Caster.Location.MapId, Caster.Location.Zone, Caster.Location.Area, Caster.AsPlayer);

            if (locRes != SpellCastResult.SpellCastOk)
                return locRes;
        }

        // not let players cast spells at mount (and let do it to creatures)
        if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
            if (Caster.IsPlayer && Caster.AsPlayer.IsMounted && !SpellInfo.IsPassive && !SpellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
            {
                if (Caster.AsPlayer.IsInFlight)
                    return SpellCastResult.NotOnTaxi;

                return SpellCastResult.NotMounted;
            }

        // check spell focus object
        if (SpellInfo.RequiresSpellFocus != 0)
            if (!Caster.IsUnit || !Caster.AsUnit.HasAuraTypeWithMiscvalue(AuraType.ProvideSpellFocus, (int)SpellInfo.RequiresSpellFocus))
            {
                _focusObject = SearchSpellFocus();

                if (_focusObject == null)
                    return SpellCastResult.RequiresSpellFocus;
            }

        // always (except passive spells) check items (focus object can be required for any type casts)
        if (!SpellInfo.IsPassive)
        {
            castResult = CheckItems(ref param1, ref param2);

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;
        }

        // Triggered spells also have range check
        // @todo determine if there is some Id to enable/disable the check
        castResult = CheckRange(strict);

        if (castResult != SpellCastResult.SpellCastOk)
            return castResult;

        if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
        {
            castResult = CheckPower();

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;
        }

        if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterAuras))
        {
            castResult = CheckCasterAuras(ref param1);

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;
        }

        // script hook
        castResult = CallScriptCheckCastHandlers();

        if (castResult != SpellCastResult.SpellCastOk)
            return castResult;

        var approximateAuraEffectMask = new HashSet<int>();
        uint nonAuraEffectMask = 0;

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            // for effects of spells that have only one target
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.Dummy:
                {
                    if (SpellInfo.Id == 19938) // Awaken Peon
                    {
                        var unit = Targets.UnitTarget;

                        if (unit == null || !unit.HasAura(17743))
                            return SpellCastResult.BadTargets;
                    }
                    else if (SpellInfo.Id == 31789) // Righteous Defense
                    {
                        if (!Caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.DontReport;

                        var target = Targets.UnitTarget;

                        if (target == null || !target.WorldObjectCombat.IsFriendlyTo(Caster) || target.Attackers.Empty())
                            return SpellCastResult.BadTargets;
                    }

                    break;
                }
                case SpellEffectName.LearnSpell:
                {
                    if (spellEffectInfo.TargetA.Target != Framework.Constants.Targets.UnitPet)
                        break;

                    var pet = Caster.AsPlayer.CurrentPet;

                    if (pet == null)
                        return SpellCastResult.NoPet;

                    var learnSpellproto = _spellManager.GetSpellInfo(spellEffectInfo.TriggerSpell);

                    if (learnSpellproto == null)
                        return SpellCastResult.NotKnown;

                    if (SpellInfo.SpellLevel > pet.Level)
                        return SpellCastResult.Lowlevel;

                    break;
                }
                case SpellEffectName.UnlockGuildVaultTab:
                {
                    if (!Caster.IsTypeId(TypeId.Player))
                        return SpellCastResult.BadTargets;

                    var guild = Caster.AsPlayer.Guild;

                    if (guild != null)
                        if (guild.GetLeaderGUID() != Caster.AsPlayer.GUID)
                            return SpellCastResult.CantDoThatRightNow;

                    break;
                }
                case SpellEffectName.LearnPetSpell:
                {
                    // check target only for unit target case
                    var target = Targets.UnitTarget;

                    if (target != null)
                    {
                        if (!Caster.IsTypeId(TypeId.Player))
                            return SpellCastResult.BadTargets;

                        var pet = target.AsPet;

                        if (pet == null || pet.OwningPlayer != Caster)
                            return SpellCastResult.BadTargets;

                        var learnSpellproto = _spellManager.GetSpellInfo(spellEffectInfo.TriggerSpell);

                        if (learnSpellproto == null)
                            return SpellCastResult.NotKnown;

                        if (SpellInfo.SpellLevel > pet.Level)
                            return SpellCastResult.Lowlevel;
                    }

                    break;
                }
                case SpellEffectName.ApplyGlyph:
                {
                    if (!Caster.IsTypeId(TypeId.Player))
                        return SpellCastResult.GlyphNoSpec;

                    var caster = Caster.AsPlayer;

                    if (!caster.HasSpell(SpellMisc.SpellId))
                        return SpellCastResult.NotKnown;

                    var glyphId = (uint)spellEffectInfo.MiscValue;

                    if (glyphId != 0)
                    {
                        if (!_cliDb.GlyphPropertiesStorage.TryGetValue(glyphId, out var glyphProperties))
                            return SpellCastResult.InvalidGlyph;

                        var glyphBindableSpells = _db2Manager.GetGlyphBindableSpells(glyphId);

                        if (glyphBindableSpells.Empty())
                            return SpellCastResult.InvalidGlyph;

                        if (!glyphBindableSpells.Contains(SpellMisc.SpellId))
                            return SpellCastResult.InvalidGlyph;

                        var glyphRequiredSpecs = _db2Manager.GetGlyphRequiredSpecs(glyphId);

                        if (!glyphRequiredSpecs.Empty())
                        {
                            if (caster.GetPrimarySpecialization() == 0)
                                return SpellCastResult.GlyphNoSpec;

                            if (!glyphRequiredSpecs.Contains(caster.GetPrimarySpecialization()))
                                return SpellCastResult.GlyphInvalidSpec;
                        }

                        uint replacedGlyph = 0;

                        foreach (var activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
                        {
                            var activeGlyphBindableSpells = _db2Manager.GetGlyphBindableSpells(activeGlyphId);

                            if (activeGlyphBindableSpells.Empty())
                                continue;

                            if (!activeGlyphBindableSpells.Contains(SpellMisc.SpellId))
                                continue;

                            replacedGlyph = activeGlyphId;

                            break;
                        }

                        foreach (var activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
                        {
                            if (activeGlyphId == replacedGlyph)
                                continue;

                            if (activeGlyphId == glyphId)
                                return SpellCastResult.UniqueGlyph;

                            if (_cliDb.GlyphPropertiesStorage.LookupByKey(activeGlyphId).GlyphExclusiveCategoryID == glyphProperties.GlyphExclusiveCategoryID)
                                return SpellCastResult.GlyphExclusiveCategory;
                        }
                    }

                    break;
                }
                case SpellEffectName.FeedPet:
                {
                    if (!Caster.IsTypeId(TypeId.Player))
                        return SpellCastResult.BadTargets;

                    var foodItem = Targets.ItemTarget;

                    if (foodItem == null)
                        return SpellCastResult.BadTargets;

                    var pet = Caster.AsPlayer.CurrentPet;

                    if (pet == null)
                        return SpellCastResult.NoPet;

                    if (!pet.HaveInDiet(foodItem.Template))
                        return SpellCastResult.WrongPetFood;

                    if (foodItem.Template.BaseItemLevel + 30 <= pet.Level)
                        return SpellCastResult.FoodLowlevel;

                    if (Caster.AsPlayer.IsInCombat || pet.IsInCombat)
                        return SpellCastResult.AffectingCombat;

                    break;
                }
                case SpellEffectName.Charge:
                {
                    if (unitCaster == null)
                        return SpellCastResult.BadTargets;

                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAuras) && unitCaster.HasUnitState(UnitState.Root))
                        return SpellCastResult.Rooted;

                    if (SpellInfo.NeedsExplicitUnitTarget)
                    {
                        var target = Targets.UnitTarget;

                        if (target == null)
                            return SpellCastResult.DontReport;

                        // first we must check to see if the target is in LoS. A path can usually be built but LoS matters for charge spells
                        if (!target.Location.IsWithinLOSInMap(unitCaster)) //Do full LoS/Path check. Don't exclude m2
                            return SpellCastResult.LineOfSight;

                        var objSize = target.CombatReach;
                        var range = SpellInfo.GetMaxRange(true, unitCaster, this) * 1.5f + objSize; // can't be overly strict

                        _preGeneratedPath = new PathGenerator(unitCaster);
                        _preGeneratedPath.SetPathLengthLimit(range);

                        // first try with raycast, if it fails fall back to normal path
                        var result = _preGeneratedPath.CalculatePath(target.Location);

                        if (_preGeneratedPath.PathType.HasAnyFlag(PathType.Short))
                            return SpellCastResult.NoPath;

                        if (!result || _preGeneratedPath.PathType.HasAnyFlag(PathType.NoPath | PathType.Incomplete))
                            return SpellCastResult.NoPath;

                        if (_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if not in a straight line
                            return SpellCastResult.NoPath;

                        _preGeneratedPath.ShortenPathUntilDist(target.Location, objSize); //move back
                    }

                    break;
                }
                case SpellEffectName.Skinning:
                {
                    if (!Caster.IsTypeId(TypeId.Player) || Targets.UnitTarget == null || !Targets.UnitTarget.IsTypeId(TypeId.Unit))
                        return SpellCastResult.BadTargets;

                    if (!Targets.UnitTarget.HasUnitFlag(UnitFlags.Skinnable))
                        return SpellCastResult.TargetUnskinnable;

                    var creature = Targets.UnitTarget.AsCreature;
                    var loot = creature.GetLootForPlayer(Caster.AsPlayer);

                    if (loot != null && (!loot.IsLooted() || loot.LootType == LootType.Skinning))
                        return SpellCastResult.TargetNotLooted;

                    var skill = creature.Template.GetRequiredLootSkill();

                    var skillValue = Caster.AsPlayer.GetSkillValue(skill);
                    var targetLevel = Targets.UnitTarget.GetLevelForTarget(Caster);
                    var reqValue = (int)(skillValue < 100 ? (targetLevel - 10) * 10 : targetLevel * 5);

                    if (reqValue > skillValue)
                        return SpellCastResult.LowCastlevel;

                    break;
                }
                case SpellEffectName.OpenLock:
                {
                    if (spellEffectInfo.TargetA.Target != Framework.Constants.Targets.GameobjectTarget &&
                        spellEffectInfo.TargetA.Target != Framework.Constants.Targets.GameobjectItemTarget)
                        break;

                    if (!Caster.IsTypeId(TypeId.Player) // only players can open locks, gather etc.
                                                        // we need a go target in case of TARGET_GAMEOBJECT_TARGET
                        ||
                        (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.GameobjectTarget && Targets.GOTarget == null))
                        return SpellCastResult.BadTargets;

                    Item pTempItem = null;

                    if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.TradeItem))
                    {
                        var pTrade = Caster.AsPlayer.TradeData;

                        if (pTrade != null)
                            pTempItem = pTrade.TraderData.GetItem(TradeSlots.NonTraded);
                    }
                    else if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.Item))
                        pTempItem = Caster.AsPlayer.GetItemByGuid(Targets.ItemTargetGuid);

                    // we need a go target, or an openable item target in case of TARGET_GAMEOBJECT_ITEM_TARGET
                    if (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.GameobjectItemTarget &&
                        Targets.GOTarget == null &&
                        (pTempItem == null || pTempItem.Template.LockID == 0 || !pTempItem.IsLocked))
                        return SpellCastResult.BadTargets;

                    if (SpellInfo.Id != 1842 ||
                        (Targets.GOTarget != null &&
                         Targets.GOTarget.Template.type != GameObjectTypes.Trap))
                        if (Caster.AsPlayer.InBattleground && // In Battlegroundplayers can use only flags and banners
                            !Caster.AsPlayer.CanUseBattlegroundObject(Targets.GOTarget))
                            return SpellCastResult.TryAgain;

                    // get the lock entry
                    uint lockId = 0;
                    var go = Targets.GOTarget;
                    var itm = Targets.ItemTarget;

                    if (go != null)
                    {
                        lockId = go.Template.GetLockId();

                        if (lockId == 0)
                            return SpellCastResult.BadTargets;

                        if (go.Template.GetNotInCombat() != 0 && Caster.AsUnit.IsInCombat)
                            return SpellCastResult.AffectingCombat;
                    }
                    else if (itm != null)
                        lockId = itm.Template.LockID;

                    var skillId = SkillType.None;
                    var reqSkillValue = 0;
                    var skillValue = 0;

                    // check lock compatibility
                    var res = CanOpenLock(spellEffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);

                    if (res != SpellCastResult.SpellCastOk)
                        return res;

                    break;
                }
                case SpellEffectName.ResurrectPet:
                {
                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster?.PetStable == null)
                        return SpellCastResult.BadTargets;

                    var pet = playerCaster.CurrentPet;

                    if (pet is { IsAlive: true })
                        return SpellCastResult.AlreadyHaveSummon;

                    var petStable = playerCaster.PetStable;
                    var deadPetInfo = petStable.ActivePets.FirstOrDefault(petInfo => petInfo?.Health == 0);

                    if (deadPetInfo == null)
                        return SpellCastResult.BadTargets;

                    break;
                }
                // This is generic summon effect
                case SpellEffectName.Summon:
                {
                    if (unitCaster == null)
                        break;

                    if (!_cliDb.SummonPropertiesStorage.TryGetValue((uint)spellEffectInfo.MiscValueB, out var summonProperties))
                        break;

                    switch (summonProperties.Control)
                    {
                        case SummonCategory.Pet:
                            if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !unitCaster.PetGUID.IsEmpty)
                                return SpellCastResult.AlreadyHaveSummon;

                            goto case SummonCategory.Puppet;
                        case SummonCategory.Puppet:
                            if (!unitCaster.CharmedGUID.IsEmpty)
                                return SpellCastResult.AlreadyHaveCharm;

                            break;
                    }

                    break;
                }
                case SpellEffectName.CreateTamedPet:
                {
                    if (Targets.UnitTarget != null)
                    {
                        if (!Targets.UnitTarget.IsTypeId(TypeId.Player))
                            return SpellCastResult.BadTargets;

                        if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !Targets.UnitTarget.PetGUID.IsEmpty)
                            return SpellCastResult.AlreadyHaveSummon;
                    }

                    break;
                }
                case SpellEffectName.SummonPet:
                {
                    if (unitCaster == null)
                        return SpellCastResult.BadTargets;

                    if (!unitCaster.PetGUID.IsEmpty) //let warlock do a replacement summon
                    {
                        if (unitCaster.IsTypeId(TypeId.Player))
                        {
                            if (strict) //starting cast, trigger pet stun (cast by pet so it doesn't attack player)
                            {
                                var pet = unitCaster.AsPlayer.CurrentPet;

                                if (pet != null)
                                    pet.SpellFactory.CastSpell(pet,
                                                               32752,
                                                               new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                                   .SetOriginalCaster(pet.GUID)
                                                                   .SetTriggeringSpell(this));
                            }
                        }
                        else if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
                            return SpellCastResult.AlreadyHaveSummon;
                    }

                    if (!unitCaster.CharmedGUID.IsEmpty)
                        return SpellCastResult.AlreadyHaveCharm;

                    var playerCaster = unitCaster.AsPlayer;

                    if (playerCaster is { PetStable: { } })
                    {
                        PetSaveMode? petSlot = null;

                        if (spellEffectInfo.MiscValue == 0)
                        {
                            petSlot = (PetSaveMode)spellEffectInfo.CalcValue();

                            // No pet can be summoned if any pet is dead
                            if (playerCaster.PetStable.ActivePets.Any(activePet => activePet?.Health == 0))
                            {
                                playerCaster.SendTameFailure(PetTameResult.Dead);

                                return SpellCastResult.DontReport;
                            }
                        }

                        var info = Pet.GetLoadPetInfo(playerCaster.PetStable, (uint)spellEffectInfo.MiscValue, 0, petSlot);

                        if (info.Item1 != null)
                        {
                            if (info.Item1.Type == PetType.Hunter)
                            {
                                var creatureInfo = _gameObjectManager.GetCreatureTemplate(info.Item1.CreatureId);

                                if (creatureInfo == null || !creatureInfo.IsTameable(playerCaster.CanTameExoticPets))
                                {
                                    // if problem in exotic pet
                                    if (creatureInfo != null && creatureInfo.IsTameable(true))
                                        playerCaster.SendTameFailure(PetTameResult.CantControlExotic);
                                    else
                                        playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

                                    return SpellCastResult.DontReport;
                                }
                            }
                        }
                        else if (spellEffectInfo.MiscValue == 0) // when miscvalue is present it is allowed to create new pets
                        {
                            playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

                            return SpellCastResult.DontReport;
                        }
                    }

                    break;
                }
                case SpellEffectName.DismissPet:
                {
                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster == null)
                        return SpellCastResult.BadTargets;

                    var pet = playerCaster.CurrentPet;

                    if (pet == null)
                        return SpellCastResult.NoPet;

                    if (!pet.IsAlive)
                        return SpellCastResult.TargetsDead;

                    break;
                }
                case SpellEffectName.SummonPlayer:
                {
                    if (!Caster.IsTypeId(TypeId.Player))
                        return SpellCastResult.BadTargets;

                    if (Caster.AsPlayer.Target.IsEmpty)
                        return SpellCastResult.BadTargets;

                    var target = Caster.ObjectAccessor.FindPlayer(Caster.AsPlayer.Target);

                    if (target == null || Caster.AsPlayer == target || (!target.IsInSameRaidWith(Caster.AsPlayer) && SpellInfo.Id != 48955)) // refer-a-friend spell
                        return SpellCastResult.BadTargets;

                    if (target.HasSummonPending)
                        return SpellCastResult.SummonPending;

                    // check if our map is dungeon
                    var map = Caster.Location.Map.ToInstanceMap;

                    if (map != null)
                    {
                        var mapId = map.Id;
                        var difficulty = map.DifficultyID;
                        var mapLock = map.InstanceLock;

                        if (mapLock != null)
                            if (_instanceLockManager.CanJoinInstanceLock(target.GUID, new MapDb2Entries(mapId, difficulty, _cliDb, _db2Manager), mapLock) != TransferAbortReason.None)
                                return SpellCastResult.TargetLockedToRaidInstance;

                        if (!target.Satisfy(_gameObjectManager.GetAccessRequirement(mapId, difficulty), mapId))
                            return SpellCastResult.BadTargets;
                    }

                    break;
                }
                // RETURN HERE
                case SpellEffectName.SummonRafFriend:
                {
                    if (!Caster.IsTypeId(TypeId.Player))
                        return SpellCastResult.BadTargets;

                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster.Target.IsEmpty)
                        return SpellCastResult.BadTargets;

                    var target = Caster.ObjectAccessor.FindPlayer(playerCaster.Target);

                    if (target == null ||
                        !(target.Session.RecruiterId == playerCaster.Session.AccountId || target.Session.AccountId == playerCaster.Session.RecruiterId))
                        return SpellCastResult.BadTargets;

                    break;
                }
                case SpellEffectName.Leap:
                case SpellEffectName.TeleportUnitsFaceCaster:
                {
                    //Do not allow to cast it before BG starts.
                    if (Caster.IsTypeId(TypeId.Player))
                    {
                        var bg = Caster.AsPlayer.Battleground;

                        if (bg != null)
                            if (bg.Status != BattlegroundStatus.InProgress)
                                return SpellCastResult.TryAgain;
                    }

                    break;
                }
                case SpellEffectName.StealBeneficialBuff:
                {
                    if (Targets.UnitTarget == null || Targets.UnitTarget == Caster)
                        return SpellCastResult.BadTargets;

                    break;
                }
                case SpellEffectName.LeapBack:
                {
                    if (unitCaster == null)
                        return SpellCastResult.BadTargets;

                    if (unitCaster.HasUnitState(UnitState.Root))
                        return unitCaster.IsTypeId(TypeId.Player) ? SpellCastResult.Rooted : SpellCastResult.DontReport;

                    break;
                }
                case SpellEffectName.Jump:
                case SpellEffectName.JumpDest:
                {
                    if (unitCaster == null)
                        return SpellCastResult.BadTargets;

                    if (unitCaster.HasUnitState(UnitState.Root))
                        return SpellCastResult.Rooted;

                    break;
                }
                case SpellEffectName.TalentSpecSelect:
                {
                    var spec = _cliDb.ChrSpecializationStorage.LookupByKey(SpellMisc.SpecializationId);
                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster == null)
                        return SpellCastResult.TargetNotPlayer;

                    if (spec == null || (spec.ClassID != (uint)player.Class && !spec.IsPetSpecialization()))
                        return SpellCastResult.NoSpec;

                    if (spec.IsPetSpecialization())
                    {
                        var pet = player.CurrentPet;

                        if (pet == null || pet.PetType != PetType.Hunter || pet.GetCharmInfo() == null)
                            return SpellCastResult.NoPet;
                    }

                    // can't change during already started arena/Battleground
                    var bg = player.Battleground;

                    if (bg is { Status: BattlegroundStatus.InProgress })
                        return SpellCastResult.NotInBattleground;

                    break;
                }
                case SpellEffectName.RemoveTalent:
                {
                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster == null)
                        return SpellCastResult.BadTargets;

                    if (!_cliDb.TalentStorage.TryGetValue(SpellMisc.TalentId, out var talent))
                        return SpellCastResult.DontReport;

                    if (playerCaster.SpellHistory.HasCooldown(talent.SpellID))
                    {
                        param1 = (int)talent.SpellID;

                        return SpellCastResult.CantUntalent;
                    }

                    break;
                }
                case SpellEffectName.GiveArtifactPower:
                case SpellEffectName.GiveArtifactPowerNoBonus:
                {
                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster == null)
                        return SpellCastResult.BadTargets;

                    var artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

                    if (artifactAura == null)
                        return SpellCastResult.NoArtifactEquipped;

                    var artifact = playerCaster.AsPlayer.GetItemByGuid(artifactAura.CastItemGuid);

                    if (artifact == null)
                        return SpellCastResult.NoArtifactEquipped;

                    if (spellEffectInfo.Effect == SpellEffectName.GiveArtifactPower)
                    {
                        var artifactEntry = _cliDb.ArtifactStorage.LookupByKey(artifact.Template.ArtifactID);

                        if (artifactEntry == null || artifactEntry.ArtifactCategoryID != spellEffectInfo.MiscValue)
                            return SpellCastResult.WrongArtifactEquipped;
                    }

                    break;
                }
                case SpellEffectName.ChangeBattlepetQuality:
                case SpellEffectName.GrantBattlepetLevel:
                case SpellEffectName.GrantBattlepetExperience:
                {
                    var playerCaster = Caster.AsPlayer;

                    if (playerCaster == null || Targets.UnitTarget == null || !Targets.UnitTarget.IsCreature)
                        return SpellCastResult.BadTargets;

                    var battlePetMgr = playerCaster.Session.BattlePetMgr;

                    if (!battlePetMgr.HasJournalLock)
                        return SpellCastResult.CantDoThatRightNow;

                    var creature = Targets.UnitTarget.AsCreature;

                    if (creature != null)
                    {
                        if (playerCaster.SummonedBattlePetGUID.IsEmpty || creature.BattlePetCompanionGUID.IsEmpty)
                            return SpellCastResult.NoPet;

                        if (playerCaster.SummonedBattlePetGUID != creature.BattlePetCompanionGUID)
                            return SpellCastResult.BadTargets;

                        var battlePet = battlePetMgr.GetPet(creature.BattlePetCompanionGUID);

                        if (battlePet != null)
                            if (_cliDb.BattlePetSpeciesStorage.TryGetValue(battlePet.PacketInfo.Species, out var battlePetSpecies))
                            {
                                var battlePetType = (uint)spellEffectInfo.MiscValue;

                                if (battlePetType != 0)
                                    if ((battlePetType & (1 << battlePetSpecies.PetTypeEnum)) == 0)
                                        return SpellCastResult.WrongBattlePetType;

                                if (spellEffectInfo.Effect == SpellEffectName.ChangeBattlepetQuality)
                                {
                                    var qualityRecord = _cliDb.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < spellEffectInfo.BasePoints);

                                    var quality = BattlePetBreedQuality.Poor;

                                    if (qualityRecord != null)
                                        quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

                                    if (battlePet.PacketInfo.Quality >= (byte)quality)
                                        return SpellCastResult.CantUpgradeBattlePet;
                                }

                                if (spellEffectInfo.Effect is SpellEffectName.GrantBattlepetLevel or SpellEffectName.GrantBattlepetExperience)
                                    if (battlePet.PacketInfo.Level >= SharedConst.MaxBattlePetLevel)
                                        return SpellCastResult.GrantPetLevelFail;

                                if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
                                    return SpellCastResult.BadTargets;
                            }
                    }

                    break;
                }
            }

            if (spellEffectInfo.IsAura)
                approximateAuraEffectMask.Add(spellEffectInfo.EffectIndex);
            else if (spellEffectInfo.IsEffect)
                nonAuraEffectMask |= 1u << spellEffectInfo.EffectIndex;
        }

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            switch (spellEffectInfo.ApplyAuraName)
            {
                case AuraType.ModPossessPet:
                {
                    if (!Caster.IsTypeId(TypeId.Player))
                        return SpellCastResult.NoPet;

                    var pet = Caster.AsPlayer.CurrentPet;

                    if (pet == null)
                        return SpellCastResult.NoPet;

                    if (!pet.CharmerGUID.IsEmpty)
                        return SpellCastResult.AlreadyHaveCharm;

                    break;
                }
                case AuraType.ModPossess:
                case AuraType.ModCharm:
                case AuraType.AoeCharm:
                {
                    var unitCaster1 = OriginalCaster ?? Caster.AsUnit;

                    if (unitCaster1 == null)
                        return SpellCastResult.BadTargets;

                    if (!unitCaster1.CharmerGUID.IsEmpty)
                        return SpellCastResult.AlreadyHaveCharm;

                    if (spellEffectInfo.ApplyAuraName is AuraType.ModCharm or AuraType.ModPossess)
                    {
                        if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) && !unitCaster1.PetGUID.IsEmpty)
                            return SpellCastResult.AlreadyHaveSummon;

                        if (!unitCaster1.CharmedGUID.IsEmpty)
                            return SpellCastResult.AlreadyHaveCharm;
                    }

                    var target = Targets.UnitTarget;

                    if (target != null)
                    {
                        if (target.IsTypeId(TypeId.Unit) && target.AsCreature.IsVehicle)
                            return SpellCastResult.BadImplicitTargets;

                        if (target.IsMounted)
                            return SpellCastResult.CantBeCharmed;

                        if (!target.CharmerGUID.IsEmpty)
                            return SpellCastResult.Charmed;

                        if (target.OwnerUnit != null && target.OwnerUnit.IsTypeId(TypeId.Player))
                            return SpellCastResult.TargetIsPlayerControlled;

                        var damage = CalculateDamage(spellEffectInfo, target);

                        if (damage != 0 && target.GetLevelForTarget(Caster) > damage)
                            return SpellCastResult.Highlevel;
                    }

                    break;
                }
                case AuraType.Mounted:
                {
                    if (unitCaster == null)
                        return SpellCastResult.BadTargets;

                    if (unitCaster.Location.IsInWater && SpellInfo.HasAura(AuraType.ModIncreaseMountedFlightSpeed))
                        return SpellCastResult.OnlyAbovewater;

                    if (unitCaster.IsInDisallowedMountForm)
                    {
                        SendMountResult(MountResult.Shapeshifted); // mount result gets sent before the cast result

                        return SpellCastResult.DontReport;
                    }

                    break;
                }
                case AuraType.RangedAttackPowerAttackerBonus:
                {
                    if (Targets.UnitTarget == null)
                        return SpellCastResult.BadImplicitTargets;

                    // can be casted at non-friendly unit or own pet/charm
                    if (Caster.WorldObjectCombat.IsFriendlyTo(Targets.UnitTarget))
                        return SpellCastResult.TargetFriendly;

                    break;
                }
                case AuraType.Fly:
                case AuraType.ModIncreaseFlightSpeed:
                {
                    // not allow cast fly spells if not have req. skills  (all spells is self target)
                    // allow always ghost flight spells
                    if (OriginalCaster != null && OriginalCaster.IsTypeId(TypeId.Player) && OriginalCaster.IsAlive)
                    {
                        var bf = _battleFieldManager.GetBattlefieldToZoneId(OriginalCaster.Location.Map, OriginalCaster.Location.Zone);

                        if (_cliDb.AreaTableStorage.TryGetValue(OriginalCaster.Location.Area, out var area))
                            if (area.HasFlag(AreaFlags.NoFlyZone) || bf is { CanFlyIn: false })
                                return SpellCastResult.NotHere;
                    }

                    break;
                }
                case AuraType.PeriodicManaLeech:
                {
                    if (spellEffectInfo.IsTargetingArea)
                        break;

                    if (Targets.UnitTarget == null)
                        return SpellCastResult.BadImplicitTargets;

                    if (!Caster.IsTypeId(TypeId.Player) || CastItem != null)
                        break;

                    if (Targets.UnitTarget.DisplayPowerType != PowerType.Mana)
                        return SpellCastResult.BadTargets;

                    break;
                }
            }

            // check if target already has the same type, but more powerful aura
            if (SpellInfo.HasAttribute(SpellAttr4.AuraNeverBounces) || (nonAuraEffectMask != 0 && !SpellInfo.HasAttribute(SpellAttr4.AuraBounceFailsSpell)) || !approximateAuraEffectMask.Contains(spellEffectInfo.EffectIndex) || SpellInfo.IsTargetingArea)
                continue;

            {
                var target = Targets.UnitTarget;

                if (target == null)
                    continue;

                if (!target.IsHighestExclusiveAuraEffect(SpellInfo, spellEffectInfo.ApplyAuraName, spellEffectInfo.CalcValue(Caster, SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex], null, CastItemEntry, CastItemLevel), approximateAuraEffectMask))
                    return SpellCastResult.AuraBounced;
            }
        }

        // check trade slot case (last, for allow catch any another cast problems)
        if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.TradeItem))
        {
            if (CastItem != null)
                return SpellCastResult.ItemEnchantTradeWindow;

            if (SpellInfo.HasAttribute(SpellAttr2.EnchantOwnItemOnly))
                return SpellCastResult.ItemEnchantTradeWindow;

            if (!Caster.IsTypeId(TypeId.Player))
                return SpellCastResult.NotTrading;

            var myTrade = Caster.AsPlayer.TradeData;

            if (myTrade == null)
                return SpellCastResult.NotTrading;

            var slot = (TradeSlots)Targets.ItemTargetGuid.LowValue;

            if (slot != TradeSlots.NonTraded)
                return SpellCastResult.BadTargets;

            if (!IsTriggered)
                if (myTrade.Spell != 0)
                    return SpellCastResult.ItemAlreadyEnchanted;
        }

        // check if caster has at least 1 combo point for spells that require combo points
        if (NeedComboPoints)
        {
            var plrCaster = Caster.AsPlayer;

            if (plrCaster != null)
                if (plrCaster.GetPower(PowerType.ComboPoints) == 0)
                    return SpellCastResult.NoComboPoints;
        }

        // all ok
        return SpellCastResult.SpellCastOk;
    }

    public SpellCastResult CheckMovement()
    {
        if (IsTriggered)
            return SpellCastResult.SpellCastOk;

        var unitCaster = Caster.AsUnit;

        if (unitCaster != null)
            if (!unitCaster.CanCastSpellWhileMoving(SpellInfo))
            {
                if (State == SpellState.Preparing)
                {
                    if (CastTime > 0 && SpellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement))
                        return SpellCastResult.Moving;
                }
                else if (State == SpellState.Casting && !SpellInfo.IsMoveAllowedChannel)
                    return SpellCastResult.Moving;
            }

        return SpellCastResult.SpellCastOk;
    }

    public SpellCastResult CheckPetCast(Unit target)
    {
        var unitCaster = Caster.AsUnit;

        if (unitCaster != null && unitCaster.HasUnitState(UnitState.Casting) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress)) //prevent spellcast interruption by another spellcast
            return SpellCastResult.SpellInProgress;

        // dead owner (pets still alive when owners ressed?)
        var owner = Caster.CharmerOrOwner;

        if (owner is { IsAlive: false })
            return SpellCastResult.CasterDead;

        if (target == null && Targets.UnitTarget != null)
            target = Targets.UnitTarget;

        if (SpellInfo.NeedsExplicitUnitTarget)
        {
            if (target == null)
                return SpellCastResult.BadImplicitTargets;

            Targets.UnitTarget = target;
        }

        // cooldown
        var creatureCaster = Caster.AsCreature;

        if (creatureCaster != null)
            if (creatureCaster.SpellHistory.HasCooldown(SpellInfo.Id))
                return SpellCastResult.NotReady;

        // Check if spell is affected by GCD
        if (SpellInfo.StartRecoveryCategory <= 0)
            return CheckCast(true);

        if (unitCaster?.GetCharmInfo() != null && unitCaster.SpellHistory.HasGlobalCooldown(SpellInfo))
            return SpellCastResult.NotReady;

        return CheckCast(true);
    }

    public bool CheckTargetHookEffect(ITargetHookHandler th, int effIndexToCheck)
    {
        return SpellInfo.Effects.Count > effIndexToCheck && CheckTargetHookEffect(th, SpellInfo.GetEffect(effIndexToCheck));
    }

    public bool CheckTargetHookEffect(ITargetHookHandler th, SpellEffectInfo spellEffectInfo)
    {
        if (th.TargetType == 0)
            return false;

        if (spellEffectInfo.TargetA.Target != th.TargetType && spellEffectInfo.TargetB.Target != th.TargetType)
            return false;

        SpellImplicitTargetInfo targetInfo = new(th.TargetType);

        switch (targetInfo.SelectionCategory)
        {
            case SpellTargetSelectionCategories.Channel: // SINGLE
                return !th.Area;

            case SpellTargetSelectionCategories.Nearby: // BOTH
                return true;

            case SpellTargetSelectionCategories.Cone: // AREA
            case SpellTargetSelectionCategories.Line: // AREA
                return th.Area;

            case SpellTargetSelectionCategories.Area: // AREA
                if (targetInfo.ObjectType == SpellTargetObjectTypes.UnitAndDest)
                    return th.Area || th.Dest;

                return th.Area;

            case SpellTargetSelectionCategories.Default:
                switch (targetInfo.ObjectType)
                {
                    case SpellTargetObjectTypes.Src: // EMPTY
                        return false;

                    case SpellTargetObjectTypes.Dest: // Dest
                        return th.Dest;

                    default:
                        switch (targetInfo.ReferenceType)
                        {
                            case SpellTargetReferenceTypes.Caster: // SINGLE
                                return !th.Area;

                            case SpellTargetReferenceTypes.Target: // BOTH
                                return true;
                        }

                        break;
                }

                break;
        }

        return false;
    }

    public void CleanupTargetList()
    {
        UniqueTargetInfo.Clear();
        _uniqueGoTargetInfo.Clear();
        _uniqueItemInfo.Clear();
        DelayMoment = 0;
    }

    public void Delayed() // only called in DealDamage()
    {
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return;

        if (IsDelayableNoMore()) // Spells may only be delayed twice
            return;

        //check pushback reduce
        var delaytime = 500;      // spellcasting delay is normally 500ms
        double delayReduce = 100; // must be initialized to 100 for percent modifiers

        var player = unitCaster.SpellModOwner;

        player?.ApplySpellMod(SpellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

        delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

        if (delayReduce >= 100)
            return;

        MathFunctions.AddPct(ref delaytime, -delayReduce);

        if (_timer + delaytime > CastTime)
        {
            delaytime = CastTime - _timer;
            _timer = CastTime;
        }
        else
            _timer += delaytime;

        SpellDelayed spellDelayed = new()
        {
            Caster = unitCaster.GUID,
            ActualDelay = delaytime
        };

        unitCaster.SendMessageToSet(spellDelayed, true);
    }

    public void DelayedChannel()
    {
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return;

        if (State != SpellState.Casting)
            return;

        if (IsDelayableNoMore()) // Spells may only be delayed twice
            return;

        //check pushback reduce
        // should be affected by modifiers, not take the dbc duration.
        var duration = _channeledDuration > 0 ? _channeledDuration : SpellInfo.Duration;

        var delaytime = MathFunctions.CalculatePct(duration, 25); // channeling delay is normally 25% of its time per hit
        double delayReduce = 100;                                 // must be initialized to 100 for percent modifiers

        var player = unitCaster.SpellModOwner;

        player?.ApplySpellMod(SpellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

        delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

        if (delayReduce >= 100)
            return;

        MathFunctions.AddPct(ref delaytime, -delayReduce);

        if (_timer <= delaytime)
        {
            delaytime = _timer;
            _timer = 0;
        }
        else
            _timer -= delaytime;

        foreach (var ihit in UniqueTargetInfo)
            if (ihit.MissCondition == SpellMissInfo.None)
            {
                var unit = unitCaster.GUID == ihit.TargetGuid ? unitCaster : Caster.ObjectAccessor.GetUnit(unitCaster, ihit.TargetGuid);

                unit?.DelayOwnedAuras(SpellInfo.Id, _originalCasterGuid, delaytime);
            }

        // partially interrupt persistent area auras
        var dynObj = unitCaster.GetDynObject(SpellInfo.Id);

        dynObj?.Delay(delaytime);

        SendChannelUpdate((uint)_timer);
    }

    public virtual void Dispose()
    {
        // unload scripts
        foreach (var script in _loadedScripts)
            script._Unload();

        if (!_referencedFromCurrentSpell || SelfContainer == null || SelfContainer != this)
            return;

        // Clean the reference to avoid later crash.
        // If this error is repeating, we may have to add an ASSERT to better track down how we get into this case.
        Log.Logger.Error("SPELL: deleting spell for spell ID {0}. However, spell still referenced.", SpellInfo.Id);
        SelfContainer = null;
    }
    public void DoSpellEffectHit(Unit unit, SpellEffectInfo spellEffectInfo, TargetInfo hitInfo)
    {
        var auraEffmask = Aura.BuildEffectMaskForOwner(SpellInfo,
                                                       new HashSet<int>
                                                       {
                                                           spellEffectInfo.EffectIndex
                                                       },
                                                       unit);

        if (auraEffmask.Count != 0)
        {
            var caster = Caster;

            if (OriginalCaster != null)
                caster = OriginalCaster;

            if (caster != null)
            {
                // delayed spells with multiple targets need to create a new aura object, otherwise we'll access a deleted aura
                if (hitInfo.HitAura == null)
                {
                    var resetPeriodicTimer = SpellInfo.StackAmount < 2 && !_triggeredCastFlags.HasFlag(TriggerCastFlags.DontResetPeriodicTimer);
                    var allAuraEffectMask = Aura.BuildEffectMaskForOwner(SpellInfo, SpellConst.MaxEffects, unit);

                    AuraCreateInfo createInfo = new(CastId, SpellInfo, CastDifficulty, allAuraEffectMask, unit)
                    {
                        CasterGuid = caster.GUID
                    };

                    createInfo.SetBaseAmount(hitInfo.AuraBasePoints);
                    createInfo.SetCastItem(CastItemGuid, CastItemEntry, CastItemLevel);
                    createInfo.ResetPeriodicTimer = resetPeriodicTimer;
                    createInfo.SetOwnerEffectMask(auraEffmask);

                    var aura = Aura.TryRefreshStackOrCreate(createInfo, false);

                    if (aura != null)
                    {
                        hitInfo.HitAura = aura.ToUnitAura();

                        // Set aura stack amount to desired value
                        if (SpellValue.AuraStackAmount > 1)
                        {
                            if (!createInfo.IsRefresh)
                                hitInfo.HitAura.SetStackAmount((byte)SpellValue.AuraStackAmount);
                            else
                                hitInfo.HitAura.ModStackAmount(SpellValue.AuraStackAmount);
                        }

                        hitInfo.HitAura.SetDiminishGroup(hitInfo.DrGroup);

                        if (!SpellValue.Duration.HasValue)
                        {
                            hitInfo.AuraDuration = ModSpellDuration(SpellInfo, unit, hitInfo.AuraDuration, hitInfo.Positive, hitInfo.HitAura.AuraEffects.Keys.ToHashSet());

                            if (hitInfo.AuraDuration > 0)
                            {
                                hitInfo.AuraDuration *= (int)SpellValue.DurationMul;

                                // Haste modifies duration of channeled spells
                                if (SpellInfo.IsChanneled)
                                {
                                    var duration = hitInfo.AuraDuration;
                                    caster.WorldObjectCombat.ModSpellDurationTime(SpellInfo, ref duration, this);
                                    hitInfo.AuraDuration = duration;
                                }
                                else if (SpellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
                                {
                                    var origDuration = hitInfo.AuraDuration;
                                    hitInfo.AuraDuration = 0;

                                    foreach (var auraEff in hitInfo.HitAura.AuraEffects)
                                    {
                                        var period = auraEff.Value.Period;

                                        if (period != 0) // period is hastened by UNIT_MOD_CAST_SPEED
                                            hitInfo.AuraDuration = Math.Max(Math.Max(origDuration / period, 1) * period, hitInfo.AuraDuration);
                                    }

                                    // if there is no periodic effect
                                    if (hitInfo.AuraDuration == 0)
                                        hitInfo.AuraDuration = (int)(origDuration * OriginalCaster?.UnitData.ModCastingSpeed);
                                }
                            }
                        }
                        else
                            hitInfo.AuraDuration = SpellValue.Duration.Value;

                        if (hitInfo.AuraDuration != hitInfo.HitAura.MaxDuration)
                        {
                            hitInfo.HitAura.SetMaxDuration(hitInfo.AuraDuration);
                            hitInfo.HitAura.SetDuration(hitInfo.AuraDuration);
                        }

                        if (createInfo.IsRefresh)
                            hitInfo.HitAura.AddStaticApplication(unit, auraEffmask);
                    }
                }
                else
                    hitInfo.HitAura.AddStaticApplication(unit, auraEffmask);
            }
        }

        SpellAura = hitInfo.HitAura;
        HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
        SpellAura = null;
    }

    public void DoTriggersOnSpellHit(Unit unit)
    {
        // handle SPELL_AURA_ADD_TARGET_TRIGGER auras
        // this is executed after spell proc spells on target hit
        // spells are triggered for each hit spell target
        // info confirmed with retail sniffs of permafrost and shadow weaving
        if (!_hitTriggerSpells.Empty())
        {
            var duration = 0;

            foreach (var hit in _hitTriggerSpells)
                if (CanExecuteTriggersOnHit(unit, hit.TriggeredByAura) && RandomHelper.randChance(hit.Chance))
                {
                    Caster.SpellFactory.CastSpell(unit,
                                                  hit.TriggeredSpell.Id,
                                                  new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                      .SetTriggeringSpell(this)
                                                      .SetCastDifficulty(hit.TriggeredSpell.Difficulty));

                    Log.Logger.Debug("Spell {0} triggered spell {1} by SPELL_AURA_ADD_TARGET_TRIGGER aura", SpellInfo.Id, hit.TriggeredSpell.Id);

                    // SPELL_AURA_ADD_TARGET_TRIGGER auras shouldn't trigger auras without duration
                    // set duration of current aura to the triggered spell
                    if (hit.TriggeredSpell.Duration == -1)
                    {
                        var triggeredAur = unit.GetAura(hit.TriggeredSpell.Id, Caster.GUID);

                        if (triggeredAur != null)
                        {
                            // get duration from aura-only once
                            if (duration == 0)
                            {
                                var aur = unit.GetAura(SpellInfo.Id, Caster.GUID);
                                duration = aur?.Duration ?? -1;
                            }

                            triggeredAur.SetDuration(duration);
                        }
                    }
                }
        }

        // trigger linked auras remove/apply
        // @todo remove/cleanup this, as this table is not documented and people are doing stupid things with it
        var spellTriggered = _spellManager.GetSpellLinked(SpellLinkedType.Hit, SpellInfo.Id);

        if (spellTriggered == null)
            return;

        foreach (var id in spellTriggered)
            if (id < 0)
                unit.RemoveAura((uint)-id);
            else
                unit.SpellFactory.CastSpell(unit, (uint)id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(Caster.GUID).SetTriggeringSpell(this));
    }

    public void EndEmpoweredSpell()
    {
        if (!GetPlayerIfIsEmpowered(out var p) ||
            !SpellInfo.EmpowerStages.TryGetValue(_empoweredSpellStage, out var stageinfo)) // ensure stage is valid
            return;

        var duration = SpellInfo.Duration;
        var timeCasted = SpellInfo.Duration - _timer;

        if (MathFunctions.GetPctOf(timeCasted, duration) < p.EmpoweredSpellMinHoldPct) // ensure we held for long enough
            return;

        ForEachSpellScript<ISpellOnEpowerSpellEnd>(s => s.EmpowerSpellEnd(stageinfo, _empoweredSpellDelta));

        var stageUpdate = new SpellEmpowerStageUpdate
        {
            Caster = p.GUID,
            CastID = CastId,
            TimeRemaining = _timer
        };

        var unusedDurations = new List<uint>();

        var nextStage = _empoweredSpellStage;
        nextStage++;

        while (SpellInfo.EmpowerStages.TryGetValue(nextStage, out var nextStageinfo))
        {
            unusedDurations.Add(nextStageinfo.DurationMs);
            nextStage++;
        }

        stageUpdate.RemainingStageDurations = unusedDurations;
        p.SendPacket(stageUpdate);
    }

    public void Finish(SpellCastResult result = SpellCastResult.SpellCastOk)
    {
        if (State == SpellState.Finished)
            return;

        State = SpellState.Finished;

        var unitCaster = Caster?.AsUnit;

        if (unitCaster == null)
            return;

        // successful cast of the initial autorepeat spell is moved to idle state so that it is not deleted as long as autorepeat is active
        if (_isAutoRepeat && unitCaster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat) == this)
            State = SpellState.Idle;

        if (SpellInfo.IsChanneled)
            unitCaster.UpdateInterruptMask();

        if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
            unitCaster.ClearUnitState(UnitState.Casting);

        // Unsummon summon as possessed creatures on spell cancel
        if (SpellInfo.IsChanneled && unitCaster.IsTypeId(TypeId.Player))
        {
            var charm = unitCaster.Charmed;

            if (charm != null)
                if (charm.IsTypeId(TypeId.Unit) && charm.AsCreature.HasUnitTypeMask(UnitTypeMask.Puppet) && charm.UnitData.CreatedBySpell == SpellInfo.Id)
                    ((Puppet)charm).UnSummon();
        }

        var creatureCaster = unitCaster.AsCreature;

        creatureCaster?.ReleaseSpellFocus(this);

        if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
            _combatHelpers.ProcSkillsAndAuras(unitCaster, null, new ProcFlagsInit(ProcFlags.CastEnded), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, this, null, null);

        if (result != SpellCastResult.SpellCastOk)
        {
            // on failure (or manual cancel) send TraitConfigCommitFailed to revert talent UI saved config selection
            if (Caster.IsPlayer && SpellInfo.HasEffect(SpellEffectName.ChangeActiveCombatTraitConfig))
                if (CustomArg is TraitConfig config)
                    Caster.AsPlayer.SendPacket(new TraitConfigCommitFailed(config.ID));

            return;
        }

        if (unitCaster.IsTypeId(TypeId.Unit) && unitCaster.AsCreature.IsSummon)
        {
            // Unsummon statue
            uint spell = unitCaster.UnitData.CreatedBySpell;
            var spellInfo = _spellManager.GetSpellInfo(spell, CastDifficulty);

            if (spellInfo is { IconFileDataId: 134230 })
            {
                Log.Logger.Debug("Statue {0} is unsummoned in spell {1} finish", unitCaster.GUID.ToString(), SpellInfo.Id);

                // Avoid infinite loops with setDeathState(JUST_DIED) being called over and over
                // It might make sense to do this check in Unit::setDeathState() and all overloaded functions
                if (unitCaster.DeathState != DeathState.JustDied)
                    unitCaster.SetDeathState(DeathState.JustDied);

                return;
            }
        }

        if (IsAutoActionResetSpell())
            if (!SpellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
            {
                unitCaster.ResetAttackTimer();

                if (unitCaster.HasOffhandWeapon)
                    unitCaster.ResetAttackTimer(WeaponAttackType.OffAttack);

                unitCaster.ResetAttackTimer(WeaponAttackType.RangedAttack);
            }

        // potions disabled by client, send event "not in combat" if need
        if (unitCaster.IsTypeId(TypeId.Player))
            if (TriggeredByAuraSpell == null)
                unitCaster.AsPlayer.UpdatePotionCooldown(this);

        // Stop Attack for some spells
        if (SpellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat))
            unitCaster.AttackStop();
    }

    public void ForEachSpellScript<T>(Action<T> action) where T : ISpellScript
    {
        foreach (T script in GetSpellScripts<T>())
            try
            {
                action.Invoke(script);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e);
            }
    }

    public long GetCorpseTargetCountForEffect(int effect)
    {
        return _uniqueCorpseTargetInfo.Count(targetInfo => targetInfo.Effects.Contains(effect));
    }

    public List<(ISpellScript, ISpellEffect)> GetEffectScripts(SpellScriptHookType h, int index)
    {
        if (_effectHandlers.TryGetValue(index, out var effDict) &&
            effDict.TryGetValue(h, out var scripts))
            return scripts;

        return DummySpellEffects;
    }

    public SpellLogEffect GetExecuteLogEffect(SpellEffectName effect)
    {
        if (_executeLogEffects.TryGetValue(effect, out var spellLogEffect))
            return spellLogEffect;

        SpellLogEffect executeLogEffect = new()
        {
            Effect = (int)effect
        };

        _executeLogEffects.Add(effect, executeLogEffect);

        return executeLogEffect;
    }

    public long GetGameObjectTargetCountForEffect(int effect)
    {
        return _uniqueGoTargetInfo.Count(targetInfo => targetInfo.Effects.Contains(effect));
    }

    public long GetItemTargetCountForEffect(int effect)
    {
        return _uniqueItemInfo.Count(targetInfo => targetInfo.Effects.Contains(effect));
    }

    public bool GetPlayerIfIsEmpowered(out Player p)
    {
        p = null;

        return SpellInfo.EmpowerStages.Count > 0 && Caster.TryGetAsPlayer(out p);
    }

    public int? GetPowerTypeCostAmount(PowerType power)
    {
        var powerCost = PowerCost.Find(cost => cost.Power == power);

        return powerCost?.Amount;
    }

    public GridMapTypeMask GetSearcherTypeMask(SpellTargetObjectTypes objType, List<Condition> condList)
    {
        // this function selects which containers need to be searched for spell target
        var retMask = GridMapTypeMask.All;

        // filter searchers based on searched object type
        switch (objType)
        {
            case SpellTargetObjectTypes.Unit:
            case SpellTargetObjectTypes.UnitAndDest:
                retMask &= GridMapTypeMask.Player | GridMapTypeMask.Creature;

                break;

            case SpellTargetObjectTypes.Corpse:
            case SpellTargetObjectTypes.CorpseEnemy:
            case SpellTargetObjectTypes.CorpseAlly:
                retMask &= GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.Creature;

                break;

            case SpellTargetObjectTypes.Gobj:
            case SpellTargetObjectTypes.GobjItem:
                retMask &= GridMapTypeMask.GameObject;

                break;
        }

        if (SpellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
            retMask &= GridMapTypeMask.Corpse | GridMapTypeMask.Player;

        if (SpellInfo.HasAttribute(SpellAttr3.OnlyOnGhosts))
            retMask &= GridMapTypeMask.Player;

        if (SpellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
            retMask &= ~GridMapTypeMask.Player;

        if (condList != null)
            retMask &= Caster.ConditionManager.GetSearcherTypeMaskForConditionList(condList);

        return retMask;
    }

    public List<ISpellScript> GetSpellScripts<T>() where T : ISpellScript
    {
        return _spellScriptsByType.TryGetValue(typeof(T), out var scripts) ? scripts : Dummy;
    }

    public long GetUnitTargetCountForEffect(int effect)
    {
        return UniqueTargetInfo.Count(targetInfo => targetInfo.MissCondition == SpellMissInfo.None && targetInfo.Effects.Contains(effect));
    }

    public ulong HandleDelayed(ulong offset)
    {
        if (!UpdatePointers())
        {
            // finish the spell if UpdatePointers() returned false, something wrong happened there
            Finish(SpellCastResult.Fizzle);

            return 0;
        }

        var singleMissile = Targets.HasDst;
        ulong nextTime = 0;

        if (!_launchHandled)
        {
            var launchMoment = (ulong)Math.Floor(SpellInfo.LaunchDelay * 1000.0f);

            if (launchMoment > offset)
                return launchMoment;

            HandleLaunchPhase();
            _launchHandled = true;

            if (DelayMoment > offset)
            {
                if (singleMissile)
                    return DelayMoment;

                nextTime = DelayMoment;

                if (UniqueTargetInfo.Count > 2 || (UniqueTargetInfo.Count == 1 && UniqueTargetInfo[0].TargetGuid == Caster.GUID) || !_uniqueGoTargetInfo.Empty())
                    offset = 0; // if LaunchDelay was present then the only target that has timeDelay = 0 is m_caster - and that is the only target we want to process now
            }
        }

        if (singleMissile && offset == 0)
            return DelayMoment;

        var modOwner = Caster.SpellModOwner;

        modOwner?.SetSpellModTakingSpell(this, true);

        PrepareTargetProcessing();

        if (!_immediateHandled && offset != 0)
        {
            _handle_immediate_phase();
            _immediateHandled = true;
        }

        // now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
        {
            List<TargetInfo> delayedTargets = new();

            UniqueTargetInfo.RemoveAll(target =>
            {
                if (singleMissile || target.TimeDelay <= offset)
                {
                    target.TimeDelay = offset;
                    delayedTargets.Add(target);

                    return true;
                }

                if (nextTime == 0 || target.TimeDelay < nextTime)
                    nextTime = target.TimeDelay;

                return false;
            });

            DoProcessTargetContainer(delayedTargets);

            if (nextTime == 0)
                CallScriptOnHitHandlers();
        }

        // now recheck gameobject targeting correctness
        {
            List<GOTargetInfo> delayedGOTargets = new();

            _uniqueGoTargetInfo.RemoveAll(goTarget =>
            {
                if (singleMissile || goTarget.TimeDelay <= offset)
                {
                    goTarget.TimeDelay = offset;
                    delayedGOTargets.Add(goTarget);

                    return true;
                }

                if (nextTime == 0 || goTarget.TimeDelay < nextTime)
                    nextTime = goTarget.TimeDelay;

                return false;
            });

            DoProcessTargetContainer(delayedGOTargets);
        }

        FinishTargetProcessing();

        modOwner?.SetSpellModTakingSpell(this, false);

        // All targets passed - need finish phase
        // spell is unfinished, return next execution time
        if (nextTime != 0)
            return nextTime;

        // spell is finished, perform some last features of the spell here
        _handle_finish_phase();

        Finish(); // successfully finish spell cast

        // return zero, spell is finished now
        return 0;
    }

    public void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGoTarget, Corpse pCorpseTarget, SpellEffectInfo spellEffectInfo, SpellEffectHandleMode mode)
    {
        _effectHandleMode = mode;
        UnitTarget = pUnitTarget;
        ItemTarget = pItemTarget;
        GameObjTarget = pGoTarget;
        CorpseTarget = pCorpseTarget;
        DestTarget = _destTargets[spellEffectInfo.EffectIndex].Position;
        EffectInfo = spellEffectInfo;

        Damage = CalculateDamage(spellEffectInfo, UnitTarget, out var variance);
        Variance = variance;

        var preventDefault = CallScriptEffectHandlers(spellEffectInfo.EffectIndex, mode);

        if (!preventDefault)
            _spellManager.GetSpellEffectHandler(spellEffectInfo.Effect).Invoke(this);
    }

    public bool HasPowerTypeCost(PowerType power)
    {
        return GetPowerTypeCostAmount(power).HasValue;
    }

    public void InitExplicitTargets(SpellCastTargets targets)
    {
        Targets = targets;

        // this function tries to correct spell explicit targets for spell
        // client doesn't send explicit targets correctly sometimes - we need to fix such spells serverside
        // this also makes sure that we correctly send explicit targets to client (removes redundant data)
        var neededTargets = SpellInfo.ExplicitTargetMask;

        var target = Targets.ObjectTarget;

        if (target != null)
        {
            // check if object target is valid with needed target flags
            // for unit case allow corpse target mask because player with not released corpse is a unit target
            if ((target.AsUnit != null && !neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)) ||
                (target.IsTypeId(TypeId.GameObject) && !neededTargets.HasFlag(SpellCastTargetFlags.GameobjectMask)) ||
                (target.IsTypeId(TypeId.Corpse) && !neededTargets.HasFlag(SpellCastTargetFlags.CorpseMask)))
                Targets.RemoveObjectTarget();
        }
        else
        {
            // try to select correct unit target if not provided by client or by serverside cast
            if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask))
            {
                Unit unit = null;
                // try to use player selection as a target
                var playerCaster = Caster.AsPlayer;

                if (playerCaster != null)
                {
                    // selection has to be found and to be valid target for the spell
                    var selectedUnit = Caster.ObjectAccessor.GetUnit(Caster, playerCaster.Target);

                    if (selectedUnit != null)
                        if (SpellInfo.CheckExplicitTarget(Caster, selectedUnit) == SpellCastResult.SpellCastOk)
                            unit = selectedUnit;
                }
                // try to use attacked unit as a target
                else if (Caster.IsTypeId(TypeId.Unit) && neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.Unit))
                    unit = Caster.AsUnit.Victim;

                // didn't find anything - let's use self as target
                if (unit == null && neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitAlly))
                    unit = Caster.AsUnit;

                Targets.UnitTarget = unit;
            }
        }

        // check if spell needs dst target
        if (neededTargets.HasFlag(SpellCastTargetFlags.DestLocation))
        {
            // and target isn't set
            // try to use unit target if provided
            // or use self if not available
            if (!Targets.HasDst)
                Targets.SetDst(targets.ObjectTarget ?? Caster);
        }
        else
            Targets.RemoveDst();

        if (neededTargets.HasFlag(SpellCastTargetFlags.SourceLocation))
        {
            if (!targets.HasSrc)
                Targets.SetSrc(Caster);
        }
        else
            Targets.RemoveSrc();
    }

    public bool IsTriggeredByAura(SpellInfo auraSpellInfo)
    {
        return auraSpellInfo == TriggeredByAuraSpell;
    }

    public int ModSpellDuration(SpellInfo spellInfo, WorldObject target, int duration, bool positive, int effIndex)
    {
        return ModSpellDuration(spellInfo,
                                target,
                                duration,
                                positive,
                                new HashSet<int>
                                {
                                    effIndex
                                });
    }

    public int ModSpellDuration(SpellInfo spellInfo, WorldObject target, int duration, bool positive, HashSet<int> effectMask)
    {
        // don't mod permanent auras duration
        if (duration < 0)
            return duration;

        // some auras are not affected by duration modifiers
        if (spellInfo.HasAttribute(SpellAttr7.IgnoreDurationMods))
            return duration;

        // cut duration only of negative effects
        var unitTarget = target.AsUnit;

        if (unitTarget == null)
            return duration;

        if (!positive)
        {
            var mechanicMask = spellInfo.GetSpellMechanicMaskByEffectMask(effectMask);

            bool MechanicCheck(AuraEffect aurEff)
            {
                return (mechanicMask & (1ul << aurEff.MiscValue)) != 0;
            }

            // Find total mod value (negative bonus)
            var durationModAlways = unitTarget.GetTotalAuraModifier(AuraType.MechanicDurationMod, MechanicCheck);
            // Find max mod (negative bonus)
            var durationModNotStack = unitTarget.GetMaxNegativeAuraModifier(AuraType.MechanicDurationModNotStack, MechanicCheck);

            // Select strongest negative mod
            var durationMod = Math.Min(durationModAlways, durationModNotStack);

            if (durationMod != 0)
                MathFunctions.AddPct(ref duration, durationMod);

            // there are only negative mods currently
            durationModAlways = unitTarget.GetTotalAuraModifierByMiscValue(AuraType.ModAuraDurationByDispel, (int)spellInfo.Dispel);
            durationModNotStack = unitTarget.GetMaxNegativeAuraModifierByMiscValue(AuraType.ModAuraDurationByDispelNotStack, (int)spellInfo.Dispel);

            durationMod = Math.Min(durationModAlways, durationModNotStack);

            if (durationMod != 0)
                MathFunctions.AddPct(ref duration, durationMod);
        }
        else
        {
            // else positive mods here, there are no currently
            // when there will be, change GetTotalAuraModifierByMiscValue to GetMaxPositiveAuraModifierByMiscValue

            // Mixology - duration boost
            if (unitTarget.IsPlayer)
                if (spellInfo.SpellFamilyName == SpellFamilyNames.Potion &&
                    (
                        _spellManager.IsSpellMemberOfSpellGroup(spellInfo.Id, SpellGroup.ElixirBattle) ||
                        _spellManager.IsSpellMemberOfSpellGroup(spellInfo.Id, SpellGroup.ElixirGuardian)))
                {
                    var effect = spellInfo.GetEffect(0);

                    if (unitTarget.HasAura(53042) && effect != null && unitTarget.HasSpell(effect.TriggerSpell))
                        duration *= 2;
                }
        }

        return Math.Max(duration, 0);
    }

    public SpellCastResult Prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
    {
        if (CastItem != null)
        {
            CastItemGuid = CastItem.GUID;
            CastItemEntry = CastItem.Entry;

            var owner = CastItem.OwnerUnit;

            if (owner != null)
                CastItemLevel = (int)CastItem.GetItemLevel(owner);
            else if (CastItem.OwnerGUID == Caster.GUID)
                CastItemLevel = (int)CastItem.GetItemLevel(Caster.AsPlayer);
            else
            {
                SendCastResult(SpellCastResult.EquippedItem);
                Finish(SpellCastResult.EquippedItem);

                return SpellCastResult.EquippedItem;
            }
        }

        InitExplicitTargets(targets);

        State = SpellState.Preparing;

        if (triggeredByAura != null)
        {
            TriggeredByAuraSpell = triggeredByAura.SpellInfo;
            CastItemLevel = triggeredByAura.Base.CastItemLevel;
        }

        // create and add update event for this spell
        _spellEvent = new SpellEvent(this);
        Caster.Events.AddEvent(_spellEvent, Caster.Events.CalculateTime(TimeSpan.FromMilliseconds(1)));

        // check disables
        if (Caster.DisableManager.IsDisabledFor(DisableType.Spell, SpellInfo.Id, Caster))
        {
            SendCastResult(SpellCastResult.SpellUnavailable);
            Finish(SpellCastResult.SpellUnavailable);

            return SpellCastResult.SpellUnavailable;
        }

        // Prevent casting at cast another spell (ServerSide check)
        if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress) && Caster.AsUnit != null && Caster.AsUnit.IsNonMeleeSpellCast(false, true, true, SpellInfo.Id == 75) && !CastId.IsEmpty)
        {
            SendCastResult(SpellCastResult.SpellInProgress);
            Finish(SpellCastResult.SpellInProgress);

            return SpellCastResult.SpellInProgress;
        }

        LoadScripts();

        // Fill cost data (not use power for item casts
        if (CastItem == null)
            PowerCost = SpellInfo.CalcPowerCost(Caster, SpellSchoolMask, this);

        // Set combo point requirement
        if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreComboPoints) || CastItem != null)
            NeedComboPoints = false;

        int param1 = 0, param2 = 0;
        var result = CheckCast(true, ref param1, ref param2);

        // target is checked in too many locations and with different results to handle each of them
        // handle just the general SPELL_FAILED_BAD_TARGETS result which is the default result for most DBC target checks
        if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreTargetCheck) && result == SpellCastResult.BadTargets)
            result = SpellCastResult.SpellCastOk;

        if (result != SpellCastResult.SpellCastOk)
        {
            // Periodic auras should be interrupted when aura triggers a spell which can't be cast
            // for example bladestorm aura should be removed on disarm as of patch 3.3.5
            // channeled periodic spells should be affected by this (arcane missiles, penance, etc)
            // a possible alternative sollution for those would be validating aura target on unit state change
            if (triggeredByAura is { IsPeriodic: true } && !triggeredByAura.Base.IsPassive)
            {
                SendChannelUpdate(0);
                triggeredByAura.Base.SetDuration(0);
            }

            if (param1 != 0 || param2 != 0)
                SendCastResult(result, param1, param2);
            else
                SendCastResult(result);

            // queue autorepeat spells for future repeating
            if (CurrentContainer == CurrentSpellTypes.AutoRepeat && Caster.IsUnit)
                Caster.AsUnit?.SetCurrentCastSpell(this);

            Finish(result);

            return result;
        }

        // Prepare data for triggers
        PrepareDataForTriggerSystem();

        CastTime = CallScriptCalcCastTimeHandlers(SpellInfo.CalcCastTime(this));

        foreach (var stage in _empowerStages)
        {
            var ct = (int)stage.Value.DurationMs;
            Caster.WorldObjectCombat.ModSpellCastTime(SpellInfo, ref ct);
            stage.Value.DurationMs = (uint)CallScriptCalcCastTimeHandlers(ct);
        }

        if (Caster.IsUnit && Caster.AsUnit?.IsMoving == true)
        {
            result = CheckMovement();

            if (result != SpellCastResult.SpellCastOk)
            {
                SendCastResult(result);
                Finish(result);

                return result;
            }
        }

        // Creatures focus their target when possible
        if (CastTime != 0 && Caster.IsCreature && !SpellInfo.IsNextMeleeSwingSpell && !_isAutoRepeat && !Caster.AsUnit?.HasUnitFlag(UnitFlags.Possessed) == true)
        {
            // Channeled spells and some triggered spells do not focus a cast target. They face their target later on via channel object guid and via spell attribute or not at all
            var focusTarget = !SpellInfo.IsChanneled && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing);

            if (focusTarget && Targets.ObjectTarget != null && Caster != Targets.ObjectTarget)
                Caster.AsCreature.SetSpellFocus(this, Targets.ObjectTarget);
            else
                Caster.AsCreature.SetSpellFocus(this, null);
        }

        CallScriptOnPrecastHandler();

        // set timer base at cast time
        ReSetTimer();

        Log.Logger.Debug("Spell.prepare: spell id {0} source {1} caster {2} customCastFlags {3} mask {4}", SpellInfo.Id, Caster.Entry, OriginalCaster != null ? (int)OriginalCaster.Entry : -1, _triggeredCastFlags, Targets.TargetMask);

        if (SpellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
            SendSpellCooldown();

        //Containers for channeled spells have to be set
        // @todoApply this to all casted spells if needed
        // Why check duration? 29350: channelled triggers channelled
        if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) && (!SpellInfo.IsChanneled || SpellInfo.MaxDuration == 0))
            Cast(true);
        else
        {
            // commented out !m_spellInfo->StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
            // as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
            // the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
            // because target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
            var willCastDirectly = CastTime == 0 && /*!m_spellInfo->StartRecoveryTime && */ CurrentContainer == CurrentSpellTypes.Generic;

            var unitCaster = Caster.AsUnit;

            if (unitCaster != null)
            {
                // stealth must be removed at cast starting (at show channel bar)
                // skip triggered spell (item equip spell casting and other not explicit character casts/item uses)
                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && !SpellInfo.HasAttribute(SpellAttr2.NotAnAction))
                    unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Action, SpellInfo);

                // Do not register as current spell when requested to ignore cast in progress
                // We don't want to interrupt that other spell with cast time
                if (!willCastDirectly || !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress))
                    unitCaster.SetCurrentCastSpell(this);
            }

            SendSpellStart();

            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
                TriggerGlobalCooldown();

            // Call CreatureAI hook OnSpellStart
            var caster = Caster.AsCreature;

            if (caster is { IsAIEnabled: true })
                caster.AI.OnSpellStart(SpellInfo);

            if (willCastDirectly)
                Cast(true);
        }

        return SpellCastResult.SpellCastOk;
    }

    public SpellMissInfo PreprocessSpellHit(Unit unit, TargetInfo hitInfo)
    {
        if (unit == null)
            return SpellMissInfo.Evade;

        // Target may have begun evading between launch and hit phases - re-check now
        var creatureTarget = unit.AsCreature;

        if (creatureTarget is { IsEvadingAttacks: true })
            return SpellMissInfo.Evade;

        // For delayed spells immunity may be applied between missile launch and hit - check immunity for that case
        if (SpellInfo.HasHitDelay && unit.IsImmunedToSpell(SpellInfo, Caster))
            return SpellMissInfo.Immune;

        CallScriptBeforeHitHandlers(hitInfo.MissCondition);

        var player = unit.AsPlayer;

        if (player != null)
        {
            player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, SpellInfo.Id);
            player.UpdateCriteria(CriteriaType.BeSpellTarget, SpellInfo.Id, 0, 0, Caster);
            player.UpdateCriteria(CriteriaType.GainAura, SpellInfo.Id);
        }

        var casterPlayer = Caster.AsPlayer;

        if (casterPlayer != null)
        {
            casterPlayer.StartCriteriaTimer(CriteriaStartEvent.CastSpell, SpellInfo.Id);
            casterPlayer.UpdateCriteria(CriteriaType.LandTargetedSpellOnTarget, SpellInfo.Id, 0, 0, unit);
        }

        if (Caster != unit)
        {
            // Recheck  UNIT_FLAG_NON_ATTACKABLE for delayed spells
            if (SpellInfo.HasHitDelay && unit.HasUnitFlag(UnitFlags.NonAttackable) && unit.CharmerOrOwnerGUID != Caster.GUID)
                return SpellMissInfo.Evade;

            if (Caster.WorldObjectCombat.IsValidAttackTarget(unit, SpellInfo))
                unit.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.HostileActionReceived);
            else if (Caster.WorldObjectCombat.IsFriendlyTo(unit))
            {
                // for delayed spells ignore negative spells (after duel end) for friendly targets
                if (SpellInfo.HasHitDelay && unit.IsPlayer && !IsPositive && !Caster.WorldObjectCombat.IsValidAssistTarget(unit, SpellInfo))
                    return SpellMissInfo.Evade;

                // assisting case, healing and resurrection
                if (unit.HasUnitState(UnitState.AttackPlayer))
                {
                    var playerOwner = Caster.CharmerOrOwnerPlayerOrPlayerItself;

                    if (playerOwner != null)
                    {
                        playerOwner.SetContestedPvP();
                        playerOwner.UpdatePvP(true);
                    }
                }

                if (OriginalCaster != null && unit.IsInCombat && SpellInfo.HasInitialAggro)
                {
                    if (OriginalCaster.HasUnitFlag(UnitFlags.PlayerControlled))     // only do explicit combat forwarding for PvP enabled units
                        OriginalCaster.CombatManager.InheritCombatStatesFrom(unit); // for creature v creature combat, the threat forward does it for us

                    unit.GetThreatManager().ForwardThreatForAssistingMe(OriginalCaster, 0.0f, null, true);
                }
            }
        }

        // original caster for auras
        var origCaster = Caster;

        if (OriginalCaster != null)
            origCaster = OriginalCaster;

        // check immunity due to diminishing returns
        if (Aura.BuildEffectMaskForOwner(SpellInfo, SpellConst.MaxEffects, unit).Count == 0)
            return SpellMissInfo.None;

        foreach (var spellEffectInfo in SpellInfo.Effects)
            hitInfo.AuraBasePoints[spellEffectInfo.EffectIndex] = (SpellValue.CustomBasePointsMask & (1 << spellEffectInfo.EffectIndex)) != 0 ? SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex] : spellEffectInfo.CalcBaseValue(OriginalCaster, unit, CastItemEntry, CastItemLevel);

        // Get Data Needed for Diminishing Returns, some effects may have multiple auras, so this must be done on spell hit, not aura add
        hitInfo.DrGroup = SpellInfo.DiminishingReturnsGroupForSpell;

        var diminishLevel = DiminishingLevels.Level1;

        if (hitInfo.DrGroup != 0)
        {
            diminishLevel = unit.GetDiminishing(hitInfo.DrGroup);
            var type = SpellInfo.DiminishingReturnsGroupType;

            // Increase Diminishing on unit, current informations for actually casts will use values above
            if (type == DiminishingReturnsType.All || (type == DiminishingReturnsType.Player && unit.IsAffectedByDiminishingReturns))
                unit.IncrDiminishing(SpellInfo);
        }

        // Now Reduce spell duration using data received at spell hit
        // check whatever effects we're going to apply, diminishing returns only apply to negative aura effects
        hitInfo.Positive = true;

        if (origCaster == unit || !origCaster.WorldObjectCombat.IsFriendlyTo(unit))
            foreach (var spellEffectInfo in SpellInfo.Effects)
                // mod duration only for effects applying aura!
                if (hitInfo.Effects.Contains(spellEffectInfo.EffectIndex) &&
                    spellEffectInfo.IsUnitOwnedAuraEffect &&
                    !SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
                {
                    hitInfo.Positive = false;

                    break;
                }

        hitInfo.AuraDuration = Aura.CalcMaxDuration(SpellInfo, origCaster);

        // unit is immune to aura if it was diminished to 0 duration
        var duration = hitInfo.AuraDuration;

        if (hitInfo.Positive || unit.ApplyDiminishingToDuration(SpellInfo, ref duration, origCaster, diminishLevel))
        {
            hitInfo.AuraDuration = duration;
            return SpellMissInfo.None;
        }

        hitInfo.AuraDuration = duration;
        return SpellInfo.Effects.All(effInfo => !effInfo.IsEffect || effInfo.IsEffectName(SpellEffectName.ApplyAura)) ? SpellMissInfo.Immune : SpellMissInfo.None;
    }

    public void RecalculateDelayMomentForDst()
    {
        DelayMoment = CalculateDelayMomentForDst(0.0f);
        Caster.Events.ModifyEventTime(_spellEvent, TimeSpan.FromMilliseconds(DelayStart + DelayMoment));
    }

    public void SelectSpellTargets()
    {
        // select targets for cast phase
        SelectExplicitTargets();

        var processedAreaEffectsMask = new HashSet<int>();

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            // not call for empty effect.
            // Also some spells use not used effect targets for store targets for dummy effect in triggered spells
            if (!spellEffectInfo.IsEffect)
                continue;

            // set expected type of implicit targets to be sent to client
            var implicitTargetMask = SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetA.ObjectType) | SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetB.ObjectType);

            if (Convert.ToBoolean(implicitTargetMask & SpellCastTargetFlags.Unit))
                Targets.SetTargetFlag(SpellCastTargetFlags.Unit);

            if (Convert.ToBoolean(implicitTargetMask & (SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem)))
                Targets.SetTargetFlag(SpellCastTargetFlags.Gameobject);

            SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetA, processedAreaEffectsMask);
            SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetB, processedAreaEffectsMask);

            // Select targets of effect based on effect type
            // those are used when no valid target could be added for spell effect based on spell target type
            // some spell effects use explicit target as a default target added to target map (like SPELL_EFFECT_LEARN_SPELL)
            // some spell effects add target to target map only when target type specified (like SPELL_EFFECT_WEAPON)
            // some spell effects don't add anything to target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
            SelectEffectTypeImplicitTargets(spellEffectInfo);

            if (Targets.HasDst)
                AddDestTarget(Targets.Dst, spellEffectInfo.EffectIndex);

            if (spellEffectInfo.TargetA.ObjectType is SpellTargetObjectTypes.Unit or SpellTargetObjectTypes.UnitAndDest || spellEffectInfo.TargetB.ObjectType is SpellTargetObjectTypes.Unit or SpellTargetObjectTypes.UnitAndDest)
            {
                if (SpellInfo.HasAttribute(SpellAttr1.RequireAllTargets))
                {
                    var noTargetFound = !UniqueTargetInfo.Any(target => target.Effects.Contains(spellEffectInfo.EffectIndex));

                    if (noTargetFound)
                    {
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(SpellCastResult.BadImplicitTargets);

                        return;
                    }
                }

                if (SpellInfo.HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
                {
                    var anyNonImmuneTargetFound = UniqueTargetInfo.Any(target => target.Effects.Contains(spellEffectInfo.EffectIndex) && target.MissCondition != SpellMissInfo.Immune && target.MissCondition != SpellMissInfo.Immune2);

                    if (!anyNonImmuneTargetFound)
                    {
                        SendCastResult(SpellCastResult.Immune);
                        Finish(SpellCastResult.Immune);

                        return;
                    }
                }
            }

            if (SpellInfo.IsChanneled)
            {
                // maybe do this for all spells?
                if (_focusObject == null && UniqueTargetInfo.Empty() && _uniqueGoTargetInfo.Empty() && _uniqueItemInfo.Empty() && !Targets.HasDst)
                {
                    SendCastResult(SpellCastResult.BadImplicitTargets);
                    Finish(SpellCastResult.BadImplicitTargets);

                    return;
                }

                foreach (var ihit in UniqueTargetInfo)
                    if (ihit.Effects.Contains(spellEffectInfo.EffectIndex))
                    {
                        _channelTargetEffectMask.Add(spellEffectInfo.EffectIndex);

                        break;
                    }
            }
        }

        var dstDelay = CalculateDelayMomentForDst(SpellInfo.LaunchDelay);

        if (dstDelay != 0)
            DelayMoment = dstDelay;
    }

    public void SendCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
    {
        if (result == SpellCastResult.SpellCastOk)
            return;

        if (!Caster.IsTypeId(TypeId.Player))
            return;

        if (Caster.AsPlayer.IsLoading) // don't send cast results at loading time
            return;

        if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
            result = SpellCastResult.DontReport;

        CastFailed castFailed = new()
        {
            Visual = SpellVisual
        };

        FillSpellCastFailedArgs(castFailed, CastId, SpellInfo, result, CustomErrors, param1, param2, Caster.AsPlayer);
        Caster.AsPlayer.SendPacket(castFailed);
    }

    public void SendChannelUpdate(uint time)
    {
        // GameObjects don't channel
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return;

        if (time == 0)
        {
            unitCaster.ClearChannelObjects();
            unitCaster.ChannelSpellId = 0;
            unitCaster.SetChannelVisual(new SpellCastVisualField());
        }

        SpellChannelUpdate spellChannelUpdate = new()
        {
            CasterGUID = unitCaster.GUID,
            TimeRemaining = (int)time
        };

        unitCaster.SendMessageToSet(spellChannelUpdate, true);
    }

    public void SendPetCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
    {
        if (result == SpellCastResult.SpellCastOk)
            return;

        var owner = Caster.CharmerOrOwner;

        if (owner == null || !owner.IsTypeId(TypeId.Player))
            return;

        if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
            result = SpellCastResult.DontReport;

        PetCastFailed petCastFailed = new();
        FillSpellCastFailedArgs(petCastFailed, CastId, SpellInfo, result, SpellCustomErrors.None, param1, param2, owner.AsPlayer);
        owner.AsPlayer.SendPacket(petCastFailed);
    }

    public void SetEmpowerState(EmpowerState state)
    {
        if (_empowerState != EmpowerState.Finished)
            _empowerState = _empowerState switch
            {
                EmpowerState.None when state == EmpowerState.Canceled              => EmpowerState.CanceledStartup,
                EmpowerState.CanceledStartup when state == EmpowerState.Empowering => EmpowerState.None,
                _                                                                  => state
            };
    }

    public void SetReferencedFromCurrent(bool yes)
    {
        _referencedFromCurrentSpell = yes;
    }

    public void SetSpellValue(SpellValueMod mod, float value)
    {
        if (mod < SpellValueMod.End)
        {
            SpellValue.EffectBasePoints[(int)mod] = value;
            SpellValue.CustomBasePointsMask |= 1u << (int)mod;

            return;
        }

        switch (mod)
        {
            case SpellValueMod.RadiusMod:
                SpellValue.RadiusMod = value / 10000;

                break;

            case SpellValueMod.MaxTargets:
                SpellValue.MaxAffectedTargets = (uint)value;

                break;

            case SpellValueMod.AuraStack:
                SpellValue.AuraStackAmount = (int)value;

                break;

            case SpellValueMod.CritChance:
                SpellValue.CriticalChance = value / 100.0f; // @todo ugly /100 remove when basepoints are double

                break;

            case SpellValueMod.DurationPct:
                SpellValue.DurationMul = value / 100.0f;

                break;

            case SpellValueMod.Duration:
                SpellValue.Duration = (int)value;

                break;

            case SpellValueMod.SummonDuration:
                SpellValue.SummonDuration = value;

                break;
        }
    }

    public bool TryGetTotalEmpowerDuration(bool includeBaseCast, out int duration)
    {
        if (_empowerStages.Count > 0)
        {
            duration = (int)(_empowerStages.Sum(a => a.Value.DurationMs) + (includeBaseCast ? 1000 : 0));

            return true;
        }

        duration = 0;

        return false;
    }

    public void Update(uint difftime)
    {
        if (!UpdatePointers())
        {
            // cancel the spell if UpdatePointers() returned false, something wrong happened there
            Cancel();

            return;
        }

        if (!Targets.UnitTargetGUID.IsEmpty && Targets.UnitTarget == null)
        {
            Log.Logger.Debug("Spell {0} is cancelled due to removal of target.", SpellInfo.Id);
            Cancel();

            return;
        }

        // check if the player caster has moved before the spell finished
        // with the exception of spells affected with SPELL_AURA_CAST_WHILE_WALKING effect
        if (_timer != 0 && Caster.IsUnit && Caster.AsUnit.IsMoving && CheckMovement() != SpellCastResult.SpellCastOk)
            // if charmed by creature, trust the AI not to cheat and allow the cast to proceed
            // @todo this is a hack, "creature" movesplines don't differentiate turning/moving right now
            // however, checking what type of movement the spline is for every single spline would be really expensive
            if (!Caster.AsUnit.CharmerGUID.IsCreature)
                Cancel();

        switch (State)
        {
            case SpellState.Preparing:
            {
                if (_timer > 0)
                {
                    if (difftime >= _timer)
                        _timer = 0;
                    else
                        _timer -= (int)difftime;
                }

                if (_timer == 0 && !SpellInfo.IsNextMeleeSwingSpell)
                    // don't CheckCast for instant spells - done in spell.prepare, skip duplicate checks, needed for range checks for example
                    Cast(CastTime == 0);

                break;
            }
            case SpellState.Casting:
            {
                if (_timer != 0)
                {
                    // check if there are alive targets left
                    if (!UpdateChanneledTargetList())
                    {
                        Log.Logger.Debug("Channeled spell {0} is removed due to lack of targets", SpellInfo.Id);
                        _timer = 0;

                        // Also remove applied auras
                        foreach (var target in UniqueTargetInfo)
                        {
                            var unit = Caster.GUID == target.TargetGuid ? Caster.AsUnit : Caster.ObjectAccessor.GetUnit(Caster, target.TargetGuid);

                            unit?.RemoveOwnedAura(SpellInfo.Id, _originalCasterGuid, AuraRemoveMode.Cancel);
                        }
                    }

                    if (_timer > 0)
                    {
                        UpdateEmpoweredSpell(difftime);

                        if (difftime >= _timer)
                            _timer = 0;
                        else
                            _timer -= (int)difftime;
                    }
                }

                if (_timer == 0)
                {
                    EndEmpoweredSpell();
                    SendChannelUpdate(0);
                    Finish();

                    // We call the hook here instead of in Spell::finish because we only want to call it for completed channeling. Everything else is handled by interrupts
                    var creatureCaster = Caster.AsCreature;

                    if (creatureCaster is { IsAIEnabled: true })
                        creatureCaster.AI.OnChannelFinished(SpellInfo);
                }

                break;
            }
        }
    }

    private static void FillSpellCastFailedArgs<T>(T packet, ObjectGuid castId, SpellInfo spellInfo, SpellCastResult result, SpellCustomErrors customError, int? param1, int? param2, Player caster) where T : CastFailedBase
    {
        packet.CastID = castId;
        packet.SpellID = (int)spellInfo.Id;
        packet.Reason = result;

        switch (result)
        {
            case SpellCastResult.NotReady:
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    packet.FailedArg1 = 0; // unknown (value 1 update cooldowns on client Id)

                break;

            case SpellCastResult.RequiresSpellFocus:
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    packet.FailedArg1 = (int)spellInfo.RequiresSpellFocus; // SpellFocusObject.dbc id

                break;

            case SpellCastResult.RequiresArea: // AreaTable.dbc id
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    // hardcode areas limitation case
                    packet.FailedArg1 = spellInfo.Id switch
                    {
                        41617 => // Cenarion Mana Salve
                            3905,
                        41619 => // Cenarion Healing Salve
                            3905,
                        41618 => // Bottled Nethergon Energy
                            3842,
                        41620 => // Bottled Nethergon Vapor
                            3842,
                        45373 => // Bloodberry Elixir
                            4075,
                        _ => 0
                    };

                break;

            case SpellCastResult.Totems:
                if (param1.HasValue)
                {
                    packet.FailedArg1 = (int)param1;

                    if (param2.HasValue)
                        packet.FailedArg2 = (int)param2;
                }
                else
                {
                    if (spellInfo.Totem[0] != 0)
                        packet.FailedArg1 = (int)spellInfo.Totem[0];

                    if (spellInfo.Totem[1] != 0)
                        packet.FailedArg2 = (int)spellInfo.Totem[1];
                }

                break;

            case SpellCastResult.TotemCategory:
                if (param1.HasValue)
                {
                    packet.FailedArg1 = (int)param1;

                    if (param2.HasValue)
                        packet.FailedArg2 = (int)param2;
                }
                else
                {
                    if (spellInfo.TotemCategory[0] != 0)
                        packet.FailedArg1 = (int)spellInfo.TotemCategory[0];

                    if (spellInfo.TotemCategory[1] != 0)
                        packet.FailedArg2 = (int)spellInfo.TotemCategory[1];
                }

                break;

            case SpellCastResult.EquippedItemClass:
            case SpellCastResult.EquippedItemClassMainhand:
            case SpellCastResult.EquippedItemClassOffhand:
                if (param1.HasValue && param2.HasValue)
                {
                    packet.FailedArg1 = (int)param1;
                    packet.FailedArg2 = (int)param2;
                }
                else
                {
                    packet.FailedArg1 = (int)spellInfo.EquippedItemClass;
                    packet.FailedArg2 = spellInfo.EquippedItemSubClassMask;
                }

                break;

            case SpellCastResult.TooManyOfItem:
            {
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                {
                    uint item = 0;

                    foreach (var spellEffectInfo in spellInfo.Effects)
                        if (spellEffectInfo.ItemType != 0)
                            item = spellEffectInfo.ItemType;

                    var proto = caster.GameObjectManager.GetItemTemplate(item);

                    if (proto != null && proto.ItemLimitCategory != 0)
                        packet.FailedArg1 = (int)proto.ItemLimitCategory;
                }

                break;
            }
            case SpellCastResult.PreventedByMechanic:
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    packet.FailedArg1 = (int)spellInfo.GetAllEffectsMechanicMask(); // SpellMechanic.dbc id

                break;

            case SpellCastResult.NeedExoticAmmo:
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    packet.FailedArg1 = spellInfo.EquippedItemSubClassMask; // seems correct...

                break;

            case SpellCastResult.NeedMoreItems:
                if (param1.HasValue && param2.HasValue)
                {
                    packet.FailedArg1 = (int)param1;
                    packet.FailedArg2 = (int)param2;
                }
                else
                {
                    packet.FailedArg1 = 0; // Item id
                    packet.FailedArg2 = 0; // Item count?
                }

                break;

            case SpellCastResult.MinSkill:
                if (param1.HasValue && param2.HasValue)
                {
                    packet.FailedArg1 = (int)param1;
                    packet.FailedArg2 = (int)param2;
                }
                else
                {
                    packet.FailedArg1 = 0; // SkillLine.dbc id
                    packet.FailedArg2 = 0; // required skill value
                }

                break;

            case SpellCastResult.FishingTooLow:
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    packet.FailedArg1 = 0; // required fishing skill

                break;

            case SpellCastResult.CustomError:
                packet.FailedArg1 = (int)customError;

                break;

            case SpellCastResult.Silenced:
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    packet.FailedArg1 = 0; // Unknown

                break;

            case SpellCastResult.Reagents:
            {
                if (param1.HasValue)
                    packet.FailedArg1 = (int)param1;
                else
                    for (uint i = 0; i < SpellConst.MaxReagents; i++)
                    {
                        if (spellInfo.Reagent[i] <= 0)
                            continue;

                        var itemid = (uint)spellInfo.Reagent[i];
                        var itemcount = spellInfo.ReagentCount[i];

                        if (!caster.HasItemCount(itemid, itemcount))
                        {
                            packet.FailedArg1 = (int)itemid; // first missing item

                            break;
                        }
                    }

                if (param2.HasValue)
                    packet.FailedArg2 = (int)param2;
                else if (!param1.HasValue)
                    foreach (var reagentsCurrency in spellInfo.ReagentsCurrency)
                        if (!caster.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
                        {
                            packet.FailedArg1 = -1;
                            packet.FailedArg2 = reagentsCurrency.CurrencyTypesID;

                            break;
                        }

                break;
            }
            case SpellCastResult.CantUntalent:
            {
                if (param1 != null)
                    packet.FailedArg1 = (int)param1;

                break;
            }
            // TODO: SPELL_FAILED_NOT_STANDING
        }
    }

    private static bool ProcessScript(int effIndex, bool preventDefault, ISpellScript script, ISpellEffect effect, SpellScriptHookType hookType)
    {
        try
        {
            script._InitHit();

            script._PrepareScriptCall(hookType);

            if (!script._IsEffectPrevented(effIndex))
                if (effect is ISpellEffectHandler seh)
                    seh.CallEffect(effIndex);

            if (!preventDefault)
                preventDefault = script._IsDefaultEffectPrevented(effIndex);

            script._FinishScriptCall();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex);
        }

        return preventDefault;
    }

    private void _cast(bool skipCheck = false)
    {
        if (!UpdatePointers())
        {
            // cancel the spell if UpdatePointers() returned false, something wrong happened there
            Cancel();

            return;
        }

        // cancel at lost explicit target during cast
        if (!Targets.ObjectTargetGUID.IsEmpty && Targets.ObjectTarget == null)
        {
            Cancel();

            return;
        }

        var playerCaster = Caster.AsPlayer;

        if (playerCaster != null)
        {
            // now that we've done the basic check, now run the scripts
            // should be done before the spell is actually executed
            _scriptManager.ForEach<IPlayerOnSpellCast>(playerCaster.Class, p => p.OnSpellCast(playerCaster, this, skipCheck));

            // As of 3.0.2 pets begin attacking their owner's target immediately
            // Let any pets know we've attacked something. Check DmgClass for harmful spells only
            // This prevents spells such as Hunter's Mark from triggering pet attack
            if (SpellInfo.DmgClass != SpellDmgClass.None)
            {
                var target = Targets.UnitTarget;

                if (target != null)
                    foreach (var controlled in playerCaster.Controlled)
                    {
                        var cControlled = controlled.AsCreature;

                        var controlledAI = cControlled?.AI;

                        controlledAI?.OwnerAttacked(target);
                    }
            }
        }

        SetExecutedCurrently(true);

        // Should this be done for original caster?
        var modOwner = Caster.SpellModOwner;

        modOwner?.SetSpellModTakingSpell(this, true);

        CallScriptBeforeCastHandlers();

        // skip check if done already (for instant cast spells for example)
        if (!skipCheck)
        {
            void CleanupSpell(SpellCastResult result, int? param1 = null, int? param2 = null)
            {
                SendCastResult(result, param1, param2);
                SendInterrupted(0);

                modOwner?.SetSpellModTakingSpell(this, false);

                Finish(result);
                SetExecutedCurrently(false);
            }

            int param1 = 0, param2 = 0;
            var castResult = CheckCast(false, ref param1, ref param2);

            if (castResult != SpellCastResult.SpellCastOk)
            {
                CleanupSpell(castResult, param1, param2);

                return;
            }

            // additional check after cast bar completes (must not be in CheckCast)
            // if trade not complete then remember it in trade data
            if (Convert.ToBoolean(Targets.TargetMask & SpellCastTargetFlags.TradeItem))
            {
                var myTrade = modOwner?.TradeData;

                if (myTrade is { IsInAcceptProcess: false })
                {
                    // Spell will be casted at completing the trade. Silently ignore at this place
                    myTrade.SetSpell(SpellInfo.Id, CastItem);
                    CleanupSpell(SpellCastResult.DontReport);

                    return;
                }
            }

            // check diminishing returns (again, only after finish cast bar, tested on retail)
            var target = Targets.UnitTarget;

            if (target != null)
            {
                var isAura = SpellInfo.Effects.Any(spellEffectInfo => spellEffectInfo.IsUnitOwnedAuraEffect);

                if (isAura)
                    if (SpellInfo.DiminishingReturnsGroupForSpell != 0)
                    {
                        var type = SpellInfo.DiminishingReturnsGroupType;

                        if (type == DiminishingReturnsType.All || (type == DiminishingReturnsType.Player && target.IsAffectedByDiminishingReturns))
                        {
                            var caster1 = OriginalCaster ?? Caster.AsUnit;

                            if (caster1 != null)
                                if (target.HasStrongerAuraWithDr(SpellInfo, caster1))
                                {
                                    CleanupSpell(SpellCastResult.AuraBounced);

                                    return;
                                }
                        }
                    }
            }
        }

        // The spell focusing is making sure that we have a valid cast target guid when we need it so only check for a guid value here.
        var creatureCaster = Caster.AsCreature;

        if (creatureCaster != null)
            if (!creatureCaster.Target.IsEmpty && !creatureCaster.HasUnitFlag(UnitFlags.Possessed))
            {
                WorldObject target = Caster.ObjectAccessor.GetUnit(creatureCaster, creatureCaster.Target);

                if (target != null)
                    creatureCaster.SetInFront(target);
            }

        SelectSpellTargets();

        // Spell may be finished after target map check
        if (State == SpellState.Finished)
        {
            SendInterrupted(0);

            if (Caster.IsTypeId(TypeId.Player))
                Caster.AsPlayer.SetSpellModTakingSpell(this, false);

            Finish(SpellCastResult.Interrupted);
            SetExecutedCurrently(false);

            return;
        }

        var unitCaster = Caster.AsUnit;

        if (unitCaster != null)
            if (SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
            {
                var pet = ObjectAccessor.GetCreature(Caster, unitCaster.PetGUID);

                pet?.DespawnOrUnsummon();
            }

        PrepareTriggersExecutedOnHit();

        CallScriptOnCastHandlers();

        // traded items have trade slot instead of guid in m_itemTargetGUID
        // set to real guid to be sent later to the client
        Targets.UpdateTradeSlotItem();

        var player = Caster.AsPlayer;

        if (player != null)
        {
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem) && CastItem != null)
            {
                player.StartCriteriaTimer(CriteriaStartEvent.UseItem, CastItem.Entry);
                player.UpdateCriteria(CriteriaType.UseItem, CastItem.Entry);
            }

            player.UpdateCriteria(CriteriaType.CastSpell, SpellInfo.Id);
        }

        var targetItem = Targets.ItemTarget;

        if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
        {
            // Powers have to be taken before SendSpellGo
            TakePower();
            TakeReagents(); // we must remove reagents before HandleEffects to allow place crafted item in same slot
        }
        else if (targetItem != null)
            // Not own traded item (in trader trade slot) req. reagents including triggered spell case
            if (targetItem.OwnerGUID != Caster.GUID)
                TakeReagents();

        // CAST SPELL
        if (!SpellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
            SendSpellCooldown();

        if (SpellInfo.LaunchDelay == 0)
        {
            HandleLaunchPhase();
            _launchHandled = true;
        }

        // we must send smsg_spell_go packet before m_castItem delete in TakeCastItem()...
        SendSpellGo();

        if (!SpellInfo.IsChanneled)
            creatureCaster?.ReleaseSpellFocus(this);

        // Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
        if ((SpellInfo.HasHitDelay && !SpellInfo.IsChanneled) || SpellInfo.HasAttribute(SpellAttr4.NoHarmfulThreat))
        {
            // Remove used for cast item if need (it can be already NULL after TakeReagents call
            // in case delayed spell remove item at cast delay start
            TakeCastItem();

            // Okay, maps created, now prepare flags
            _immediateHandled = false;
            State = SpellState.Delayed;
            DelayStart = 0;

            unitCaster = Caster.AsUnit;

            if (unitCaster != null)
                if (unitCaster.HasUnitState(UnitState.Casting) && !unitCaster.IsNonMeleeSpellCast(false, false, true))
                    unitCaster.ClearUnitState(UnitState.Casting);
        }
        else
            // Immediate spell, no big deal
            HandleImmediate();

        CallScriptAfterCastHandlers();

        var spellTriggered = _spellManager.GetSpellLinked(SpellLinkedType.Cast, SpellInfo.Id);

        if (spellTriggered != null)
            foreach (var spellId in spellTriggered)
                if (spellId < 0)
                {
                    unitCaster = Caster.AsUnit;

                    unitCaster?.RemoveAura((uint)-spellId);
                }
                else
                    Caster.SpellFactory.CastSpell(Targets.UnitTarget ?? Caster, (uint)spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(this));

        if (modOwner != null)
        {
            modOwner.SetSpellModTakingSpell(this, false);

            //Clear spell cooldowns after every spell is cast if .cheat cooldown is enabled.
            if (OriginalCaster != null && modOwner.GetCommandStatus(PlayerCommandStates.Cooldown))
            {
                OriginalCaster.SpellHistory.ResetCooldown(SpellInfo.Id, true);
                OriginalCaster.SpellHistory.RestoreCharge(SpellInfo.ChargeCategoryId);
            }
        }

        SetExecutedCurrently(false);

        if (OriginalCaster == null)
            return;

        // Handle procs on cast
        var procAttacker = ProcAttacker;

        if (!procAttacker)
        {
            if (SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                procAttacker.Or(IsPositive ? ProcFlags.DealHelpfulPeriodic : ProcFlags.DealHarmfulPeriodic);
            else if (SpellInfo.HasAttribute(SpellAttr0.IsAbility))
                procAttacker.Or(IsPositive ? ProcFlags.DealHelpfulAbility : ProcFlags.DealHarmfulSpell);
            else
                procAttacker.Or(IsPositive ? ProcFlags.DealHelpfulSpell : ProcFlags.DealHarmfulSpell);
        }

        procAttacker.Or(ProcFlags2.CastSuccessful);

        var hitMask = HitMask;

        if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
            hitMask |= ProcFlagsHit.Normal;

        if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) && !SpellInfo.HasAttribute(SpellAttr2.NotAnAction))
            OriginalCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ActionDelayed, SpellInfo);

        if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
            _combatHelpers.ProcSkillsAndAuras(OriginalCaster, null, procAttacker, new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

        // Call CreatureAI hook OnSpellCast
        var caster = OriginalCaster.AsCreature;

        if (caster == null)
            return;

        if (caster.IsAIEnabled)
            caster.AI.OnSpellCast(SpellInfo);
    }

    private void _handle_finish_phase()
    {
        var unitCaster = Caster.AsUnit;

        if (unitCaster != null)
        {
            // Take for real after all targets are processed
            if (NeedComboPoints)
                unitCaster.ClearComboPoints();

            // Real add combo points from effects
            if (ComboPointGain != 0)
                unitCaster.AddComboPoints(ComboPointGain);

            if (SpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                unitCaster.SetLastExtraAttackSpell(SpellInfo.Id);
        }

        // Handle procs on finish
        if (OriginalCaster == null)
            return;

        var procAttacker = ProcAttacker;

        if (!procAttacker)
        {
            if (SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                procAttacker.Or(IsPositive ? ProcFlags.DealHelpfulPeriodic : ProcFlags.DealHarmfulPeriodic);
            else if (SpellInfo.HasAttribute(SpellAttr0.IsAbility))
                procAttacker.Or(IsPositive ? ProcFlags.DealHelpfulAbility : ProcFlags.DealHarmfulAbility);
            else
                procAttacker.Or(IsPositive ? ProcFlags.DealHelpfulSpell : ProcFlags.DealHarmfulSpell);
        }

        if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
            _combatHelpers.ProcSkillsAndAuras(OriginalCaster, null, procAttacker, new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Finish, HitMask, this, null, null);
    }

    private void _handle_immediate_phase()
    {
        // handle some immediate features of the spell here
        HandleThreatSpells();

        // handle effects with SPELL_EFFECT_HANDLE_HIT mode
        foreach (var spellEffectInfo in SpellInfo.Effects.Where(spellEffectInfo => spellEffectInfo.IsEffect))
            // call effect handlers to handle destination hit
            HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Hit);

        // process items
        DoProcessTargetContainer(_uniqueItemInfo);
    }

    private void AddCorpseTarget(Corpse corpse, int effIndex)
    {
        AddCorpseTarget(corpse,
                        new HashSet<int>
                        {
                            effIndex
                        });
    }

    private void AddCorpseTarget(Corpse corpse, HashSet<int> effMask)
    {
        var effectMask = effMask.ToHashSet();

        foreach (var spellEffectInfo in SpellInfo.Effects.Where(spellEffectInfo => !spellEffectInfo.IsEffect))
            effectMask.Remove(spellEffectInfo.EffectIndex);

        // no effects left
        if (effectMask.Count == 0)
            return;

        var targetGUID = corpse.GUID;

        // Lookup target in already in list
        var corpseTargetInfo = _uniqueCorpseTargetInfo.Find(target => target.TargetGuid == targetGUID);

        if (corpseTargetInfo != null) // Found in list
        {
            // Add only effect mask
            corpseTargetInfo.Effects.UnionWith(effectMask);

            return;
        }

        // This is new target calculate data for him
        CorpseTargetInfo target = new()
        {
            TargetGuid = targetGUID,
            Effects = effectMask
        };

        // Spell have speed -need calculate incoming time
        if (Caster != corpse)
        {
            var hitDelay = SpellInfo.LaunchDelay;

            if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                hitDelay += SpellInfo.Speed;
            else if (SpellInfo.Speed > 0.0f)
            {
                // calculate spell incoming interval
                var dist = Math.Max(Caster.Location.GetDistance(corpse.Location.X, corpse.Location.Y, corpse.Location.Z), 5.0f);
                hitDelay += dist / SpellInfo.Speed;
            }

            target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
        }
        else
            target.TimeDelay = 0;

        // Calculate minimum incoming time
        if (target.TimeDelay != 0 && (DelayMoment == 0 || DelayMoment > target.TimeDelay))
            DelayMoment = target.TimeDelay;

        // Add target to list
        _uniqueCorpseTargetInfo.Add(target);
    }

    private void AddDestTarget(SpellDestination dest, int effIndex)
    {
        _destTargets[effIndex] = dest;
    }

    private void AddGOTarget(GameObject go, int effIndex)
    {
        AddGOTarget(go,
                    new HashSet<int>
                    {
                        effIndex
                    });
    }

    private void AddGOTarget(GameObject go, HashSet<int> effMask)
    {
        var effectMask = effMask.ToHashSet();

        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (!spellEffectInfo.IsEffect || !CheckEffectTarget(go, spellEffectInfo))
                effectMask.Remove(spellEffectInfo.EffectIndex);

        // no effects left
        if (effectMask.Count == 0)
            return;

        var targetGUID = go.GUID;

        // Lookup target in already in list
        var index = _uniqueGoTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);

        if (index != -1) // Found in list
        {
            // Add only effect mask
            _uniqueGoTargetInfo[index].Effects.UnionWith(effectMask);

            return;
        }

        // This is new target calculate data for him
        GOTargetInfo target = new()
        {
            TargetGUID = targetGUID,
            Effects = effectMask
        };

        // Spell have speed -need calculate incoming time
        if (Caster != go)
        {
            var hitDelay = SpellInfo.LaunchDelay;

            if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                hitDelay += SpellInfo.Speed;
            else if (SpellInfo.Speed > 0.0f)
            {
                // calculate spell incoming interval
                var dist = Math.Max(Caster.Location.GetDistance(go.Location.X, go.Location.Y, go.Location.Z), 5.0f);
                hitDelay += dist / SpellInfo.Speed;
            }

            target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
        }
        else
            target.TimeDelay = 0UL;

        // Calculate minimum incoming time
        if (target.TimeDelay != 0 && (DelayMoment == 0 || DelayMoment > target.TimeDelay))
            DelayMoment = target.TimeDelay;

        // Add target to list
        _uniqueGoTargetInfo.Add(target);
    }

    private void AddItemTarget(Item item, int effectIndex)
    {
        AddItemTarget(item,
                      new HashSet<int>
                      {
                          effectIndex
                      });
    }

    private void AddItemTarget(Item item, HashSet<int> effMask)
    {
        var effectMask = effMask.ToHashSet();

        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (!spellEffectInfo.IsEffect || !CheckEffectTarget(spellEffectInfo))
                effectMask.Remove(spellEffectInfo.EffectIndex);

        // no effects left
        if (effectMask.Count == 0)
            return;

        // Lookup target in already in list
        var index = _uniqueItemInfo.FindIndex(target => target.TargetItem == item);

        if (index != -1) // Found in list
        {
            // Add only effect mask
            _uniqueItemInfo[index].Effects.UnionWith(effectMask);

            return;
        }

        // This is new target add data

        ItemTargetInfo target = new()
        {
            TargetItem = item,
            Effects = effectMask
        };

        _uniqueItemInfo.Add(target);
    }

    private void AddSpellEffect(int index, ISpellScript script, ISpellEffect effect)
    {
        if (!_effectHandlers.TryGetValue(index, out var effecTypes))
        {
            effecTypes = new Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>();
            _effectHandlers.Add(index, effecTypes);
        }

        if (!effecTypes.TryGetValue(effect.HookType, out var effects))
        {
            effects = new List<(ISpellScript, ISpellEffect)>();
            effecTypes.Add(effect.HookType, effects);
        }

        effects.Add((script, effect));
    }

    private void AddUnitTarget(Unit target, int effIndex, bool checkIfValid = true, bool @implicit = true, Position losPosition = null)
    {
        AddUnitTarget(target,
                      new HashSet<int>
                      {
                          effIndex
                      },
                      checkIfValid,
                      @implicit,
                      losPosition);
    }

    private void AddUnitTarget(Unit target, HashSet<int> efftMask, bool checkIfValid = true, bool @implicit = true, Position losPosition = null)
    {
        var removeEffect = efftMask.ToHashSet();

        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (!spellEffectInfo.IsEffect || !CheckEffectTarget(target, spellEffectInfo, losPosition))
                removeEffect.Remove(spellEffectInfo.EffectIndex);

        if (removeEffect.Count == 0)
            return;

        if (checkIfValid)
            if (SpellInfo.CheckTarget(Caster, target, @implicit) != SpellCastResult.SpellCastOk) // skip stealth checks for AOE
                return;

        // Check for effect immune skip if immuned
        foreach (var spellEffectInfo in SpellInfo.Effects.Where(spellEffectInfo => target.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, Caster)))
            removeEffect.Remove(spellEffectInfo.EffectIndex);

        var targetGUID = target.GUID;

        // Lookup target in already in list
        var index = UniqueTargetInfo.FindIndex(uniqueTarget => uniqueTarget.TargetGuid == targetGUID);

        if (index != -1) // Found in list
        {
            // Immune effects removed from mask
            UniqueTargetInfo[index].Effects.UnionWith(removeEffect);

            return;
        }

        // remove immunities

        // This is new target calculate data for him

        // Get spell hit result on target
        TargetInfo targetInfo = new()
        {
            TargetGuid = targetGUID, // Store target GUID
            Effects = removeEffect,  // Store all effects not immune
            IsAlive = target.IsAlive
        };

        // Calculate hit result
        var caster = OriginalCaster ?? Caster;
        targetInfo.MissCondition = caster.WorldObjectCombat.SpellHitResult(target, SpellInfo, _canReflect && !(IsPositive && Caster.WorldObjectCombat.IsFriendlyTo(target)));

        // Spell have speed - need calculate incoming time
        // Incoming time is zero for self casts. At least I think so.
        if (Caster != target)
        {
            var hitDelay = SpellInfo.LaunchDelay;
            var missileSource = Caster;

            if (SpellInfo.HasAttribute(SpellAttr4.BouncyChainMissiles))
            {
                var mask = removeEffect.ToMask();
                var previousTargetInfo = UniqueTargetInfo.FindLast(uniqueTarget => (uniqueTarget.Effects.ToMask() & mask) != 0);

                if (previousTargetInfo != null)
                {
                    hitDelay = 0.0f; // this is not the first target in chain, LaunchDelay was already included

                    var previousTarget = Caster.ObjectAccessor.GetWorldObject(Caster, previousTargetInfo.TargetGuid);

                    if (previousTarget != null)
                        missileSource = previousTarget;

                    targetInfo.TimeDelay += previousTargetInfo.TimeDelay;
                }
            }

            if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                hitDelay += SpellInfo.Speed;
            else if (SpellInfo.Speed > 0.0f)
            {
                // calculate spell incoming interval
                // @todo this is a hack
                var dist = Math.Max(missileSource.Location.GetDistance(target.Location.X, target.Location.Y, target.Location.Z), 5.0f);
                hitDelay += dist / SpellInfo.Speed;
            }

            targetInfo.TimeDelay += (ulong)Math.Floor(hitDelay * 1000.0f);
        }
        else
            targetInfo.TimeDelay = 0L;

        // If target reflect spell back to caster
        if (targetInfo.MissCondition == SpellMissInfo.Reflect)
        {
            // Calculate reflected spell result on caster (shouldn't be able to reflect gameobject spells)
            var unitCaster = Caster.AsUnit;
            targetInfo.ReflectResult = unitCaster.WorldObjectCombat.SpellHitResult(unitCaster, SpellInfo); // can't reflect twice

            // Proc spell reflect aura when missile hits the original target
            target.Events.AddEvent(new ProcReflectDelayed(target, _originalCasterGuid), target.Events.CalculateTime(TimeSpan.FromMilliseconds(targetInfo.TimeDelay)));

            // Increase time interval for reflected spells by 1.5
            targetInfo.TimeDelay += targetInfo.TimeDelay >> 1;
        }
        else
            targetInfo.ReflectResult = SpellMissInfo.None;

        // Calculate minimum incoming time
        if (targetInfo.TimeDelay != 0 && (DelayMoment == 0 || DelayMoment > targetInfo.TimeDelay))
            DelayMoment = targetInfo.TimeDelay;

        // Add target to list
        UniqueTargetInfo.Add(targetInfo);
        UniqueTargetInfoOrgi.Add(targetInfo);
    }

    private double CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target)
    {
        return CalculateDamage(spellEffectInfo, target, out _);
    }

    private double CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target, out double variance)
    {
        var needRecalculateBasePoints = (SpellValue.CustomBasePointsMask & (1 << spellEffectInfo.EffectIndex)) == 0;

        return CalculateSpellDamage(out variance, target, spellEffectInfo, needRecalculateBasePoints ? null : SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex], CastItemEntry, CastItemLevel);
    }

    private ulong CalculateDelayMomentForDst(float launchDelay)
    {
        if (Targets.HasDst)
        {
            if (Targets.HasTraj)
            {
                var speed = Targets.SpeedXY;

                if (speed > 0.0f)
                    return (ulong)Math.Floor((Targets.Dist2d / speed + launchDelay) * 1000.0f);
            }
            else if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                return (ulong)Math.Floor((SpellInfo.Speed + launchDelay) * 1000.0f);
            else if (SpellInfo.Speed > 0.0f)
            {
                // We should not subtract caster size from dist calculation (fixes execution time desync with animation on client, eg. Malleable Goo cast by PP)
                var dist = Caster.Location.GetExactDist(Targets.DstPos);

                return (ulong)Math.Floor((dist / SpellInfo.Speed + launchDelay) * 1000.0f);
            }

            return (ulong)Math.Floor(launchDelay * 1000.0f);
        }

        return 0;
    }

    private void CallScriptAfterCastHandlers()
    {
        foreach (var script in GetSpellScripts<ISpellAfterCast>())
            try
            {
                script._PrepareScriptCall(SpellScriptHookType.AfterCast);

                ((ISpellAfterCast)script).AfterCast();

                script._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private void CallScriptBeforeCastHandlers()
    {
        foreach (var script in GetSpellScripts<ISpellBeforeCast>())
            try
            {
                script._PrepareScriptCall(SpellScriptHookType.BeforeCast);

                ((ISpellBeforeCast)script).BeforeCast();

                script._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private int CallScriptCalcCastTimeHandlers(int castTime)
    {
        foreach (var script in GetSpellScripts<ISpellCalculateCastTime>())
            try
            {
                script._PrepareScriptCall(SpellScriptHookType.CalcCastTime);
                castTime = ((ISpellCalculateCastTime)script).CalcCastTime(castTime);
                script._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return castTime;
    }

    private SpellCastResult CallScriptCheckCastHandlers()
    {
        var retVal = SpellCastResult.SpellCastOk;

        foreach (var script in GetSpellScripts<ISpellCheckCast>())
            try
            {
                script._PrepareScriptCall(SpellScriptHookType.CheckCast);

                var tempResult = ((ISpellCheckCast)script).CheckCast();

                if (tempResult != SpellCastResult.SpellCastOk)
                    retVal = tempResult;

                script._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return retVal;
    }

    private void CallScriptDestinationTargetSelectHandlers(ref SpellDestination target, int effIndex, SpellImplicitTargetInfo targetType)
    {
        foreach (var script in GetEffectScripts(SpellScriptHookType.DestinationTargetSelect, effIndex))
            try
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.DestinationTargetSelect);

                if (script.Item2 is ISpellDestinationTargetSelectHandler dts)
                    if (targetType.Target == dts.TargetType)
                        dts.SetDest(target);

                script.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private bool CallScriptEffectHandlers(int effIndex, SpellEffectHandleMode mode)
    {
        // execute script effect handler hooks and check if effects was prevented
        var preventDefault = false;

        switch (mode)
        {
            case SpellEffectHandleMode.Launch:

                foreach (var script in GetEffectScripts(SpellScriptHookType.Launch, effIndex))
                    preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Launch);

                break;

            case SpellEffectHandleMode.LaunchTarget:

                foreach (var script in GetEffectScripts(SpellScriptHookType.LaunchTarget, effIndex))
                    preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.LaunchTarget);

                break;

            case SpellEffectHandleMode.Hit:

                foreach (var script in GetEffectScripts(SpellScriptHookType.Hit, effIndex))
                    preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Hit);

                break;

            case SpellEffectHandleMode.HitTarget:

                foreach (var script in GetEffectScripts(SpellScriptHookType.EffectHitTarget, effIndex))
                    preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.EffectHitTarget);

                break;

            default:
                return false;
        }

        return preventDefault;
    }

    private void CallScriptObjectAreaTargetSelectHandlers(List<WorldObject> targets, int effIndex, SpellImplicitTargetInfo targetType)
    {
        foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex))
            try
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectAreaTargetSelect);

                if (script.Item2 is ISpellObjectAreaTargetSelect oas)
                    if (targetType.Target == oas.TargetType)
                        oas.FilterTargets(targets);

                script.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private void CallScriptObjectTargetSelectHandlers(ref WorldObject target, int effIndex, SpellImplicitTargetInfo targetType)
    {
        foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex))
            try
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectTargetSelect);

                if (script.Item2 is ISpellObjectTargetSelectHandler ots)
                    if (targetType.Target == ots.TargetType)
                        ots.TargetSelect(target);

                script.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private void CallScriptOnCastHandlers()
    {
        foreach (var script in GetSpellScripts<ISpellOnCast>())
            try
            {
                script._PrepareScriptCall(SpellScriptHookType.OnCast);

                ((ISpellOnCast)script).OnCast();

                script._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private void CallScriptOnPrecastHandler()
    {
        foreach (var script in GetSpellScripts<ISpellOnPrecast>())
            try
            {
                script._PrepareScriptCall(SpellScriptHookType.OnPrecast);
                ((ISpellOnPrecast)script).OnPrecast();
                script._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private void CallScriptSuccessfulDispel(int effIndex)
    {
        foreach (var script in GetEffectScripts(SpellScriptHookType.EffectSuccessfulDispel, effIndex))
            try
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.EffectSuccessfulDispel);

                if (script.Item2 is ISpellEffectHandler seh)
                    seh.CallEffect(effIndex);

                script.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    private void CancelGlobalCooldown()
    {
        if (!CanHaveGlobalCooldown(Caster))
            return;

        if (SpellInfo.StartRecoveryTime == 0)
            return;

        // Cancel global cooldown when interrupting current cast
        if (Caster.AsUnit.GetCurrentSpell(CurrentSpellTypes.Generic) != this)
            return;

        Caster.AsUnit.SpellHistory.CancelGlobalCooldown(SpellInfo);
    }

    private bool CanHaveGlobalCooldown(WorldObject caster)
    {
        // Only players or controlled units have global cooldown
        if (!caster.IsPlayer && (!caster.IsCreature || caster.AsCreature.GetCharmInfo() == null))
            return false;

        return true;
    }

    private SpellCastResult CanOpenLock(SpellEffectInfo effect, uint lockId, ref SkillType skillId, ref int reqSkillValue, ref int skillValue)
    {
        if (lockId == 0) // possible case for GO and maybe for items.
            return SpellCastResult.SpellCastOk;

        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return SpellCastResult.BadTargets;

        // Get LockInfo
        if (!_cliDb.LockStorage.TryGetValue(lockId, out var lockInfo))
            return SpellCastResult.BadTargets;

        var reqKey = false; // some locks not have reqs

        for (var j = 0; j < SharedConst.MaxLockCase; ++j)
            switch ((LockKeyType)lockInfo.LockType[j])
            {
                // check key item (many fit cases can be)
                case LockKeyType.Item:
                    if (lockInfo.Index[j] != 0 && CastItem != null && CastItem.Entry == lockInfo.Index[j])
                        return SpellCastResult.SpellCastOk;

                    reqKey = true;

                    break;
                // check key skill (only single first fit case can be)
                case LockKeyType.Skill:
                {
                    reqKey = true;

                    // wrong locktype, skip
                    if (effect.MiscValue != lockInfo.Index[j])
                        continue;

                    skillId = SharedConst.SkillByLockType((LockType)lockInfo.Index[j]);

                    if (skillId == SkillType.None && lockInfo.Index[j] != (uint)LockType.Lockpicking)
                        return SpellCastResult.SpellCastOk;

                    reqSkillValue = lockInfo.Skill[j];

                    // castitem check: rogue using skeleton keys. the skill values should not be added in this case.
                    skillValue = 0;

                    if (CastItem == null && unitCaster.IsTypeId(TypeId.Player))
                        skillValue = unitCaster.AsPlayer.GetSkillValue(skillId);
                    else if (lockInfo.Index[j] == (uint)LockType.Lockpicking)
                        skillValue = (int)unitCaster.Level * 5;

                    // skill bonus provided by casting spell (mostly item spells)
                    // add the effect base points modifier from the spell cast (cheat lock / skeleton key etc.)
                    if (effect.TargetA.Target == Framework.Constants.Targets.GameobjectItemTarget || effect.TargetB.Target == Framework.Constants.Targets.GameobjectItemTarget)
                        skillValue += (int)effect.CalcValue();

                    return skillValue < reqSkillValue ? SpellCastResult.LowCastlevel : SpellCastResult.SpellCastOk;
                }
                case LockKeyType.Spell:
                    if (SpellInfo.Id == lockInfo.Index[j])
                        return SpellCastResult.SpellCastOk;

                    reqKey = true;

                    break;
            }

        return reqKey ? SpellCastResult.BadTargets : SpellCastResult.SpellCastOk;
    }

    private SpellCastResult CheckArenaAndRatedBattlegroundCastRules()
    {
        var isRatedBattleground = false; // NYI
        var isArena = !isRatedBattleground;

        // check USABLE attributes
        // USABLE takes precedence over NOT_USABLE
        if (isRatedBattleground && SpellInfo.HasAttribute(SpellAttr9.UsableInRatedBattlegrounds))
            return SpellCastResult.SpellCastOk;

        if (isArena && SpellInfo.HasAttribute(SpellAttr4.IgnoreDefaultArenaRestrictions))
            return SpellCastResult.SpellCastOk;

        // check NOT_USABLE attributes
        if (SpellInfo.HasAttribute(SpellAttr4.NotInArenaOrRatedBattleground))
            return isArena ? SpellCastResult.NotInArena : SpellCastResult.NotInBattleground;

        if (isArena && SpellInfo.HasAttribute(SpellAttr9.NotUsableInArena))
            return SpellCastResult.NotInArena;

        // check cooldowns
        var spellCooldown = SpellInfo.RecoveryTime1;

        if (isArena && spellCooldown > 10 * Time.MINUTE * Time.IN_MILLISECONDS) // not sure if still needed
            return SpellCastResult.NotInArena;

        if (isRatedBattleground && spellCooldown > 15 * Time.MINUTE * Time.IN_MILLISECONDS)
            return SpellCastResult.NotInBattleground;

        return SpellCastResult.SpellCastOk;
    }

    private SpellCastResult CheckCasterAuras(ref int paramOne)
    {
        var unitCaster = OriginalCaster ?? Caster.AsUnit;

        if (unitCaster == null)
            return SpellCastResult.SpellCastOk;

        // these attributes only show the spell as usable on the client when it has related aura applied
        // still they need to be checked against certain mechanics

        // SPELL_ATTR5_USABLE_WHILE_STUNNED by default only MECHANIC_STUN (ie no sleep, knockout, freeze, etc.)
        var usableWhileStunned = SpellInfo.HasAttribute(SpellAttr5.AllowWhileStunned);

        // SPELL_ATTR5_USABLE_WHILE_FEARED by default only fear (ie no horror)
        var usableWhileFeared = SpellInfo.HasAttribute(SpellAttr5.AllowWhileFleeing);

        // SPELL_ATTR5_USABLE_WHILE_CONFUSED by default only disorient (ie no polymorph)
        var usableWhileConfused = SpellInfo.HasAttribute(SpellAttr5.AllowWhileConfused);

        // Check whether the cast should be prevented by any state you might have.
        var result = SpellCastResult.SpellCastOk;
        // Get unit state
        var unitflag = (UnitFlags)(uint)unitCaster.UnitData.Flags;

        // this check should only be done when player does cast directly
        // (ie not when it's called from a script) Breaks for example PlayerAI when charmed
        /*if (!unitCaster.GetCharmerGUID().IsEmpty())
        {
            Unit charmer = unitCaster.GetCharmer();
            if (charmer)
                if (charmer.GetUnitBeingMoved() != unitCaster && !CheckSpellCancelsCharm(ref paramOne))
                    result = SpellCastResult.Charmed;
        }*/

        // spell has attribute usable while having a cc state, check if caster has allowed mechanic auras, another mechanic types must prevent cast spell
        SpellCastResult MechanicCheck(AuraType auraType, ref int param1)
        {
            var foundNotMechanic = false;
            var auras = unitCaster.GetAuraEffectsByType(auraType);

            foreach (var aurEff in auras)
            {
                var mechanicMask = aurEff.SpellInfo.GetAllEffectsMechanicMask();

                if (mechanicMask != 0 && !Convert.ToBoolean(mechanicMask & SpellInfo.AllowedMechanicMask))
                {
                    foundNotMechanic = true;

                    // fill up aura mechanic info to send client proper error message
                    param1 = (int)aurEff.SpellEffectInfo.Mechanic;

                    if (param1 == 0)
                        param1 = (int)aurEff.SpellInfo.Mechanic;

                    break;
                }
            }

            if (foundNotMechanic)
                return auraType switch
                {
                    AuraType.ModStun               => SpellCastResult.Stunned,
                    AuraType.ModStunDisableGravity => SpellCastResult.Stunned,
                    AuraType.ModFear               => SpellCastResult.Fleeing,
                    AuraType.ModConfuse            => SpellCastResult.Confused,
                    _                              => SpellCastResult.NotKnown
                };

            return SpellCastResult.SpellCastOk;
        }

        if (unitflag.HasAnyFlag(UnitFlags.Stunned))
        {
            if (usableWhileStunned)
            {
                var mechanicResult = MechanicCheck(AuraType.ModStun, ref paramOne);

                if (mechanicResult != SpellCastResult.SpellCastOk)
                    result = mechanicResult;
            }
            else if (!CheckSpellCancelsStun(ref paramOne))
                result = SpellCastResult.Stunned;
            else if ((SpellInfo.Mechanic & Mechanics.ImmuneShield) != 0 && Caster.IsUnit && Caster.AsUnit.HasAuraWithMechanic(1 << (int)Mechanics.Banish))
                result = SpellCastResult.Stunned;
        }
        else if (unitCaster.IsSilenced(SpellSchoolMask) && SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) && !CheckSpellCancelsSilence(ref paramOne))
            result = SpellCastResult.Silenced;
        else if (unitflag.HasAnyFlag(UnitFlags.Pacified) && SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) && !CheckSpellCancelsPacify(ref paramOne))
            result = SpellCastResult.Pacified;
        else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
        {
            if (usableWhileFeared)
            {
                var mechanicResult = MechanicCheck(AuraType.ModFear, ref paramOne);

                if (mechanicResult != SpellCastResult.SpellCastOk)
                    result = mechanicResult;
                else
                {
                    mechanicResult = MechanicCheck(AuraType.ModStunDisableGravity, ref paramOne);

                    if (mechanicResult != SpellCastResult.SpellCastOk)
                        result = mechanicResult;
                }
            }
            else if (!CheckSpellCancelsFear(ref paramOne))
                result = SpellCastResult.Fleeing;
        }
        else if (unitflag.HasAnyFlag(UnitFlags.Confused))
        {
            if (usableWhileConfused)
            {
                var mechanicResult = MechanicCheck(AuraType.ModConfuse, ref paramOne);

                if (mechanicResult != SpellCastResult.SpellCastOk)
                    result = mechanicResult;
            }
            else if (!CheckSpellCancelsConfuse(ref paramOne))
                result = SpellCastResult.Confused;
        }
        else if (unitCaster.HasUnitFlag2(UnitFlags2.NoActions) && SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions) && !CheckSpellCancelsNoActions(ref paramOne))
            result = SpellCastResult.NoActions;

        // Attr must make Id drop spell totally immune from all effects
        if (result != SpellCastResult.SpellCastOk)
            return paramOne != 0 ? SpellCastResult.PreventedByMechanic : result;

        return SpellCastResult.SpellCastOk;
    }

    private void CheckDst()
    {
        if (!Targets.HasDst) Targets.SetDst(Caster);
    }

    private bool CheckEffectTarget(Unit target, SpellEffectInfo spellEffectInfo, Position losPosition)
    {
        if (spellEffectInfo == null || !spellEffectInfo.IsEffect)
            return false;

        switch (spellEffectInfo.ApplyAuraName)
        {
            case AuraType.ModPossess:
            case AuraType.ModCharm:
            case AuraType.ModPossessPet:
            case AuraType.AoeCharm:
                if (target.VehicleKit != null && target.VehicleKit.IsControllableVehicle)
                    return false;

                if (target.IsMounted)
                    return false;

                if (!target.CharmerGUID.IsEmpty)
                    return false;

                var damage = CalculateDamage(spellEffectInfo, target);

                if (damage != 0)
                    if (target.GetLevelForTarget(Caster) > damage)
                        return false;

                break;
        }

        // check for ignore LOS on the effect itself
        if (SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Caster.DisableManager.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS))
            return true;

        // check if gameobject ignores LOS
        var gobCaster = Caster.AsGameObject;

        if (gobCaster != null)
            if (gobCaster.Template.GetRequireLOS() == 0)
                return true;

        // if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
        if (!SpellInfo.HasAttribute(SpellAttr5.AlwaysLineOfSight) && IsTriggered && TriggeredByAuraSpell != null && (TriggeredByAuraSpell.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Caster.DisableManager.IsDisabledFor(DisableType.Spell, TriggeredByAuraSpell.Id, null, (byte)DisableFlags.SpellLOS)))
            return true;

        // @todo shit below shouldn't be here, but it's temporary
        //Check targets for LOS visibility
        switch (spellEffectInfo.Effect)
        {
            case SpellEffectName.SkinPlayerCorpse:
            {
                if (Targets.CorpseTargetGUID.IsEmpty)
                {
                    if (target.Location.IsWithinLOSInMap(Caster, LineOfSightChecks.All, ModelIgnoreFlags.M2) && target.HasUnitFlag(UnitFlags.Skinnable))
                        return true;

                    return false;
                }

                var corpse = ObjectAccessor.GetCorpse(Caster, Targets.CorpseTargetGUID);

                if (corpse == null)
                    return false;

                if (target.GUID != corpse.OwnerGUID)
                    return false;

                if (!corpse.HasCorpseDynamicFlag(CorpseDynFlags.Lootable))
                    return false;

                if (!corpse.Location.IsWithinLOSInMap(Caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                    return false;

                break;
            }
            default:
            {
                if (losPosition == null || SpellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight))
                {
                    // Get GO cast coordinates if original caster . GO
                    WorldObject caster = null;

                    if (_originalCasterGuid.IsGameObject)
                        caster = Caster.Location.Map.GetGameObject(_originalCasterGuid);

                    if (caster == null)
                        caster = Caster;

                    if (target != Caster && !target.Location.IsWithinLOSInMap(caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        return false;
                }

                if (losPosition != null)
                    if (!target.Location.IsWithinLOS(losPosition.X, losPosition.Y, losPosition.Z, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        return false;

                break;
            }
        }

        return true;
    }

    private bool CheckEffectTarget(GameObject target, SpellEffectInfo spellEffectInfo)
    {
        if (spellEffectInfo == null || !spellEffectInfo.IsEffect)
            return false;

        switch (spellEffectInfo.Effect)
        {
            case SpellEffectName.GameObjectDamage:
            case SpellEffectName.GameobjectRepair:
            case SpellEffectName.GameobjectSetDestructionState:
                if (target.GoType != GameObjectTypes.DestructibleBuilding)
                    return false;

                break;
        }

        return true;
    }

    private bool CheckEffectTarget(SpellEffectInfo spellEffectInfo)
    {
        return spellEffectInfo != null && spellEffectInfo.IsEffect;
    }

    private SpellCastResult CheckItems(ref int param1, ref int param2)
    {
        var player = Caster.AsPlayer;

        if (player == null)
            return SpellCastResult.SpellCastOk;

        if (CastItem == null)
        {
            if (!CastItemGuid.IsEmpty)
                return SpellCastResult.ItemNotReady;
        }
        else
        {
            var itemid = CastItem.Entry;

            if (!player.HasItemCount(itemid))
                return SpellCastResult.ItemNotReady;

            var proto = CastItem.Template;

            if (proto == null)
                return SpellCastResult.ItemNotReady;

            foreach (var itemEffect in CastItem.Effects)
                if (itemEffect.LegacySlotIndex < CastItem.ItemData.SpellCharges.GetSize() && itemEffect.Charges != 0)
                    if (CastItem.GetSpellCharges(itemEffect.LegacySlotIndex) == 0)
                        return SpellCastResult.NoChargesRemain;

            // consumable cast item checks
            if (proto.Class == ItemClass.Consumable && Targets.UnitTarget != null)
            {
                // such items should only fail if there is no suitable effect at all - see Rejuvenation Potions for example
                var failReason = SpellCastResult.SpellCastOk;

                foreach (var spellEffectInfo in SpellInfo.Effects)
                {
                    // skip check, pet not required like checks, and for TARGET_UNIT_PET m_targets.GetUnitTarget() is not the real target but the caster
                    if (spellEffectInfo.TargetA.Target == Framework.Constants.Targets.UnitPet)
                        continue;

                    if (spellEffectInfo.Effect == SpellEffectName.Heal)
                    {
                        if (Targets.UnitTarget.IsFullHealth)
                        {
                            failReason = SpellCastResult.AlreadyAtFullHealth;

                            continue;
                        }

                        failReason = SpellCastResult.SpellCastOk;

                        break;
                    }

                    // Mana Potion, Rage Potion, Thistle Tea(Rogue), ...
                    if (spellEffectInfo.Effect != SpellEffectName.Energize)
                        continue;

                    if (spellEffectInfo.MiscValue is < 0 or >= (int)PowerType.Max)
                    {
                        failReason = SpellCastResult.AlreadyAtFullPower;

                        continue;
                    }

                    var power = (PowerType)spellEffectInfo.MiscValue;

                    if (Targets.UnitTarget.GetPower(power) == Targets.UnitTarget.GetMaxPower(power))
                        failReason = SpellCastResult.AlreadyAtFullPower;
                    else
                    {
                        failReason = SpellCastResult.SpellCastOk;

                        break;
                    }
                }

                if (failReason != SpellCastResult.SpellCastOk)
                    return failReason;
            }
        }

        // check target item
        if (!Targets.ItemTargetGuid.IsEmpty)
        {
            var item = Targets.ItemTarget;

            if (item == null)
                return SpellCastResult.ItemGone;

            if (!item.IsFitToSpellRequirements(SpellInfo))
                return SpellCastResult.EquippedItemClass;
        }
        // if not item target then required item must be equipped
        else
        {
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement))
                if (Caster.IsTypeId(TypeId.Player) && !Caster.AsPlayer.HasItemFitToSpellRequirements(SpellInfo))
                    return SpellCastResult.EquippedItemClass;
        }

        // do not take reagents for these item casts
        if (!(CastItem != null && CastItem.Template.HasFlag(ItemFlags.NoReagentCost)))
        {
            var checkReagents = !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost) && !player.CanNoReagentCast(SpellInfo);

            // Not own traded item (in trader trade slot) requires reagents even if triggered spell
            if (!checkReagents)
            {
                var targetItem = Targets.ItemTarget;

                if (targetItem != null)
                    if (targetItem.OwnerGUID != player.GUID)
                        checkReagents = true;
            }

            // check reagents (ignore triggered spells with reagents processed by original spell) and special reagent ignore case.
            if (checkReagents)
            {
                for (byte i = 0; i < SpellConst.MaxReagents; i++)
                {
                    if (SpellInfo.Reagent[i] <= 0)
                        continue;

                    var itemid = (uint)SpellInfo.Reagent[i];
                    var itemcount = SpellInfo.ReagentCount[i];

                    // if CastItem is also spell reagent
                    if (CastItem != null && CastItem.Entry == itemid)
                    {
                        var proto = CastItem.Template;

                        if (proto == null)
                            return SpellCastResult.ItemNotReady;

                        foreach (var itemEffect in CastItem.Effects)
                        {
                            if (itemEffect.LegacySlotIndex >= CastItem.ItemData.SpellCharges.GetSize())
                                continue;

                            // CastItem will be used up and does not count as reagent
                            var charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                            if (itemEffect.Charges >= 0 || Math.Abs(charges) >= 2)
                                continue;

                            ++itemcount;

                            break;
                        }
                    }

                    if (player.HasItemCount(itemid, itemcount))
                        continue;

                    param1 = (int)itemid;

                    return SpellCastResult.Reagents;
                }

                foreach (var reagentsCurrency in SpellInfo.ReagentsCurrency)
                    if (!player.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
                    {
                        param1 = -1;
                        param2 = reagentsCurrency.CurrencyTypesID;

                        return SpellCastResult.Reagents;
                    }
            }

            // check totem-item requirements (items presence in inventory)
            uint totems = 2;

            for (var i = 0; i < 2; ++i)
                if (SpellInfo.Totem[i] != 0)
                {
                    if (player.HasItemCount(SpellInfo.Totem[i]))
                        totems -= 1;
                }
                else
                    totems -= 1;

            if (totems != 0)
                return SpellCastResult.Totems;

            // Check items for TotemCategory (items presence in inventory)
            uint totemCategory = 2;

            for (byte i = 0; i < 2; ++i)
                if (SpellInfo.TotemCategory[i] != 0)
                {
                    if (player.HasItemTotemCategory(SpellInfo.TotemCategory[i]))
                        totemCategory -= 1;
                }
                else
                    totemCategory -= 1;

            if (totemCategory != 0)
                return SpellCastResult.TotemCategory;
        }

        // special checks for spell effects
        foreach (var spellEffectInfo in SpellInfo.Effects)
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.CreateItem:
                case SpellEffectName.CreateLoot:
                {
                    // m_targets.GetUnitTarget() means explicit cast, otherwise we dont check for possible equip error
                    var target = Targets.UnitTarget ?? player;

                    if (target.IsPlayer && !IsTriggered)
                    {
                        // SPELL_EFFECT_CREATE_ITEM_2 differs from SPELL_EFFECT_CREATE_ITEM in that it picks the random item to create from a pool of potential items,
                        // so we need to make sure there is at least one free space in the player's inventory
                        if (spellEffectInfo.Effect == SpellEffectName.CreateLoot)
                            if (target.AsPlayer.GetFreeInventorySpace() == 0)
                            {
                                player.SendEquipError(InventoryResult.InvFull, null, null, spellEffectInfo.ItemType);

                                return SpellCastResult.DontReport;
                            }

                        if (spellEffectInfo.ItemType != 0)
                        {
                            var itemTemplate = _gameObjectManager.GetItemTemplate(spellEffectInfo.ItemType);

                            if (itemTemplate == null)
                                return SpellCastResult.ItemNotFound;

                            var createCount = (uint)Math.Clamp(spellEffectInfo.CalcValue(), 1u, itemTemplate.MaxStackSize);

                            List<ItemPosCount> dest = new();
                            var msg = target.AsPlayer.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, createCount);

                            if (msg != InventoryResult.Ok)
                            {
                                // @todo Needs review
                                if (itemTemplate.ItemLimitCategory == 0)
                                {
                                    player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

                                    return SpellCastResult.DontReport;
                                }

                                // Conjure Food/Water/Refreshment spells
                                if (SpellInfo.SpellFamilyName != SpellFamilyNames.Mage || !SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u))
                                    return SpellCastResult.TooManyOfItem;

                                if (!target.AsPlayer.HasItemCount(spellEffectInfo.ItemType))
                                {
                                    player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

                                    return SpellCastResult.DontReport;
                                }

                                if (SpellInfo.Effects.Count > 1)
                                    player.SpellFactory.CastSpell(player,
                                                                  (uint)SpellInfo.GetEffect(1).CalcValue(),
                                                                  new CastSpellExtraArgs()
                                                                      .SetTriggeringSpell(this)); // move this to anywhere

                                return SpellCastResult.DontReport;
                            }
                        }
                    }

                    break;
                }
                case SpellEffectName.EnchantItem:
                    if (spellEffectInfo.ItemType != 0 && Targets.ItemTarget is { IsVellum: true })
                    {
                        // cannot enchant vellum for other player
                        if (Targets.ItemTarget.OwnerUnit != player)
                            return SpellCastResult.NotTradeable;

                        // do not allow to enchant vellum from scroll made by vellum-prevent exploit
                        if (CastItem != null && CastItem.Template.HasFlag(ItemFlags.NoReagentCost))
                            return SpellCastResult.TotemCategory;

                        List<ItemPosCount> dest = new();
                        var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, 1);

                        if (msg != InventoryResult.Ok)
                        {
                            player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

                            return SpellCastResult.DontReport;
                        }
                    }

                    goto case SpellEffectName.EnchantItemPrismatic;
                case SpellEffectName.EnchantItemPrismatic:
                {
                    var targetItem = Targets.ItemTarget;

                    if (targetItem == null)
                        return SpellCastResult.ItemNotFound;

                    // required level has to be checked also! Exploit fix
                    if (targetItem.GetItemLevel(targetItem.OwnerUnit) < SpellInfo.BaseLevel || (targetItem.GetRequiredLevel() != 0 && targetItem.GetRequiredLevel() < SpellInfo.BaseLevel))
                        return SpellCastResult.Lowlevel;

                    var isItemUsable = false;

                    foreach (var itemEffect in targetItem.Effects)
                        if (itemEffect.SpellID != 0 && itemEffect.TriggerType == ItemSpelltriggerType.OnUse)
                        {
                            isItemUsable = true;

                            break;
                        }

                    var enchantEntry = _cliDb.SpellItemEnchantmentStorage.LookupByKey(spellEffectInfo.MiscValue);

                    // do not allow adding usable enchantments to items that have use effect already
                    if (enchantEntry != null)
                        for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                            switch (enchantEntry.Effect[s])
                            {
                                case ItemEnchantmentType.UseSpell:
                                    if (isItemUsable)
                                        return SpellCastResult.OnUseEnchant;

                                    break;

                                case ItemEnchantmentType.PrismaticSocket:
                                {
                                    uint numSockets = 0;

                                    for (uint socket = 0; socket < ItemConst.MaxGemSockets; ++socket)
                                        if (targetItem.GetSocketColor(socket) != 0)
                                            ++numSockets;

                                    if (numSockets == ItemConst.MaxGemSockets || targetItem.GetEnchantmentId(EnchantmentSlot.Prismatic) != 0)
                                        return SpellCastResult.MaxSockets;

                                    break;
                                }
                            }

                    // Not allow enchant in trade slot for some enchant type
                    if (targetItem.OwnerUnit != player)
                    {
                        if (enchantEntry == null)
                            return SpellCastResult.Error;

                        if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
                            return SpellCastResult.NotTradeable;
                    }

                    break;
                }
                case SpellEffectName.EnchantItemTemporary:
                {
                    var item = Targets.ItemTarget;

                    if (item == null)
                        return SpellCastResult.ItemNotFound;

                    // Not allow enchant in trade slot for some enchant type
                    if (item.OwnerUnit != player)
                    {
                        var enchantID = spellEffectInfo.MiscValue;

                        if (!_cliDb.SpellItemEnchantmentStorage.TryGetValue((uint)enchantID, out var enchantEntry))
                            return SpellCastResult.Error;

                        if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
                            return SpellCastResult.NotTradeable;
                    }

                    // Apply item level restriction if the enchanting spell has max level restrition set
                    if (CastItem != null && SpellInfo.MaxLevel > 0)
                    {
                        if (item.Template.BaseItemLevel < CastItem.Template.BaseRequiredLevel)
                            return SpellCastResult.Lowlevel;

                        if (item.Template.BaseItemLevel > SpellInfo.MaxLevel)
                            return SpellCastResult.Highlevel;
                    }

                    break;
                }
                case SpellEffectName.EnchantHeldItem:
                    // check item existence in effect code (not output errors at offhand hold item effect to main hand for example
                    break;

                case SpellEffectName.Disenchant:
                {
                    var item = Targets.ItemTarget;

                    if (item == null)
                        return SpellCastResult.CantBeSalvaged;

                    // prevent disenchanting in trade slot
                    if (item.OwnerGUID != player.GUID)
                        return SpellCastResult.CantBeSalvaged;

                    var itemProto = item.Template;

                    if (itemProto == null)
                        return SpellCastResult.CantBeSalvaged;

                    var itemDisenchantLoot = item.GetDisenchantLoot(Caster.AsPlayer);

                    if (itemDisenchantLoot == null)
                        return SpellCastResult.CantBeSalvaged;

                    if (itemDisenchantLoot.SkillRequired > player.GetSkillValue(SkillType.Enchanting))
                        return SpellCastResult.CantBeSalvagedSkill;

                    break;
                }
                case SpellEffectName.Prospecting:
                {
                    var item = Targets.ItemTarget;

                    if (item == null)
                        return SpellCastResult.CantBeProspected;

                    //ensure item is a prospectable ore
                    if (!item.Template.HasFlag(ItemFlags.IsProspectable))
                        return SpellCastResult.CantBeProspected;

                    //prevent prospecting in trade slot
                    if (item.OwnerGUID != player.GUID)
                        return SpellCastResult.CantBeProspected;

                    //Check for enough skill in jewelcrafting
                    var itemProspectingskilllevel = item.Template.RequiredSkillRank;

                    if (itemProspectingskilllevel > player.GetSkillValue(SkillType.Jewelcrafting))
                        return SpellCastResult.LowCastlevel;

                    //make sure the player has the required ores in inventory
                    if (item.Count < 5)
                    {
                        param1 = (int)item.Entry;
                        param2 = 5;

                        return SpellCastResult.NeedMoreItems;
                    }

                    if (!_lootStoreBox.Prospecting.HaveLootFor(Targets.ItemTargetEntry))
                        return SpellCastResult.CantBeProspected;

                    break;
                }
                case SpellEffectName.Milling:
                {
                    var item = Targets.ItemTarget;

                    if (item == null)
                        return SpellCastResult.CantBeMilled;

                    //ensure item is a millable herb
                    if (!item.Template.HasFlag(ItemFlags.IsMillable))
                        return SpellCastResult.CantBeMilled;

                    //prevent milling in trade slot
                    if (item.OwnerGUID != player.GUID)
                        return SpellCastResult.CantBeMilled;

                    //Check for enough skill in inscription
                    var itemMillingskilllevel = item.Template.RequiredSkillRank;

                    if (itemMillingskilllevel > player.GetSkillValue(SkillType.Inscription))
                        return SpellCastResult.LowCastlevel;

                    //make sure the player has the required herbs in inventory
                    if (item.Count < 5)
                    {
                        param1 = (int)item.Entry;
                        param2 = 5;

                        return SpellCastResult.NeedMoreItems;
                    }

                    if (!_lootStoreBox.Milling.HaveLootFor(Targets.ItemTargetEntry))
                        return SpellCastResult.CantBeMilled;

                    break;
                }
                case SpellEffectName.WeaponDamage:
                case SpellEffectName.WeaponDamageNoSchool:
                {
                    if (AttackType != WeaponAttackType.RangedAttack)
                        break;

                    var item = player.GetWeaponForAttack(AttackType);

                    if (item == null || item.IsBroken)
                        return SpellCastResult.EquippedItem;

                    switch ((ItemSubClassWeapon)item.Template.SubClass)
                    {
                        case ItemSubClassWeapon.Thrown:
                        {
                            var ammo = item.Entry;

                            if (!player.HasItemCount(ammo))
                                return SpellCastResult.NoAmmo;

                            break;
                        }
                        case ItemSubClassWeapon.Gun:
                        case ItemSubClassWeapon.Bow:
                        case ItemSubClassWeapon.Crossbow:
                        case ItemSubClassWeapon.Wand:
                            break;
                    }

                    break;
                }
                case SpellEffectName.RechargeItem:
                {
                    var itemId = spellEffectInfo.ItemType;

                    var proto = _gameObjectManager.GetItemTemplate(itemId);

                    if (proto == null)
                        return SpellCastResult.ItemAtMaxCharges;

                    var item = player.GetItemByEntry(itemId);

                    if (item != null)
                        if (item.Effects.Any(itemEffect => itemEffect.LegacySlotIndex <= item.ItemData.SpellCharges.GetSize() && itemEffect.Charges != 0 && item.GetSpellCharges(itemEffect.LegacySlotIndex) == itemEffect.Charges))
                            return SpellCastResult.ItemAtMaxCharges;

                    break;
                }
                case SpellEffectName.RespecAzeriteEmpoweredItem:
                {
                    var item = Targets.ItemTarget;

                    if (item == null)
                        return SpellCastResult.AzeriteEmpoweredOnly;

                    if (item.OwnerGUID != Caster.GUID)
                        return SpellCastResult.DontReport;

                    var azeriteEmpoweredItem = item.AsAzeriteEmpoweredItem;

                    if (azeriteEmpoweredItem == null)
                        return SpellCastResult.AzeriteEmpoweredOnly;

                    var hasSelections = false;

                    for (var tier = 0; tier < SharedConst.MaxAzeriteEmpoweredTier; ++tier)
                        if (azeriteEmpoweredItem.GetSelectedAzeritePower(tier) != 0)
                        {
                            hasSelections = true;

                            break;
                        }

                    if (!hasSelections)
                        return SpellCastResult.AzeriteEmpoweredNoChoicesToUndo;

                    if (!Caster.AsPlayer.HasEnoughMoney(azeriteEmpoweredItem.GetRespecCost()))
                        return SpellCastResult.DontReport;

                    break;
                }
            }

        // check weapon presence in slots for main/offhand weapons
        if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement) && SpellInfo.EquippedItemClass >= 0)
        {
            var weaponCheck = new Func<WeaponAttackType, SpellCastResult>(attackType =>
            {
                var item = player.AsPlayer.GetWeaponForAttack(attackType);

                // skip spell if no weapon in slot or broken
                if (item == null || item.IsBroken)
                    return SpellCastResult.EquippedItemClass;

                // skip spell if weapon not fit to triggered spell
                return !item.IsFitToSpellRequirements(SpellInfo) ? SpellCastResult.EquippedItemClass : SpellCastResult.SpellCastOk;
            });

            // main hand weapon required
            if (SpellInfo.HasAttribute(SpellAttr3.RequiresMainHandWeapon))
            {
                var mainHandResult = weaponCheck(WeaponAttackType.BaseAttack);

                if (mainHandResult != SpellCastResult.SpellCastOk)
                    return mainHandResult;
            }

            // offhand hand weapon required
            if (!SpellInfo.HasAttribute(SpellAttr3.RequiresOffHandWeapon))
                return SpellCastResult.SpellCastOk;

            var offHandResult = weaponCheck(WeaponAttackType.OffAttack);

            if (offHandResult != SpellCastResult.SpellCastOk)
                return offHandResult;
        }

        return SpellCastResult.SpellCastOk;
    }

    private SpellCastResult CheckPower()
    {
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return SpellCastResult.SpellCastOk;

        // item cast not used power
        if (CastItem != null)
            return SpellCastResult.SpellCastOk;

        foreach (var cost in PowerCost)
        {
            switch (cost.Power)
            {
                // health as power used - need check health amount
                case PowerType.Health when unitCaster.Health <= cost.Amount:
                    return SpellCastResult.CasterAurastate;

                case PowerType.Health:
                    continue;
                // Check valid power type
                case >= PowerType.Max:
                    Log.Logger.Error("Spell.CheckPower: Unknown power type '{0}'", cost.Power);

                    return SpellCastResult.Unknown;
                //check rune cost only if a spell has PowerType == POWER_RUNES
                case PowerType.Runes:
                {
                    var failReason = CheckRuneCost();

                    if (failReason != SpellCastResult.SpellCastOk)
                        return failReason;

                    break;
                }
            }

            // Check power amount
            if (unitCaster.GetPower(cost.Power) < cost.Amount)
                return SpellCastResult.NoPower;
        }

        return SpellCastResult.SpellCastOk;
    }

    private SpellCastResult CheckRange(bool strict)
    {
        // Don't check for instant cast spells
        if (!strict && CastTime == 0)
            return SpellCastResult.SpellCastOk;

        var (minRange, maxRange) = GetMinMaxRange(strict);

        // dont check max_range to strictly after cast
        if (SpellInfo.RangeEntry != null && SpellInfo.RangeEntry.Flags != SpellRangeFlag.Melee && !strict)
            maxRange += Math.Min(3.0f, maxRange * 0.1f); // 10% but no more than 3.0f

        // get square values for sqr distance checks
        minRange *= minRange;
        maxRange *= maxRange;

        var target = Targets.UnitTarget;

        if (target != null && target != Caster)
        {
            if (Caster.Location.GetExactDistSq(target.Location) > maxRange)
                return SpellCastResult.OutOfRange;

            if (minRange > 0.0f && Caster.Location.GetExactDistSq(target.Location) < minRange)
                return SpellCastResult.OutOfRange;

            if (Caster.IsTypeId(TypeId.Player) &&
                SpellInfo.FacingCasterFlags.HasAnyFlag(1u) &&
                !Caster.Location.HasInArc((float)Math.PI, target.Location) &&
                !Caster.AsPlayer.IsWithinBoundaryRadius(target))
                return SpellCastResult.UnitNotInfront;
        }

        var goTarget = Targets.GOTarget;

        if (goTarget != null)
            if (!goTarget.IsAtInteractDistance(Caster.AsPlayer, SpellInfo))
                return SpellCastResult.OutOfRange;

        if (!Targets.HasDst || Targets.HasTraj)
            return SpellCastResult.SpellCastOk;

        if (Caster.Location.GetExactDistSq(Targets.DstPos) > maxRange)
            return SpellCastResult.OutOfRange;

        if (minRange > 0.0f && Caster.Location.GetExactDistSq(Targets.DstPos) < minRange)
            return SpellCastResult.OutOfRange;

        return SpellCastResult.SpellCastOk;
    }

    private SpellCastResult CheckRuneCost()
    {
        var runeCost = PowerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

        if (runeCost == 0)
            return SpellCastResult.SpellCastOk;

        var player = Caster.AsPlayer;

        if (player is not { Class: PlayerClass.Deathknight })
            return SpellCastResult.SpellCastOk;

        var readyRunes = 0;

        for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
            if (player.GetRuneCooldown(i) == 0)
                ++readyRunes;

        return readyRunes < runeCost
                   ? SpellCastResult.NoPower
                   : // not sure if result code is correct
                   SpellCastResult.SpellCastOk;
    }

    private bool CheckScriptEffectImplicitTargets(int effIndex, int effIndexToCheck)
    {
        // Skip if there are not any script
        if (_loadedScripts.Empty())
            return true;

        var otsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex).Count > 0;
        var otsEffIndexCheck = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndexToCheck).Count > 0;

        var oatsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex).Count > 0;
        var oatsEffIndexCheck = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndexToCheck).Count > 0;

        if ((otsTargetEffIndex && !otsEffIndexCheck) ||
            (!otsTargetEffIndex && otsEffIndexCheck))
            return false;

        return (!oatsTargetEffIndex || oatsEffIndexCheck) &&
               (oatsTargetEffIndex || !oatsEffIndexCheck);
    }

    private bool CheckSpellCancelsAuraEffect(AuraType auraType, ref int param1)
    {
        var unitCaster = OriginalCaster ?? Caster.AsUnit;

        if (unitCaster == null)
            return false;

        // Checking auras is needed now, because you are prevented by some state but the spell grants immunity.
        var auraEffects = unitCaster.GetAuraEffectsByType(auraType);

        if (auraEffects.Empty())
            return true;

        foreach (var aurEff in auraEffects)
        {
            if (SpellInfo.SpellCancelsAuraEffect(aurEff))
                continue;

            param1 = (int)aurEff.SpellEffectInfo.Mechanic;

            if (param1 == 0)
                param1 = (int)aurEff.SpellInfo.Mechanic;

            return false;
        }

        return true;
    }

    private bool CheckSpellCancelsConfuse(ref int param1)
    {
        return CheckSpellCancelsAuraEffect(AuraType.ModConfuse, ref param1);
    }

    private bool CheckSpellCancelsFear(ref int param1)
    {
        return CheckSpellCancelsAuraEffect(AuraType.ModFear, ref param1);
    }

    private bool CheckSpellCancelsNoActions(ref int param1)
    {
        return CheckSpellCancelsAuraEffect(AuraType.ModNoActions, ref param1);
    }

    private bool CheckSpellCancelsPacify(ref int param1)
    {
        return CheckSpellCancelsAuraEffect(AuraType.ModPacify, ref param1) ||
               CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
    }

    private bool CheckSpellCancelsSilence(ref int param1)
    {
        return CheckSpellCancelsAuraEffect(AuraType.ModSilence, ref param1) ||
               CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
    }

    private bool CheckSpellCancelsStun(ref int param1)
    {
        return CheckSpellCancelsAuraEffect(AuraType.ModStun, ref param1) &&
               CheckSpellCancelsAuraEffect(AuraType.ModStunDisableGravity, ref param1);
    }

    private bool CheckSpellEffectHandler(ISpellEffectHandler se, int effIndex)
    {
        if (SpellInfo.Effects.Count <= effIndex)
            return false;

        var spellEffectInfo = SpellInfo.GetEffect(effIndex);

        return CheckSpellEffectHandler(se, spellEffectInfo);
    }

    private bool CheckSpellEffectHandler(ISpellEffectHandler se, SpellEffectInfo spellEffectInfo)
    {
        return spellEffectInfo.Effect switch
        {
            0 when se.EffectName == 0 => true,
            0                         => false,
            _                         => se.EffectName == SpellEffectName.Any || spellEffectInfo.Effect == se.EffectName
        };
    }

    private void DoEffectOnLaunchTarget(TargetInfo targetInfo, double multiplier, SpellEffectInfo spellEffectInfo)
    {
        Unit unit = null;

        // In case spell hit target, do all effect on that target
        if (targetInfo.MissCondition == SpellMissInfo.None || (targetInfo.MissCondition == SpellMissInfo.Block && !SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked)))
            unit = Caster.GUID == targetInfo.TargetGuid ? Caster.AsUnit : Caster.ObjectAccessor.GetUnit(Caster, targetInfo.TargetGuid);
        // In case spell reflect from target, do all effect on caster (if hit)
        else if (targetInfo.MissCondition == SpellMissInfo.Reflect && targetInfo.ReflectResult == SpellMissInfo.None)
            unit = Caster.AsUnit;

        if (unit == null)
            return;

        DamageInEffects = 0;
        HealingInEffects = 0;

        HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.LaunchTarget);

        if (OriginalCaster != null && DamageInEffects > 0)
            if (spellEffectInfo.IsTargetingArea || spellEffectInfo.IsAreaAuraEffect || spellEffectInfo.IsEffectName(SpellEffectName.PersistentAreaAura) || SpellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
            {
                DamageInEffects = unit.CalculateAoeAvoidance(DamageInEffects, (uint)SpellInfo.SchoolMask, OriginalCaster.GUID);

                if (OriginalCaster.IsPlayer)
                {
                    // cap damage of player AOE
                    var targetAmount = GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);

                    if (targetAmount > 20)
                        DamageInEffects = (int)(DamageInEffects * 20 / targetAmount);
                }
            }

        if (_applyMultiplierMask.Contains(spellEffectInfo.EffectIndex))
        {
            DamageInEffects = (int)(DamageInEffects * _damageMultipliers[spellEffectInfo.EffectIndex]);
            HealingInEffects = (int)(HealingInEffects * _damageMultipliers[spellEffectInfo.EffectIndex]);

            _damageMultipliers[spellEffectInfo.EffectIndex] *= multiplier;
        }

        targetInfo.Damage += DamageInEffects;
        targetInfo.Healing += HealingInEffects;
    }

    private void DoProcessTargetContainer<T>(List<T> targetContainer) where T : TargetInfoBase
    {
        foreach (TargetInfoBase target in targetContainer)
            target.PreprocessTarget(this);

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            foreach (TargetInfoBase target in targetContainer)
                if (target.Effects.Contains(spellEffectInfo.EffectIndex))
                    target.DoTargetSpellHit(this, spellEffectInfo);
        }

        foreach (TargetInfoBase target in targetContainer)
            target.DoDamageAndTriggers(this);
    }

    private void ExecuteLogEffectCreateItem(SpellEffectName effect, uint entry)
    {
        SpellLogEffectTradeSkillItemParams spellLogEffectTradeSkillItemParams;
        spellLogEffectTradeSkillItemParams.ItemID = (int)entry;

        GetExecuteLogEffect(effect).TradeSkillTargets.Add(spellLogEffectTradeSkillItemParams);
    }

    private void ExecuteLogEffectDestroyItem(SpellEffectName effect, uint entry)
    {
        SpellLogEffectFeedPetParams spellLogEffectFeedPetParams;
        spellLogEffectFeedPetParams.ItemID = (int)entry;

        GetExecuteLogEffect(effect).FeedPetTargets.Add(spellLogEffectFeedPetParams);
    }

    private void ExecuteLogEffectDurabilityDamage(SpellEffectName effect, Unit victim, int itemId, int amount)
    {
        SpellLogEffectDurabilityDamageParams spellLogEffectDurabilityDamageParams;
        spellLogEffectDurabilityDamageParams.Victim = victim.GUID;
        spellLogEffectDurabilityDamageParams.ItemID = itemId;
        spellLogEffectDurabilityDamageParams.Amount = amount;

        GetExecuteLogEffect(effect).DurabilityDamageTargets.Add(spellLogEffectDurabilityDamageParams);
    }

    private void ExecuteLogEffectExtraAttacks(SpellEffectName effect, Unit victim, uint numAttacks)
    {
        SpellLogEffectExtraAttacksParams spellLogEffectExtraAttacksParams;
        spellLogEffectExtraAttacksParams.Victim = victim.GUID;
        spellLogEffectExtraAttacksParams.NumAttacks = numAttacks;

        GetExecuteLogEffect(effect).ExtraAttacksTargets.Add(spellLogEffectExtraAttacksParams);
    }

    private void ExecuteLogEffectOpenLock(SpellEffectName effect, WorldObject obj)
    {
        SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
        spellLogEffectGenericVictimParams.Victim = obj.GUID;

        GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
    }

    private void ExecuteLogEffectResurrect(SpellEffectName effect, Unit target)
    {
        SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
        spellLogEffectGenericVictimParams.Victim = target.GUID;

        GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
    }

    private void ExecuteLogEffectSummonObject(SpellEffectName effect, WorldObject obj)
    {
        SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
        spellLogEffectGenericVictimParams.Victim = obj.GUID;

        GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
    }

    private void ExecuteLogEffectTakeTargetPower(SpellEffectName effect, Unit target, PowerType powerType, uint points, double amplitude)
    {
        SpellLogEffectPowerDrainParams spellLogEffectPowerDrainParams;

        spellLogEffectPowerDrainParams.Victim = target.GUID;
        spellLogEffectPowerDrainParams.Points = points;
        spellLogEffectPowerDrainParams.PowerType = (uint)powerType;
        spellLogEffectPowerDrainParams.Amplitude = (float)amplitude;

        GetExecuteLogEffect(effect).PowerDrainTargets.Add(spellLogEffectPowerDrainParams);
    }

    private void ExecuteLogEffectUnsummonObject(SpellEffectName effect, WorldObject obj)
    {
        SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
        spellLogEffectGenericVictimParams.Victim = obj.GUID;

        GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
    }

    private void FinishTargetProcessing()
    {
        SendSpellExecuteLog();
    }

    private string GetDebugInfo()
    {
        return $"Id: {SpellInfo.Id} Name: '{SpellInfo.SpellName[_worldManager.DefaultDbcLocale]}' OriginalCaster: {_originalCasterGuid} State: {State}";
    }

    private (float minRange, float maxRange) GetMinMaxRange(bool strict)
    {
        var rangeMod = 0.0f;
        var minRange = 0.0f;
        var maxRange = 0.0f;

        if (strict && SpellInfo.IsNextMeleeSwingSpell)
            return (0.0f, 100.0f);

        var unitCaster = Caster.AsUnit;

        if (SpellInfo.RangeEntry != null)
        {
            var target = Targets.UnitTarget;

            if (SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
            {
                // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                if (unitCaster != null)
                    rangeMod = unitCaster.GetMeleeRange(target ?? unitCaster);
            }
            else
            {
                var meleeRange = 0.0f;

                if (SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                    // when the target is not a unit, take the caster's combat reach as the target's combat reach.
                    if (unitCaster != null)
                        meleeRange = unitCaster.GetMeleeRange(target ?? unitCaster);

                minRange = Caster.WorldObjectCombat.GetSpellMinRangeForTarget(target, SpellInfo) + meleeRange;
                maxRange = Caster.WorldObjectCombat.GetSpellMaxRangeForTarget(target, SpellInfo);

                if (target != null || Targets.CorpseTarget != null)
                {
                    rangeMod = Caster.CombatReach + (target?.CombatReach ?? Caster.CombatReach);

                    if (minRange > 0.0f && !SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                        minRange += rangeMod;
                }
            }

            if (target != null &&
                unitCaster is { IsMoving: true } &&
                target.IsMoving &&
                !unitCaster.IsWalking &&
                !target.IsWalking &&
                (SpellInfo.RangeEntry.Flags.HasFlag(SpellRangeFlag.Melee) || target.IsPlayer))
                rangeMod += 8.0f / 3.0f;
        }

        if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && Caster.IsTypeId(TypeId.Player))
        {
            var ranged = Caster.AsPlayer.GetWeaponForAttack(WeaponAttackType.RangedAttack, true);

            if (ranged != null)
                maxRange *= ranged.Template.RangedModRange * 0.01f;
        }

        var modOwner = Caster.SpellModOwner;

        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.Range, ref maxRange, this);

        maxRange += rangeMod;

        return (minRange, maxRange);
    }

    private void HandleImmediate()
    {
        // start channeling if applicable
        if (SpellInfo.IsChanneled)
        {
            if (!TryGetTotalEmpowerDuration(true, out var duration))
                duration = SpellInfo.Duration;

            if (duration > 0 || SpellValue.Duration.HasValue)
            {
                if (!SpellValue.Duration.HasValue)
                {
                    // First mod_duration then haste - see Missile Barrage
                    // Apply duration mod
                    var modOwner = Caster.SpellModOwner;

                    modOwner?.ApplySpellMod(SpellInfo, SpellModOp.Duration, ref duration);

                    duration = (int)(duration * SpellValue.DurationMul);

                    // Apply haste mods
                    Caster.WorldObjectCombat.ModSpellDurationTime(SpellInfo, ref duration, this);
                }
                else
                    duration = SpellValue.Duration.Value;

                _channeledDuration = duration;
                SendChannelStart((uint)duration);
            }
            else if (duration == -1)
                SendChannelStart(unchecked((uint)duration));

            if (duration != 0)
            {
                State = SpellState.Casting;

                // GameObjects shouldn't cast channeled spells
                Caster. // GameObjects shouldn't cast channeled spells
                    AsUnit?.AddInterruptMask(SpellInfo.ChannelInterruptFlags, SpellInfo.ChannelInterruptFlags2);
            }
        }

        PrepareTargetProcessing();

        // process immediate effects (items, ground, etc.) also initialize some variables
        _handle_immediate_phase();

        // consider spell hit for some spells without target, so they may proc on finish phase correctly
        if (UniqueTargetInfo.Empty())
            HitMask = ProcFlagsHit.Normal;
        else
            DoProcessTargetContainer(UniqueTargetInfo);

        DoProcessTargetContainer(_uniqueGoTargetInfo);

        DoProcessTargetContainer(_uniqueCorpseTargetInfo);
        CallScriptOnHitHandlers();

        FinishTargetProcessing();

        // spell is finished, perform some last features of the spell here
        _handle_finish_phase();

        // Remove used for cast item if need (it can be already NULL after TakeReagents call
        TakeCastItem();

        if (State != SpellState.Casting)
            Finish(); // successfully finish spell cast (not last in case autorepeat or channel spell)
    }

    private void HandleLaunchPhase()
    {
        // handle effects with SPELL_EFFECT_HANDLE_LAUNCH mode
        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            // don't do anything for empty effect
            if (!spellEffectInfo.IsEffect)
                continue;

            HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Launch);
        }

        PrepareTargetProcessing();

        foreach (var target in UniqueTargetInfo)
            PreprocessSpellLaunch(target);

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            double multiplier = 1.0f;

            if (_applyMultiplierMask.Contains(spellEffectInfo.EffectIndex))
                multiplier = spellEffectInfo.CalcDamageMultiplier(OriginalCaster, this);

            foreach (var target in UniqueTargetInfo)
            {
                var mask = target.Effects;

                if (!mask.Contains(spellEffectInfo.EffectIndex))
                    continue;

                DoEffectOnLaunchTarget(target, multiplier, spellEffectInfo);
            }
        }

        FinishTargetProcessing();
    }

    private void HandleThreatSpells()
    {
        // wild GameObject spells don't cause threat
        var unitCaster = OriginalCaster ?? Caster.AsUnit;

        if (unitCaster == null)
            return;

        if (UniqueTargetInfo.Empty())
            return;

        if (!SpellInfo.HasInitialAggro)
            return;

        double threat = 0.0f;
        var threatEntry = _spellManager.GetSpellThreatEntry(SpellInfo.Id);

        if (threatEntry != null)
        {
            if (threatEntry.ApPctMod != 0.0f)
                threat += threatEntry.ApPctMod * unitCaster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

            threat += threatEntry.FlatMod;
        }
        else if (!SpellInfo.HasAttribute(SpellCustomAttributes.NoInitialThreat))
            threat += SpellInfo.SpellLevel;

        // past this point only multiplicative effects occur
        if (threat == 0.0f)
            return;

        // since 2.0.1 threat from positive effects also is distributed among all targets, so the overall caused threat is at most the defined bonus
        threat /= UniqueTargetInfo.Count;

        foreach (var ihit in UniqueTargetInfo)
        {
            var threatToAdd = threat;

            if (ihit.MissCondition != SpellMissInfo.None)
                threatToAdd = 0.0f;

            var target = Caster.ObjectAccessor.GetUnit(unitCaster, ihit.TargetGuid);

            if (target == null)
                continue;

            // positive spells distribute threat among all units that are in combat with target, like healing
            if (IsPositive)
                target.GetThreatManager().ForwardThreatForAssistingMe(unitCaster, threatToAdd, SpellInfo);
            // for negative spells threat gets distributed among affected targets
            else
            {
                if (!target.CanHaveThreatList)
                    continue;

                target.GetThreatManager().AddThreat(unitCaster, threatToAdd, SpellInfo, true);
            }
        }

        Log.Logger.Debug("Spell {0}, added an additional {1} threat for {2} {3} target(s)", SpellInfo.Id, threat, IsPositive ? "assisting" : "harming", UniqueTargetInfo.Count);
    }

    private bool HasGlobalCooldown()
    {
        if (!CanHaveGlobalCooldown(Caster))
            return false;

        return Caster.AsUnit.SpellHistory.HasGlobalCooldown(SpellInfo);
    }

    private bool IsAutoActionResetSpell()
    {
        if (IsTriggered)
            return false;

        if (CastTime == 0 && SpellInfo.HasAttribute(SpellAttr6.DoesntResetSwingTimerIfInstant))
            return false;

        return true;
    }

    private bool IsDelayableNoMore()
    {
        if (_delayAtDamageCount >= 2)
            return true;

        ++_delayAtDamageCount;

        return false;
    }

    private bool IsNeedSendToClient()
    {
        return SpellVisual.SpellXSpellVisualID != 0 ||
               SpellVisual.ScriptVisualID != 0 ||
               SpellInfo.IsChanneled ||
               SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) ||
               SpellInfo.HasHitDelay ||
               (TriggeredByAuraSpell == null && !IsTriggered);
    }

    private bool IsValidDeadOrAliveTarget(Unit target)
    {
        if (target.IsAlive)
            return !SpellInfo.IsRequiringDeadTarget;

        if (SpellInfo.IsAllowingDeadTarget)
            return true;

        return false;
    }

    private void LoadScripts()
    {
        _loadedScripts = _scriptManager.CreateSpellScripts(SpellInfo.Id, this);

        foreach (var script in _loadedScripts)
        {
            Log.Logger.Debug("Spell.LoadScripts: Script `{0}` for spell `{1}` is loaded now", script._GetScriptName(), SpellInfo.Id);
            script.Register();

            foreach (var iFace in script.GetType().GetInterfaces())
            {
                if (iFace.Name is nameof(ISpellScript) or nameof(IBaseSpellScript))
                    continue;

                if (!_spellScriptsByType.TryGetValue(iFace, out var spellScripts))
                {
                    spellScripts = new List<ISpellScript>();
                    _spellScriptsByType[iFace] = spellScripts;
                }

                spellScripts.Add(script);
                RegisterSpellEffectHandler(script);
            }
        }
    }

    private void PrepareDataForTriggerSystem()
    {
        //==========================================================================================
        // Now fill data for trigger system, need know:
        // Create base triggers flags for Attacker and Victim (m_procAttacker, m_procVictim and m_hitMask)
        //==========================================================================================

        ProcVictim = ProcAttacker = new ProcFlagsInit();

        // Get data for type of attack and fill base info for trigger
        switch (SpellInfo.DmgClass)
        {
            case SpellDmgClass.Melee:
                ProcAttacker = new ProcFlagsInit(ProcFlags.DealMeleeAbility);

                ProcAttacker.Or(AttackType == WeaponAttackType.OffAttack ? ProcFlags.OffHandWeaponSwing : ProcFlags.MainHandWeaponSwing);

                ProcVictim = new ProcFlagsInit(ProcFlags.TakeMeleeAbility);

                break;

            case SpellDmgClass.Ranged:
                // Auto attack
                if (SpellInfo.HasAttribute(SpellAttr2.AutoRepeat))
                {
                    ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
                    ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
                }
                else // Ranged spell attack
                {
                    ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAbility);
                    ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAbility);
                }

                break;

            default:
                if (SpellInfo.EquippedItemClass == ItemClass.Weapon &&
                    Convert.ToBoolean(SpellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassWeapon.Wand)) &&
                    SpellInfo.HasAttribute(SpellAttr2.AutoRepeat)) // Wands auto attack
                {
                    ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
                    ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
                }

                break;
            // For other spells trigger procflags are set in Spell::TargetInfo::DoDamageAndTriggers
            // Because spell positivity is dependant on target
        }
    }

    private void PrepareTargetProcessing() { }

    private void PrepareTriggersExecutedOnHit()
    {
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return;

        // handle SPELL_AURA_ADD_TARGET_TRIGGER auras:
        // save auras which were present on spell caster on cast, to prevent triggered auras from affecting caster
        // and to correctly calculate proc chance when combopoints are present
        var targetTriggers = unitCaster.GetAuraEffectsByType(AuraType.AddTargetTrigger);

        foreach (var aurEff in targetTriggers)
        {
            if (!aurEff.IsAffectingSpell(SpellInfo))
                continue;

            var spellInfo = _spellManager.GetSpellInfo(aurEff.SpellEffectInfo.TriggerSpell, CastDifficulty);

            if (spellInfo != null)
            {
                // calculate the chance using spell base amount, because aura amount is not updated on combo-points change
                // this possibly needs fixing
                var auraBaseAmount = aurEff.BaseAmount;
                // proc chance is stored in effect amount
                var chance = CalculateSpellDamage(null, aurEff.SpellEffectInfo, auraBaseAmount);
                chance *= aurEff.Base.StackAmount;

                // build trigger and add to the list
                _hitTriggerSpells.Add(new HitTriggerSpell(spellInfo, aurEff.SpellInfo, chance));
            }
        }
    }

    private void PreprocessSpellLaunch(TargetInfo targetInfo)
    {
        var targetUnit = Caster.GUID == targetInfo.TargetGuid ? Caster.AsUnit : Caster.ObjectAccessor.GetUnit(Caster, targetInfo.TargetGuid);

        if (targetUnit == null)
            return;

        // This will only cause combat - the target will engage once the projectile hits (in Spell::TargetInfo::PreprocessTarget)
        if (OriginalCaster != null && targetInfo.MissCondition != SpellMissInfo.Evade && !OriginalCaster.WorldObjectCombat.IsFriendlyTo(targetUnit) && (!SpellInfo.IsPositive || SpellInfo.HasEffect(SpellEffectName.Dispel)) && (SpellInfo.HasInitialAggro || targetUnit.IsEngaged))
            OriginalCaster.SetInCombatWith(targetUnit, true);

        var unit = targetInfo.MissCondition switch
        {
            // In case spell hit target, do all effect on that target
            SpellMissInfo.None => targetUnit,
            // In case spell reflect from target, do all effect on caster (if hit)
            SpellMissInfo.Reflect when targetInfo.ReflectResult == SpellMissInfo.None => Caster.AsUnit,
            _                                                                         => null
        };

        if (unit == null)
            return;

        double critChance = SpellValue.CriticalChance;

        if (OriginalCaster != null)
        {
            if (critChance == 0)
                critChance = OriginalCaster.SpellCritChanceDone(this, null, SpellSchoolMask, AttackType);

            critChance = unit.SpellCritChanceTaken(OriginalCaster, this, null, SpellSchoolMask, critChance, AttackType);
        }

        targetInfo.IsCrit = RandomHelper.randChance(critChance);
    }

    private void RegisterSpellEffectHandler(SpellScript script)
    {
        if (script is not IHasSpellEffects hse)
            return;

        foreach (var effect in hse.SpellEffects)
            switch (effect)
            {
                case ISpellEffectHandler se:
                {
                    var first = false;

                    if (se.EffectIndex is SpellConst.EffectAll or SpellConst.EffectFirstFound)
                        foreach (var effInfo in SpellInfo.Effects)
                        {
                            if (se.EffectIndex == SpellConst.EffectFirstFound && first)
                                break;

                            if (!CheckSpellEffectHandler(se, effInfo))
                                continue;

                            AddSpellEffect(effInfo.EffectIndex, script, se);
                            first = true;
                        }
                    else
                    {
                        if (CheckSpellEffectHandler(se, se.EffectIndex))
                            AddSpellEffect(se.EffectIndex, script, se);
                    }

                    break;
                }
                case ITargetHookHandler th:
                {
                    var first = false;

                    if (th.EffectIndex is SpellConst.EffectAll or SpellConst.EffectFirstFound)
                        foreach (var effInfo in SpellInfo.Effects)
                        {
                            if (th.EffectIndex == SpellConst.EffectFirstFound && first)
                                break;

                            if (!CheckTargetHookEffect(th, effInfo))
                                continue;

                            AddSpellEffect(effInfo.EffectIndex, script, th);
                            first = true;
                        }
                    else
                    {
                        if (CheckTargetHookEffect(th, th.EffectIndex))
                            AddSpellEffect(th.EffectIndex, script, th);
                    }

                    break;
                }
            }
    }

    private void ReSetTimer()
    {
        _timer = CastTime > 0 ? CastTime : 0;
    }

    private void SearchAreaTargets(List<WorldObject> targets, float range, Position position, WorldObject referer, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
    {
        var containerTypeMask = GetSearcherTypeMask(objectType, condList);

        if (containerTypeMask == 0)
            return;

        var extraSearchRadius = range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
        var check = new WorldObjectSpellAreaTargetCheck(range, position, Caster, referer, SpellInfo, selectionType, condList, objectType);
        var searcher = new WorldObjectListSearcher(Caster, targets, check, containerTypeMask);
        SearchTargets(searcher, containerTypeMask, Caster, position, range + extraSearchRadius);
    }

    private void SearchChainTargets(List<WorldObject> targets, uint chainTargets, WorldObject target, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectType, SpellEffectInfo spellEffectInfo, bool isChainHeal)
    {
        // max dist for jump target selection
        var jumpRadius = 0.0f;

        switch (SpellInfo.DmgClass)
        {
            case SpellDmgClass.Ranged:
                // 7.5y for multi shot
                jumpRadius = 7.5f;

                break;

            case SpellDmgClass.Melee:
                // 5y for swipe, cleave and similar
                jumpRadius = 5.0f;

                break;

            case SpellDmgClass.None:
            case SpellDmgClass.Magic:
                // 12.5y for chain heal spell since 3.2 patch
                if (isChainHeal)
                    jumpRadius = 12.5f;
                // 10y as default for magic chain spells
                else
                    jumpRadius = 10.0f;

                break;
        }

        var modOwner = Caster.SpellModOwner;

        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.ChainJumpDistance, ref jumpRadius, this);

        float searchRadius;

        if (SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster))
            searchRadius = GetMinMaxRange(false).maxRange;
        else if (spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
            searchRadius = jumpRadius;
        else
            searchRadius = jumpRadius * chainTargets;

        var chainSource = SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? Caster : target;
        List<WorldObject> tempTargets = new();
        SearchAreaTargets(tempTargets, searchRadius, chainSource.Location, Caster, objectType, selectType, spellEffectInfo.ImplicitTargetConditions);
        tempTargets.Remove(target);

        // remove targets which are always invalid for chain spells
        // for some spells allow only chain targets in front of caster (swipe for example)
        if (SpellInfo.HasAttribute(SpellAttr5.MeleeChainTargeting))
            tempTargets.RemoveAll(obj => !Caster.Location.HasInArc(MathF.PI, obj.Location));

        while (chainTargets != 0)
        {
            // try to get unit for next chain jump
            WorldObject found = null;

            // get unit with highest hp deficit in dist
            if (isChainHeal)
            {
                uint maxHpDeficit = 0;

                foreach (var obj in tempTargets)
                {
                    var unitTarget = obj.AsUnit;

                    if (unitTarget != null)
                    {
                        var deficit = (uint)(unitTarget.MaxHealth - unitTarget.Health);

                        if ((deficit > maxHpDeficit || found == null) && chainSource.Location.IsWithinDist(unitTarget, jumpRadius) && chainSource.Location.IsWithinLOSInMap(unitTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        {
                            found = obj;
                            maxHpDeficit = deficit;
                        }
                    }
                }
            }
            // get closest object
            else
                foreach (var obj in tempTargets)
                    if (found == null)
                    {
                        if (chainSource.Location.IsWithinDist(obj, jumpRadius) && chainSource.Location.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            found = obj;
                    }
                    else if (chainSource.Location.GetDistanceOrder(obj, found) && chainSource.Location.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        found = obj;

            // not found any valid target - chain ends
            if (found == null)
                break;

            if (!SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) && !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                chainSource = found;

            targets.Add(found);
            tempTargets.Remove(found);
            --chainTargets;
        }
    }

    private WorldObject SearchNearbyTarget(float range, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
    {
        var containerTypeMask = GetSearcherTypeMask(objectType, condList);

        if (containerTypeMask == 0)
            return null;

        var check = new WorldObjectSpellNearbyTargetCheck(range, Caster, SpellInfo, selectionType, condList, objectType);
        var searcher = new WorldObjectLastSearcher(Caster, check, containerTypeMask);
        SearchTargets(searcher, containerTypeMask, Caster, Caster.Location, range);

        return searcher.GetTarget();
    }

    private GameObject SearchSpellFocus()
    {
        var check = new GameObjectFocusCheck(Caster, SpellInfo.RequiresSpellFocus);
        var searcher = new GameObjectSearcher(Caster, check, GridType.All);
        SearchTargets(searcher, GridMapTypeMask.GameObject, Caster, Caster.Location, Caster.Visibility.VisibilityRange);

        return searcher.GetTarget();
    }

    private void SearchTargets(IGridNotifier notifier, GridMapTypeMask containerMask, WorldObject referer, Position pos, float radius)
    {
        if (containerMask == 0)
            return;

        var searchInWorld = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.GameObject);

        if (!searchInWorld)
            return;

        var x = pos.X;
        var y = pos.Y;

        var p = _gridDefines.ComputeCellCoord(x, y);
        Cell cell = new(p, _gridDefines);
        cell.Data.NoCreate = true;

        var map = referer.Location.Map;

        _cellCalculator.VisitGrid(x, y, map, notifier, radius);
    }

    private void SelectEffectImplicitTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, HashSet<int> processedEffectMask)
    {
        if (targetType.Target == 0)
            return;

        // set the same target list for all effects
        // some spells appear to need this, however this requires more research
        switch (targetType.SelectionCategory)
        {
            case SpellTargetSelectionCategories.Nearby:
            case SpellTargetSelectionCategories.Cone:
            case SpellTargetSelectionCategories.Area:
            case SpellTargetSelectionCategories.Line:
            {
                // targets for effect already selected
                if (processedEffectMask.Contains(spellEffectInfo.EffectIndex))
                    return;

                var j = 0;

                // choose which targets we can select at once
                foreach (var effect in SpellInfo.Effects)
                {
                    if (effect.IsEffect &&
                        spellEffectInfo.TargetA.Target == effect.TargetA.Target &&
                        spellEffectInfo.TargetB.Target == effect.TargetB.Target &&
                        spellEffectInfo.ImplicitTargetConditions == effect.ImplicitTargetConditions &&
                        spellEffectInfo.CalcRadius(Caster) == effect.CalcRadius(Caster) &&
                        CheckScriptEffectImplicitTargets(spellEffectInfo.EffectIndex, j))
                        processedEffectMask.Add(j);

                    j++;
                }

                break;
            }
        }

        switch (targetType.SelectionCategory)
        {
            case SpellTargetSelectionCategories.Channel:
                SelectImplicitChannelTargets(spellEffectInfo, targetType);

                break;

            case SpellTargetSelectionCategories.Nearby:
                SelectImplicitNearbyTargets(spellEffectInfo, targetType, processedEffectMask);

                break;

            case SpellTargetSelectionCategories.Cone:
                SelectImplicitConeTargets(spellEffectInfo, targetType, processedEffectMask);

                break;

            case SpellTargetSelectionCategories.Area:
                SelectImplicitAreaTargets(spellEffectInfo, targetType, processedEffectMask);

                break;

            case SpellTargetSelectionCategories.Traj:
                // just in case there is no dest, explanation in SelectImplicitDestDestTargets
                CheckDst();

                SelectImplicitTrajTargets(spellEffectInfo, targetType);

                break;

            case SpellTargetSelectionCategories.Line:
                SelectImplicitLineTargets(spellEffectInfo, targetType, processedEffectMask);

                break;

            case SpellTargetSelectionCategories.Default:
                switch (targetType.ObjectType)
                {
                    case SpellTargetObjectTypes.Src:
                        switch (targetType.ReferenceType)
                        {
                            case SpellTargetReferenceTypes.Caster:
                                Targets.SetSrc(Caster);

                                break;
                        }

                        break;

                    case SpellTargetObjectTypes.Dest:
                        switch (targetType.ReferenceType)
                        {
                            case SpellTargetReferenceTypes.Caster:
                                SelectImplicitCasterDestTargets(spellEffectInfo, targetType);

                                break;

                            case SpellTargetReferenceTypes.Target:
                                SelectImplicitTargetDestTargets(spellEffectInfo, targetType);

                                break;

                            case SpellTargetReferenceTypes.Dest:
                                SelectImplicitDestDestTargets(spellEffectInfo, targetType);

                                break;
                        }

                        break;

                    default:
                        switch (targetType.ReferenceType)
                        {
                            case SpellTargetReferenceTypes.Caster:
                                SelectImplicitCasterObjectTargets(spellEffectInfo, targetType);

                                break;

                            case SpellTargetReferenceTypes.Target:
                                SelectImplicitTargetObjectTargets(spellEffectInfo, targetType);

                                break;
                        }

                        break;
                }

                break;

            case SpellTargetSelectionCategories.Nyi:
                Log.Logger.Debug("SPELL: target type {0}, found in spellID {1}, effect {2} is not implemented yet!", SpellInfo.Id, spellEffectInfo.EffectIndex, targetType.Target);

                break;
        }
    }

    private void SelectEffectTypeImplicitTargets(SpellEffectInfo spellEffectInfo)
    {
        // special case for SPELL_EFFECT_SUMMON_RAF_FRIEND and SPELL_EFFECT_SUMMON_PLAYER, queue them on map for later execution
        switch (spellEffectInfo.Effect)
        {
            case SpellEffectName.SummonRafFriend:
            case SpellEffectName.SummonPlayer:
                if (Caster.IsTypeId(TypeId.Player) && !Caster.AsPlayer.Target.IsEmpty)
                {
                    WorldObject rafTarget = Caster.ObjectAccessor.FindPlayer(Caster.AsPlayer.Target);

                    CallScriptObjectTargetSelectHandlers(ref rafTarget, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

                    // scripts may modify the target - recheck
                    if (rafTarget is { IsPlayer: true })
                    {
                        // target is not stored in target map for those spells
                        // since we're completely skipping AddUnitTarget logic, we need to check immunity manually
                        // eg. aura 21546 makes target immune to summons
                        var player = rafTarget.AsPlayer;

                        if (player.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, null))
                            return;

                        var spell = this;
                        var targetGuid = rafTarget.GUID;

                        rafTarget.Location.Map.AddFarSpellCallback(map =>
                        {
                            var farTarget = Caster.ObjectAccessor.GetPlayer(map, targetGuid);

                            if (farTarget == null)
                                return;

                            // check immunity again in case it changed during update
                            if (farTarget.IsImmunedToSpellEffect(spell.SpellInfo, spellEffectInfo, null))
                                return;

                            spell.HandleEffects(farTarget, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
                        });
                    }
                }

                return;
        }

        // select spell implicit targets based on effect type
        if (spellEffectInfo.ImplicitTargetType == 0)
            return;

        var targetMask = spellEffectInfo.GetMissingTargetMask();

        if (targetMask == 0)
            return;

        WorldObject target = null;

        switch (spellEffectInfo.ImplicitTargetType)
        {
            // add explicit object target or self to the target map
            case SpellEffectImplicitTargetTypes.Explicit:
                // player which not released his spirit is Unit, but target Id for it is TARGET_FLAG_CORPSE_MASK
                if (Convert.ToBoolean(targetMask & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)))
                {
                    var unitTarget = Targets.UnitTarget;

                    if (unitTarget != null)
                        target = unitTarget;
                    else if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.CorpseMask))
                    {
                        var corpseTarget = Targets.CorpseTarget;

                        if (corpseTarget != null)
                            target = corpseTarget;
                    }
                    else //if (targetMask & TARGET_FLAG_UNIT_MASK)
                        target = Caster;
                }

                if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.ItemMask))
                {
                    var itemTarget = Targets.ItemTarget;

                    if (itemTarget != null)
                        AddItemTarget(itemTarget, spellEffectInfo.EffectIndex);

                    return;
                }

                if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.GameobjectMask))
                    target = Targets.GOTarget;

                break;
            // add self to the target map
            case SpellEffectImplicitTargetTypes.Caster:
                if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.UnitMask))
                    target = Caster;

                break;
        }

        CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

        if (target != null)
        {
            if (target.IsUnit)
                AddUnitTarget(target.AsUnit, spellEffectInfo.EffectIndex, false);
            else if (target.IsGameObject)
                AddGOTarget(target.AsGameObject, spellEffectInfo.EffectIndex);
            else if (target.IsCorpse)
                AddCorpseTarget(target.AsCorpse, spellEffectInfo.EffectIndex);
        }
    }

    private void SelectExplicitTargets()
    {
        // here go all explicit target changes made to explicit targets after spell prepare phase is finished
        var target = Targets.UnitTarget;

        if (target != null)
            // check for explicit target redirection, for Grounding Totem for example
            if (SpellInfo.ExplicitTargetMask.HasAnyFlag(SpellCastTargetFlags.UnitEnemy) || (SpellInfo.ExplicitTargetMask.HasAnyFlag(SpellCastTargetFlags.Unit) && !Caster.WorldObjectCombat.IsFriendlyTo(target)))
            {
                var redirect = SpellInfo.DmgClass switch
                {
                    SpellDmgClass.Magic => Caster.WorldObjectCombat.GetMagicHitRedirectTarget(target, SpellInfo),
                    SpellDmgClass.Melee =>
                        // should gameobjects cast damagetype melee/ranged spells this needs to be changed
                        Caster.AsUnit.GetMeleeHitRedirectTarget(target, SpellInfo),
                    SpellDmgClass.Ranged =>
                        // should gameobjects cast damagetype melee/ranged spells this needs to be changed
                        Caster.AsUnit.GetMeleeHitRedirectTarget(target, SpellInfo),
                    _ => null
                };

                if (redirect != null && redirect != target)
                    Targets.UnitTarget = redirect;
            }
    }

    private void SelectImplicitAreaTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, HashSet<int> effMask)
    {
        WorldObject referer = null;

        switch (targetType.ReferenceType)
        {
            case SpellTargetReferenceTypes.Src:
            case SpellTargetReferenceTypes.Dest:
            case SpellTargetReferenceTypes.Caster:
                referer = Caster;

                break;

            case SpellTargetReferenceTypes.Target:
                referer = Targets.UnitTarget;

                break;

            case SpellTargetReferenceTypes.Last:
            {
                referer = Caster;

                // find last added target for this effect
                foreach (var target in UniqueTargetInfo)
                    if (target.Effects.Contains(spellEffectInfo.EffectIndex))
                    {
                        referer = Caster.ObjectAccessor.GetUnit(Caster, target.TargetGuid);

                        break;
                    }

                break;
            }
        }

        if (referer == null)
            return;

        var center = targetType.ReferenceType switch
        {
            SpellTargetReferenceTypes.Src    => Targets.SrcPos,
            SpellTargetReferenceTypes.Dest   => Targets.DstPos,
            SpellTargetReferenceTypes.Caster => referer.Location,
            SpellTargetReferenceTypes.Target => referer.Location,
            SpellTargetReferenceTypes.Last   => referer.Location,
            _                                => Caster.Location
        };

        var radius = spellEffectInfo.CalcRadius(Caster) * SpellValue.RadiusMod;
        List<WorldObject> targets = new();

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.UnitCasterAndPassengers:
                targets.Add(Caster);
                var unit = Caster.AsUnit;

                var vehicleKit = unit?.VehicleKit;

                if (vehicleKit != null)
                    for (sbyte seat = 0; seat < SharedConst.MaxVehicleSeats; ++seat)
                    {
                        var passenger = vehicleKit.GetPassenger(seat);

                        if (passenger != null)
                            targets.Add(passenger);
                    }

                break;

            case Framework.Constants.Targets.UnitTargetAllyOrRaid:
                var targetedUnit = Targets.UnitTarget;

                if (targetedUnit != null)
                {
                    if (!Caster.IsUnit || !Caster.AsUnit.IsInRaidWith(targetedUnit))
                        targets.Add(Targets.UnitTarget);
                    else
                        SearchAreaTargets(targets, radius, targetedUnit.Location, referer, targetType.ObjectType, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions);
                }

                break;

            case Framework.Constants.Targets.UnitCasterAndSummons:
                targets.Add(Caster);
                SearchAreaTargets(targets, radius, center, referer, targetType.ObjectType, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions);

                break;

            default:
                SearchAreaTargets(targets, radius, center, referer, targetType.ObjectType, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions);

                break;
        }

        if (targetType.ObjectType == SpellTargetObjectTypes.UnitAndDest)
        {
            SpellDestination dest = new(referer);

            if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.Orientation = spellEffectInfo.PositionFacing;

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);

            Targets.ModDst(dest);
        }

        CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

        if (targetType.Target == Framework.Constants.Targets.UnitSrcAreaFurthestEnemy)
            targets.Sort(new ObjectDistanceOrderPred(referer, false));

        if (!targets.Empty())
        {
            // Other special target selection goes here
            var maxTargets = SpellValue.MaxAffectedTargets;

            if (maxTargets != 0)
            {
                if (targetType.Target != Framework.Constants.Targets.UnitSrcAreaFurthestEnemy)
                    targets.RandomResize(maxTargets);
                else if (targets.Count > maxTargets)
                    targets.Resize(maxTargets);
            }

            foreach (var obj in targets)
                if (obj.IsUnit)
                    AddUnitTarget(obj.AsUnit, effMask, false, true, center);
                else if (obj.IsGameObject)
                    AddGOTarget(obj.AsGameObject, effMask);
                else if (obj.IsCorpse)
                    AddCorpseTarget(obj.AsCorpse, effMask);
        }
    }

    private void SelectImplicitCasterDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        SpellDestination dest = new(Caster);

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.DestCaster:
                break;

            case Framework.Constants.Targets.DestHome:
                var playerCaster = Caster.AsPlayer;

                if (playerCaster != null)
                    dest = new SpellDestination(playerCaster.Homebind);

                break;

            case Framework.Constants.Targets.DestDb:
                var st = _spellManager.GetSpellTargetPosition(SpellInfo.Id, spellEffectInfo.EffectIndex);

                if (st != null)
                {
                    // @todo fix this check
                    if (SpellInfo.HasEffect(SpellEffectName.TeleportUnits) || SpellInfo.HasEffect(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen) || SpellInfo.HasEffect(SpellEffectName.Bind))
                        dest = new SpellDestination(st.X, st.Y, st.Z, st.Orientation, st.TargetMapId);
                    else if (st.TargetMapId == Caster.Location.MapId)
                        dest = new SpellDestination(st.X, st.Y, st.Z, st.Orientation);
                }
                else
                {
                    Log.Logger.Debug("SPELL: unknown target coordinates for spell ID {0}", SpellInfo.Id);
                    var target = Targets.ObjectTarget;

                    if (target != null)
                        dest = new SpellDestination(target);
                }

                break;

            case Framework.Constants.Targets.DestCasterFishing:
            {
                var minDist = SpellInfo.GetMinRange(true);
                var maxDist = SpellInfo.GetMaxRange(true);
                var dis = (float)RandomHelper.NextDouble() * (maxDist - minDist) + minDist;
                var angle = (float)RandomHelper.NextDouble() * (MathFunctions.PI * 35.0f / 180.0f) - (float)(Math.PI * 17.5f / 180.0f);
                var pos = new Position();
                Caster.Location.GetClosePoint(pos, SharedConst.DefaultPlayerBoundingRadius, dis, angle);

                var ground = Caster.Location.GetMapHeight(pos);
                var liquidLevel = MapConst.VMAPInvalidHeightValue;

                if (Caster.Location.Map.GetLiquidStatus(Caster.Location.PhaseShift, pos, LiquidHeaderTypeFlags.AllLiquids, out var liquidData, Caster.Location.CollisionHeight) != 0)
                    liquidLevel = liquidData.Level;

                if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
                {
                    SendCastResult(SpellCastResult.NotHere);
                    SendChannelUpdate(0);
                    Finish(SpellCastResult.NotHere);

                    return;
                }

                if (ground + 0.75 > liquidLevel)
                {
                    SendCastResult(SpellCastResult.TooShallow);
                    SendChannelUpdate(0);
                    Finish(SpellCastResult.TooShallow);

                    return;
                }

                dest = new SpellDestination(pos.X, pos.Y, liquidLevel, Caster.Location.Orientation);

                break;
            }
            case Framework.Constants.Targets.DestCasterFrontLeap:
            case Framework.Constants.Targets.DestCasterMovementDirection:
            {
                var unitCaster = Caster.AsUnit;

                if (unitCaster == null)
                    break;

                var dist = spellEffectInfo.CalcRadius(unitCaster);
                var angle = targetType.CalcDirectionAngle();

                if (targetType.Target == Framework.Constants.Targets.DestCasterMovementDirection)
                    angle = (Caster.MovementInfo.MovementFlags & (MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight)) switch
                    {
                        MovementFlag.None                                                                                 => 0.0f,
                        MovementFlag.Forward                                                                              => 0.0f,
                        MovementFlag.Forward | MovementFlag.Backward                                                      => 0.0f,
                        MovementFlag.StrafeLeft | MovementFlag.StrafeRight                                                => 0.0f,
                        MovementFlag.Forward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight                         => 0.0f,
                        MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight => 0.0f,
                        MovementFlag.Backward                                                                             => MathF.PI,
                        MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight                        => MathF.PI,
                        MovementFlag.StrafeLeft                                                                           => MathF.PI / 2,
                        MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft                            => MathF.PI / 2,
                        MovementFlag.Forward | MovementFlag.StrafeLeft                                                    => MathF.PI / 4,
                        MovementFlag.Backward | MovementFlag.StrafeLeft                                                   => 3 * MathF.PI / 4,
                        MovementFlag.StrafeRight                                                                          => -MathF.PI / 2,
                        MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeRight                           => -MathF.PI / 2,
                        MovementFlag.Forward | MovementFlag.StrafeRight                                                   => -MathF.PI / 4,
                        MovementFlag.Backward | MovementFlag.StrafeRight                                                  => -3 * MathF.PI / 4,
                        _                                                                                                 => 0.0f
                    };

                Position pos = new(dest.Position);

                unitCaster.MovePositionToFirstCollision(pos, dist, angle);
                dest.Relocate(pos);

                break;
            }
            case Framework.Constants.Targets.DestCasterGround:
            case Framework.Constants.Targets.DestCasterGround2:
                dest.Position.Z = Caster.Location.GetMapWaterOrGroundLevel(dest.Position.X, dest.Position.Y, dest.Position.Z);

                break;

            case Framework.Constants.Targets.DestSummoner:
            {
                var unitCaster = Caster.AsUnit;

                var casterSummon = unitCaster?.ToTempSummon();

                var summoner = casterSummon?.Summoner;

                if (summoner != null)
                    dest = new SpellDestination(summoner);

                break;
            }
            default:
            {
                var dist = spellEffectInfo.CalcRadius(Caster);
                var angl = targetType.CalcDirectionAngle();
                var objSize = Caster.CombatReach;

                switch (targetType.Target)
                {
                    case Framework.Constants.Targets.DestCasterSummon:
                        dist = SharedConst.PetFollowDist;

                        break;

                    case Framework.Constants.Targets.DestCasterRandom:
                        if (dist > objSize)
                            dist = objSize + (dist - objSize) * (float)RandomHelper.NextDouble();

                        break;

                    case Framework.Constants.Targets.DestCasterFrontLeft:
                    case Framework.Constants.Targets.DestCasterBackLeft:
                    case Framework.Constants.Targets.DestCasterFrontRight:
                    case Framework.Constants.Targets.DestCasterBackRight:
                    {
                        var defaultTotemDistance = 3.0f;

                        if (!spellEffectInfo.HasRadius && !spellEffectInfo.HasMaxRadius)
                            dist = defaultTotemDistance;

                        break;
                    }
                }

                if (dist < objSize)
                    dist = objSize;

                Position pos = new(dest.Position);
                Caster.MovePositionToFirstCollision(pos, dist, angl);

                dest.Relocate(pos);

                break;
            }
        }

        if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
            dest.Position.Orientation = spellEffectInfo.PositionFacing;

        CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
        Targets.Dst = dest;
    }

    private void SelectImplicitCasterObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        WorldObject target = null;
        var checkIfValid = true;

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.UnitCaster:
                target = Caster;
                checkIfValid = false;

                break;

            case Framework.Constants.Targets.UnitMaster:
                target = Caster.CharmerOrOwner;

                break;

            case Framework.Constants.Targets.UnitPet:
            {
                var unitCaster = Caster.AsUnit;

                if (unitCaster != null)
                    target = unitCaster.GetGuardianPet();

                break;
            }
            case Framework.Constants.Targets.UnitSummoner:
            {
                var unitCaster = Caster.AsUnit;

                if (unitCaster is { IsSummon: true })
                    target = unitCaster.ToTempSummon().SummonerUnit;

                break;
            }
            case Framework.Constants.Targets.UnitVehicle:
            {
                var unitCaster = Caster.AsUnit;

                if (unitCaster != null)
                    target = unitCaster.VehicleBase;

                break;
            }
            case Framework.Constants.Targets.UnitPassenger0:
            case Framework.Constants.Targets.UnitPassenger1:
            case Framework.Constants.Targets.UnitPassenger2:
            case Framework.Constants.Targets.UnitPassenger3:
            case Framework.Constants.Targets.UnitPassenger4:
            case Framework.Constants.Targets.UnitPassenger5:
            case Framework.Constants.Targets.UnitPassenger6:
            case Framework.Constants.Targets.UnitPassenger7:
                var vehicleBase = Caster.AsCreature;

                if (vehicleBase is { IsVehicle: true })
                    target = vehicleBase.VehicleKit.GetPassenger((sbyte)(targetType.Target - Framework.Constants.Targets.UnitPassenger0));

                break;

            case Framework.Constants.Targets.UnitTargetTapList:
                var creatureCaster = Caster.AsCreature;

                if (creatureCaster != null && !creatureCaster.TapList.Empty())
                    target = Caster.ObjectAccessor.GetWorldObject(creatureCaster, creatureCaster.TapList.SelectRandom());

                break;

            case Framework.Constants.Targets.UnitOwnCritter:
            {
                var unitCaster = Caster.AsUnit;

                if (unitCaster != null)
                    target = ObjectAccessor.GetCreatureOrPetOrVehicle(Caster, unitCaster.CritterGUID);

                break;
            }
        }

        CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

        if (target == null)
            return;

        if (target.IsUnit)
            AddUnitTarget(target.AsUnit, spellEffectInfo.EffectIndex, checkIfValid);
        else if (target.IsGameObject)
            AddGOTarget(target.AsGameObject, spellEffectInfo.EffectIndex);
        else if (target.IsCorpse)
            AddCorpseTarget(target.AsCorpse, spellEffectInfo.EffectIndex);
    }

    private void SelectImplicitChainTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, WorldObject target, int effIndex)
    {
        SelectImplicitChainTargets(spellEffectInfo,
                                   targetType,
                                   target,
                                   new HashSet<int>
                                   {
                                       effIndex
                                   });
    }

    private void SelectImplicitChainTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, WorldObject target, HashSet<int> effMask)
    {
        var maxTargets = spellEffectInfo.ChainTargets;
        var modOwner = Caster.SpellModOwner;
        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.ChainTargets, ref maxTargets, this);

        if (maxTargets <= 1)
            return;

        // mark damage multipliers as used
        foreach (var eff in SpellInfo.Effects)
            if (effMask.Contains(eff.EffectIndex))
                _damageMultipliers[spellEffectInfo.EffectIndex] = 1.0f;

        _applyMultiplierMask.UnionWith(effMask);

        List<WorldObject> targets = new();
        SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.ObjectType, targetType.CheckType, spellEffectInfo, targetType.Target == Framework.Constants.Targets.UnitChainhealAlly);

        // Chain primary target is added earlier
        CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

        Position losPosition = SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? Caster.Location : target.Location;

        foreach (var obj in targets)
        {
            var unitTarget = obj.AsUnit;

            if (unitTarget != null)
                AddUnitTarget(unitTarget, effMask, false, true, losPosition);

            if (!SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) && !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                losPosition = obj.Location;
        }
    }

    private void SelectImplicitChannelTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        if (targetType.ReferenceType != SpellTargetReferenceTypes.Caster)
            return;

        var channeledSpell = OriginalCaster.GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (channeledSpell == null)
        {
            Log.Logger.Debug("Spell.SelectImplicitChannelTargets: cannot find channel spell for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);

            return;
        }

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.UnitChannelTarget:
            {
                foreach (var channelTarget in OriginalCaster.UnitData.ChannelObjects)
                {
                    WorldObject target = Caster.ObjectAccessor.GetUnit(Caster, channelTarget);
                    CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
                    // unit target may be no longer avalible - teleported out of map for example
                    var unitTarget = target?.AsUnit;

                    if (unitTarget != null)
                        AddUnitTarget(unitTarget, spellEffectInfo.EffectIndex);
                    else
                        Log.Logger.Debug("SPELL: cannot find channel spell target for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
                }

                break;
            }
            case Framework.Constants.Targets.DestChannelTarget:
            {
                if (channeledSpell.Targets.HasDst)
                    Targets.SetDst(channeledSpell.Targets);
                else
                {
                    List<ObjectGuid> channelObjects = OriginalCaster.UnitData.ChannelObjects;
                    var target = !channelObjects.Empty() ? Caster.ObjectAccessor.GetWorldObject(Caster, channelObjects[0]) : null;

                    if (target != null)
                    {
                        CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

                        if (target != null)
                        {
                            SpellDestination dest = new(target);

                            if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                                dest.Position.Orientation = spellEffectInfo.PositionFacing;

                            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                            Targets.Dst = dest;
                        }
                    }
                    else
                        Log.Logger.Debug("SPELL: cannot find channel spell destination for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
                }

                break;
            }
            case Framework.Constants.Targets.DestChannelCaster:
            {
                SpellDestination dest = new(channeledSpell.Caster);

                if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                    dest.Position.Orientation = spellEffectInfo.PositionFacing;

                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                Targets.Dst = dest;

                break;
            }
        }
    }

    private void SelectImplicitConeTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, HashSet<int> effMask)
    {
        Position coneSrc = new(Caster.Location);
        var coneAngle = SpellInfo.ConeAngle;

        switch (targetType.ReferenceType)
        {
            case SpellTargetReferenceTypes.Caster:
                break;

            case SpellTargetReferenceTypes.Dest:
                if (Caster.Location.GetExactDist2d(Targets.DstPos) > 0.1f)
                    coneSrc.Orientation = Caster.Location.GetAbsoluteAngle(Targets.DstPos);

                break;
        }

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.UnitCone180DegEnemy:
                if (coneAngle == 0.0f)
                    coneAngle = 180.0f;

                break;
        }

        List<WorldObject> targets = new();
        var objectType = targetType.ObjectType;
        var selectionType = targetType.CheckType;

        var condList = spellEffectInfo.ImplicitTargetConditions;
        var radius = spellEffectInfo.CalcRadius(Caster) * SpellValue.RadiusMod;

        var containerTypeMask = GetSearcherTypeMask(objectType, condList);

        if (containerTypeMask != 0)
        {
            var extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
            var spellCone = new WorldObjectSpellConeTargetCheck(coneSrc, MathFunctions.DegToRad(coneAngle), SpellInfo.Width != 0 ? SpellInfo.Width : Caster.CombatReach, radius, Caster, SpellInfo, selectionType, condList, objectType);
            var searcher = new WorldObjectListSearcher(Caster, targets, spellCone, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, Caster, Caster.Location, radius + extraSearchRadius);

            CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

            if (!targets.Empty())
            {
                // Other special target selection goes here
                var maxTargets = SpellValue.MaxAffectedTargets;

                if (maxTargets != 0)
                    targets.RandomResize(maxTargets);

                foreach (var obj in targets)
                    if (obj.IsUnit)
                        AddUnitTarget(obj.AsUnit, effMask, false);
                    else if (obj.IsGameObject)
                        AddGOTarget(obj.AsGameObject, effMask);
                    else if (obj.IsCorpse)
                        AddCorpseTarget(obj.AsCorpse, effMask);
            }
        }
    }

    private void SelectImplicitDestDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        // set destination to caster if no dest provided
        // can only happen if previous destination target could not be set for some reason
        // (not found nearby target, or channel target for example
        // maybe we should abort the spell in such case?
        CheckDst();

        var dest = Targets.Dst;

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.DestDynobjEnemy:
            case Framework.Constants.Targets.DestDynobjAlly:
            case Framework.Constants.Targets.DestDynobjNone:
            case Framework.Constants.Targets.DestDest:
                break;

            case Framework.Constants.Targets.DestDestGround:
                dest.Position.Z = Caster.Location.GetMapHeight(dest.Position.X, dest.Position.Y, dest.Position.Z);

                break;

            default:
            {
                var angle = targetType.CalcDirectionAngle();
                var dist = spellEffectInfo.CalcRadius(Caster);

                if (targetType.Target == Framework.Constants.Targets.DestRandom)
                    dist *= (float)RandomHelper.NextDouble();

                Position pos = new(Targets.DstPos);
                Caster.MovePositionToFirstCollision(pos, dist, angle);

                dest.Relocate(pos);
            }

                break;
        }

        if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
            dest.Position.Orientation = spellEffectInfo.PositionFacing;

        CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
        Targets.ModDst(dest);
    }

    private void SelectImplicitLineTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, HashSet<int> effMask)
    {
        List<WorldObject> targets = new();
        var objectType = targetType.ObjectType;
        var selectionType = targetType.CheckType;

        var dst = targetType.ReferenceType switch
        {
            SpellTargetReferenceTypes.Src    => Targets.SrcPos,
            SpellTargetReferenceTypes.Dest   => Targets.DstPos,
            SpellTargetReferenceTypes.Caster => Caster.Location,
            SpellTargetReferenceTypes.Target => Targets.UnitTarget.Location,
            _                                => Caster.Location
        };

        var condList = spellEffectInfo.ImplicitTargetConditions;
        var radius = spellEffectInfo.CalcRadius(Caster) * SpellValue.RadiusMod;

        var containerTypeMask = GetSearcherTypeMask(objectType, condList);

        if (containerTypeMask != 0)
        {
            WorldObjectSpellLineTargetCheck check = new(Caster.Location, dst, SpellInfo.Width != 0 ? SpellInfo.Width : Caster.CombatReach, radius, Caster, SpellInfo, selectionType, condList, objectType);
            WorldObjectListSearcher searcher = new(Caster, targets, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, Caster, Caster.Location, radius);

            CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

            if (!targets.Empty())
            {
                // Other special target selection goes here
                var maxTargets = SpellValue.MaxAffectedTargets;

                if (maxTargets != 0)
                    if (maxTargets < targets.Count)
                    {
                        targets.Sort(new ObjectDistanceOrderPred(Caster));
                        targets.Resize(maxTargets);
                    }

                foreach (var obj in targets)
                    if (obj.IsUnit)
                        AddUnitTarget(obj.AsUnit, effMask, false);
                    else if (obj.IsGameObject)
                        AddGOTarget(obj.AsGameObject, effMask);
                    else if (obj.IsCorpse)
                        AddCorpseTarget(obj.AsCorpse, effMask);
            }
        }
    }

    private void SelectImplicitNearbyTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, HashSet<int> effMask)
    {
        if (targetType.ReferenceType != SpellTargetReferenceTypes.Caster)
            return;

        var range = targetType.CheckType switch
        {
            SpellTargetCheckTypes.Enemy     => SpellInfo.GetMaxRange(false, Caster, this),
            SpellTargetCheckTypes.Ally      => SpellInfo.GetMaxRange(true, Caster, this),
            SpellTargetCheckTypes.Party     => SpellInfo.GetMaxRange(true, Caster, this),
            SpellTargetCheckTypes.Raid      => SpellInfo.GetMaxRange(true, Caster, this),
            SpellTargetCheckTypes.RaidClass => SpellInfo.GetMaxRange(true, Caster, this),
            SpellTargetCheckTypes.Entry     => SpellInfo.GetMaxRange(IsPositive, Caster, this),
            SpellTargetCheckTypes.Default   => SpellInfo.GetMaxRange(IsPositive, Caster, this),
            _                               => 0.0f
        };

        var condList = spellEffectInfo.ImplicitTargetConditions;

        // handle emergency case - try to use other provided targets if no conditions provided
        if (targetType.CheckType == SpellTargetCheckTypes.Entry && (condList == null || condList.Empty()))
        {
            Log.Logger.Debug("Spell.SelectImplicitNearbyTargets: no conditions entry for target with TARGET_CHECK_ENTRY of spell ID {0}, effect {1} - selecting default targets", SpellInfo.Id, spellEffectInfo.EffectIndex);

            switch (targetType.ObjectType)
            {
                case SpellTargetObjectTypes.Gobj:
                    if (SpellInfo.RequiresSpellFocus != 0)
                    {
                        if (_focusObject != null)
                            AddGOTarget(_focusObject, effMask);
                        else
                        {
                            SendCastResult(SpellCastResult.BadImplicitTargets);
                            Finish(SpellCastResult.BadImplicitTargets);
                        }

                        return;
                    }

                    break;

                case SpellTargetObjectTypes.Dest:
                    if (SpellInfo.RequiresSpellFocus != 0)
                    {
                        if (_focusObject != null)
                        {
                            SpellDestination dest = new(_focusObject);

                            if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                                dest.Position.Orientation = spellEffectInfo.PositionFacing;

                            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                            Targets.Dst = dest;
                        }
                        else
                        {
                            SendCastResult(SpellCastResult.BadImplicitTargets);
                            Finish(SpellCastResult.BadImplicitTargets);
                        }

                        return;
                    }

                    break;
            }
        }

        var target = SearchNearbyTarget(range, targetType.ObjectType, targetType.CheckType, condList);

        if (target == null)
        {
            Log.Logger.Debug("Spell.SelectImplicitNearbyTargets: cannot find nearby target for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
            SendCastResult(SpellCastResult.BadImplicitTargets);
            Finish(SpellCastResult.BadImplicitTargets);

            return;
        }

        CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

        if (target == null)
        {
            Log.Logger.Debug($"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set NULL target, effect {spellEffectInfo.EffectIndex}");
            SendCastResult(SpellCastResult.BadImplicitTargets);
            Finish(SpellCastResult.BadImplicitTargets);

            return;
        }

        switch (targetType.ObjectType)
        {
            case SpellTargetObjectTypes.Unit:
                var unitTarget = target.AsUnit;

                if (unitTarget != null)
                    AddUnitTarget(unitTarget, effMask, true, false);
                else
                {
                    Log.Logger.Debug($"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong type, expected unit, got {target.GUID.High}, effect {effMask}");
                    SendCastResult(SpellCastResult.BadImplicitTargets);
                    Finish(SpellCastResult.BadImplicitTargets);

                    return;
                }

                break;

            case SpellTargetObjectTypes.Gobj:
                var gobjTarget = target.AsGameObject;

                if (gobjTarget != null)
                    AddGOTarget(gobjTarget, effMask);
                else
                {
                    Log.Logger.Debug($"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong type, expected gameobject, got {target.GUID.High}, effect {effMask}");
                    SendCastResult(SpellCastResult.BadImplicitTargets);
                    Finish(SpellCastResult.BadImplicitTargets);

                    return;
                }

                break;

            case SpellTargetObjectTypes.Corpse:
                var corpseTarget = target.AsCorpse;

                if (corpseTarget != null)
                    AddCorpseTarget(corpseTarget, effMask);
                else
                {
                    Log.Logger.Debug($"Spell::SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong type, expected corpse, got {target.GUID.TypeId}, effect {effMask}");
                    SendCastResult(SpellCastResult.BadImplicitTargets);
                    Finish(SpellCastResult.BadImplicitTargets);

                    return;
                }

                break;

            case SpellTargetObjectTypes.Dest:
                SpellDestination dest = new(target);

                if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                    dest.Position.Orientation = spellEffectInfo.PositionFacing;

                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                Targets.Dst = dest;

                break;
        }

        SelectImplicitChainTargets(spellEffectInfo, targetType, target, effMask);
    }

    private void SelectImplicitTargetDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        var target = Targets.ObjectTarget;

        SpellDestination dest = new(target);

        switch (targetType.Target)
        {
            case Framework.Constants.Targets.DestTargetEnemy:
            case Framework.Constants.Targets.DestAny:
            case Framework.Constants.Targets.DestTargetAlly:
                break;

            default:
            {
                var angle = targetType.CalcDirectionAngle();
                var dist = spellEffectInfo.CalcRadius();

                if (targetType.Target == Framework.Constants.Targets.DestRandom)
                    dist *= (float)RandomHelper.NextDouble();

                Position pos = new(dest.Position);
                target.MovePositionToFirstCollision(pos, dist, angle);

                dest.Relocate(pos);
            }

                break;
        }

        if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
            dest.Position.Orientation = spellEffectInfo.PositionFacing;

        CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
        Targets.Dst = dest;
    }

    private void SelectImplicitTargetObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        var target = Targets.ObjectTarget;

        CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

        var item = Targets.ItemTarget;

        if (target != null)
        {
            if (target.IsUnit)
                AddUnitTarget(target.AsUnit, spellEffectInfo.EffectIndex, true, false);
            else if (target.IsGameObject)
                AddGOTarget(target.AsGameObject, spellEffectInfo.EffectIndex);
            else if (target.IsCorpse)
                AddCorpseTarget(target.AsCorpse, spellEffectInfo.EffectIndex);

            SelectImplicitChainTargets(spellEffectInfo, targetType, target, spellEffectInfo.EffectIndex);
        }
        // Script hook can remove object target and we would wrongly land here
        else if (item != null)
            AddItemTarget(item, spellEffectInfo.EffectIndex);
    }

    private void SelectImplicitTrajTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
    {
        if (!Targets.HasTraj)
            return;

        var dist2d = Targets.Dist2d;

        if (dist2d == 0)
            return;

        var srcPos = Targets.SrcPos;
        srcPos.Orientation = Caster.Location.Orientation;
        var srcToDestDelta = Targets.DstPos.Z - srcPos.Z;

        List<WorldObject> targets = new();
        var spellTraj = new WorldObjectSpellTrajTargetCheck(dist2d, srcPos, Caster, SpellInfo, targetType.CheckType, spellEffectInfo.ImplicitTargetConditions, SpellTargetObjectTypes.None);
        var searcher = new WorldObjectListSearcher(Caster, targets, spellTraj);
        SearchTargets(searcher, GridMapTypeMask.All, Caster, srcPos, dist2d);

        if (targets.Empty())
            return;

        targets.Sort(new ObjectDistanceOrderPred(Caster));

        var b = Tangent(Targets.Pitch);
        var a = (srcToDestDelta - dist2d * b) / (dist2d * dist2d);

        if (a > -0.0001f)
            a = 0f;

        // We should check if triggered spell has greater range (which is true in many cases, and initial spell has too short max range)
        // limit max range to 300 yards, sometimes triggered spells can have 50000yds
        var bestDist = SpellInfo.GetMaxRange();
        var triggerSpellInfo = _spellManager.GetSpellInfo(spellEffectInfo.TriggerSpell, CastDifficulty);

        if (triggerSpellInfo != null)
            bestDist = Math.Min(Math.Max(bestDist, triggerSpellInfo.GetMaxRange()), Math.Min(dist2d, 300.0f));

        // GameObjects don't cast traj
        var unitCaster = Caster.AsUnit;

        foreach (var obj in targets)
        {
            if (SpellInfo.CheckTarget(unitCaster, obj) != SpellCastResult.SpellCastOk)
                continue;

            var unitTarget = obj.AsUnit;

            if (unitTarget != null)
            {
                if (unitCaster == obj || unitCaster.IsOnVehicle(unitTarget) || unitTarget.Vehicle != null)
                    continue;

                var creatureTarget = unitTarget.AsCreature;

                if (creatureTarget != null)
                    if (!creatureTarget.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.CollideWithMissiles))
                        continue;
            }

            var size = Math.Max(obj.CombatReach, 1.0f);
            var objDist2d = srcPos.GetExactDist2d(obj.Location);
            var dz = obj.Location.Z - srcPos.Z;

            var horizontalDistToTraj = (float)Math.Abs(objDist2d * Math.Sin(srcPos.GetRelativeAngle(obj.Location)));
            var sizeFactor = (float)Math.Cos(horizontalDistToTraj / size * (Math.PI / 2.0f));
            var distToHitPoint = (float)Math.Max(objDist2d * Math.Cos(srcPos.GetRelativeAngle(obj.Location)) - size * sizeFactor, 0.0f);
            var height = distToHitPoint * (a * distToHitPoint + b);

            if (Math.Abs(dz - height) > size + b / 2.0f + SpellConst.TrajectoryMissileSize)
                continue;

            if (!(distToHitPoint < bestDist))
                continue;

            bestDist = distToHitPoint;

            break;
        }

        if (!(dist2d > bestDist))
            return;

        var x = (float)(Targets.SrcPos.X + Math.Cos(unitCaster.Location.Orientation) * bestDist);
        var y = (float)(Targets.SrcPos.Y + Math.Sin(unitCaster.Location.Orientation) * bestDist);
        var z = Targets.SrcPos.Z + bestDist * (a * bestDist + b);

        SpellDestination dest = new(x, y, z, unitCaster.Location.Orientation);

        if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
            dest.Position.Orientation = spellEffectInfo.PositionFacing;

        CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
        Targets.ModDst(dest);
    }

    private void SendChannelStart(uint duration)
    {
        // GameObjects don't channel
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return;

        SpellChannelStart spellChannelStart = new()
        {
            CasterGUID = unitCaster.GUID,
            SpellID = (int)SpellInfo.Id,
            Visual = SpellVisual,
            ChannelDuration = duration
        };

        if (IsEmpowered) // remove the first second of casting time to display correctly
            spellChannelStart.ChannelDuration -= 1000;

        var schoolImmunityMask = unitCaster.SchoolImmunityMask;
        var mechanicImmunityMask = unitCaster.MechanicImmunityMask;

        if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
        {
            SpellChannelStartInterruptImmunities interruptImmunities = new()
            {
                SchoolImmunities = (int)schoolImmunityMask,
                Immunities = (int)mechanicImmunityMask
            };

            spellChannelStart.InterruptImmunities = interruptImmunities;
        }

        unitCaster.SendMessageToSet(spellChannelStart, true);

        _timer = (int)duration;

        if (!Targets.HasDst)
        {
            var channelAuraMask = new HashSet<int>();
            var explicitTargetEffectMask = SpellConst.MaxEffects;

            // if there is an explicit target, only add channel objects from effects that also hit ut
            if (!Targets.UnitTargetGUID.IsEmpty)
            {
                var explicitTarget = UniqueTargetInfo.Find(target => target.TargetGuid == Targets.UnitTargetGUID);

                if (explicitTarget != null)
                    explicitTargetEffectMask = explicitTarget.Effects;
            }

            foreach (var spellEffectInfo in SpellInfo.Effects)
                if (spellEffectInfo.Effect == SpellEffectName.ApplyAura && explicitTargetEffectMask.Contains(spellEffectInfo.EffectIndex))
                    channelAuraMask.Add(spellEffectInfo.EffectIndex);

            var chanMask = channelAuraMask.ToMask();

            foreach (var target in UniqueTargetInfo)
            {
                if ((target.Effects.ToMask() & chanMask) == 0)
                    continue;

                var requiredAttribute = target.TargetGuid != unitCaster.GUID ? SpellAttr1.IsChannelled : SpellAttr1.IsSelfChannelled;

                if (!SpellInfo.HasAttribute(requiredAttribute))
                    continue;

                unitCaster.AddChannelObject(target.TargetGuid);
            }

            foreach (var target in _uniqueGoTargetInfo)
                if ((target.Effects.ToMask() & chanMask) != 0)
                    unitCaster.AddChannelObject(target.TargetGUID);
        }
        else if (SpellInfo.HasAttribute(SpellAttr1.IsSelfChannelled))
            unitCaster.AddChannelObject(unitCaster.GUID);

        var creatureCaster = unitCaster.AsCreature;

        if (creatureCaster != null)
            if (unitCaster.UnitData.ChannelObjects.Size() == 1 && unitCaster.UnitData.ChannelObjects[0].IsUnit)
                if (!creatureCaster.HasSpellFocus(this))
                    creatureCaster.SetSpellFocus(this, Caster.ObjectAccessor.GetWorldObject(creatureCaster, unitCaster.UnitData.ChannelObjects[0]));

        unitCaster.ChannelSpellId = SpellInfo.Id;
        unitCaster.SetChannelVisual(SpellVisual);
    }

    private void SendInterrupted(byte result)
    {
        SpellFailure failurePacket = new()
        {
            CasterUnit = Caster.GUID,
            CastID = CastId,
            SpellID = SpellInfo.Id,
            Visual = SpellVisual,
            Reason = result
        };

        Caster.SendMessageToSet(failurePacket, true);

        SpellFailedOther failedPacket = new()
        {
            CasterUnit = Caster.GUID,
            CastID = CastId,
            SpellID = SpellInfo.Id,
            Visual = SpellVisual,
            Reason = result
        };

        Caster.SendMessageToSet(failedPacket, true);
    }

    private void SendMountResult(MountResult result)
    {
        if (result == MountResult.Ok)
            return;

        if (!Caster.IsPlayer)
            return;

        var caster = Caster.AsPlayer;

        if (caster.IsLoading) // don't send mount results at loading time
            return;

        MountResultPacket packet = new()
        {
            Result = (uint)result
        };

        caster.SendPacket(packet);
    }

    private void SendResurrectRequest(Player target)
    {
        // get resurrector name for creature resurrections, otherwise packet will be not accepted
        // for player resurrections the name is looked up by guid
        var sentName = "";

        if (!Caster.IsPlayer)
            sentName = Caster.GetName(target.Session.SessionDbLocaleIndex);

        ResurrectRequest resurrectRequest = new()
        {
            ResurrectOffererGUID = Caster.GUID,
            ResurrectOffererVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
            Name = sentName,
            Sickness = Caster.IsUnit && !Caster.IsTypeId(TypeId.Player), // "you'll be afflicted with resurrection sickness"
            UseTimer = !SpellInfo.HasAttribute(SpellAttr3.NoResTimer)
        };

        var pet = target.CurrentPet;

        var charmInfo = pet?.GetCharmInfo();

        if (charmInfo != null)
            resurrectRequest.PetNumber = charmInfo.GetPetNumber();

        resurrectRequest.SpellID = SpellInfo.Id;

        target.SendPacket(resurrectRequest);
    }

    private void SendSpellCooldown()
    {
        if (!Caster.IsUnit)
            return;

        if (CastItem != null)
            Caster.AsUnit.SpellHistory.HandleCooldowns(SpellInfo, CastItem, this);
        else
            Caster.AsUnit.SpellHistory.HandleCooldowns(SpellInfo, CastItemEntry, this);

        if (_isAutoRepeat)
            Caster.AsUnit.ResetAttackTimer(WeaponAttackType.RangedAttack);
    }

    private void SendSpellExecuteLog()
    {
        if (_executeLogEffects.Empty())
            return;

        SpellExecuteLog spellExecuteLog = new()
        {
            Caster = Caster.GUID,
            SpellID = SpellInfo.Id,
            Effects = _executeLogEffects.Values.ToList()
        };

        spellExecuteLog.LogData.Initialize(this);

        Caster.SendCombatLogMessage(spellExecuteLog);
    }

    private void SendSpellGo()
    {
        // not send invisible spell casting
        if (!IsNeedSendToClient())
            return;

        Log.Logger.Debug("Sending SMSG_SPELL_GO id={0}", SpellInfo.Id);

        var castFlags = SpellCastFlags.Unk9;

        // triggered spells with spell visual != 0
        if (((IsTriggered && !SpellInfo.IsAutoRepeatRangedSpell) || TriggeredByAuraSpell != null) && !FromClient)
            castFlags |= SpellCastFlags.Pending;

        if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || SpellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) || SpellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
            castFlags |= SpellCastFlags.Projectile; // arrows/bullets visual

        if ((Caster.IsTypeId(TypeId.Player) || (Caster.IsTypeId(TypeId.Unit) && Caster.AsCreature.IsPet)) && PowerCost.Any(cost => cost.Power != PowerType.Health))
            castFlags |= SpellCastFlags.PowerLeftSelf;

        if (Caster.IsTypeId(TypeId.Player) &&
            Caster.AsPlayer.Class == PlayerClass.Deathknight &&
            HasPowerTypeCost(PowerType.Runes) &&
            !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerAndReagentCost))
        {
            castFlags |= SpellCastFlags.NoGCD;    // same as in SMSG_SPELL_START
            castFlags |= SpellCastFlags.RuneList; // rune cooldowns list
        }

        if (Targets.HasTraj)
            castFlags |= SpellCastFlags.AdjustMissile;

        if (SpellInfo.StartRecoveryTime == 0)
            castFlags |= SpellCastFlags.NoGCD;

        SpellGo packet = new();
        var castData = packet.Cast;

        castData.CasterGUID = CastItem?.GUID ?? Caster.GUID;

        castData.CasterUnit = Caster.GUID;
        castData.CastID = CastId;
        castData.OriginalCastID = OriginalCastId;
        castData.SpellID = (int)SpellInfo.Id;
        castData.Visual = SpellVisual;
        castData.CastFlags = castFlags;
        castData.CastFlagsEx = CastFlagsEx;
        castData.CastTime = Time.MSTime;

        castData.HitTargets = new List<ObjectGuid>();
        UpdateSpellCastDataTargets(castData);

        Targets.Write(castData.Target);

        if (Convert.ToBoolean(castFlags & SpellCastFlags.PowerLeftSelf))
        {
            castData.RemainingPower = new List<SpellPowerData>();

            foreach (var cost in PowerCost)
            {
                SpellPowerData powerData;
                powerData.Type = cost.Power;
                powerData.Cost = Caster.AsUnit.GetPower(cost.Power);
                castData.RemainingPower.Add(powerData);
            }
        }

        if (Convert.ToBoolean(castFlags & SpellCastFlags.RuneList)) // rune cooldowns list
        {
            castData.RemainingRunes = new RuneData();
            var runeData = castData.RemainingRunes;

            var player = Caster.AsPlayer;
            runeData.Start = _runesState;            // runes state before
            runeData.Count = player.GetRunesState(); // runes state after

            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
            {
                // float casts ensure the division is performed on floats as we need float result
                var baseCd = (float)player.GetRuneBaseCooldown();
                runeData.Cooldowns.Add((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
            }
        }

        if (castFlags.HasFlag(SpellCastFlags.AdjustMissile))
        {
            castData.MissileTrajectory.TravelTime = (uint)DelayMoment;
            castData.MissileTrajectory.Pitch = Targets.Pitch;
        }

        packet.LogData.Initialize(this);

        Caster.SendCombatLogMessage(packet);

        if (GetPlayerIfIsEmpowered(out var p))
        {
            ForEachSpellScript<ISpellOnEpowerSpellStart>(s => s.EmpowerSpellStart());

            SpellEmpowerStart spellEmpowerSart = new()
            {
                CastID = packet.Cast.CastID,
                Caster = packet.Cast.CasterGUID,
                Targets = UniqueTargetInfo.Select(t => t.TargetGuid).ToList(),
                SpellID = SpellInfo.Id,
                Visual = packet.Cast.Visual
            };

            TryGetTotalEmpowerDuration(false, out var dur);
            spellEmpowerSart.Duration = (uint)dur;
            spellEmpowerSart.FirstStageDuration = _empowerStages.FirstOrDefault().Value.DurationMs;
            spellEmpowerSart.FinalStageDuration = _empowerStages.LastOrDefault().Value.DurationMs;
            spellEmpowerSart.StageDurations = _empowerStages.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DurationMs);

            var schoolImmunityMask = p.SchoolImmunityMask;
            var mechanicImmunityMask = p.MechanicImmunityMask;

            if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
            {
                SpellChannelStartInterruptImmunities interruptImmunities = new()
                {
                    SchoolImmunities = (int)schoolImmunityMask,
                    Immunities = (int)mechanicImmunityMask
                };

                spellEmpowerSart.Immunities = interruptImmunities;
            }

            p.SendPacket(spellEmpowerSart);
        }
    }

    private void SendSpellInterruptLog(Unit victim, uint spellId)
    {
        SpellInterruptLog data = new()
        {
            Caster = Caster.GUID,
            Victim = victim.GUID,
            InterruptedSpellID = SpellInfo.Id,
            SpellID = spellId
        };

        Caster.SendMessageToSet(data, true);
    }

    private void SendSpellStart()
    {
        if (!IsNeedSendToClient())
            return;

        var castFlags = SpellCastFlags.HasTrajectory;
        uint schoolImmunityMask = 0;
        ulong mechanicImmunityMask = 0;
        var unitCaster = Caster.AsUnit;

        if (unitCaster != null)
        {
            schoolImmunityMask = _timer != 0 ? unitCaster.SchoolImmunityMask : 0;
            mechanicImmunityMask = _timer != 0 ? SpellInfo.GetMechanicImmunityMask(unitCaster) : 0;
        }

        if (schoolImmunityMask != 0 || mechanicImmunityMask != 0)
            castFlags |= SpellCastFlags.Immunity;

        if (((IsTriggered && !SpellInfo.IsAutoRepeatRangedSpell) || TriggeredByAuraSpell != null) && !FromClient)
            castFlags |= SpellCastFlags.Pending;

        if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) || SpellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) || SpellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
            castFlags |= SpellCastFlags.Projectile;

        if ((Caster.IsTypeId(TypeId.Player) || (Caster.IsTypeId(TypeId.Unit) && Caster.AsCreature.IsPet)) && PowerCost.Any(cost => cost.Power != PowerType.Health))
            castFlags |= SpellCastFlags.PowerLeftSelf;

        if (HasPowerTypeCost(PowerType.Runes))
            castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

        SpellStart packet = new();
        var castData = packet.Cast;

        castData.CasterGUID = CastItem?.GUID ?? Caster.GUID;
        castData.CasterUnit = Caster.GUID;
        castData.CastID = CastId;
        castData.OriginalCastID = OriginalCastId;
        castData.SpellID = (int)SpellInfo.Id;
        castData.Visual = SpellVisual;
        castData.CastFlags = castFlags;
        castData.CastFlagsEx = CastFlagsEx;
        castData.CastTime = (uint)CastTime;

        Targets.Write(castData.Target);

        if (castFlags.HasAnyFlag(SpellCastFlags.PowerLeftSelf))
            foreach (var cost in PowerCost)
            {
                SpellPowerData powerData;
                powerData.Type = cost.Power;
                powerData.Cost = Caster.AsUnit.GetPower(cost.Power);
                castData.RemainingPower.Add(powerData);
            }

        if (castFlags.HasAnyFlag(SpellCastFlags.RuneList)) // rune cooldowns list
        {
            castData.RemainingRunes = new RuneData();

            var runeData = castData.RemainingRunes;
            //TODO: There is a crash caused by a spell with CAST_FLAG_RUNE_LIST casted by a creature
            //The creature is the mover of a player, so HandleCastSpellOpcode uses it as the caster

            var player = Caster.AsPlayer;

            if (player != null)
            {
                runeData.Start = _runesState;            // runes state before
                runeData.Count = player.GetRunesState(); // runes state after

                for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                {
                    // float casts ensure the division is performed on floats as we need float result
                    float baseCd = player.GetRuneBaseCooldown();
                    runeData.Cooldowns.Add((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
                }
            }
            else
            {
                runeData.Start = 0;
                runeData.Count = 0;

                for (byte i = 0; i < Caster.AsUnit?.GetMaxPower(PowerType.Runes); ++i)
                    runeData.Cooldowns.Add(0);
            }
        }

        UpdateSpellCastDataAmmo(ref castData.Ammo);

        if (castFlags.HasAnyFlag(SpellCastFlags.Immunity))
        {
            castData.Immunities.School = schoolImmunityMask;
            castData.Immunities.Value = (uint)mechanicImmunityMask;
        }

        /* @todo implement heal prediction packet data
        if (castFlags & CAST_FLAG_HEAL_PREDICTION)
        {
            castData.Predict.BeconGUID = ??
            castData.Predict.Points = 0;
            castData.Predict.Type = 0;
        }*/

        Caster.SendMessageToSet(packet, true);
    }

    private void SetExecutedCurrently(bool yes)
    {
        _executedCurrently = yes;
    }

    private void TakeCastItem()
    {
        if (CastItem == null || !Caster.IsTypeId(TypeId.Player))
            return;

        // not remove cast item at triggered spell (equipping, weapon damage, etc)
        if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem))
            return;

        var proto = CastItem.Template;

        if (proto == null)
        {
            // This code is to avoid a crash
            // I'm not sure, if this is really an error, but I guess every item needs a prototype
            Log.Logger.Error("Cast item has no item prototype {0}", CastItem.GUID.ToString());

            return;
        }

        var expendable = false;
        var withoutCharges = false;

        foreach (var itemEffect in CastItem.Effects)
        {
            if (itemEffect.LegacySlotIndex >= CastItem.ItemData.SpellCharges.GetSize())
                continue;

            // item has limited charges
            if (itemEffect.Charges != 0)
            {
                if (itemEffect.Charges < 0)
                    expendable = true;

                var charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                // item has charges left
                if (charges != 0)
                {
                    if (charges > 0)
                        --charges;
                    else
                        ++charges;

                    if (proto.MaxStackSize == 1)
                        CastItem.SetSpellCharges(itemEffect.LegacySlotIndex, charges);

                    CastItem.SetState(ItemUpdateState.Changed, Caster.AsPlayer);
                }

                // all charges used
                withoutCharges = charges == 0;
            }
        }

        if (expendable && withoutCharges)
        {
            uint count = 1;
            Caster.AsPlayer.DestroyItemCount(CastItem, ref count, true);

            // prevent crash at access to deleted m_targets.GetItemTarget
            if (CastItem == Targets.ItemTarget)
                Targets.ItemTarget = null;

            CastItem = null;
            CastItemGuid.Clear();
            CastItemEntry = 0;
        }
    }

    private void TakePower()
    {
        // GameObjects don't use power
        var unitCaster = Caster.AsUnit;

        if (unitCaster == null)
            return;

        if (CastItem != null || TriggeredByAuraSpell != null)
            return;

        //Don't take power if the spell is cast while .cheat power is enabled.
        if (unitCaster.IsTypeId(TypeId.Player))
            if (unitCaster.AsPlayer.GetCommandStatus(PlayerCommandStates.Power))
                return;

        foreach (var cost in PowerCost)
        {
            var hit = true;

            if (unitCaster.IsTypeId(TypeId.Player))
                if (SpellInfo.HasAttribute(SpellAttr1.DiscountPowerOnMiss))
                {
                    var targetGUID = Targets.UnitTargetGUID;

                    if (!targetGUID.IsEmpty)
                    {
                        var ihit = UniqueTargetInfo.FirstOrDefault(targetInfo => targetInfo.TargetGuid == targetGUID && targetInfo.MissCondition != SpellMissInfo.None);

                        if (ihit != null)
                        {
                            hit = false;
                            //lower spell cost on fail (by talent aura)
                            var modOwner = unitCaster.SpellModOwner;
                            var amount = cost.Amount;
                            modOwner?.ApplySpellMod(SpellInfo, SpellModOp.PowerCostOnMiss, ref amount);
                            cost.Amount = amount;
                        }
                    }
                }

            if (cost.Power == PowerType.Runes)
            {
                TakeRunePower(hit);

                continue;
            }

            if (cost.Amount == 0)
                continue;

            // health as power used
            if (cost.Power == PowerType.Health)
            {
                unitCaster.ModifyHealth(-cost.Amount);

                continue;
            }

            if (cost.Power >= PowerType.Max)
            {
                Log.Logger.Error("Spell.TakePower: Unknown power type '{0}'", cost.Power);

                continue;
            }

            unitCaster.ModifyPower(cost.Power, -cost.Amount);
            ForEachSpellScript<ISpellOnTakePower>(a => a.TakePower(cost));
        }
    }

    private void TakeReagents()
    {
        if (!Caster.IsTypeId(TypeId.Player))
            return;

        // do not take reagents for these item casts
        if (CastItem != null && CastItem.Template.HasFlag(ItemFlags.NoReagentCost))
            return;

        var pCaster = Caster.AsPlayer;

        if (pCaster.CanNoReagentCast(SpellInfo))
            return;

        for (var x = 0; x < SpellConst.MaxReagents; ++x)
        {
            if (SpellInfo.Reagent[x] <= 0)
                continue;

            var itemid = (uint)SpellInfo.Reagent[x];
            var itemcount = SpellInfo.ReagentCount[x];

            // if CastItem is also spell reagent
            if (CastItem != null && CastItem.Entry == itemid)
            {
                foreach (var itemEffect in CastItem.Effects)
                {
                    if (itemEffect.LegacySlotIndex >= CastItem.ItemData.SpellCharges.GetSize())
                        continue;

                    // CastItem will be used up and does not count as reagent
                    var charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                    if (itemEffect.Charges < 0 && Math.Abs(charges) < 2)
                    {
                        ++itemcount;

                        break;
                    }
                }

                CastItem = null;
                CastItemGuid.Clear();
                CastItemEntry = 0;
            }

            // if GetItemTarget is also spell reagent
            if (Targets.ItemTargetEntry == itemid)
                Targets.ItemTarget = null;

            pCaster.DestroyItemCount(itemid, itemcount, true);
        }

        foreach (var reagentsCurrency in SpellInfo.ReagentsCurrency)
            pCaster.RemoveCurrency(reagentsCurrency.CurrencyTypesID, -reagentsCurrency.CurrencyCount, CurrencyDestroyReason.Spell);
    }

    private void TakeRunePower(bool didHit)
    {
        if (!Caster.IsTypeId(TypeId.Player) || Caster.AsPlayer.Class != PlayerClass.Deathknight)
            return;

        var player = Caster.AsPlayer;
        _runesState = player.GetRunesState(); // store previous state

        var runeCost = PowerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

        for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
            if (player.GetRuneCooldown(i) == 0 && runeCost > 0)
            {
                player.SetRuneCooldown(i, didHit ? player.GetRuneBaseCooldown() : RuneCooldowns.Miss);
                --runeCost;
            }
    }

    private float Tangent(float x)
    {
        x = (float)Math.Tan(x);

        return x switch
        {
            < 100000.0f and > -100000.0f => x,
            >= 100000.0f                 => 100000.0f,
            <= 100000.0f                 => -100000.0f,
            _                            => 0.0f
        };
    }

    private void TriggerGlobalCooldown()
    {
        if (!CanHaveGlobalCooldown(Caster))
            return;

        var gcd = TimeSpan.FromMilliseconds(SpellInfo.StartRecoveryTime);

        if (gcd == TimeSpan.Zero || SpellInfo.StartRecoveryCategory == 0)
            return;

        if (Caster.IsTypeId(TypeId.Player))
            if (Caster.AsPlayer.GetCommandStatus(PlayerCommandStates.Cooldown))
                return;

        var minGcd = TimeSpan.FromMilliseconds(750);
        var maxGcd = TimeSpan.FromMilliseconds(1500);

        // Global cooldown can't leave range 1..1.5 secs
        // There are some spells (mostly not casted directly by player) that have < 1 sec and > 1.5 sec global cooldowns
        // but as tests show are not affected by any spell mods.
        if (gcd >= minGcd && gcd <= maxGcd)
        {
            // gcd modifier auras are applied only to own spells and only players have such mods
            var modOwner = Caster.SpellModOwner;

            if (modOwner != null)
            {
                var intGcd = (int)gcd.TotalMilliseconds;
                modOwner.ApplySpellMod(SpellInfo, SpellModOp.StartCooldown, ref intGcd, this);
                gcd = TimeSpan.FromMilliseconds(intGcd);
            }

            var isMeleeOrRangedSpell = SpellInfo.DmgClass is SpellDmgClass.Melee or SpellDmgClass.Ranged ||
                                       SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
                                       SpellInfo.HasAttribute(SpellAttr0.IsAbility);

            // Apply haste rating
            if (gcd > minGcd && SpellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell)
            {
                gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * Caster.AsUnit.UnitData.ModSpellHaste);
                var intGcd = (int)gcd.TotalMilliseconds;
                MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
                gcd = TimeSpan.FromMilliseconds(intGcd);
            }

            if (gcd > minGcd && Caster.AsUnit.HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, SpellInfo))
            {
                gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * Caster.AsUnit.UnitData.ModHasteRegen);
                var intGcd = (int)gcd.TotalMilliseconds;
                MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
                gcd = TimeSpan.FromMilliseconds(intGcd);
            }
        }

        Caster.AsUnit.SpellHistory.AddGlobalCooldown(SpellInfo, gcd);
    }

    private bool UpdateChanneledTargetList()
    {
        // Not need check return true
        if (_channelTargetEffectMask.Count == 0)
            return true;

        var channelAuraMask = new HashSet<int>();

        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (spellEffectInfo.IsEffectName(SpellEffectName.ApplyAura))
                channelAuraMask.Add(spellEffectInfo.EffectIndex);

        channelAuraMask.IntersectWith(_channelTargetEffectMask);

        float range = 0;

        if (channelAuraMask.Count != 0)
        {
            range = SpellInfo.GetMaxRange(IsPositive);
            var modOwner = Caster.SpellModOwner;

            modOwner?.ApplySpellMod(SpellInfo, SpellModOp.Range, ref range, this);

            // add little tolerance level
            range += Math.Min(3.0f, range * 0.1f); // 10% but no more than 3.0f
        }

        foreach (var targetInfo in UniqueTargetInfo)
            if (targetInfo.MissCondition == SpellMissInfo.None && Convert.ToBoolean(_channelTargetEffectMask.ToMask() & targetInfo.Effects.ToMask()))
            {
                var unit = Caster.GUID == targetInfo.TargetGuid ? Caster.AsUnit : Caster.ObjectAccessor.GetUnit(Caster, targetInfo.TargetGuid);

                if (unit == null)
                {
                    var unitCaster = Caster.AsUnit;

                    unitCaster?.RemoveChannelObject(targetInfo.TargetGuid);

                    continue;
                }

                if (IsValidDeadOrAliveTarget(unit))
                {
                    if (Convert.ToBoolean(channelAuraMask.ToMask() & targetInfo.Effects.ToMask()))
                    {
                        var aurApp = unit.GetAuraApplication(SpellInfo.Id, _originalCasterGuid);

                        if (aurApp != null)
                        {
                            if (Caster != unit && !Caster.Location.IsWithinDistInMap(unit, range))
                            {
                                targetInfo.Effects.ExceptWith(aurApp.EffectMask);
                                unit.RemoveAura(aurApp);
                                var unitCaster = Caster.AsUnit;

                                unitCaster?.RemoveChannelObject(targetInfo.TargetGuid);

                                continue;
                            }
                        }
                        else // aura is dispelled
                        {
                            var unitCaster = Caster.AsUnit;

                            unitCaster?.RemoveChannelObject(targetInfo.TargetGuid);

                            continue;
                        }
                    }

                    _channelTargetEffectMask.ExceptWith(targetInfo.Effects); // remove from need alive mask effect that have alive target
                }
            }

        // is all effects from m_needAliveTargetMask have alive targets
        return _channelTargetEffectMask.Count == 0;
    }

    private void UpdateEmpoweredSpell(uint difftime)
    {
        if (GetPlayerIfIsEmpowered(out var p))
        {
            if (_empowerState == EmpowerState.None && _empoweredSpellDelta >= 1000)
            {
                _empowerState = EmpowerState.Prepared;
                _empoweredSpellDelta -= 1000;
            }

            if (_empowerState == EmpowerState.CanceledStartup && _empoweredSpellDelta >= 1000)
                _empowerState = EmpowerState.Canceled;

            if (_empowerState == EmpowerState.Prepared && _empoweredSpellStage == 0 && _empowerStages.TryGetValue(_empoweredSpellStage, out var stageinfo)) // send stage 0
            {
                ForEachSpellScript<ISpellOnEpowerSpellStageChange>(s => s.EmpowerSpellStageChange(null, stageinfo));

                var stageZero = new SpellEmpowerSetStage
                {
                    Stage = 0,
                    Caster = p.GUID,
                    CastID = CastId
                };

                p.SendPacket(stageZero);
                _empowerState = EmpowerState.Empowering;
            }

            _empoweredSpellDelta += difftime;

            if (_empowerState == EmpowerState.Empowering && _empowerStages.TryGetValue(_empoweredSpellStage, out stageinfo) && _empoweredSpellDelta >= stageinfo.DurationMs)
            {
                var nextStageId = _empoweredSpellStage;
                nextStageId++;

                if (_empowerStages.TryGetValue(nextStageId, out var nextStage))
                {
                    _empoweredSpellStage = nextStageId;
                    _empoweredSpellDelta -= stageinfo.DurationMs;

                    var stageUpdate = new SpellEmpowerSetStage
                    {
                        Stage = 0,
                        Caster = p.GUID,
                        CastID = CastId
                    };

                    p.SendPacket(stageUpdate);
                    ForEachSpellScript<ISpellOnEpowerSpellStageChange>(s => s.EmpowerSpellStageChange(stageinfo, nextStage));
                }
                else
                    _empowerState = EmpowerState.Finished;
            }

            if (_empowerState is EmpowerState.Finished or EmpowerState.Canceled)
                _timer = 0;
        }
    }

    private bool UpdatePointers()
    {
        if (_originalCasterGuid == Caster.GUID)
            OriginalCaster = Caster.AsUnit;
        else
        {
            OriginalCaster = Caster.ObjectAccessor.GetUnit(Caster, _originalCasterGuid);

            OriginalCaster = OriginalCaster is { Location.IsInWorld: false } ? null : Caster.AsUnit;
        }

        if (!CastItemGuid.IsEmpty && Caster.IsTypeId(TypeId.Player))
        {
            CastItem = Caster.AsPlayer.GetItemByGuid(CastItemGuid);
            CastItemLevel = -1;

            // cast item not found, somehow the item is no longer where we expected
            if (CastItem == null)
                return false;

            // check if the item is really the same, in case it has been wrapped for example
            if (CastItemEntry != CastItem.Entry)
                return false;

            CastItemLevel = (int)CastItem.GetItemLevel(Caster.AsPlayer);
        }

        Targets.Update(Caster);

        // further actions done only for dest targets
        if (!Targets.HasDst)
            return true;

        // cache last transport
        WorldObject transport = null;

        // update effect destinations (in case of moved transport dest target)
        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            var dest = _destTargets[spellEffectInfo.EffectIndex];

            if (dest.TransportGuid.IsEmpty)
                continue;

            if (transport == null || transport.GUID != dest.TransportGuid)
                transport = Caster.ObjectAccessor.GetWorldObject(Caster, dest.TransportGuid);

            if (transport != null)
            {
                dest.Position.Relocate(transport.Location);
                dest.Position.RelocateOffset(dest.TransportOffset);
            }
        }

        return true;
    }

    private void UpdateSpellCastDataAmmo(ref SpellAmmo ammo)
    {
        InventoryType ammoInventoryType = 0;
        uint ammoDisplayID = 0;

        var playerCaster = Caster.AsPlayer;

        if (playerCaster != null)
        {
            var pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

            if (pItem != null)
            {
                ammoInventoryType = pItem.Template.InventoryType;

                if (ammoInventoryType == InventoryType.Thrown)
                    ammoDisplayID = pItem.GetDisplayId(playerCaster);
                else if (playerCaster.HasAura(46699)) // Requires No Ammo
                {
                    ammoDisplayID = 5996; // normal arrow
                    ammoInventoryType = InventoryType.Ammo;
                }
            }
        }
        else
        {
            var unitCaster = Caster.AsUnit;

            if (unitCaster != null)
            {
                uint nonRangedAmmoDisplayID = 0;
                InventoryType nonRangedAmmoInventoryType = 0;

                for (byte i = (int)WeaponAttackType.BaseAttack; i < (int)WeaponAttackType.Max; ++i)
                {
                    var itemId = unitCaster.GetVirtualItemId(i);

                    if (itemId == 0)
                        continue;

                    var itemEntry = _cliDb.ItemStorage.LookupByKey(itemId);

                    if (itemEntry is not { ClassID: ItemClass.Weapon })
                        continue;

                    switch ((ItemSubClassWeapon)itemEntry.SubclassID)
                    {
                        case ItemSubClassWeapon.Thrown:
                            ammoDisplayID = _db2Manager.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
                            ammoInventoryType = itemEntry.inventoryType;

                            break;

                        case ItemSubClassWeapon.Bow:
                        case ItemSubClassWeapon.Crossbow:
                            ammoDisplayID = 5996; // is this need fixing?
                            ammoInventoryType = InventoryType.Ammo;

                            break;

                        case ItemSubClassWeapon.Gun:
                            ammoDisplayID = 5998; // is this need fixing?
                            ammoInventoryType = InventoryType.Ammo;

                            break;

                        default:
                            nonRangedAmmoDisplayID = _db2Manager.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
                            nonRangedAmmoInventoryType = itemEntry.inventoryType;

                            break;
                    }

                    if (ammoDisplayID != 0)
                        break;
                }

                if (ammoDisplayID == 0 && ammoInventoryType == 0)
                {
                    ammoDisplayID = nonRangedAmmoDisplayID;
                    ammoInventoryType = nonRangedAmmoInventoryType;
                }
            }
        }

        ammo.DisplayID = (int)ammoDisplayID;
        ammo.InventoryType = (sbyte)ammoInventoryType;
    }

    // Writes miss and hit targets for a SMSG_SPELL_GO packet
    private void UpdateSpellCastDataTargets(SpellCastData data)
    {
        // This function also fill data for channeled spells:
        // m_needAliveTargetMask req for stop channelig if one target die
        foreach (var targetInfo in UniqueTargetInfo)
        {
            if (targetInfo.Effects.Count == 0) // No effect apply - all immuned add state
                // possibly SPELL_MISS_IMMUNE2 for this??
                targetInfo.MissCondition = SpellMissInfo.Immune2;

            if (targetInfo.MissCondition == SpellMissInfo.None || (targetInfo.MissCondition == SpellMissInfo.Block && !SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked))) // Add only hits and partial blocked
            {
                data.HitTargets.Add(targetInfo.TargetGuid);
                data.HitStatus.Add(new SpellHitStatus(SpellMissInfo.None));

                _channelTargetEffectMask.UnionWith(targetInfo.Effects);
            }
            else // misses
            {
                data.MissTargets.Add(targetInfo.TargetGuid);

                data.MissStatus.Add(new SpellMissStatus(targetInfo.MissCondition, targetInfo.ReflectResult));
            }
        }

        foreach (var targetInfo in _uniqueGoTargetInfo)
            data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

        foreach (var targetInfo in _uniqueCorpseTargetInfo)
            data.HitTargets.Add(targetInfo.TargetGuid); // Always hits

        // Reset m_needAliveTargetMask for non channeled spell
        if (!SpellInfo.IsChanneled)
            _channelTargetEffectMask.Clear();
    }
}