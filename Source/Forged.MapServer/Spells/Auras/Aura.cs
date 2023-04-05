// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Models;
using Serilog;

namespace Forged.MapServer.Spells.Auras;

public class Aura
{
    private const int UPDATE_TARGET_MAP_INTERVAL = 500;

    private static readonly List<IAuraScript> Dummy = new();
    private static readonly List<(IAuraScript, IAuraEffectHandler)> DummyAuraEffects = new();
    private static readonly Dictionary<Unit, HashSet<int>> DummyAuraFill = new();
    private static readonly HashSet<int> DummyHashset = new();
    private readonly Dictionary<Type, List<IAuraScript>> _auraScriptsByType = new();
    private readonly uint _casterLevel;
    private readonly Dictionary<int, Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>> _effectHandlers = new();
    private readonly List<SpellPowerRecord> _periodicCosts = new(); // Periodic costs

     // Aura level (store caster level for correct show level dep amount)
    private readonly List<AuraApplication> _removedApplications = new();

    private ObjectGuid _castItemGuid;
    private ChargeDropEvent _chargeDropEvent;
    private DateTime _lastProcAttemptTime;
    private DateTime _lastProcSuccessTime;
    private List<AuraScript> _loadedScripts = new();
    //might need to be arrays still
    private DateTime _procCooldown;

    private int _timeCla;                 // Timer for power per sec calcultion
    private int _updateTargetMapInterval; // Timer for UpdateTargetMapOfEffect
    public Aura(AuraCreateInfo createInfo)
    {
        SpellInfo = createInfo.SpellInfoInternal;
        CastDifficulty = createInfo.CastDifficulty;
        CastId = createInfo.CastId;
        CasterGuid = createInfo.CasterGuid;
        _castItemGuid = createInfo.CastItemGuid;
        CastItemId = createInfo.CastItemId;
        CastItemLevel = createInfo.CastItemLevel;
        SpellVisual = new SpellCastVisual(createInfo.Caster ? createInfo.Caster.GetCastSpellXSpellVisualId(createInfo.SpellInfoInternal) : createInfo.SpellInfoInternal.GetSpellXSpellVisualId(), 0);
        ApplyTime = GameTime.CurrentTime;
        Owner = createInfo.OwnerInternal;
        _timeCla = 0;
        _updateTargetMapInterval = 0;
        _casterLevel = createInfo.Caster ? createInfo.Caster.Level : SpellInfo.SpellLevel;
        Charges = 0;
        StackAmount = 1;
        IsRemoved = false;
        IsSingleTarget = false;
        IsUsingCharges = false;
        _lastProcAttemptTime = (DateTime.Now - TimeSpan.FromSeconds(10));
        _lastProcSuccessTime = (DateTime.Now - TimeSpan.FromSeconds(120));

        foreach (var power in SpellInfo.PowerCosts)
            if (power != null && (power.ManaPerSecond != 0 || power.PowerPctPerSecond > 0.0f))
                _periodicCosts.Add(power);

        if (!_periodicCosts.Empty())
            _timeCla = 1 * Time.IN_MILLISECONDS;

        MaxDuration = CalcMaxDuration(createInfo.Caster);
        Duration = MaxDuration;
        Charges = CalcMaxCharges(createInfo.Caster);
        IsUsingCharges = Charges != 0;
        // m_casterLevel = cast item level/caster level, caster level should be saved to db, confirmed with sniffs
    }

    public Dictionary<ObjectGuid, AuraApplication> ApplicationMap { get; } = new();
    public long ApplyTime { get; }
    public Dictionary<int, AuraEffect> AuraEffects { get; private set; }
    public AuraObjectType AuraObjType => (Owner.TypeId == TypeId.DynamicObject) ? AuraObjectType.DynObj : AuraObjectType.Unit;
    public Difficulty CastDifficulty { get; }
    public Unit Caster
    {
        get
        {
            if (Owner.GUID == CasterGuid)
                return OwnerAsUnit;

            return Global.ObjAccessor.GetUnit(Owner, CasterGuid);
        }
    }

    public ObjectGuid CasterGuid { get; }
    public byte CasterLevel => (byte)_casterLevel;
    public ObjectGuid CastId { get; }
    public ObjectGuid CastItemGuid
    {
        get => _castItemGuid;
        set => _castItemGuid = value;
    }

    public uint CastItemId { get; set; }
    public int CastItemLevel { get; set; }
    public byte Charges { get; private set; }
    public int Duration { get; private set; }
    public DynamicObject DynobjOwner => Owner.AsDynamicObject;
    public byte? EmpoweredStage { get; set; }
    public Guid Guid { get; } = Guid.NewGuid();
    public uint Id => SpellInfo.Id;
    public bool IsDeathPersistent => SpellInfo.IsDeathPersistent;
    public bool IsExpired => Duration == 0 && _chargeDropEvent == null;
    public bool IsPassive => SpellInfo.IsPassive;
    public bool IsPermanent => MaxDuration == -1;
    public bool IsRemoved { get; private set; }
    public bool IsSingleTarget { get; set; }
    public bool IsUsingCharges { get; set; }
    public int MaxDuration { get; private set; }
    public WorldObject Owner { get; }
    public Unit OwnerAsUnit => Owner.AsUnit;
    public SpellInfo SpellInfo { get; }
    public SpellCastVisual SpellVisual { get; }
    public byte StackAmount { get; private set; }
    //Static Methods
    public static HashSet<int> BuildEffectMaskForOwner(SpellInfo spellProto, HashSet<int> availableEffectMask, WorldObject owner)
    {
        var effMask = new HashSet<int>();

        switch (owner.TypeId)
        {
            case TypeId.Unit:
            case TypeId.Player:
                foreach (var spellEffectInfo in spellProto.Effects)
                    if (spellEffectInfo.IsUnitOwnedAuraEffect)
                        effMask.Add(spellEffectInfo.EffectIndex);

                break;
            case TypeId.DynamicObject:
                foreach (var spellEffectInfo in spellProto.Effects)
                    if (spellEffectInfo.Effect == SpellEffectName.PersistentAreaAura)
                        effMask.Add(spellEffectInfo.EffectIndex);

                break;
            default:
                break;
        }

        effMask.IntersectWith(availableEffectMask);

        return effMask;
    }

    public static int CalcMaxDuration(SpellInfo spellInfo, WorldObject caster)
    {
        Player modOwner = null;
        int maxDuration;

        if (caster != null)
        {
            modOwner = caster.SpellModOwner;
            maxDuration = CalcSpellDuration(spellInfo, caster);
        }
        else
        {
            maxDuration = spellInfo.Duration;
        }

        if (spellInfo.IsPassive && spellInfo.DurationEntry == null)
            maxDuration = -1;

        // IsPermanent() checks max duration (which we are supposed to calculate here)
        if (maxDuration != -1 && modOwner != null)
            modOwner.ApplySpellMod(spellInfo, SpellModOp.Duration, ref maxDuration);

        return maxDuration;
    }

    public static int CalcSpellDuration(SpellInfo spellInfo, WorldObject caster)
    {
        var comboPoints = 0;
        var maxComboPoints = 5;
        var unit = caster.AsUnit;

        if (unit != null)
        {
            comboPoints = unit.GetPower(PowerType.ComboPoints);
            maxComboPoints = unit.GetMaxPower(PowerType.ComboPoints);
        }

        var minduration = spellInfo.Duration;
        var maxduration = spellInfo.MaxDuration;

        int duration;

        if (comboPoints != 0 && minduration != -1 && minduration != maxduration)
            duration = minduration + ((maxduration - minduration) * comboPoints / maxComboPoints);
        else
            duration = minduration;

        return duration;
    }

    public static Aura Create(AuraCreateInfo createInfo)
    {
        // try to get caster of aura
        if (!createInfo.CasterGuid.IsEmpty)
        {
            if (createInfo.CasterGuid.IsUnit)
            {
                if (createInfo.OwnerInternal.GUID == createInfo.CasterGuid)
                    createInfo.Caster = createInfo.OwnerInternal.AsUnit;
                else
                    createInfo.Caster = Global.ObjAccessor.GetUnit(createInfo.OwnerInternal, createInfo.CasterGuid);
            }
        }
        else if (createInfo.Caster != null)
        {
            createInfo.CasterGuid = createInfo.Caster.GUID;
        }

        // check if aura can be owned by owner
        if (createInfo.Owner.IsTypeMask(TypeMask.Unit))
            if (!createInfo.Owner.Location.IsInWorld || createInfo.Owner.AsUnit.IsDuringRemoveFromWorld)
                // owner not in world so don't allow to own not self casted single target auras
                if (createInfo.CasterGuid != createInfo.Owner.GUID && createInfo.SpellInfo.IsSingleTarget())
                    return null;

        Aura aura;

        switch (createInfo.Owner.TypeId)
        {
            case TypeId.Unit:
            case TypeId.Player:
                aura = new UnitAura(createInfo);

                // aura can be removed in Unit::AddAura call
                if (aura.IsRemoved)
                    return null;

                // add owner
                var effMask = createInfo.AuraEffectMask;

                if (createInfo.TargetEffectMask.Count != 0)
                    effMask = createInfo.TargetEffectMask;

                effMask = BuildEffectMaskForOwner(createInfo.SpellInfo, effMask, createInfo.Owner);

                var unit = createInfo.Owner.AsUnit;
                aura.ToUnitAura().AddStaticApplication(unit, effMask);

                break;
            case TypeId.DynamicObject:
                createInfo.AuraEffectMask = BuildEffectMaskForOwner(createInfo.SpellInfo, createInfo.AuraEffectMask, createInfo.Owner);

                aura = new DynObjAura(createInfo);

                break;
            default:
                return null;
        }

        // scripts, etc.
        if (aura.IsRemoved)
            return null;

        return aura;
    }

    public static bool EffectTypeNeedsSendingAmount(AuraType type)
    {
        switch (type)
        {
            case AuraType.OverrideActionbarSpells:
            case AuraType.OverrideActionbarSpellsTriggered:
            case AuraType.ModSpellCategoryCooldown:
            case AuraType.ModMaxCharges:
            case AuraType.ChargeRecoveryMod:
            case AuraType.ChargeRecoveryMultiplier:
                return true;
            default:
                break;
        }

        return false;
    }

    public static Aura TryCreate(AuraCreateInfo createInfo)
    {
        var effMask = createInfo.AuraEffectMask;

        if (createInfo.TargetEffectMask.Count != 0)
            effMask = createInfo.TargetEffectMask;

        effMask = BuildEffectMaskForOwner(createInfo.SpellInfo, effMask, createInfo.Owner);

        if (effMask.Count == 0)
            return null;

        return Create(createInfo);
    }

    public static Aura TryRefreshStackOrCreate(AuraCreateInfo createInfo, bool updateEffectMask = true)
    {
        createInfo.IsRefresh = false;

        createInfo.AuraEffectMask = BuildEffectMaskForOwner(createInfo.SpellInfo, createInfo.AuraEffectMask, createInfo.Owner);
        createInfo.TargetEffectMask = createInfo.AuraEffectMask.ToHashSet();

        var effMask = createInfo.AuraEffectMask;

        if (createInfo.TargetEffectMask.Count != 0)
            effMask = createInfo.TargetEffectMask;

        if (effMask.Count == 0)
            return null;

        var foundAura = createInfo.Owner.AsUnit.TryStackingOrRefreshingExistingAura(createInfo);

        if (foundAura != null)
        {
            // we've here aura, which script triggered removal after modding stack amount
            // check the state here, so we won't create new Aura object
            if (foundAura.IsRemoved)
                return null;

            createInfo.IsRefresh = true;

            // add owner
            var unit = createInfo.Owner.AsUnit;

            // check effmask on owner application (if existing)
            if (updateEffectMask)
            {
                var aurApp = foundAura.GetApplicationOfTarget(unit.GUID);

                aurApp?.UpdateApplyEffectMask(effMask, false);
            }

            return foundAura;
        }
        else
        {
            return Create(createInfo);
        }
    }

    // targets have to be registered and not have effect applied yet to use this function
    public void _ApplyEffectForTargets(int effIndex)
    {
        // prepare list of aura targets
        List<Unit> targetList = new();

        foreach (var app in ApplicationMap.Values)
            if (app.EffectsToApply.Contains(effIndex) && !app.HasEffect(effIndex))
                targetList.Add(app.Target);

        // apply effect to targets
        foreach (var unit in targetList)
            if (GetApplicationOfTarget(unit.GUID) != null)
                // owner has to be in world, or effect has to be applied to self
                unit.ApplyAuraEffect(this, effIndex);
    }

    public virtual void _ApplyForTarget(Unit target, Unit caster, AuraApplication auraApp)
    {
        if (target == null || auraApp == null) return;
        // aura mustn't be already applied on target
        //Cypher.Assert(!IsAppliedOnTarget(target.GetGUID()) && "Aura._ApplyForTarget: aura musn't be already applied on target");

        ApplicationMap[target.GUID] = auraApp;

        // set infinity cooldown state for spells
        if (caster != null && caster.IsTypeId(TypeId.Player))
            if (SpellInfo.IsCooldownStartedOnEvent)
            {
                var castItem = !_castItemGuid.IsEmpty ? caster.AsPlayer.GetItemByGuid(_castItemGuid) : null;
                caster.SpellHistory.StartCooldown(SpellInfo, castItem?.Entry ?? 0, null, true);
            }

        ForEachAuraScript<IAuraOnApply>(a => a.AuraApply());
    }

    public void _InitEffects(HashSet<int> effMask, Unit caster, Dictionary<int, double> baseAmount)
    {
        // shouldn't be in constructor - functions in AuraEffect.AuraEffect use polymorphism
        AuraEffects = new Dictionary<int, AuraEffect>();

        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (effMask.Contains(spellEffectInfo.EffectIndex))
                AuraEffects[spellEffectInfo.EffectIndex] = new AuraEffect(this, spellEffectInfo, baseAmount != null ? baseAmount[spellEffectInfo.EffectIndex] : null, caster);
    }

    public void _RegisterForTargets()
    {
        var caster = Caster;
        UpdateTargetMap(caster, false);
    }

    // removes aura from all targets
    // and marks aura as removed
    public void _Remove(AuraRemoveMode removeMode)
    {
        IsRemoved = true;

        foreach (var pair in ApplicationMap.ToList())
        {
            var aurApp = pair.Value;
            var target = aurApp.Target;
            target.UnapplyAura(aurApp, removeMode);
        }

        if (_chargeDropEvent != null)
        {
            _chargeDropEvent.ScheduleAbort();
            _chargeDropEvent = null;
        }

        ForEachAuraScript<IAuraOnRemove>(a => a.AuraRemoved(removeMode));
    }

    public virtual void UnapplyForTarget(Unit target, Unit caster, AuraApplication auraApp)
    {
        if (target == null || !auraApp.HasRemoveMode || auraApp == null)
            return;

        var app = ApplicationMap.LookupByKey(target.GUID);

        // @todo Figure out why this happens
        if (app == null)
        {
            Log.Logger.Error("Aura.UnapplyForTarget, target: {0}, caster: {1}, spell: {2} was not found in owners application map!",
                             target.GUID.ToString(),
                             caster ? caster.GUID.ToString() : "",
                             auraApp.Base.SpellInfo.Id);

            return;
        }

        // aura has to be already applied

        ApplicationMap.Remove(target.GUID);

        _removedApplications.Add(auraApp);

        // reset cooldown state for spells
        if (caster != null && SpellInfo.IsCooldownStartedOnEvent)
            // note: item based cooldowns and cooldown spell mods with charges ignored (unknown existed cases)
            caster. // note: item based cooldowns and cooldown spell mods with charges ignored (unknown existed cases)
                SpellHistory.SendCooldownEvent(SpellInfo);
    }

    public void AddProcCooldown(SpellProcEntry procEntry, DateTime now)
    {
        // cooldowns should be added to the whole aura (see 51698 area aura)
        var procCooldown = (int)procEntry.Cooldown;
        var caster = Caster;

        var modOwner = caster?.SpellModOwner;

        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.ProcCooldown, ref procCooldown);

        _procCooldown = now + TimeSpan.FromMilliseconds(procCooldown);
    }

    public void ApplyForTargets()
    {
        var caster = Caster;
        UpdateTargetMap(caster);
    }

    public int CalcDispelChance(Unit auraTarget, bool offensive)
    {
        // we assume that aura dispel chance is 100% on start
        // need formula for level difference based chance
        var resistChance = 0;

        // Apply dispel mod from aura caster
        var caster = Caster;

        var modOwner = caster?.SpellModOwner;

        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.DispelResistance, ref resistChance);

        resistChance = resistChance < 0 ? 0 : resistChance;
        resistChance = resistChance > 100 ? 100 : resistChance;

        return 100 - resistChance;
    }

    public byte CalcMaxCharges()
    {
        return CalcMaxCharges(Caster);
    }

    public int CalcMaxDuration(Unit caster)
    {
        return CalcMaxDuration(SpellInfo, caster);
    }

    public int CalcMaxDuration()
    {
        return CalcMaxDuration(Caster);
    }

    public uint CalcMaxStackAmount()
    {
        var maxStackAmount = SpellInfo.StackAmount;
        var caster = Caster;

        var modOwner = caster?.SpellModOwner;

        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.MaxAuraStacks, ref maxStackAmount);

        return maxStackAmount;
    }

    public double CalcPPMProcChance(Unit actor)
    {
        // Formula see http://us.battle.net/wow/en/forum/topic/8197741003#1
        var ppm = SpellInfo.CalcProcPPM(actor, CastItemLevel);
        var averageProcInterval = 60.0f / ppm;

        var currentTime = GameTime.Now;
        var secondsSinceLastAttempt = Math.Min((float)(currentTime - _lastProcAttemptTime).TotalSeconds, 10.0f);
        var secondsSinceLastProc = Math.Min((float)(currentTime - _lastProcSuccessTime).TotalSeconds, 1000.0f);

        var chance = Math.Max(1.0f, 1.0f + ((secondsSinceLastProc / averageProcInterval - 1.5f) * 3.0f)) * ppm * secondsSinceLastAttempt / 60.0f;
        MathFunctions.RoundToInterval(ref chance, 0.0f, 1.0f);

        return chance * 100.0f;
    }

    public bool CanBeSaved()
    {
        if (IsPassive)
            return false;

        if (SpellInfo.IsChanneled)
            return false;

        // Check if aura is single target, not only spell info
        if (CasterGuid != Owner.GUID)
        {
            // owner == caster for area auras, check for possible bad data in DB
            foreach (var spellEffectInfo in SpellInfo.Effects)
            {
                if (!spellEffectInfo.IsEffect())
                    continue;

                if (spellEffectInfo.IsTargetingArea || spellEffectInfo.IsAreaAuraEffect)
                    return false;
            }

            if (IsSingleTarget || SpellInfo.IsSingleTarget())
                return false;
        }

        if (SpellInfo.HasAttribute(SpellCustomAttributes.AuraCannotBeSaved))
            return false;

        // don't save auras removed by proc system
        if (IsUsingCharges && Charges == 0)
            return false;

        // don't save permanent auras triggered by items, they'll be recasted on login if necessary
        if (!CastItemGuid.IsEmpty && IsPermanent)
            return false;

        return true;
    }

    public bool CanStackWith(Aura existingAura)
    {
        // Can stack with self
        if (this == existingAura)
            return true;

        var sameCaster = CasterGuid == existingAura.CasterGuid;
        var existingSpellInfo = existingAura.SpellInfo;

        // Dynobj auras do not stack when they come from the same spell cast by the same caster
        if (AuraObjType == AuraObjectType.DynObj || existingAura.AuraObjType == AuraObjectType.DynObj)
        {
            if (sameCaster && SpellInfo.Id == existingSpellInfo.Id)
                return false;

            return true;
        }

        // passive auras don't stack with another rank of the spell cast by same caster
        if (IsPassive && sameCaster && (SpellInfo.IsDifferentRankOf(existingSpellInfo) || (SpellInfo.Id == existingSpellInfo.Id && _castItemGuid.IsEmpty)))
            return false;

        foreach (var spellEffectInfo in existingSpellInfo.Effects)
            // prevent remove triggering aura by triggered aura
            if (spellEffectInfo.TriggerSpell == Id)
                return true;

        foreach (var spellEffectInfo in SpellInfo.Effects)
            // prevent remove triggered aura by triggering aura refresh
            if (spellEffectInfo.TriggerSpell == existingAura.Id)
                return true;

        // check spell specific stack rules
        if (SpellInfo.IsAuraExclusiveBySpecificWith(existingSpellInfo) || (sameCaster && SpellInfo.IsAuraExclusiveBySpecificPerCasterWith(existingSpellInfo)))
            return false;

        // check spell group stack rules
        switch (Global.SpellMgr.CheckSpellGroupStackRules(SpellInfo, existingSpellInfo))
        {
            case SpellGroupStackRule.Exclusive:
            case SpellGroupStackRule.ExclusiveHighest: // if it reaches this point, existing aura is lower/equal
                return false;
            case SpellGroupStackRule.ExclusiveFromSameCaster:
                if (sameCaster)
                    return false;

                break;
            case SpellGroupStackRule.Default:
            case SpellGroupStackRule.ExclusiveSameEffect:
            default:
                break;
        }

        if (SpellInfo.SpellFamilyName != existingSpellInfo.SpellFamilyName)
            return true;

        if (!sameCaster)
        {
            // Channeled auras can stack if not forbidden by db or aura type
            if (existingAura.SpellInfo.IsChanneled)
                return true;

            if (SpellInfo.HasAttribute(SpellAttr3.DotStackingRule))
                return true;

            // check same periodic auras
            bool hasPeriodicNonAreaEffect(SpellInfo spellInfo)
            {
                foreach (var spellEffectInfo in spellInfo.Effects)
                    switch (spellEffectInfo.ApplyAuraName)
                    {
                        // DOT or HOT from different casters will stack
                        case AuraType.PeriodicDamage:
                        case AuraType.PeriodicDummy:
                        case AuraType.PeriodicHeal:
                        case AuraType.PeriodicTriggerSpell:
                        case AuraType.PeriodicEnergize:
                        case AuraType.PeriodicManaLeech:
                        case AuraType.PeriodicLeech:
                        case AuraType.PowerBurn:
                        case AuraType.ObsModPower:
                        case AuraType.ObsModHealth:
                        case AuraType.PeriodicTriggerSpellWithValue:
                        {
                            // periodic auras which target areas are not allowed to stack this way (replenishment for example)
                            if (spellEffectInfo.IsTargetingArea)
                                return false;

                            return true;
                        }
                        default:
                            break;
                    }

                return false;
            }

            if (hasPeriodicNonAreaEffect(SpellInfo) && hasPeriodicNonAreaEffect(existingSpellInfo))
                return true;
        }

        if (HasEffectType(AuraType.ControlVehicle) && existingAura.HasEffectType(AuraType.ControlVehicle))
        {
            Vehicle veh = null;

            if (Owner.AsUnit)
                veh = Owner.AsUnit.VehicleKit;

            if (!veh) // We should probably just let it stack. Vehicle system will prevent undefined behaviour later
                return true;

            if (veh.GetAvailableSeatCount() == 0)
                return false; // No empty seat available

            return true; // Empty seat available (skip rest)
        }

        if (HasEffectType(AuraType.ShowConfirmationPrompt) || HasEffectType(AuraType.ShowConfirmationPromptWithDifficulty))
            if (existingAura.HasEffectType(AuraType.ShowConfirmationPrompt) || existingAura.HasEffectType(AuraType.ShowConfirmationPromptWithDifficulty))
                return false;

        // spell of same spell rank chain
        if (SpellInfo.IsRankOf(existingSpellInfo))
        {
            // don't allow passive area auras to stack
            if (SpellInfo.IsMultiSlotAura && !IsArea())
                return true;

            if (!CastItemGuid.IsEmpty && !existingAura.CastItemGuid.IsEmpty)
                if (CastItemGuid != existingAura.CastItemGuid && SpellInfo.HasAttribute(SpellCustomAttributes.EnchantProc))
                    return true;

            // same spell with same caster should not stack
            return false;
        }

        return true;
    }

    public void ConsumeProcCharges(SpellProcEntry procEntry)
    {
        // Remove aura if we've used last charge to proc
        if (procEntry.AttributesMask.HasFlag(ProcAttributes.UseStacksForCharges))
            ModStackAmount(-1);
        else if (IsUsingCharges)
            if (Charges == 0)
                Remove();
    }

    public virtual void Dispose()
    {
        // unload scripts
        foreach (var itr in _loadedScripts.ToList())
            itr._Unload();

        if (ApplicationMap.Count > 0)
            foreach (var app in ApplicationMap.Values.ToArray())
                OwnerAsUnit?.RemoveAura(app);

        ApplicationMap.Clear();
        _DeleteRemovedApplications();
    }

    public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        return ModCharges(-1, removeMode);
    }

    public void DropChargeDelayed(uint delay, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        // aura is already during delayed charge drop
        if (_chargeDropEvent != null)
            return;

        // only units have events
        var owner = Owner.AsUnit;

        if (!owner)
            return;

        _chargeDropEvent = new ChargeDropEvent(this, removeMode);
        owner.Events.AddEvent(_chargeDropEvent, owner.Events.CalculateTime(TimeSpan.FromMilliseconds(delay)));
    }

    public virtual Dictionary<Unit, HashSet<int>> FillTargetMap(Unit caster)
    {
        return DummyAuraFill;
    }

    public void ForEachAuraScript<T>(Action<T> action) where T : IAuraScript
    {
        foreach (T script in GetAuraScripts<T>())
            try
            {
                action.Invoke(script);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public AuraKey GenerateKey(out uint recalculateMask)
    {
        AuraKey key = new(CasterGuid, CastItemGuid, Id, 0);
        recalculateMask = 0;

        foreach (var aurEff in AuraEffects)
        {
            key.EffectMask |= 1u << aurEff.Key;

            if (aurEff.Value.CanBeRecalculated())
                recalculateMask |= 1u << aurEff.Key;
        }

        return key;
    }

    public List<AuraApplication> GetApplicationList()
    {
        var applicationList = new List<AuraApplication>();

        foreach (var aurApp in ApplicationMap.Values)
            if (aurApp.EffectMask.Count != 0)
                applicationList.Add(aurApp);

        return applicationList;
    }

    public AuraApplication GetApplicationOfTarget(ObjectGuid guid)
    {
        return ApplicationMap.LookupByKey(guid);
    }

    public List<IAuraScript> GetAuraScripts<T>() where T : IAuraScript
    {
        if (_auraScriptsByType.TryGetValue(typeof(T), out var scripts))
            return scripts;

        return Dummy;
    }

    public AuraEffect GetEffect(int index)
    {
        if (AuraEffects.TryGetValue(index, out var val))
            return val;

        return null;
    }

    public List<(IAuraScript, IAuraEffectHandler)> GetEffectScripts(AuraScriptHookType h, int index)
    {
        if (_effectHandlers.TryGetValue(index, out var effDict) &&
            effDict.TryGetValue(h, out var scripts))
            return scripts;

        return DummyAuraEffects;
    }

    public HashSet<int> GetProcEffectMask(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
    {
        SpellProcEntry procEntry = null;
        ForEachAuraScript<IAuraOverrideProcInfo>(a => procEntry = a.SpellProcEntry);

        if (procEntry == null)
            Global.SpellMgr.GetSpellProcEntry(SpellInfo);

        // only auras with spell proc entry can trigger proc
        if (procEntry == null)
            return DummyHashset;

        // check spell triggering us
        var spell = eventInfo.ProcSpell;

        if (spell)
        {
            // Do not allow auras to proc from effect triggered from itself
            if (spell.IsTriggeredByAura(SpellInfo))
                return DummyHashset;

            // check if aura can proc when spell is triggered (exception for hunter auto shot & wands)
            if (!spell.TriggeredAllowProc && !SpellInfo.HasAttribute(SpellAttr3.CanProcFromProcs) && !procEntry.AttributesMask.HasFlag(ProcAttributes.TriggeredCanProc) && !eventInfo.TypeMask.HasFlag(ProcFlags.AutoAttackMask))
                if (spell.IsTriggered && !spell.SpellInfo.HasAttribute(SpellAttr3.NotAProc))
                    return DummyHashset;

            if (spell.CastItem != null && procEntry.AttributesMask.HasFlag(ProcAttributes.CantProcFromItemCast))
                return DummyHashset;

            if (spell.SpellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs) && SpellInfo.HasAttribute(SpellAttr6.AuraIsWeaponProc))
                return DummyHashset;

            if (SpellInfo.HasAttribute(SpellAttr12.OnlyProcFromClassAbilities) && !spell.SpellInfo.HasAttribute(SpellAttr13.AllowClassAbilityProcs))
                return DummyHashset;
        }

        // check don't break stealth attr present
        if (SpellInfo.HasAura(AuraType.ModStealth))
        {
            var eventSpellInfo = eventInfo.SpellInfo;

            if (eventSpellInfo != null)
                if (eventSpellInfo.HasAttribute(SpellCustomAttributes.DontBreakStealth))
                    return DummyHashset;
        }

        // check if we have charges to proc with
        if (IsUsingCharges)
        {
            if (Charges == 0)
                return DummyHashset;

            if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReqSpellmod))
            {
                var eventSpell = eventInfo.ProcSpell;

                if (eventSpell != null)
                    if (!eventSpell.AppliedMods.Contains(this))
                        return DummyHashset;
            }
        }

        // check proc cooldown
        if (IsProcOnCooldown(now))
            return DummyHashset;

        // do checks against db data

        if (!SpellManager.CanSpellTriggerProcOnEvent(procEntry, eventInfo))
            return DummyHashset;

        // do checks using conditions table
        if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.SpellProc, Id, eventInfo.Actor, eventInfo.ActionTarget))
            return DummyHashset;

        // AuraScript Hook
        var check = CallScriptCheckProcHandlers(aurApp, eventInfo);

        if (!check)
            return DummyHashset;

        // At least one effect has to pass checks to proc aura
        var procEffectMask = aurApp.EffectMask.ToHashSet();

        foreach (var aurEff in AuraEffects)
            if (procEffectMask.Contains(aurEff.Key))
                if ((procEntry.DisableEffectsMask & (1u << aurEff.Key)) != 0 || !aurEff.Value.CheckEffectProc(aurApp, eventInfo))
                    procEffectMask.Remove(aurEff.Key);

        if (procEffectMask.Count == 0)
            return DummyHashset;

        // @todo
        // do allow additional requirements for procs
        // this is needed because this is the last moment in which you can prevent aura charge drop on proc
        // and possibly a way to prevent default checks (if there're going to be any)

        // Check if current equipment meets aura requirements
        // do that only for passive spells
        // @todo this needs to be unified for all kinds of auras
        var target = aurApp.Target;

        if (IsPassive && target.IsPlayer && SpellInfo.EquippedItemClass != ItemClass.None)
            if (!SpellInfo.HasAttribute(SpellAttr3.NoProcEquipRequirement))
            {
                Item item = null;

                if (SpellInfo.EquippedItemClass == ItemClass.Weapon)
                {
                    if (target.AsPlayer.IsInFeralForm)
                        return DummyHashset;

                    var damageInfo = eventInfo.DamageInfo;

                    if (damageInfo != null)
                    {
                        if (damageInfo.AttackType != WeaponAttackType.OffAttack)
                            item = target.AsPlayer.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                        else
                            item = target.AsPlayer.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                    }
                }
                else if (SpellInfo.EquippedItemClass == ItemClass.Armor)
                {
                    // Check if player is wearing shield
                    item = target.AsPlayer.GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                }

                if (!item || item.IsBroken || !item.IsFitToSpellRequirements(SpellInfo))
                    return DummyHashset;
            }

        if (SpellInfo.HasAttribute(SpellAttr3.OnlyProcOutdoors))
            if (!target.Location.IsOutdoors)
                return DummyHashset;

        if (SpellInfo.HasAttribute(SpellAttr3.OnlyProcOnCaster))
            if (target.GUID != CasterGuid)
                return DummyHashset;

        if (!SpellInfo.HasAttribute(SpellAttr4.AllowProcWhileSitting))
            if (!target.IsStandState)
                return DummyHashset;

        var success = RandomHelper.randChance(CalcProcChance(procEntry, eventInfo));

        SetLastProcAttemptTime(now);

        if (success)
            return procEffectMask;

        return DummyHashset;
    }

    public T GetScript<T>() where T : AuraScript
    {
        return (T)GetScriptByType(typeof(T));
    }

    public AuraScript GetScriptByType(Type type)
    {
        foreach (var auraScript in _loadedScripts)
            if (auraScript.GetType() == type)
                return auraScript;

        return null;
    }
    public void HandleAllEffects(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        foreach (var effect in AuraEffects)
            if (!IsRemoved)
                effect.Value.HandleEffect(aurApp, mode, apply);
    }

    // trigger effects on real aura apply/remove
    public void HandleAuraSpecificMods(AuraApplication aurApp, Unit caster, bool apply, bool onReapply)
    {
        var target = aurApp.Target;
        var removeMode = aurApp.RemoveMode;
        // handle spell_area table
        var saBounds = Global.SpellMgr.GetSpellAreaForAuraMapBounds(Id);

        if (saBounds != null)
        {
            foreach (var spellArea in saBounds)
                // some auras remove at aura remove
                if (spellArea.Flags.HasAnyFlag(SpellAreaFlag.AutoRemove) && !spellArea.IsFitToRequirements((Player)target, target.Location.Zone, target.Location.Area))
                    target.RemoveAura(spellArea.SpellId);
                // some auras applied at aura apply
                else if (spellArea.Flags.HasAnyFlag(SpellAreaFlag.AutoCast))
                    if (!target.HasAura(spellArea.SpellId))
                        target.CastSpell(target, spellArea.SpellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));
        }

        // handle spell_linked_spell table
        if (!onReapply)
        {
            // apply linked auras
            if (apply)
            {
                var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, Id);

                if (spellTriggered != null)
                    foreach (var spell in spellTriggered)
                        if (spell < 0)
                            target.ApplySpellImmune(Id, SpellImmunity.Id, (uint)-spell, true);
                        else
                        {
                            caster?.AddAura((uint)spell, target);
                        }
            }
            else
            {
                // remove linked auras
                var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Remove, Id);

                if (spellTriggered != null)
                    foreach (var spell in spellTriggered)
                        if (spell < 0)
                            target.RemoveAura((uint)-spell);
                        else if (removeMode != AuraRemoveMode.Death)
                            target.CastSpell(target,
                                             (uint)spell,
                                             new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                 .SetOriginalCaster(CasterGuid)
                                                 .SetOriginalCastId(CastId));

                spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, Id);

                if (spellTriggered != null)
                    foreach (var id in spellTriggered)
                        if (id < 0)
                            target.ApplySpellImmune(Id, SpellImmunity.Id, (uint)-id, false);
                        else
                            target.RemoveAura((uint)id, CasterGuid, removeMode);
            }
        }
        else if (apply)
        {
            // modify stack amount of linked auras
            var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, Id);

            if (spellTriggered != null)
                foreach (var id in spellTriggered)
                    if (id > 0)
                    {
                        var triggeredAura = target.GetAura((uint)id, CasterGuid);

                        triggeredAura?.ModStackAmount(StackAmount - triggeredAura.StackAmount);
                    }
        }

        // mods at aura apply
        if (apply)
            switch (SpellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    switch (Id)
                    {
                        case 33572: // Gronn Lord's Grasp, becomes stoned
                            if (StackAmount >= 5 && !target.HasAura(33652))
                                target.CastSpell(target, 33652, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));

                            break;
                        case 50836: //Petrifying Grip, becomes stoned
                            if (StackAmount >= 5 && !target.HasAura(50812))
                                target.CastSpell(target, 50812, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));

                            break;
                        case 60970: // Heroic Fury (remove Intercept cooldown)
                            if (target.IsTypeId(TypeId.Player))
                                target.SpellHistory.ResetCooldown(20252, true);

                            break;
                    }

                    break;
                case SpellFamilyNames.Druid:
                    if (caster == null)
                        break;

                    // Rejuvenation
                    if (SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x10u) && GetEffect(0) != null)
                        // Druid T8 Restoration 4P Bonus
                        if (caster.HasAura(64760))
                        {
                            CastSpellExtraArgs args = new(GetEffect(0));
                            args.AddSpellMod(SpellValueMod.BasePoint0, GetEffect(0).Amount);
                            caster.CastSpell(target, 64801, args);
                        }

                    break;
            }
        // mods at aura remove
        else
            switch (SpellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Mage:
                    switch (Id)
                    {
                        case 66: // Invisibility
                            if (removeMode != AuraRemoveMode.Expire)
                                break;

                            target.CastSpell(target, 32612, new CastSpellExtraArgs(GetEffect(1)));

                            break;
                        default:
                            break;
                    }

                    break;
                case SpellFamilyNames.Priest:
                    if (caster == null)
                        break;

                    // Power word: shield
                    if (removeMode == AuraRemoveMode.EnemySpell && SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
                    {
                        // Rapture
                        var aura = caster.GetAuraOfRankedSpell(47535);

                        if (aura != null)
                        {
                            // check cooldown
                            if (caster.IsTypeId(TypeId.Player))
                            {
                                if (caster.SpellHistory.HasCooldown(aura.SpellInfo))
                                {
                                    // This additional check is needed to add a minimal delay before cooldown in in effect
                                    // to allow all bubbles broken by a single damage source proc mana return
                                    if (caster.SpellHistory.GetRemainingCooldown(aura.SpellInfo) <= TimeSpan.FromSeconds(11))
                                        break;
                                }
                                else // and add if needed
                                {
                                    caster.SpellHistory.AddCooldown(aura.Id, 0, TimeSpan.FromSeconds(12));
                                }
                            }

                            // effect on caster
                            var aurEff = aura.GetEffect(0);

                            if (aurEff != null)
                            {
                                var multiplier = aurEff.Amount;
                                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                args.SetOriginalCastId(CastId);
                                args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(caster.GetMaxPower(PowerType.Mana), multiplier));
                                caster.CastSpell(caster, 47755, args);
                            }
                        }
                    }

                    break;
                case SpellFamilyNames.Rogue:
                    // Remove Vanish on stealth remove
                    if (Id == 1784)
                        target.RemoveAurasWithFamily(SpellFamilyNames.Rogue, new FlagArray128(0x0000800), target.GUID);

                    break;
            }

        // mods at aura apply or remove
        switch (SpellInfo.SpellFamilyName)
        {
            case SpellFamilyNames.Hunter:
                switch (Id)
                {
                    case 19574: // Bestial Wrath
                        // The Beast Within cast on owner if talent present
                        var owner = target.OwnerUnit;

                        if (owner != null)
                            // Search talent
                            if (owner.HasAura(34692))
                            {
                                if (apply)
                                    owner.CastSpell(owner, 34471, new CastSpellExtraArgs(GetEffect(0)));
                                else
                                    owner.RemoveAura(34471);
                            }

                        break;
                }

                break;
            case SpellFamilyNames.Paladin:
                switch (Id)
                {
                    case 31821:
                        // Aura Mastery Triggered Spell Handler
                        // If apply Concentration Aura . trigger . apply Aura Mastery Immunity
                        // If remove Concentration Aura . trigger . remove Aura Mastery Immunity
                        // If remove Aura Mastery . trigger . remove Aura Mastery Immunity
                        // Do effects only on aura owner
                        if (CasterGuid != target.GUID)
                            break;

                        if (apply)
                        {
                            if ((SpellInfo.Id == 31821 && target.HasAura(19746, CasterGuid)) || (SpellInfo.Id == 19746 && target.HasAura(31821)))
                                target.CastSpell(target, 64364, new CastSpellExtraArgs(true));
                        }
                        else
                        {
                            target.RemoveAurasDueToSpell(64364, CasterGuid);
                        }

                        break;
                    case 31842: // Divine Favor
                        // Item - Paladin T10 Holy 2P Bonus
                        if (target.HasAura(70755))
                        {
                            if (apply)
                                target.CastSpell(target, 71166, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(CastId));
                            else
                                target.RemoveAura(71166);
                        }

                        break;
                }

                break;
            case SpellFamilyNames.Warlock:
                // Drain Soul - If the target is at or below 25% health, Drain Soul causes four times the normal damage
                if (SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
                {
                    if (caster == null)
                        break;

                    if (apply)
                    {
                        if (target != caster && !target.HealthAbovePct(25))
                            caster.CastSpell(caster, 100001, new CastSpellExtraArgs(true));
                    }
                    else
                    {
                        if (target != caster)
                            caster.RemoveAura(Id);
                        else
                            caster.RemoveAura(100001);
                    }
                }

                break;
        }
    }

    public bool HasEffect(int index)
    {
        return GetEffect(index) != null;
    }

    public bool HasEffectType(AuraType type)
    {
        foreach (var eff in AuraEffects)
            if (eff.Value.AuraType == type)
                return true;

        return false;
    }

    public bool HasMoreThanOneEffectForType(AuraType auraType)
    {
        uint count = 0;

        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.ApplyAuraName == auraType)
                ++count;

        return count > 1;
    }

    public bool IsAppliedOnTarget(ObjectGuid guid)
    {
        return ApplicationMap.ContainsKey(guid);
    }

    public bool IsArea()
    {
        foreach (var spellEffectInfo in SpellInfo.Effects)
            if (HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.IsAreaAuraEffect)
                return true;

        return false;
    }

    public bool IsProcOnCooldown(DateTime now)
    {
        return _procCooldown > now;
    }

    public bool IsRemovedOnShapeLost(Unit target)
    {
        return CasterGuid == target.GUID && SpellInfo.Stances != 0 && !SpellInfo.HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm) && !SpellInfo.HasAttribute(SpellAttr0.NotShapeshifted);
    }

    public bool IsSingleTargetWith(Aura aura)
    {
        // Same spell?
        if (SpellInfo.IsRankOf(aura.SpellInfo))
            return true;

        var spec = SpellInfo.GetSpellSpecific();

        // spell with single target specific types
        switch (spec)
        {
            case SpellSpecificType.MagePolymorph:
                if (aura.SpellInfo.GetSpellSpecific() == spec)
                    return true;

                break;
            default:
                break;
        }

        return false;
    }

    public bool IsUsingStacks()
    {
        return SpellInfo.StackAmount > 0;
    }

    public void LoadScripts()
    {
        _loadedScripts = ScriptManager.CreateAuraScripts(SpellInfo.Id, this);

        foreach (var script in _loadedScripts)
        {
            Log.Logger.Debug("Aura.LoadScripts: Script `{0}` for aura `{1}` is loaded now", script._GetScriptName(), SpellInfo.Id);
            script.Register();

            if (script is IAuraScript)
                foreach (var iFace in script.GetType().GetInterfaces())
                {
                    if (iFace.Name is nameof(IBaseSpellScript) or nameof(IAuraScript))
                        continue;

                    if (!_auraScriptsByType.TryGetValue(iFace, out var spellScripts))
                    {
                        spellScripts = new List<IAuraScript>();
                        _auraScriptsByType[iFace] = spellScripts;
                    }

                    spellScripts.Add(script);
                    RegisterSpellEffectHandler(script);
                }
        }
    }

    public bool ModCharges(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        if (IsUsingCharges)
        {
            var charges = Charges + num;
            int maxCharges = CalcMaxCharges();

            // limit charges (only on charges increase, charges may be changed manually)
            if ((num > 0) && (charges > maxCharges))
            {
                charges = maxCharges;
            }
            // we're out of charges, remove
            else if (charges <= 0)
            {
                Remove(removeMode);

                return true;
            }

            SetCharges((byte)charges);
        }

        return false;
    }

    public void ModChargesDelayed(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        _chargeDropEvent = null;
        ModCharges(num, removeMode);
    }

    /// <summary>
    ///     Adds the given duration to the auras duration.
    /// </summary>
    public void ModDuration(int duration, bool withMods = false, bool updateMaxDuration = false)
    {
        SetDuration(Duration + duration, withMods, updateMaxDuration);
    }

    public void ModDuration(double duration, bool withMods = false, bool updateMaxDuration = false)
    {
        SetDuration((int)duration, withMods, updateMaxDuration);
    }

    public bool ModStackAmount(double num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
    {
        return ModStackAmount((int)num, removeMode, resetPeriodicTimer);
    }

    public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
    {
        var stackAmount = StackAmount + num;
        var maxStackAmount = CalcMaxStackAmount();

        // limit the stack amount (only on stack increase, stack amount may be changed manually)
        if ((num > 0) && (stackAmount > maxStackAmount))
        {
            // not stackable aura - set stack amount to 1
            if (SpellInfo.StackAmount == 0)
                stackAmount = 1;
            else
                stackAmount = (int)SpellInfo.StackAmount;
        }
        // we're out of stacks, remove
        else if (stackAmount <= 0)
        {
            Remove(removeMode);

            return true;
        }

        var refresh = stackAmount >= StackAmount && (SpellInfo.StackAmount != 0 || (!SpellInfo.HasAttribute(SpellAttr1.AuraUnique) && !SpellInfo.HasAttribute(SpellAttr5.AuraUniquePerCaster)));

        // Update stack amount
        SetStackAmount((byte)stackAmount);

        if (refresh)
        {
            RefreshTimers(resetPeriodicTimer);

            // reset charges
            SetCharges(CalcMaxCharges());
        }

        SetNeedClientUpdateForTargets();

        return false;
    }

    public void PrepareProcChargeDrop(SpellProcEntry procEntry, ProcEventInfo eventInfo)
    {
        // take one charge, aura expiration will be handled in Aura.TriggerProcOnEvent (if needed)
        if (!procEntry.AttributesMask.HasAnyFlag(ProcAttributes.UseStacksForCharges) && IsUsingCharges && (eventInfo.SpellInfo == null || !eventInfo.SpellInfo.HasAttribute(SpellAttr6.DoNotConsumeResources)))
        {
            --Charges;
            SetNeedClientUpdateForTargets();
        }
    }

    public void PrepareProcToTrigger(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
    {
        var prepare = CallScriptPrepareProcHandlers(aurApp, eventInfo);

        if (!prepare)
            return;

        var procEntry = Global.SpellMgr.GetSpellProcEntry(SpellInfo);

        PrepareProcChargeDrop(procEntry, eventInfo);

        // cooldowns should be added to the whole aura (see 51698 area aura)
        AddProcCooldown(procEntry, now);

        SetLastProcSuccessTime(now);
    }

    public void RecalculateAmountOfEffects()
    {
        var caster = Caster;

        foreach (var effect in AuraEffects)
            if (!IsRemoved)
                effect.Value.RecalculateAmount(caster);
    }

    public void RefreshDuration(bool withMods = false)
    {
        var caster = Caster;

        if (withMods && caster)
        {
            var duration = SpellInfo.MaxDuration;

            // Calculate duration of periodics affected by haste.
            if (SpellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
                duration = (int)(duration * caster.UnitData.ModCastingSpeed);

            SetMaxDuration(duration);
            SetDuration(duration);
        }
        else
        {
            SetDuration(MaxDuration);
        }

        if (!_periodicCosts.Empty())
            _timeCla = 1 * Time.IN_MILLISECONDS;

        // also reset periodic counters
        foreach (var aurEff in AuraEffects)
            aurEff.Value.ResetTicks();
    }

    public virtual void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        ForEachAuraScript<IAuraOnRemove>(a => a.AuraRemoved(removeMode));
    }

    public void ResetProcCooldown()
    {
        _procCooldown = DateTime.Now;
    }

    public void SetCharges(int charges)
    {
        if (Charges == charges)
            return;

        Charges = (byte)charges;
        IsUsingCharges = Charges != 0;
        SetNeedClientUpdateForTargets();
    }

    public void SetDuration(double duration, bool withMods = false, bool updateMaxDuration = false)
    {
        SetDuration((int)duration, withMods, updateMaxDuration);
    }

    public void SetDuration(int duration, bool withMods = false, bool updateMaxDuration = false)
    {
        if (withMods)
        {
            var caster = Caster;

            if (caster)
            {
                var modOwner = caster.SpellModOwner;

                if (modOwner)
                    modOwner.ApplySpellMod(SpellInfo, SpellModOp.Duration, ref duration);
            }
        }

        if (updateMaxDuration && duration > MaxDuration)
            MaxDuration = duration;

        Duration = duration;
        SetNeedClientUpdateForTargets();
    }

    public void SetLastProcAttemptTime(DateTime lastProcAttemptTime)
    {
        _lastProcAttemptTime = lastProcAttemptTime;
    }

    public void SetLastProcSuccessTime(DateTime lastProcSuccessTime)
    {
        _lastProcSuccessTime = lastProcSuccessTime;
    }

    public void SetLoadedState(int maxduration, int duration, int charges, byte stackamount, uint recalculateMask, Dictionary<int, double> amount)
    {
        MaxDuration = maxduration;
        Duration = duration;
        Charges = (byte)charges;
        IsUsingCharges = Charges != 0;
        StackAmount = stackamount;
        var caster = Caster;

        foreach (var effect in AuraEffects)
        {
            effect.Value.SetAmount(amount[effect.Value.EffIndex]);
            effect.Value.SetCanBeRecalculated(Convert.ToBoolean(recalculateMask & (1 << effect.Value.EffIndex)));
            effect.Value.CalculatePeriodic(caster, false, true);
            effect.Value.CalculateSpellMod();
            effect.Value.RecalculateAmount(caster);
        }
    }

    public void SetMaxDuration(double duration)
    {
        SetMaxDuration((int)duration);
    }

    public void SetMaxDuration(int duration)
    {
        MaxDuration = duration;
    }

    public void SetNeedClientUpdateForTargets()
    {
        foreach (var app in ApplicationMap.Values)
            app.SetNeedClientUpdate();
    }

    public void SetStackAmount(byte stackAmount)
    {
        StackAmount = stackAmount;
        var caster = Caster;

        var applications = GetApplicationList();

        foreach (var aurApp in applications)
            if (!aurApp.HasRemoveMode)
                HandleAuraSpecificMods(aurApp, caster, false, true);

        foreach (var aurEff in AuraEffects)
            aurEff.Value.ChangeAmount(aurEff.Value.CalculateAmount(caster), false, true);

        foreach (var aurApp in applications)
            if (!aurApp.HasRemoveMode)
                HandleAuraSpecificMods(aurApp, caster, true, true);

        SetNeedClientUpdateForTargets();
    }

    public DynObjAura ToDynObjAura()
    {
        if (AuraObjType == AuraObjectType.DynObj) return (DynObjAura)this;

        return null;
    }

    public UnitAura ToUnitAura()
    {
        if (AuraObjType == AuraObjectType.Unit) return (UnitAura)this;

        return null;
    }

    public void TriggerProcOnEvent(HashSet<int> procEffectMask, AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var prevented = CallScriptProcHandlers(aurApp, eventInfo);

        if (!prevented)
        {
            foreach (var aurEff in AuraEffects)
            {
                if (!procEffectMask.Contains(aurEff.Key))
                    continue;

                // OnEffectProc / AfterEffectProc hooks handled in AuraEffect.HandleProc()
                if (aurApp.HasEffect(aurEff.Key))
                    aurEff.Value.HandleProc(aurApp, eventInfo);
            }

            CallScriptAfterProcHandlers(aurApp, eventInfo);
        }

        ConsumeProcCharges(Global.SpellMgr.GetSpellProcEntry(SpellInfo));
    }

    public bool TryGetEffect(int index, out AuraEffect val)
    {
        if (AuraEffects.TryGetValue(index, out val))
            return true;

        return false;
    }

    public void UnregisterSingleTarget()
    {
        var caster = Caster;
        caster.SingleCastAuras.Remove(this);
        IsSingleTarget = false;
    }

    public void UpdateOwner(uint diff, WorldObject owner)
    {
        var caster = Caster;
        // Apply spellmods for channeled auras
        // used for example when triggered spell of spell:10 is modded
        Spell modSpell = null;
        Player modOwner = null;

        if (caster != null)
        {
            modOwner = caster.SpellModOwner;

            if (modOwner != null)
            {
                modSpell = modOwner.FindCurrentSpellBySpellId(Id);

                if (modSpell != null)
                    modOwner.SetSpellModTakingSpell(modSpell, true);
            }
        }

        Update(diff, caster);

        if (_updateTargetMapInterval <= diff)
            UpdateTargetMap(caster);
        else
            _updateTargetMapInterval -= (int)diff;

        // update aura effects
        foreach (var effect in AuraEffects)
            effect.Value.Update(diff, caster);

        // remove spellmods after effects update
        if (modSpell != null)
            modOwner.SetSpellModTakingSpell(modSpell, false);

        _DeleteRemovedApplications();
    }

    public void UpdateTargetMap(Unit caster, bool apply = true)
    {
        if (IsRemoved)
            return;

        _updateTargetMapInterval = UPDATE_TARGET_MAP_INTERVAL;

        // fill up to date target list
        //       target, effMask
        var targets = FillTargetMap(caster);

        List<Unit> targetsToRemove = new();

        // mark all auras as ready to remove
        foreach (var app in ApplicationMap)
            // not found in current area - remove the aura
            if (!targets.TryGetValue(app.Value.Target, out var existing))
            {
                targetsToRemove.Add(app.Value.Target);
            }
            else
            {
                // needs readding - remove now, will be applied in next update cycle
                // (dbcs do not have auras which apply on same type of targets but have different radius, so this is not really needed)
                if (app.Value.Target.IsImmunedToSpell(SpellInfo, caster, true) || !CanBeAppliedOn(app.Value.Target))
                {
                    targetsToRemove.Add(app.Value.Target);

                    continue;
                }

                // check target immunities (for existing targets)
                foreach (var spellEffectInfo in SpellInfo.Effects)
                    if (app.Value.Target.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, caster, true))
                        existing.Remove(spellEffectInfo.EffectIndex);

                targets[app.Value.Target] = existing;

                // needs to add/remove effects from application, don't remove from map so it gets updated
                if (!app.Value.EffectMask.SetEquals(existing))
                    continue;

                // nothing to do - aura already applied
                // remove from auras to register list
                targets.Remove(app.Value.Target);
            }

        // register auras for units
        foreach (var unit in targets.Keys.ToList())
        {
            var addUnit = true;
            // check target immunities
            var aurApp = GetApplicationOfTarget(unit.GUID);

            if (aurApp == null)
            {
                // check target immunities (for new targets)
                foreach (var spellEffectInfo in SpellInfo.Effects)
                    if (unit.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, caster))
                        targets[unit].Remove(spellEffectInfo.EffectIndex);

                if (targets[unit].Count == 0 || unit.IsImmunedToSpell(SpellInfo, caster) || !CanBeAppliedOn(unit))
                    addUnit = false;
            }

            if (addUnit && !unit.IsHighestExclusiveAura(this, true))
                addUnit = false;

            // Dynobj auras don't hit flying targets
            if (AuraObjType == AuraObjectType.DynObj && unit.IsInFlight)
                addUnit = false;

            // Do not apply aura if it cannot stack with existing auras
            if (addUnit)
                // Allow to remove by stack when aura is going to be applied on owner
                if (unit != Owner)
                    // check if not stacking aura already on target
                    // this one prevents unwanted usefull buff loss because of stacking and prevents overriding auras periodicaly by 2 near area aura owners
                    foreach (var iter in unit.AppliedAuras)
                    {
                        var aura = iter.Base;

                        if (!CanStackWith(aura))
                        {
                            addUnit = false;

                            break;
                        }
                    }

            if (!addUnit)
            {
                targets.Remove(unit);
            }
            else
            {
                // owner has to be in world, or effect has to be applied to self
                if (!Owner.Location.IsSelfOrInSameMap(unit))
                    // @todo There is a crash caused by shadowfiend load addon
                    Log.Logger.Fatal("Aura {0}: Owner {1} (map {2}) is not in the same map as target {3} (map {4}).",
                                     SpellInfo.Id,
                                     Owner.GetName(),
                                     Owner.Location.IsInWorld ? (int)Owner.Location.Map.Id : -1,
                                     unit.GetName(),
                                     unit.Location.IsInWorld ? (int)unit.Location.Map.Id : -1);

                if (aurApp != null)
                {
                    aurApp.UpdateApplyEffectMask(targets[unit], true); // aura is already applied, this means we need to update effects of current application
                    targets.Remove(unit);
                }
                else
                {
                    unit._CreateAuraApplication(this, targets[unit]);
                }
            }
        }

        // remove auras from units no longer needing them
        foreach (var unit in targetsToRemove)
        {
            var aurApp = GetApplicationOfTarget(unit.GUID);

            if (aurApp != null)
                unit.UnapplyAura(aurApp, AuraRemoveMode.Default);
        }

        if (!apply)
            return;

        // apply aura effects for units
        foreach (var pair in targets)
        {
            var aurApp = GetApplicationOfTarget(pair.Key.GUID);

            if (aurApp != null && ((!Owner.Location.IsInWorld && Owner == pair.Key) || Owner.Location.IsInMap(pair.Key)))
                // owner has to be in world, or effect has to be applied to self
                pair.Key.ApplyAura(aurApp, pair.Value);
        }
    }
    public bool UsesScriptType<T>()
    {
        return _auraScriptsByType.ContainsKey(typeof(T));
    }
    private void _DeleteRemovedApplications()
    {
        _removedApplications.Clear();
    }

    private void AddAuraEffect(int index, IAuraScript script, IAuraEffectHandler effect)
    {
        if (!_effectHandlers.TryGetValue(index, out var effecTypes))
        {
            effecTypes = new Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>();
            _effectHandlers.Add(index, effecTypes);
        }

        if (!effecTypes.TryGetValue(effect.HookType, out var effects))
        {
            effects = new List<(IAuraScript, IAuraEffectHandler)>();
            effecTypes.Add(effect.HookType, effects);
        }

        effects.Add((script, effect));
    }

    private byte CalcMaxCharges(Unit caster)
    {
        var maxProcCharges = SpellInfo.ProcCharges;
        var procEntry = Global.SpellMgr.GetSpellProcEntry(SpellInfo);

        if (procEntry != null)
            maxProcCharges = procEntry.Charges;

        var modOwner = caster?.SpellModOwner;

        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.ProcCharges, ref maxProcCharges);

        return (byte)maxProcCharges;
    }

    private double CalcProcChance(SpellProcEntry procEntry, ProcEventInfo eventInfo)
    {
        double chance = procEntry.Chance;
        // calculate chances depending on unit with caster's data
        // so talents modifying chances and judgements will have properly calculated proc chance
        var caster = Caster;

        if (caster != null)
        {
            // calculate ppm chance if present and we're using weapon
            if (eventInfo.DamageInfo != null && procEntry.ProcsPerMinute != 0)
            {
                var WeaponSpeed = caster.GetBaseAttackTime(eventInfo.DamageInfo.AttackType);
                chance = caster.GetPpmProcChance(WeaponSpeed, procEntry.ProcsPerMinute, SpellInfo);
            }

            if (SpellInfo.ProcBasePpm > 0.0f)
                chance = CalcPPMProcChance(caster);

            // apply chance modifer aura, applies also to ppm chance (see improved judgement of light spell)
            var modOwner = caster.SpellModOwner;

            modOwner?.ApplySpellMod(SpellInfo, SpellModOp.ProcChance, ref chance);
        }

        // proc chance is reduced by an additional 3.333% per level past 60
        if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReduceProc60) && eventInfo.Actor.Level > 60)
            chance = Math.Max(0.0f, (1.0f - ((eventInfo.Actor.Level - 60) * 1.0f / 30.0f)) * chance);

        return chance;
    }

    private bool CanBeAppliedOn(Unit target)
    {
        foreach (var label in SpellInfo.Labels)
            if (target.HasAuraTypeWithMiscvalue(AuraType.SuppressItemPassiveEffectBySpellLabel, (int)label))
                return false;

        // unit not in world or during remove from world
        if (!target.Location.IsInWorld || target.IsDuringRemoveFromWorld)
        {
            // area auras mustn't be applied
            if (Owner != target)
                return false;

            // not selfcasted single target auras mustn't be applied
            if (CasterGuid != Owner.GUID && SpellInfo.IsSingleTarget())
                return false;

            return true;
        }
        else
        {
            return CheckAreaTarget(target);
        }
    }

    private bool CheckAreaTarget(Unit target)
    {
        return CallScriptCheckAreaTargetHandlers(target);
    }

    private bool CheckAuraEffectHandler(IAuraEffectHandler ae, int effIndex)
    {
        if (SpellInfo.Effects.Count <= effIndex)
            return false;

        var spellEffectInfo = SpellInfo.GetEffect(effIndex);

        if (spellEffectInfo.ApplyAuraName == 0 && ae.AuraType == 0)
            return true;

        if (spellEffectInfo.ApplyAuraName == 0)
            return false;

        return ae.AuraType == AuraType.Any || spellEffectInfo.ApplyAuraName == ae.AuraType;
    }

    private WorldObject GetWorldObjectCaster()
    {
        if (CasterGuid.IsUnit)
            return Caster;

        return Global.ObjAccessor.GetWorldObject(Owner, CasterGuid);
    }

    private void RefreshTimers(bool resetPeriodicTimer)
    {
        MaxDuration = CalcMaxDuration();

        if (SpellInfo.HasAttribute(SpellAttr8.DontResetPeriodicTimer))
        {
            var minPeriod = MaxDuration;

            foreach (var aurEff in AuraEffects)
            {
                var period = aurEff.Value.Period;

                if (period != 0)
                    minPeriod = Math.Min(period, minPeriod);
            }

            // If only one tick remaining, roll it over into new duration
            if (Duration <= minPeriod)
            {
                MaxDuration += Duration;
                resetPeriodicTimer = false;
            }
        }

        RefreshDuration();
        var caster = Caster;

        foreach (var aurEff in AuraEffects)
            aurEff.Value.CalculatePeriodic(caster, resetPeriodicTimer);
    }

    private void RegisterSpellEffectHandler(AuraScript script)
    {
        if (script is IHasAuraEffects hse)
            foreach (var effect in hse.AuraEffects)
                if (effect is IAuraEffectHandler se)
                {
                    uint mask = 0;

                    if (se.EffectIndex is SpellConst.EffectAll or SpellConst.EffectFirstFound)
                    {
                        foreach (var aurEff in AuraEffects)
                        {
                            if (se.EffectIndex == SpellConst.EffectFirstFound && mask != 0)
                                break;

                            if (CheckAuraEffectHandler(se, aurEff.Key))
                                AddAuraEffect(aurEff.Key, script, se);
                        }
                    }
                    else
                    {
                        if (CheckAuraEffectHandler(se, se.EffectIndex))
                            AddAuraEffect(se.EffectIndex, script, se);
                    }
                }
    }

    private void Update(uint diff, Unit caster)
    {
        ForEachAuraScript<IAuraOnUpdate>(u => u.AuraOnUpdate(diff));

        if (Duration > 0)
        {
            Duration -= (int)diff;

            if (Duration < 0)
                Duration = 0;

            // handle manaPerSecond/manaPerSecondPerLevel
            if (_timeCla != 0)
            {
                if (_timeCla > diff)
                    _timeCla -= (int)diff;
                else if (caster != null && (caster == Owner || !SpellInfo.HasAttribute(SpellAttr2.NoTargetPerSecondCosts)))
                    if (!_periodicCosts.Empty())
                    {
                        _timeCla += (int)(1000 - diff);

                        foreach (var power in _periodicCosts)
                        {
                            if (power.RequiredAuraSpellID != 0 && !caster.HasAura(power.RequiredAuraSpellID))
                                continue;

                            var manaPerSecond = power.ManaPerSecond;

                            if (power.PowerType != PowerType.Health)
                                manaPerSecond += MathFunctions.CalculatePct(caster.GetMaxPower(power.PowerType), power.PowerPctPerSecond);
                            else
                                manaPerSecond += (int)MathFunctions.CalculatePct(caster.MaxHealth, power.PowerPctPerSecond);

                            if (manaPerSecond != 0)
                            {
                                if (power.PowerType == PowerType.Health)
                                {
                                    if ((int)caster.Health > manaPerSecond)
                                        caster.ModifyHealth(-manaPerSecond);
                                    else
                                        Remove();
                                }
                                else if (caster.GetPower(power.PowerType) >= manaPerSecond)
                                {
                                    caster.ModifyPower(power.PowerType, -manaPerSecond);
                                }
                                else
                                {
                                    Remove();
                                }
                            }
                        }
                    }
            }
        }
    }
    #region CallScripts

    public void CallScriptAfterDispel(DispelInfo dispelInfo)
    {
        foreach (IAfterAuraDispel auraScript in GetAuraScripts<IAfterAuraDispel>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.AfterDispel);

                auraScript.HandleDispel(dispelInfo);

                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptAfterEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterApply, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterApply, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptAfterEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterProc, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterProc, aurApp);

                ((IAuraEffectProcHandler)auraScript.Item2).HandleProc(aurEff, eventInfo);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptAfterEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterRemove, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterRemove, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptAfterProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        foreach (IAuraAfterProc auraScript in GetAuraScripts<IAuraAfterProc>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.AfterProc, aurApp);

                auraScript.AfterProc(eventInfo);

                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public bool CallScriptCheckEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var result = true;

        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.CheckEffectProc, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.CheckEffectProc, aurApp);

                result &= ((IAuraCheckEffectProc)auraScript.Item2).CheckProc(aurEff, eventInfo);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return result;
    }

    public bool CallScriptCheckProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var result = true;

        foreach (IAuraCheckProc auraScript in GetAuraScripts<IAuraCheckProc>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckProc, aurApp);

                result &= auraScript.CheckProc(eventInfo);

                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return result;
    }

    public void CallScriptDispel(DispelInfo dispelInfo)
    {
        foreach (IAuraOnDispel auraScript in GetAuraScripts<IAuraOnDispel>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.Dispel);

                auraScript.OnDispel(dispelInfo);

                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount, ref bool defaultPrevented)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAbsorb, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

                absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

                defaultPrevented = auraScript.Item1._IsDefaultActionPrevented();
                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, HealInfo healInfo, ref double absorbAmount, ref bool defaultPrevented)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAbsorbHeal, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

                absorbAmount = ((IAuraEffectAbsorbHeal)auraScript.Item2).HandleAbsorb(aurEff, healInfo, absorbAmount);

                defaultPrevented = auraScript.Item1._IsDefaultActionPrevented();
                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterAbsorb, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorb, aurApp);

                absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, HealInfo healInfo, ref double absorbAmount)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterAbsorb, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorbHeal, aurApp);

                absorbAmount = ((IAuraEffectAbsorbHeal)auraScript.Item2).HandleAbsorb(aurEff, healInfo, absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectAfterManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterManaShield, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterManaShield, aurApp);

                absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public bool CallScriptEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
    {
        var preventDefault = false;

        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectApply, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectApply, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return preventDefault;
    }

    public void CallScriptEffectCalcAmountHandlers(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcAmount, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcAmount);

                ((IAuraCalcAmount)auraScript.Item2).HandleCalcAmount(aurEff, ref amount, ref canBeRecalculated);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectCalcCritChanceHandlers(AuraEffect aurEff, AuraApplication aurApp, Unit victim, ref double critChance)
    {
        foreach (var loadedScript in GetEffectScripts(AuraScriptHookType.EffectCalcCritChance, aurEff.EffIndex))
            try
            {
                loadedScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcCritChance, aurApp);

                critChance = ((IAuraCalcCritChance)loadedScript.Item2).CalcCritChance(aurEff, victim, critChance);

                loadedScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectCalcPeriodicHandlers(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcPeriodic, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcPeriodic);

                var boxed = new BoxedValue<bool>(isPeriodic);
                var amp = new BoxedValue<int>(amplitude);

                ((IAuraCalcPeriodic)auraScript.Item2).CalcPeriodic(aurEff, boxed, amp);

                isPeriodic = boxed.Value;
                amplitude = amp.Value;

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectCalcSpellModHandlers(AuraEffect aurEff, SpellModifier spellMod)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcSpellmod, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcSpellmod);

                ((IAuraCalcSpellMod)auraScript.Item2).CalcSpellMod(aurEff, spellMod);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount, ref bool defaultPrevented)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectManaShield, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectManaShield, aurApp);

                absorbAmount = ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public bool CallScriptEffectPeriodicHandlers(AuraEffect aurEff, AuraApplication aurApp)
    {
        var preventDefault = false;

        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectPeriodic, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectPeriodic, aurApp);

                ((IAuraPeriodic)auraScript.Item2).HandlePeriodic(aurEff);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return preventDefault;
    }

    public bool CallScriptEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var preventDefault = false;

        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectProc, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectProc, aurApp);

                ((IAuraEffectProcHandler)auraScript.Item2).HandleProc(aurEff, eventInfo);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return preventDefault;
    }

    public bool CallScriptEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
    {
        var preventDefault = false;

        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectRemove, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectRemove, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return preventDefault;
    }

    public void CallScriptEffectSplitHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double splitAmount)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectSplit, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectSplit, aurApp);

                splitAmount = ((IAuraSplitHandler)auraScript.Item2).Split(aurEff, dmgInfo, splitAmount);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEffectUpdatePeriodicHandlers(AuraEffect aurEff)
    {
        foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectUpdatePeriodic, aurEff.EffIndex))
            try
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectUpdatePeriodic);

                ((IAuraUpdatePeriodic)auraScript.Item2).UpdatePeriodic(aurEff);

                auraScript.Item1._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void CallScriptEnterLeaveCombatHandlers(AuraApplication aurApp, bool isNowInCombat)
    {
        foreach (IAuraEnterLeaveCombat loadedScript in GetAuraScripts<IAuraEnterLeaveCombat>())
            try
            {
                loadedScript._PrepareScriptCall(AuraScriptHookType.EnterLeaveCombat, aurApp);

                loadedScript.EnterLeaveCombat(isNowInCombat);

                loadedScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public bool CallScriptPrepareProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var prepare = true;

        foreach (IAuraPrepareProc auraScript in GetAuraScripts<IAuraPrepareProc>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.PrepareProc, aurApp);

                auraScript.DoPrepareProc(eventInfo);

                if (prepare)
                    prepare = !auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return prepare;
    }

    public bool CallScriptProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var handled = false;

        foreach (IAuraOnProc auraScript in GetAuraScripts<IAuraOnProc>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.Proc, aurApp);

                auraScript.OnProc(eventInfo);

                handled |= auraScript._IsDefaultActionPrevented();
                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return handled;
    }

    public virtual string GetDebugInfo()
    {
        return $"Id: {Id} Name: '{SpellInfo.SpellName[Global.WorldMgr.DefaultDbcLocale]}' Caster: {CasterGuid}\nOwner: {(Owner != null ? Owner.GetDebugInfo() : "NULL")}";
    }

    private bool CallScriptCheckAreaTargetHandlers(Unit target)
    {
        var result = true;

        foreach (IAuraCheckAreaTarget auraScript in GetAuraScripts<IAuraCheckAreaTarget>())
            try
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckAreaTarget);

                result &= auraScript.CheckAreaTarget(target);

                auraScript._FinishScriptCall();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }

        return result;
    }
    #endregion
}