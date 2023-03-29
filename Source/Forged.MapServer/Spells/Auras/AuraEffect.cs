// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Weather;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Spells.Auras;

public class AuraEffect
{
    public double BaseAmount;


    private readonly Aura _auraBase;
    private readonly SpellInfo _spellInfo;
    private readonly SpellEffectInfo _effectInfo;
    private SpellModifier _spellModifier;
    private double _amount;
    private double? _estimatedAmount; // for periodic damage and healing auras this will include damage done bonuses

    // periodic stuff
    private int _periodicTimer;
    private int _period;     // time between consecutive ticks
    private uint _ticksDone; // ticks counter

    private bool _canBeRecalculated;
    private bool _isPeriodic;

    public Unit Caster => _auraBase.Caster;

    public ObjectGuid CasterGuid => _auraBase.CasterGuid;

    public Aura Base => _auraBase;

    public SpellInfo SpellInfo => _spellInfo;

    public uint Id => _spellInfo.Id;

    public int EffIndex => _effectInfo.EffectIndex;

    public int Period
    {
        get => _period;
        set => _period = value;
    }

    public int MiscValueB => _effectInfo.MiscValueB;

    public int MiscValue => _effectInfo.MiscValue;

    public AuraType AuraType => _effectInfo.ApplyAuraName;

    public double Amount => _amount;

    public float AmountAsFloat => (float)_amount;
    public int AmountAsInt => (int)_amount;
    public uint AmountAsUInt => (uint)_amount;
    public ulong AmountAsULong => (ulong)_amount;
    public long AmountAsLong => (long)_amount;

    public AuraEffect(Aura baseAura, SpellEffectInfo spellEfffectInfo, double? baseAmount, Unit caster)
    {
        _auraBase = baseAura;
        _spellInfo = baseAura.SpellInfo;
        _effectInfo = spellEfffectInfo;
        BaseAmount = baseAmount.HasValue ? baseAmount.Value : _effectInfo.CalcBaseValue(caster, baseAura.AuraObjType == AuraObjectType.Unit ? baseAura.Owner.AsUnit : null, baseAura.CastItemId, baseAura.CastItemLevel);
        _canBeRecalculated = true;
        _isPeriodic = false;

        CalculatePeriodic(caster, true, false);
        _amount = CalculateAmount(caster);
        CalculateSpellMod();
    }

    public double CalculateAmount(Unit caster)
    {
        // default amount calculation
        double amount = 0;

        if (!_spellInfo.HasAttribute(SpellAttr8.MasteryAffectPoints) || MathFunctions.fuzzyEq(GetSpellEffectInfo().BonusCoefficient, 0.0f))
            amount = GetSpellEffectInfo().CalcValue(caster, BaseAmount, Base.Owner.AsUnit, Base.CastItemId, Base.CastItemLevel);
        else if (caster != null && caster.IsTypeId(TypeId.Player))
            amount = caster.AsPlayer.ActivePlayerData.Mastery * GetSpellEffectInfo().BonusCoefficient;

        // custom amount calculations go here
        switch (AuraType)
        {
            // crowd control auras
            case AuraType.ModConfuse:
            case AuraType.ModFear:
            case AuraType.ModStun:
            case AuraType.ModRoot:
            case AuraType.Transform:
            case AuraType.ModRoot2:
                _canBeRecalculated = false;

                if (_spellInfo.ProcFlags == null)
                    break;

                amount = (int)(Base.OwnerAsUnit.CountPctFromMaxHealth(10));

                break;
            case AuraType.SchoolAbsorb:
            case AuraType.ManaShield:
                _canBeRecalculated = false;

                break;
            case AuraType.Mounted:
                var mountType = (uint)MiscValueB;
                var mountEntry = Global.DB2Mgr.GetMount(Id);

                if (mountEntry != null)
                    mountType = mountEntry.MountTypeID;

                var mountCapability = Base.OwnerAsUnit.GetMountCapability(mountType);

                if (mountCapability != null)
                    amount = (int)mountCapability.Id;

                break;
            case AuraType.ShowConfirmationPromptWithDifficulty:
                if (caster)
                    amount = (int)caster.Location.Map.DifficultyID;

                _canBeRecalculated = false;

                break;
            default:
                break;
        }

        if (SpellInfo.HasAttribute(SpellAttr10.RollingPeriodic))
        {
            var periodicAuras = Base.OwnerAsUnit.GetAuraEffectsByType(AuraType);

            amount = periodicAuras.Aggregate(0d,
                                             (val, aurEff) =>
                                             {
                                                 if (aurEff.CasterGuid == CasterGuid && aurEff.Id == Id && aurEff.EffIndex == EffIndex && aurEff.GetTotalTicks() > 0)
                                                     val += aurEff.Amount * aurEff.GetRemainingTicks() / aurEff.GetTotalTicks();

                                                 return val;
                                             });
        }

        Base.CallScriptEffectCalcAmountHandlers(this, ref amount, ref _canBeRecalculated);

        if (!GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack))
            amount *= Base.StackAmount;

        if (caster && Base.AuraObjType == AuraObjectType.Unit)
        {
            var stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? Base.StackAmount : 1u;

            switch (AuraType)
            {
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicLeech:
                    _estimatedAmount = caster.SpellDamageBonusDone(Base.OwnerAsUnit, SpellInfo, amount, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

                    break;
                case AuraType.PeriodicHeal:
                    _estimatedAmount = caster.SpellHealingBonusDone(Base.OwnerAsUnit, SpellInfo, amount, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

                    break;
                default:
                    break;
            }
        }

        return amount;
    }

    public uint GetTotalTicks()
    {
        uint totalTicks = 0;

        if (_period != 0 && !Base.IsPermanent)
        {
            totalTicks = (uint)(Base.MaxDuration / _period);

            if (_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                ++totalTicks;
        }

        return totalTicks;
    }

    public void CalculatePeriodic(Unit caster, bool resetPeriodicTimer = true, bool load = false)
    {
        _period = (int)GetSpellEffectInfo().ApplyAuraPeriod;

        // prepare periodics
        switch (AuraType)
        {
            case AuraType.ObsModPower:
            case AuraType.PeriodicDamage:
            case AuraType.PeriodicHeal:
            case AuraType.ObsModHealth:
            case AuraType.PeriodicTriggerSpell:
            case AuraType.PeriodicTriggerSpellFromClient:
            case AuraType.PeriodicEnergize:
            case AuraType.PeriodicLeech:
            case AuraType.PeriodicHealthFunnel:
            case AuraType.PeriodicManaLeech:
            case AuraType.PeriodicDamagePercent:
            case AuraType.PowerBurn:
            case AuraType.PeriodicDummy:
            case AuraType.PeriodicTriggerSpellWithValue:
                _isPeriodic = true;

                break;
            default:
                break;
        }

        Base.CallScriptEffectCalcPeriodicHandlers(this, ref _isPeriodic, ref _period);

        if (!_isPeriodic)
            return;

        var modOwner = caster != null ? caster.SpellModOwner : null;

        // Apply casting time mods
        if (_period != 0)
        {
            // Apply periodic time mod
            if (modOwner != null)
                modOwner.ApplySpellMod(SpellInfo, SpellModOp.Period, ref _period);

            if (caster != null)
            {
                // Haste modifies periodic time of channeled spells
                if (_spellInfo.IsChanneled)
                    caster.WorldObjectCombat.ModSpellDurationTime(_spellInfo, ref _period);
                else if (_spellInfo.HasAttribute(SpellAttr5.SpellHasteAffectsPeriodic))
                    _period = (int)(_period * caster.UnitData.ModCastingSpeed);
            }
        }
        else // prevent infinite loop on Update
        {
            _isPeriodic = false;
        }

        if (load) // aura loaded from db
        {
            if (_period != 0 && !Base.IsPermanent)
            {
                var elapsedTime = (uint)(Base.MaxDuration - Base.Duration);
                _ticksDone = elapsedTime / (uint)_period;
                _periodicTimer = (int)(elapsedTime % _period);
            }

            if (_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                ++_ticksDone;
        }
        else // aura just created or reapplied
        {
            // reset periodic timer on aura create or reapply
            // we don't reset periodic timers when aura is triggered by proc
            ResetPeriodic(resetPeriodicTimer);
        }
    }

    public void CalculateSpellMod()
    {
        switch (AuraType)
        {
            case AuraType.AddFlatModifier:
            case AuraType.AddPctModifier:
                if (_spellModifier == null)
                {
                    SpellModifierByClassMask spellmod = new(Base)
                    {
                        Op = (SpellModOp)MiscValue,
                        Type = AuraType == AuraType.AddPctModifier ? SpellModType.Pct : SpellModType.Flat,
                        SpellId = Id,
                        Mask = GetSpellEffectInfo().SpellClassMask
                    };

                    _spellModifier = spellmod;
                }

                (_spellModifier as SpellModifierByClassMask).Value = Amount;

                break;
            case AuraType.AddFlatModifierBySpellLabel:
                if (_spellModifier == null)
                {
                    SpellFlatModifierByLabel spellmod = new(Base)
                    {
                        Op = (SpellModOp)MiscValue,
                        Type = SpellModType.LabelFlat,
                        SpellId = Id,
                        Value =
                        {
                            ModIndex = MiscValue,
                            LabelID = MiscValueB
                        }
                    };

                    _spellModifier = spellmod;
                }

                (_spellModifier as SpellFlatModifierByLabel).Value.ModifierValue = Amount;

                break;
            case AuraType.AddPctModifierBySpellLabel:
                if (_spellModifier == null)
                {
                    SpellPctModifierByLabel spellmod = new(Base)
                    {
                        Op = (SpellModOp)MiscValue,
                        Type = SpellModType.LabelPct,
                        SpellId = Id,
                        Value =
                        {
                            ModIndex = MiscValue,
                            LabelID = MiscValueB
                        }
                    };

                    _spellModifier = spellmod;
                }

                (_spellModifier as SpellPctModifierByLabel).Value.ModifierValue = 1.0f + MathFunctions.CalculatePct(1.0f, Amount);

                break;
            default:
                break;
        }

        Base.CallScriptEffectCalcSpellModHandlers(this, _spellModifier);
    }

    public void ChangeAmount(double newAmount, bool mark = true, bool onStackOrReapply = false, AuraEffect triggeredBy = null)
    {
        // Reapply if amount change
        AuraEffectHandleModes handleMask = 0;

        if (newAmount != Amount)
            handleMask |= AuraEffectHandleModes.ChangeAmount;

        if (onStackOrReapply)
            handleMask |= AuraEffectHandleModes.Reapply;

        if (handleMask == 0)
            return;

        GetApplicationList(out var effectApplications);

        foreach (var aurApp in effectApplications)
        {
            aurApp.Target._RegisterAuraEffect(this, false);
            HandleEffect(aurApp, handleMask, false, triggeredBy);
        }

        if (Convert.ToBoolean(handleMask & AuraEffectHandleModes.ChangeAmount))
        {
            if (!mark)
                _amount = newAmount;
            else
                SetAmount(newAmount);

            CalculateSpellMod();
        }

        foreach (var aurApp in effectApplications)
        {
            if (aurApp.RemoveMode != AuraRemoveMode.None)
                continue;

            aurApp.Target._RegisterAuraEffect(this, true);
            HandleEffect(aurApp, handleMask, true, triggeredBy);
        }

        if (SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) || Aura.EffectTypeNeedsSendingAmount(AuraType))
            Base.SetNeedClientUpdateForTargets();
    }

    public void HandleEffect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply, AuraEffect triggeredBy = null)
    {
        // register/unregister effect in lists in case of real AuraEffect apply/remove
        // registration/unregistration is done always before real effect handling (some effect handlers code is depending on this)
        if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            aurApp.Target._RegisterAuraEffect(this, apply);

        // real aura apply/remove, handle modifier
        if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            ApplySpellMod(aurApp.Target, apply, triggeredBy);

        // call scripts helping/replacing effect handlers
        bool prevented;

        if (apply)
            prevented = Base.CallScriptEffectApplyHandlers(this, aurApp, mode);
        else
            prevented = Base.CallScriptEffectRemoveHandlers(this, aurApp, mode);

        // check if script events have removed the aura already
        if (apply && aurApp.HasRemoveMode)
            return;

        // call default effect handler if it wasn't prevented
        if (!prevented)
            Global.SpellMgr.GetAuraEffectHandler(AuraType).Invoke(this, aurApp, mode, apply);

        // check if the default handler reemoved the aura
        if (apply && aurApp.HasRemoveMode)
            return;

        // call scripts triggering additional events after apply/remove
        if (apply)
            Base.CallScriptAfterEffectApplyHandlers(this, aurApp, mode);
        else
            Base.CallScriptAfterEffectRemoveHandlers(this, aurApp, mode);
    }

    public void HandleEffect(Unit target, AuraEffectHandleModes mode, bool apply, AuraEffect triggeredBy = null)
    {
        var aurApp = Base.GetApplicationOfTarget(target.GUID);
        HandleEffect(aurApp, mode, apply, triggeredBy);
    }

    public void Update(uint diff, Unit caster)
    {
        if (!_isPeriodic || (Base.Duration < 0 && !Base.IsPassive && !Base.IsPermanent))
            return;

        var totalTicks = GetTotalTicks();

        _periodicTimer += (int)diff;

        while (_periodicTimer >= _period)
        {
            _periodicTimer -= _period;

            if (!Base.IsPermanent && (_ticksDone + 1) > totalTicks)
                break;

            ++_ticksDone;

            Base.CallScriptEffectUpdatePeriodicHandlers(this);

            GetApplicationList(out var effectApplications);

            // tick on targets of effects
            foreach (var appt in effectApplications)
                PeriodicTick(appt, caster);
        }
    }

    public double GetCritChanceFor(Unit caster, Unit target)
    {
        return target.SpellCritChanceTaken(caster, null, this, SpellInfo.GetSchoolMask(), CalcPeriodicCritChance(caster), SpellInfo.GetAttackType());
    }

    public bool IsAffectingSpell(SpellInfo spell)
    {
        if (spell == null)
            return false;

        // Check family name and EffectClassMask
        if (!spell.IsAffected(_spellInfo.SpellFamilyName, GetSpellEffectInfo().SpellClassMask))
            return false;

        return true;
    }

    public bool CheckEffectProc(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var result = Base.CallScriptCheckEffectProcHandlers(this, aurApp, eventInfo);

        if (!result)
            return false;

        var spellInfo = eventInfo.SpellInfo;

        switch (AuraType)
        {
            case AuraType.ModConfuse:
            case AuraType.ModFear:
            case AuraType.ModStun:
            case AuraType.ModRoot:
            case AuraType.Transform:
            {
                var damageInfo = eventInfo.DamageInfo;

                if (damageInfo == null || damageInfo.Damage == 0)
                    return false;

                // Spell own damage at apply won't break CC
                if (spellInfo != null && spellInfo == SpellInfo)
                {
                    var aura = Base;

                    // called from spellcast, should not have ticked yet
                    if (aura.Duration == aura.MaxDuration)
                        return false;
                }

                break;
            }
            case AuraType.MechanicImmunity:
            case AuraType.ModMechanicResistance:
                // compare mechanic
                if (spellInfo == null || (spellInfo.GetAllEffectsMechanicMask() & (1ul << MiscValue)) == 0)
                    return false;

                break;
            case AuraType.ModCastingSpeedNotStack:
                // skip melee hits and instant cast spells
                if (!eventInfo.ProcSpell || eventInfo.ProcSpell.CastTime == 0)
                    return false;

                break;
            case AuraType.ModSchoolMaskDamageFromCaster:
            case AuraType.ModSpellDamageFromCaster:
                // Compare casters
                if (CasterGuid != eventInfo.Actor.GUID)
                    return false;

                break;
            case AuraType.ModPowerCostSchool:
            case AuraType.ModPowerCostSchoolPct:
            {
                // Skip melee hits and spells with wrong school or zero cost
                if (spellInfo == null ||
                    !Convert.ToBoolean((int)spellInfo.GetSchoolMask() & MiscValue) // School Check
                    ||
                    !eventInfo.ProcSpell)
                    return false;

                // Costs Check
                var costs = eventInfo.ProcSpell.PowerCost;
                var m = costs.Find(cost => cost.Amount > 0);

                if (m == null)
                    return false;

                break;
            }
            case AuraType.ReflectSpellsSchool:
                // Skip melee hits and spells with wrong school
                if (spellInfo == null || !Convert.ToBoolean((int)spellInfo.GetSchoolMask() & MiscValue))
                    return false;

                break;
            case AuraType.ProcTriggerSpell:
            case AuraType.ProcTriggerSpellWithValue:
            {
                // Don't proc extra attacks while already processing extra attack spell
                var triggerSpellId = GetSpellEffectInfo().TriggerSpell;
                var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, Base.CastDifficulty);

                if (triggeredSpellInfo != null)
                    if (triggeredSpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                    {
                        var lastExtraAttackSpell = eventInfo.Actor.GetLastExtraAttackSpell();

                        // Patch 1.12.0(?) extra attack abilities can no longer chain proc themselves
                        if (lastExtraAttackSpell == triggerSpellId)
                            return false;
                    }

                break;
            }
            case AuraType.ModSpellCritChance:
                // skip spells that can't crit
                if (spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
                    return false;

                break;
            default:
                break;
        }

        return result;
    }

    public void HandleProc(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var prevented = Base.CallScriptEffectProcHandlers(this, aurApp, eventInfo);

        if (prevented)
            return;

        switch (AuraType)
        {
            // CC Auras which use their amount to drop
            // Are there any more auras which need this?
            case AuraType.ModConfuse:
            case AuraType.ModFear:
            case AuraType.ModStun:
            case AuraType.ModRoot:
            case AuraType.Transform:
            case AuraType.ModRoot2:
                HandleBreakableCCAuraProc(aurApp, eventInfo);

                break;
            case AuraType.Dummy:
            case AuraType.ProcTriggerSpell:
                HandleProcTriggerSpellAuraProc(aurApp, eventInfo);

                break;
            case AuraType.ProcTriggerSpellWithValue:
                HandleProcTriggerSpellWithValueAuraProc(aurApp, eventInfo);

                break;
            case AuraType.ProcTriggerDamage:
                HandleProcTriggerDamageAuraProc(aurApp, eventInfo);

                break;
            default:
                break;
        }

        Base.CallScriptAfterEffectProcHandlers(this, aurApp, eventInfo);
    }

    public void HandleShapeshiftBoosts(Unit target, bool apply)
    {
        uint spellId = 0;
        uint spellId2 = 0;
        uint spellId3 = 0;
        uint spellId4 = 0;

        switch ((ShapeShiftForm)MiscValue)
        {
            case ShapeShiftForm.CatForm:
                spellId = 3025;
                spellId2 = 48629;
                spellId3 = 106840;
                spellId4 = 113636;

                break;
            case ShapeShiftForm.TreeOfLife:
                spellId = 5420;
                spellId2 = 81097;

                break;
            case ShapeShiftForm.TravelForm:
                spellId = 5419;

                break;
            case ShapeShiftForm.AquaticForm:
                spellId = 5421;

                break;
            case ShapeShiftForm.BearForm:
                spellId = 1178;
                spellId2 = 21178;
                spellId3 = 106829;
                spellId4 = 106899;

                break;
            case ShapeShiftForm.FlightForm:
                spellId = 33948;
                spellId2 = 34764;

                break;
            case ShapeShiftForm.FlightFormEpic:
                spellId = 40122;
                spellId2 = 40121;

                break;
            case ShapeShiftForm.SpiritOfRedemption:
                spellId = 27792;
                spellId2 = 27795;
                spellId3 = 62371;

                break;
            case ShapeShiftForm.Shadowform:
                if (target.HasAura(107906)) // Glyph of Shadow
                    spellId = 107904;
                else if (target.HasAura(126745)) // Glyph of Shadowy Friends
                    spellId = 142024;
                else
                    spellId = 107903;

                break;
            case ShapeShiftForm.GhostWolf:
                if (target.HasAura(58135)) // Glyph of Spectral Wolf
                    spellId = 160942;

                break;
            default:
                break;
        }

        if (apply)
        {
            if (spellId != 0)
                target.CastSpell(target, spellId, new CastSpellExtraArgs(this));

            if (spellId2 != 0)
                target.CastSpell(target, spellId2, new CastSpellExtraArgs(this));

            if (spellId3 != 0)
                target.CastSpell(target, spellId3, new CastSpellExtraArgs(this));

            if (spellId4 != 0)
                target.CastSpell(target, spellId4, new CastSpellExtraArgs(this));

            if (target.IsTypeId(TypeId.Player))
            {
                var plrTarget = target.AsPlayer;

                var sp_list = plrTarget.GetSpellMap();

                foreach (var pair in sp_list)
                {
                    if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
                        continue;

                    if (pair.Key == spellId || pair.Key == spellId2 || pair.Key == spellId3 || pair.Key == spellId4)
                        continue;

                    var spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);

                    if (spellInfo == null || !(spellInfo.IsPassive || spellInfo.HasAttribute(SpellAttr0.DoNotDisplaySpellbookAuraIconCombatLog)))
                        continue;

                    if (Convert.ToBoolean(spellInfo.Stances & (1ul << (MiscValue - 1))))
                        target.CastSpell(target, pair.Key, new CastSpellExtraArgs(this));
                }
            }
        }
        else
        {
            if (spellId != 0)
                target.RemoveOwnedAura(spellId, target.GUID);

            if (spellId2 != 0)
                target.RemoveOwnedAura(spellId2, target.GUID);

            if (spellId3 != 0)
                target.RemoveOwnedAura(spellId3, target.GUID);

            if (spellId4 != 0)
                target.RemoveOwnedAura(spellId4, target.GUID);

            var shapeshifts = target.GetAuraEffectsByType(AuraType.ModShapeshift);
            AuraEffect newAura = null;

            // Iterate through all the shapeshift auras that the target has, if there is another aura with SPELL_AURA_MOD_SHAPESHIFT, then this aura is being removed due to that one being applied
            foreach (var eff in shapeshifts)
                if (eff != this)
                {
                    newAura = eff;

                    break;
                }

            target.AppliedAuras
                  .CallOnMatch((app) =>
                               {
                                   if (app == null)
                                       return false;

                                   // Use the new aura to see on what stance the target will be
                                   var newStance = newAura != null ? (1ul << (newAura.MiscValue - 1)) : 0;

                                   // If the stances are not compatible with the spell, remove it
                                   if (app.Base.IsRemovedOnShapeLost(target) && !Convert.ToBoolean(app.Base.SpellInfo.Stances & newStance))
                                       return true;

                                   return false;
                               },
                               (app) => target.RemoveAura(app));
        }
    }

    public bool HasAmount()
    {
        return _amount != 0;
    }

    public void SetAmount(double amount)
    {
        _amount = amount;
        _canBeRecalculated = false;
    }

    public void SetAmount(long amount)
    {
        SetAmount((double)amount);
    }

    public void SetAmount(int amount)
    {
        SetAmount((double)amount);
    }

    public void SetAmount(uint amount)
    {
        SetAmount((double)amount);
    }

    public void ModAmount(double amount)
    {
        _amount += amount;
        _canBeRecalculated = false;
    }

    public void ModAmount(long amount)
    {
        ModAmount((double)amount);
    }

    public void ModAmount(int amount)
    {
        ModAmount((double)amount);
    }

    public void ModAmount(uint amount)
    {
        ModAmount((double)amount);
    }

    public double? GetEstimatedAmount()
    {
        return _estimatedAmount;
    }

    public bool TryGetEstimatedAmount(out double amount)
    {
        amount = _estimatedAmount.HasValue ? _estimatedAmount.Value : 0;

        return _estimatedAmount.HasValue;
    }

    public int GetPeriodicTimer()
    {
        return _periodicTimer;
    }

    public void SetPeriodicTimer(int periodicTimer)
    {
        _periodicTimer = periodicTimer;
    }

    public void RecalculateAmount(AuraEffect triggeredBy = null)
    {
        if (!CanBeRecalculated())
            return;

        ChangeAmount(CalculateAmount(Caster), false, false, triggeredBy);
    }

    public void RecalculateAmount(Unit caster, AuraEffect triggeredBy = null)
    {
        if (!CanBeRecalculated())
            return;

        ChangeAmount(CalculateAmount(caster), false, false, triggeredBy);
    }

    public bool CanBeRecalculated()
    {
        return _canBeRecalculated;
    }

    public void SetCanBeRecalculated(bool val)
    {
        _canBeRecalculated = val;
    }

    public void ResetTicks()
    {
        _ticksDone = 0;
    }

    public uint GetTickNumber()
    {
        return _ticksDone;
    }

    public uint GetRemainingTicks()
    {
        return GetTotalTicks() - _ticksDone;
    }

    public double GetRemainingAmount(int maxDurationIfPermanent = 0)
    {
        return GetRemainingAmount((double)maxDurationIfPermanent);
    }

    public double GetRemainingAmount(double maxDurationIfPermanent = 0)
    {
        var ticks = GetTotalTicks();

        if (!Base.IsPermanent)
            ticks -= GetTickNumber();

        var total = Amount * ticks;

        if (total > maxDurationIfPermanent)
            return maxDurationIfPermanent;

        return total;
    }


    public bool IsPeriodic()
    {
        return _isPeriodic;
    }

    public void SetPeriodic(bool isPeriodic)
    {
        _isPeriodic = isPeriodic;
    }

    public SpellEffectInfo GetSpellEffectInfo()
    {
        return _effectInfo;
    }

    public bool IsEffect()
    {
        return _effectInfo.Effect != 0;
    }

    public bool IsEffect(SpellEffectName effectName)
    {
        return _effectInfo.Effect == effectName;
    }

    public bool IsAreaAuraEffect()
    {
        return _effectInfo.IsAreaAuraEffect;
    }

    [AuraEffectHandler(AuraType.ModSpellPowerPct)]
    public void HandleAuraModSpellPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if ((mode & (AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)) == 0)
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        // Recalculate bonus
        target.UpdateSpellDamageAndHealingBonus();
    }

    [AuraEffectHandler(AuraType.ModNextSpell)]
    public void HandleModNextSpell(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if ((mode & AuraEffectHandleModes.Real) == 0)
            return;

        var player = aurApp.Target.AsPlayer;

        if (player == null)
            return;

        var triggeredSpellId = GetSpellEffectInfo().TriggerSpell;

        if (apply)
            player.AddTemporarySpell(triggeredSpellId);
        else
            player.RemoveTemporarySpell(triggeredSpellId);
    }

    private void GetTargetList(out List<Unit> targetList)
    {
        targetList = new List<Unit>();
        var targetMap = Base.ApplicationMap;

        // remove all targets which were not added to new list - they no longer deserve area aura
        foreach (var app in targetMap.Values)
            if (app.HasEffect(EffIndex))
                targetList.Add(app.Target);
    }

    private void GetApplicationList(out List<AuraApplication> applicationList)
    {
        applicationList = new List<AuraApplication>();
        var targetMap = Base.ApplicationMap;

        foreach (var app in targetMap.Values)
            if (app.HasEffect(EffIndex))
                applicationList.Add(app);
    }

    private void ResetPeriodic(bool resetPeriodicTimer = false)
    {
        _ticksDone = 0;

        if (resetPeriodicTimer)
        {
            _periodicTimer = 0;

            // Start periodic on next tick or at aura apply
            if (_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                _periodicTimer = _period;
        }
    }

    private void ApplySpellMod(Unit target, bool apply, AuraEffect triggeredBy = null)
    {
        if (_spellModifier == null || !target.IsTypeId(TypeId.Player))
            return;

        target.AsPlayer.AddSpellMod(_spellModifier, apply);

        // Auras with charges do not mod amount of passive auras
        if (Base.IsUsingCharges)
            return;

        // reapply some passive spells after add/remove related spellmods
        // Warning: it is a dead loop if 2 auras each other amount-shouldn't happen
        BitSet recalculateEffectMask = new(Math.Max(Base.AuraEffects.Count(), 5));

        switch ((SpellModOp)MiscValue)
        {
            case SpellModOp.Points:
                recalculateEffectMask.SetAll(true);

                break;
            case SpellModOp.PointsIndex0:
                recalculateEffectMask.Set(0, true);

                break;
            case SpellModOp.PointsIndex1:
                recalculateEffectMask.Set(1, true);

                break;
            case SpellModOp.PointsIndex2:
                recalculateEffectMask.Set(2, true);

                break;
            case SpellModOp.PointsIndex3:
                recalculateEffectMask.Set(3, true);

                break;
            case SpellModOp.PointsIndex4:
                recalculateEffectMask.Set(4, true);

                break;
            default:
                break;
        }

        if (recalculateEffectMask.Any())
        {
            if (triggeredBy == null)
                triggeredBy = this;

            var guid = target.GUID;

            // only passive and permament auras-active auras should have amount set on spellcast and not be affected
            // if aura is cast by others, it will not be affected
            target.GetAppliedAurasQuery()
                  .HasCasterGuid(guid)
                  .IsPassiveOrPerm()
                  .AlsoMatches(arApp => arApp.Base.SpellInfo.IsAffectedBySpellMod(_spellModifier))
                  .ForEachResult(arApp =>
                  {
                      var aura = arApp.Base;

                      for (var i = 0; i < recalculateEffectMask.Count; ++i)
                          if (recalculateEffectMask[i])
                          {
                              var aurEff = aura.GetEffect(i);

                              if (aurEff != null)
                                  if (aurEff != triggeredBy)
                                      aurEff.RecalculateAmount(triggeredBy);
                          }
                  });
        }
    }

    private void SendTickImmune(Unit target, Unit caster)
    {
        if (caster != null)
            caster.SendSpellDamageImmune(target, _spellInfo.Id, true);
    }

    private void PeriodicTick(AuraApplication aurApp, Unit caster)
    {
        var prevented = Base.CallScriptEffectPeriodicHandlers(this, aurApp);

        if (prevented)
            return;

        var target = aurApp.Target;

        // Update serverside orientation of tracking channeled auras on periodic update ticks
        // exclude players because can turn during channeling and shouldn't desync orientation client/server
        if (caster is { IsPlayer: false } && _spellInfo.IsChanneled && _spellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel) && caster.UnitData.ChannelObjects.Size() != 0)
        {
            var channelGuid = caster.UnitData.ChannelObjects[0];

            if (channelGuid != caster.GUID)
            {
                var objectTarget = Global.ObjAccessor.GetWorldObject(caster, channelGuid);

                if (objectTarget != null)
                    caster.SetInFront(objectTarget);
            }
        }

        switch (AuraType)
        {
            case AuraType.PeriodicDummy:
                // handled via scripts
                break;
            case AuraType.PeriodicTriggerSpell:
                HandlePeriodicTriggerSpellAuraTick(target, caster);

                break;
            case AuraType.PeriodicTriggerSpellFromClient:
                // Don't actually do anything - client will trigger casts of these spells by itself
                break;
            case AuraType.PeriodicTriggerSpellWithValue:
                HandlePeriodicTriggerSpellWithValueAuraTick(target, caster);

                break;
            case AuraType.PeriodicDamage:
            case AuraType.PeriodicWeaponPercentDamage:
            case AuraType.PeriodicDamagePercent:
                HandlePeriodicDamageAurasTick(target, caster);

                break;
            case AuraType.PeriodicLeech:
                HandlePeriodicHealthLeechAuraTick(target, caster);

                break;
            case AuraType.PeriodicHealthFunnel:
                HandlePeriodicHealthFunnelAuraTick(target, caster);

                break;
            case AuraType.PeriodicHeal:
            case AuraType.ObsModHealth:
                HandlePeriodicHealAurasTick(target, caster);

                break;
            case AuraType.PeriodicManaLeech:
                HandlePeriodicManaLeechAuraTick(target, caster);

                break;
            case AuraType.ObsModPower:
                HandleObsModPowerAuraTick(target, caster);

                break;
            case AuraType.PeriodicEnergize:
                HandlePeriodicEnergizeAuraTick(target, caster);

                break;
            case AuraType.PowerBurn:
                HandlePeriodicPowerBurnAuraTick(target, caster);

                break;
            default:
                break;
        }
    }

    private bool HasSpellClassMask()
    {
        return GetSpellEffectInfo().SpellClassMask;
    }


    #region AuraEffect Handlers

    /**************************************/
    /***       VISIBILITY & PHASES      ***/
    /**************************************/
    [AuraEffectHandler(AuraType.None)]
    private void HandleUnused(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply) { }

    [AuraEffectHandler(AuraType.ModInvisibilityDetect)]
    private void HandleModInvisibilityDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;
        var type = (InvisibilityType)MiscValue;

        if (apply)
        {
            target.Visibility.InvisibilityDetect.AddFlag(type);
            target.Visibility.InvisibilityDetect.AddValue(type, Amount);
        }
        else
        {
            if (!target.HasAuraType(AuraType.ModInvisibilityDetect))
                target.Visibility.InvisibilityDetect.DelFlag(type);

            target.Visibility.InvisibilityDetect.AddValue(type, -Amount);
        }

        // call functions which may have additional effects after changing state of unit
        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.ModInvisibility)]
    private void HandleModInvisibility(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
            return;

        var target = aurApp.Target;
        var playerTarget = target.AsPlayer;
        var type = (InvisibilityType)MiscValue;

        if (apply)
        {
            // apply glow vision
            if (playerTarget != null && type == InvisibilityType.General)
                playerTarget.AddAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

            target.Visibility.Invisibility.AddFlag(type);
            target.Visibility.Invisibility.AddValue(type, Amount);

            target.SetVisFlag(UnitVisFlags.Invisible);
        }
        else
        {
            if (!target.HasAuraType(AuraType.ModInvisibility))
            {
                // if not have different invisibility auras.
                // always remove glow vision
                if (playerTarget != null)
                    playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                target.Visibility.Invisibility.DelFlag(type);
            }
            else
            {
                var found = false;
                var invisAuras = target.GetAuraEffectsByType(AuraType.ModInvisibility);

                foreach (var eff in invisAuras)
                    if (MiscValue == eff.MiscValue)
                    {
                        found = true;

                        break;
                    }

                if (!found)
                {
                    // if not have invisibility auras of type INVISIBILITY_GENERAL
                    // remove glow vision
                    if (playerTarget != null && type == InvisibilityType.General)
                        playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                    target.Visibility.Invisibility.DelFlag(type);

                    target.RemoveVisFlag(UnitVisFlags.Invisible);
                }
            }

            target.Visibility.Invisibility.AddValue(type, -Amount);
        }

        // call functions which may have additional effects after changing state of unit
        if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            // drop flag at invisibiliy in bg
            target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.ModStealthDetect)]
    private void HandleModStealthDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;
        var type = (StealthType)MiscValue;

        if (apply)
        {
            target.Visibility.StealthDetect.AddFlag(type);
            target.Visibility.StealthDetect.AddValue(type, Amount);
        }
        else
        {
            if (!target.HasAuraType(AuraType.ModStealthDetect))
                target.Visibility.StealthDetect.DelFlag(type);

            target.Visibility.StealthDetect.AddValue(type, -Amount);
        }

        // call functions which may have additional effects after changing state of unit
        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.ModStealth)]
    private void HandleModStealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
            return;

        var target = aurApp.Target;
        var type = (StealthType)MiscValue;

        if (apply)
        {
            target.Visibility.Stealth.AddFlag(type);
            target.Visibility.Stealth.AddValue(type, Amount);
            target.SetVisFlag(UnitVisFlags.Stealthed);
            var playerTarget = target.AsPlayer;

            if (playerTarget != null)
                playerTarget.AddAuraVision(PlayerFieldByte2Flags.Stealth);
        }
        else
        {
            target.Visibility.Stealth.AddValue(type, -Amount);

            if (!target.HasAuraType(AuraType.ModStealth)) // if last SPELL_AURA_MOD_STEALTH
            {
                target.Visibility.Stealth.DelFlag(type);

                target.RemoveVisFlag(UnitVisFlags.Stealthed);
                var playerTarget = target.AsPlayer;

                if (playerTarget != null)
                    playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.Stealth);
            }
        }

        // call functions which may have additional effects after changing state of unit
        if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            // drop flag at stealth in bg
            target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.ModStealthLevel)]
    private void HandleModStealthLevel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;
        var type = (StealthType)MiscValue;

        if (apply)
            target.Visibility.Stealth.AddValue(type, Amount);
        else
            target.Visibility.Stealth.AddValue(type, -Amount);

        // call functions which may have additional effects after changing state of unit
        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.DetectAmore)]
    private void HandleDetectAmore(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (target.IsTypeId(TypeId.Player))
            return;

        if (apply)
        {
            var playerTarget = target.AsPlayer;

            if (playerTarget != null)
                playerTarget.AddAuraVision((PlayerFieldByte2Flags)(1 << (MiscValue - 1)));
        }
        else
        {
            if (target.HasAuraType(AuraType.DetectAmore))
            {
                var amoreAuras = target.GetAuraEffectsByType(AuraType.DetectAmore);

                foreach (var auraEffect in amoreAuras)
                    if (MiscValue == auraEffect.MiscValue)
                        return;
            }

            var playerTarget = target.AsPlayer;

            if (playerTarget != null)
                playerTarget.RemoveAuraVision((PlayerFieldByte2Flags)(1 << (MiscValue - 1)));
        }
    }

    [AuraEffectHandler(AuraType.SpiritOfRedemption)]
    private void HandleSpiritOfRedemption(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        // prepare spirit state
        if (apply)
        {
            if (target.IsTypeId(TypeId.Player))
                // set stand state (expected in this form)
                if (!target.IsStandState)
                    target.SetStandState(UnitStandStateType.Stand);
        }
        // die at aura end
        else if (target.IsAlive)
            // call functions which may have additional effects after changing state of unit
        {
            target.SetDeathState(DeathState.JustDied);
        }
    }

    [AuraEffectHandler(AuraType.Ghost)]
    private void HandleAuraGhost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
        {
            target.SetPlayerFlag(PlayerFlags.Ghost);
            target.Visibility.ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
            target.Visibility.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
        }
        else
        {
            if (target.HasAuraType(AuraType.Ghost))
                return;

            target.RemovePlayerFlag(PlayerFlags.Ghost);
            target.Visibility.ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
            target.Visibility.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
        }
    }

    [AuraEffectHandler(AuraType.Phase)]
    private void HandlePhase(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
            PhasingHandler.AddPhase(target, (uint)MiscValueB, true);
        else
            PhasingHandler.RemovePhase(target, (uint)MiscValueB, true);
    }

    [AuraEffectHandler(AuraType.PhaseGroup)]
    private void HandlePhaseGroup(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
            PhasingHandler.AddPhaseGroup(target, (uint)MiscValueB, true);
        else
            PhasingHandler.RemovePhaseGroup(target, (uint)MiscValueB, true);
    }

    [AuraEffectHandler(AuraType.PhaseAlwaysVisible)]
    private void HandlePhaseAlwaysVisible(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!apply)
        {
            PhasingHandler.SetAlwaysVisible(target, true, true);
        }
        else
        {
            if (target.HasAuraType(AuraType.PhaseAlwaysVisible) || (target.IsPlayer && target.AsPlayer.IsGameMaster))
                return;

            PhasingHandler.SetAlwaysVisible(target, false, true);
        }
    }

    /**********************/
    /***   UNIT MODEL   ***/
    /**********************/
    [AuraEffectHandler(AuraType.ModShapeshift)]
    private void HandleAuraModShapeshift(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.RealOrReapplyMask))
            return;

        var shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(MiscValue);
        //ASSERT(shapeInfo, "Spell {0} uses unknown ShapeshiftForm (%u).", GetId(), GetMiscValue());

        var target = aurApp.Target;
        var form = (ShapeShiftForm)MiscValue;
        var modelid = target.GetModelForForm(form, Id);

        if (apply)
        {
            // remove polymorph before changing display id to keep new display id
            switch (form)
            {
                case ShapeShiftForm.CatForm:
                case ShapeShiftForm.TreeOfLife:
                case ShapeShiftForm.TravelForm:
                case ShapeShiftForm.AquaticForm:
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.FlightFormEpic:
                case ShapeShiftForm.FlightForm:
                case ShapeShiftForm.MoonkinForm:
                {
                    // remove movement affects
                    target.RemoveAurasByShapeShift();

                    // and polymorphic affects
                    if (target.IsPolymorphed)
                        target.RemoveAura(target.TransformSpell);

                    break;
                }
                default:
                    break;
            }

            // remove other shapeshift before applying a new one
            target.RemoveAurasByType(AuraType.ModShapeshift, ObjectGuid.Empty, Base);

            // stop handling the effect if it was removed by linked event
            if (aurApp.HasRemoveMode)
                return;

            var prevForm = target.ShapeshiftForm;
            target.ShapeshiftForm = form;

            // add the shapeshift aura's boosts
            if (prevForm != form)
                HandleShapeshiftBoosts(target, true);

            if (modelid > 0)
            {
                var transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.TransformSpell, Base.CastDifficulty);

                if (transformSpellInfo == null || !SpellInfo.IsPositive)
                    target.SetDisplayId(modelid);
            }

            if (!shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance))
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Shapeshifting, SpellInfo);
        }
        else
        {
            // reset model id if no other auras present
            // may happen when aura is applied on linked event on aura removal
            if (!target.HasAuraType(AuraType.ModShapeshift))
            {
                target.ShapeshiftForm = ShapeShiftForm.None;

                if (target.Class == PlayerClass.Druid)
                    // Remove movement impairing effects also when shifting out
                    target.RemoveAurasByShapeShift();
            }

            if (modelid > 0)
                target.RestoreDisplayId(target.IsMounted);

            switch (form)
            {
                // Nordrassil Harness - bonus
                case ShapeShiftForm.BearForm:
                case ShapeShiftForm.CatForm:
                    var dummy = target.GetAuraEffect(37315, 0);

                    if (dummy != null)
                        target.CastSpell(target, 37316, new CastSpellExtraArgs(dummy));

                    break;
                // Nordrassil Regalia - bonus
                case ShapeShiftForm.MoonkinForm:
                    dummy = target.GetAuraEffect(37324, 0);

                    if (dummy != null)
                        target.CastSpell(target, 37325, new CastSpellExtraArgs(dummy));

                    break;
                default:
                    break;
            }

            // remove the shapeshift aura's boosts
            HandleShapeshiftBoosts(target, apply);
        }

        var playerTarget = target.AsPlayer;

        if (playerTarget != null)
        {
            playerTarget.SendMovementSetCollisionHeight(playerTarget.CollisionHeight, UpdateCollisionHeightReason.Force);
            playerTarget.InitDataForForm();
        }
        else
        {
            target.UpdateDisplayPower();
        }

        if (target.Class == PlayerClass.Druid)
        {
            // Dash
            var aurEff = target.GetAuraEffect(AuraType.ModIncreaseSpeed, SpellFamilyNames.Druid, new FlagArray128(0, 0, 0x8));

            if (aurEff != null)
                aurEff.RecalculateAmount();

            // Disarm handling
            // If druid shifts while being disarmed we need to deal with that since forms aren't affected by disarm
            // and also HandleAuraModDisarm is not triggered
            if (!target.CanUseAttackType(WeaponAttackType.BaseAttack))
            {
                var pItem = target.AsPlayer.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

                if (pItem != null)
                    target.AsPlayer._ApplyWeaponDamage(EquipmentSlot.MainHand, pItem, apply);
            }
        }

        // stop handling the effect if it was removed by linked event
        if (apply && aurApp.HasRemoveMode)
            return;

        if (target.IsTypeId(TypeId.Player))
            // Learn spells for shapeshift form - no need to send action bars or add spells to spellbook
            for (byte i = 0; i < SpellConst.MaxShapeshift; ++i)
            {
                if (shapeInfo.PresetSpellID[i] == 0)
                    continue;

                if (apply)
                    target.AsPlayer.AddTemporarySpell(shapeInfo.PresetSpellID[i]);
                else
                    target.AsPlayer.RemoveTemporarySpell(shapeInfo.PresetSpellID[i]);
            }
    }

    [AuraEffectHandler(AuraType.Transform)]
    private void HandleAuraTransform(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            // update active transform spell only when transform not set or not overwriting negative by positive case
            var transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.TransformSpell, Base.CastDifficulty);

            if (transformSpellInfo == null || !SpellInfo.IsPositive || transformSpellInfo.IsPositive)
            {
                target.TransformSpell = Id;

                // special case (spell specific functionality)
                if (MiscValue == 0)
                {
                    var isFemale = target.NativeGender == Gender.Female;

                    switch (Id)
                    {
                        // Orb of Deception
                        case 16739:
                        {
                            if (!target.IsTypeId(TypeId.Player))
                                return;

                            switch (target.Race)
                            {
                                // Blood Elf
                                case Race.BloodElf:
                                    target.SetDisplayId(isFemale ? 17830 : 17829u);

                                    break;
                                // Orc
                                case Race.Orc:
                                    target.SetDisplayId(isFemale ? 10140 : 10139u);

                                    break;
                                // Troll
                                case Race.Troll:
                                    target.SetDisplayId(isFemale ? 10134 : 10135u);

                                    break;
                                // Tauren
                                case Race.Tauren:
                                    target.SetDisplayId(isFemale ? 10147 : 10136u);

                                    break;
                                // Undead
                                case Race.Undead:
                                    target.SetDisplayId(isFemale ? 10145 : 10146u);

                                    break;
                                // Draenei
                                case Race.Draenei:
                                    target.SetDisplayId(isFemale ? 17828 : 17827u);

                                    break;
                                // Dwarf
                                case Race.Dwarf:
                                    target.SetDisplayId(isFemale ? 10142 : 10141u);

                                    break;
                                // Gnome
                                case Race.Gnome:
                                    target.SetDisplayId(isFemale ? 10149 : 10148u);

                                    break;
                                // Human
                                case Race.Human:
                                    target.SetDisplayId(isFemale ? 10138 : 10137u);

                                    break;
                                // Night Elf
                                case Race.NightElf:
                                    target.SetDisplayId(isFemale ? 10144 : 10143u);

                                    break;
                                default:
                                    break;
                            }

                            break;
                        }
                        // Murloc costume
                        case 42365:
                            target.SetDisplayId(21723);

                            break;
                        // Dread Corsair
                        case 50517:
                        // Corsair Costume
                        case 51926:
                        {
                            if (!target.IsTypeId(TypeId.Player))
                                return;

                            switch (target.Race)
                            {
                                // Blood Elf
                                case Race.BloodElf:
                                    target.SetDisplayId(isFemale ? 25043 : 25032u);

                                    break;
                                // Orc
                                case Race.Orc:
                                    target.SetDisplayId(isFemale ? 25050 : 25039u);

                                    break;
                                // Troll
                                case Race.Troll:
                                    target.SetDisplayId(isFemale ? 25052 : 25041u);

                                    break;
                                // Tauren
                                case Race.Tauren:
                                    target.SetDisplayId(isFemale ? 25051 : 25040u);

                                    break;
                                // Undead
                                case Race.Undead:
                                    target.SetDisplayId(isFemale ? 25053 : 25042u);

                                    break;
                                // Draenei
                                case Race.Draenei:
                                    target.SetDisplayId(isFemale ? 25044 : 25033u);

                                    break;
                                // Dwarf
                                case Race.Dwarf:
                                    target.SetDisplayId(isFemale ? 25045 : 25034u);

                                    break;
                                // Gnome
                                case Race.Gnome:
                                    target.SetDisplayId(isFemale ? 25035 : 25046u);

                                    break;
                                // Human
                                case Race.Human:
                                    target.SetDisplayId(isFemale ? 25037 : 25048u);

                                    break;
                                // Night Elf
                                case Race.NightElf:
                                    target.SetDisplayId(isFemale ? 25038 : 25049u);

                                    break;
                                default:
                                    break;
                            }

                            break;
                        }
                        // Pygmy Oil
                        case 53806:
                            target.SetDisplayId(22512);

                            break;
                        // Honor the Dead
                        case 65386:
                        case 65495:
                            target.SetDisplayId(isFemale ? 29204 : 29203u);

                            break;
                        // Darkspear Pride
                        case 75532:
                            target.SetDisplayId(isFemale ? 31738 : 31737u);

                            break;
                        // Gnomeregan Pride
                        case 75531:
                            target.SetDisplayId(isFemale ? 31655 : 31654u);

                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var ci = Global.ObjectMgr.GetCreatureTemplate((uint)MiscValue);

                    if (ci == null)
                    {
                        target.SetDisplayId(16358); // pig pink ^_^
                        Log.Logger.Error("Auras: unknown creature id = {0} (only need its modelid) From Spell Aura Transform in Spell ID = {1}", MiscValue, Id);
                    }
                    else
                    {
                        uint model_id = 0;
                        var modelid = GameObjectManager.ChooseDisplayId(ci).CreatureDisplayId;

                        if (modelid != 0)
                            model_id = modelid; // Will use the default model here

                        target.SetDisplayId(model_id);

                        // Dragonmaw Illusion (set mount model also)
                        if (Id == 42016 && target.MountDisplayId != 0 && !target.GetAuraEffectsByType(AuraType.ModIncreaseMountedFlightSpeed).Empty())
                            target.MountDisplayId = 16314;
                    }
                }
            }

            // polymorph case
            if (mode.HasAnyFlag(AuraEffectHandleModes.Real) && target.IsTypeId(TypeId.Player) && target.IsPolymorphed)
            {
                // for players, start regeneration after 1s (in polymorph fast regeneration case)
                // only if caster is Player (after patch 2.4.2)
                if (CasterGuid.IsPlayer)
                    target.AsPlayer.SetRegenTimerCount(1 * Time.InMilliseconds);

                //dismount polymorphed target (after patch 2.4.2)
                if (target.IsMounted)
                    target.RemoveAurasByType(AuraType.Mounted);
            }
        }
        else
        {
            if (target.TransformSpell == Id)
                target.TransformSpell = 0;

            target.RestoreDisplayId(target.IsMounted);

            // Dragonmaw Illusion (restore mount model)
            if (Id == 42016 && target.MountDisplayId == 16314)
                if (!target.GetAuraEffectsByType(AuraType.Mounted).Empty())
                {
                    var cr_id = target.GetAuraEffectsByType(AuraType.Mounted)[0].MiscValue;
                    var ci = Global.ObjectMgr.GetCreatureTemplate((uint)cr_id);

                    if (ci != null)
                    {
                        var model = GameObjectManager.ChooseDisplayId(ci);
                        Global.ObjectMgr.GetCreatureModelRandomGender(ref model, ci);

                        target.MountDisplayId = model.CreatureDisplayId;
                    }
                }
        }
    }

    [AuraEffectHandler(AuraType.ModScale)]
    [AuraEffectHandler(AuraType.ModScale2)]
    private void HandleAuraModScale(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
            return;

        aurApp.Target.RecalculateObjectScale();
    }

    [AuraEffectHandler(AuraType.CloneCaster)]
    private void HandleAuraCloneCaster(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            var caster = Caster;

            if (caster == null || caster == target)
                return;

            // What must be cloned? at least display and scale
            target.SetDisplayId(caster.DisplayId);
            //target.SetObjectScale(caster.GetFloatValue(OBJECT_FIELD_SCALE_X)); // we need retail info about how scaling is handled (aura maybe?)
            target.SetUnitFlag2(UnitFlags2.MirrorImage);
        }
        else
        {
            target.SetDisplayId(target.NativeDisplayId);
            target.RemoveUnitFlag2(UnitFlags2.MirrorImage);
        }
    }

    /************************/
    /***      FIGHT       ***/
    /************************/
    [AuraEffectHandler(AuraType.FeignDeath)]
    private void HandleFeignDeath(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            List<Unit> targets = new();
            var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(target, target, target.Location.Map.VisibilityRange, u => u.HasUnitState(UnitState.Casting));
            var searcher = new UnitListSearcher(target, targets, u_check, GridType.All);

            Cell.VisitGrid(target, searcher, target.Location.Map.VisibilityRange);

            foreach (var unit in targets)
                for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
                    if (unit.GetCurrentSpell(i) != null && unit.GetCurrentSpell(i).Targets.UnitTargetGUID == target.GUID)
                        unit.InterruptSpell(i, false);

            foreach (var pair in target.GetThreatManager().ThreatenedByMeList)
                pair.Value.ScaleThreat(0.0f);

            if (target.Location.Map.IsDungeon) // feign death does not remove combat in dungeons
            {
                target.AttackStop();
                var targetPlayer = target.AsPlayer;

                if (targetPlayer != null)
                    targetPlayer.SendAttackSwingCancelAttack();
            }
            else
            {
                target.CombatStop(false, false);
            }

            // prevent interrupt message
            if (CasterGuid == target.GUID && target.GetCurrentSpell(CurrentSpellTypes.Generic) != null)
                target.FinishSpell(CurrentSpellTypes.Generic, SpellCastResult.Interrupted);

            target.InterruptNonMeleeSpells(true);

            // stop handling the effect if it was removed by linked event
            if (aurApp.HasRemoveMode)
                return;

            target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
            target.SetUnitFlag2(UnitFlags2.FeignDeath);
            target.SetUnitFlag3(UnitFlags3.FakeDead);
            target.AddUnitState(UnitState.Died);

            var creature = target.AsCreature;

            if (creature != null)
                creature.ReactState = ReactStates.Passive;
        }
        else
        {
            target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
            target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
            target.RemoveUnitFlag3(UnitFlags3.FakeDead);
            target.ClearUnitState(UnitState.Died);

            var creature = target.AsCreature;

            if (creature != null)
                creature.InitializeReactState();
        }
    }

    [AuraEffectHandler(AuraType.ModUnattackable)]
    private void HandleModUnattackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
        if (!apply && target.HasAuraType(AuraType.ModUnattackable))
            return;

        if (apply)
            target.SetUnitFlag(UnitFlags.NonAttackable2);
        else
            target.RemoveUnitFlag(UnitFlags.NonAttackable2);

        // call functions which may have additional effects after changing state of unit
        if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
        {
            if (target.Location.Map.IsDungeon)
            {
                target.AttackStop();
                var targetPlayer = target.AsPlayer;

                if (targetPlayer != null)
                    targetPlayer.SendAttackSwingCancelAttack();
            }
            else
            {
                target.CombatStop();
            }
        }
    }

    [AuraEffectHandler(AuraType.ModDisarm)]
    private void HandleAuraModDisarm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        //Prevent handling aura twice
        var type = AuraType;

        if (apply ? target.GetAuraEffectsByType(type).Count > 1 : target.HasAuraType(type))
            return;

        Action<Unit> flagChangeFunc = null;
        byte slot;
        WeaponAttackType attType;

        switch (type)
        {
            case AuraType.ModDisarm:
                if (apply)
                    flagChangeFunc = unit => { unit.SetUnitFlag(UnitFlags.Disarmed); };
                else
                    flagChangeFunc = unit => { unit.RemoveUnitFlag(UnitFlags.Disarmed); };

                slot = EquipmentSlot.MainHand;
                attType = WeaponAttackType.BaseAttack;

                break;
            case AuraType.ModDisarmOffhand:
                if (apply)
                    flagChangeFunc = unit => { unit.SetUnitFlag2(UnitFlags2.DisarmOffhand); };
                else
                    flagChangeFunc = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmOffhand); };

                slot = EquipmentSlot.OffHand;
                attType = WeaponAttackType.OffAttack;

                break;
            case AuraType.ModDisarmRanged:
                if (apply)
                    flagChangeFunc = unit => { unit.SetUnitFlag2(UnitFlags2.DisarmRanged); };
                else
                    flagChangeFunc = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmRanged); };

                slot = EquipmentSlot.MainHand;
                attType = WeaponAttackType.RangedAttack;

                break;
            default:
                return;
        }

        // set/remove flag before weapon bonuses so it's properly reflected in CanUseAttackType
        flagChangeFunc?.Invoke(target);

        // Handle damage modification, shapeshifted druids are not affected
        if (target.IsTypeId(TypeId.Player) && !target.IsInFeralForm)
        {
            var player = target.AsPlayer;

            var item = player.GetItemByPos(InventorySlots.Bag0, slot);

            if (item != null)
            {
                var attackType = Player.GetAttackBySlot(slot, item.Template.InventoryType);

                player.ApplyItemDependentAuras(item, !apply);

                if (attackType < WeaponAttackType.Max)
                {
                    player._ApplyWeaponDamage(slot, item, !apply);

                    if (!apply) // apply case already handled on item dependent aura removal (if any)
                        player.UpdateWeaponDependentAuras(attackType);
                }
            }
        }

        if (target.IsTypeId(TypeId.Unit) && target.AsCreature.CurrentEquipmentId != 0)
            target.UpdateDamagePhysical(attType);
    }

    [AuraEffectHandler(AuraType.ModSilence)]
    private void HandleAuraModSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetSilencedSchoolMask((SpellSchoolMask)MiscValue);

            // call functions which may have additional effects after changing state of unit
            // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
            for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
            {
                var spell = target.GetCurrentSpell(i);

                if (spell != null)
                    if (spell.SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                        // Stop spells on prepare or casting state
                        target.InterruptSpell(i, false);
            }
        }
        else
        {
            var silenceSchoolMask = 0;

            foreach (var eff in target.GetAuraEffectsByType(AuraType.ModSilence))
                silenceSchoolMask |= eff.MiscValue;

            foreach (var eff in target.GetAuraEffectsByType(AuraType.ModPacifySilence))
                silenceSchoolMask |= eff.MiscValue;

            target.ReplaceAllSilencedSchoolMask((uint)silenceSchoolMask);
        }
    }

    [AuraEffectHandler(AuraType.ModPacify)]
    private void HandleAuraModPacify(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetUnitFlag(UnitFlags.Pacified);
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType.ModPacify) || target.HasAuraType(AuraType.ModPacifySilence))
                return;

            target.RemoveUnitFlag(UnitFlags.Pacified);
        }
    }

    [AuraEffectHandler(AuraType.ModPacifySilence)]
    private void HandleAuraModPacifyAndSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        // Vengeance of the Blue Flight (@todo REMOVE THIS!)
        // @workaround
        if (_spellInfo.Id == 45839)
        {
            if (apply)
                target.SetUnitFlag(UnitFlags.NonAttackable);
            else
                target.RemoveUnitFlag(UnitFlags.NonAttackable);
        }

        if (!(apply))
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType.ModPacifySilence))
                return;

        HandleAuraModPacify(aurApp, mode, apply);
        HandleAuraModSilence(aurApp, mode, apply);
    }

    [AuraEffectHandler(AuraType.ModNoActions)]
    private void HandleAuraModNoActions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetUnitFlag2(UnitFlags2.NoActions);

            // call functions which may have additional effects after changing state of unit
            // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
            for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
            {
                var spell = target.GetCurrentSpell(i);

                if (spell)
                    if (spell.SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions))
                        // Stop spells on prepare or casting state
                        target.InterruptSpell(i, false);
            }
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType.ModNoActions))
                return;

            target.RemoveUnitFlag2(UnitFlags2.NoActions);
        }
    }

    /****************************/
    /***      TRACKING        ***/
    /****************************/
    [AuraEffectHandler(AuraType.TrackCreatures)]
    private void HandleAuraTrackCreatures(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
            target.SetTrackCreatureFlag(1u << (MiscValue - 1));
        else
            target.RemoveTrackCreatureFlag(1u << (MiscValue - 1));
    }

    [AuraEffectHandler(AuraType.TrackStealthed)]
    private void HandleAuraTrackStealthed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (!(apply))
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        if (apply)
            target.SetPlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
        else
            target.RemovePlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
    }

    [AuraEffectHandler(AuraType.ModStalked)]
    private void HandleAuraModStalked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        // used by spells: Hunter's Mark, Mind Vision, Syndicate Tracker (MURP) DND
        if (apply)
        {
            target.SetDynamicFlag(UnitDynFlags.TrackUnit);
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (!target.HasAuraType(AuraType))
                target.RemoveDynamicFlag(UnitDynFlags.TrackUnit);
        }

        // call functions which may have additional effects after changing state of unit
        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.Untrackable)]
    private void HandleAuraUntrackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
        {
            target.SetVisFlag(UnitVisFlags.Untrackable);
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

            target.RemoveVisFlag(UnitVisFlags.Untrackable);
        }
    }

    /****************************/
    /***  SKILLS & TALENTS    ***/
    /****************************/
    [AuraEffectHandler(AuraType.ModSkill)]
    [AuraEffectHandler(AuraType.ModSkill2)]
    private void HandleAuraModSkill(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Skill)))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        var prot = (SkillType)MiscValue;
        var points = Amount;

        if (prot == SkillType.Defense)
            return;

        target.ModifySkillBonus(prot, (int)(apply ? points : -points), AuraType == AuraType.ModSkillTalent);
    }

    [AuraEffectHandler(AuraType.AllowTalentSwapping)]
    private void HandleAuraAllowTalentSwapping(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
            target.SetUnitFlag2(UnitFlags2.AllowChangingTalents);
        else if (!target.HasAuraType(AuraType))
            target.RemoveUnitFlag2(UnitFlags2.AllowChangingTalents);
    }

    /****************************/
    /***       MOVEMENT       ***/
    /****************************/
    [AuraEffectHandler(AuraType.Mounted)]
    private void HandleAuraMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            {
                var creatureEntry = (uint)MiscValue;
                uint displayId = 0;
                uint vehicleId = 0;

                var mountEntry = Global.DB2Mgr.GetMount(Id);

                if (mountEntry != null)
                {
                    var mountDisplays = Global.DB2Mgr.GetMountDisplays(mountEntry.Id);

                    if (mountDisplays != null)
                    {
                        if (mountEntry.IsSelfMount())
                        {
                            displayId = SharedConst.DisplayIdHiddenMount;
                        }
                        else
                        {
                            var usableDisplays = mountDisplays.Where(mountDisplay =>
                                                              {
                                                                  var playerTarget = target.AsPlayer;

                                                                  if (playerTarget != null)
                                                                  {
                                                                      var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountDisplay.PlayerConditionID);

                                                                      if (playerCondition != null)
                                                                          return ConditionManager.IsPlayerMeetingCondition(playerTarget, playerCondition);
                                                                  }

                                                                  return true;
                                                              })
                                                              .ToList();

                            if (!usableDisplays.Empty())
                                displayId = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                        }
                    }
                    // TODO: CREATE TABLE mount_vehicle (mountId, vehicleCreatureId) for future mounts that are vehicles (new mounts no longer have proper data in MiscValue)
                    //if (MountVehicle const* mountVehicle = sObjectMgr->GetMountVehicle(mountEntry->Id))
                    //    creatureEntry = mountVehicle->VehicleCreatureId;
                }

                var creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creatureEntry);

                if (creatureInfo != null)
                {
                    vehicleId = creatureInfo.VehicleId;

                    if (displayId == 0)
                    {
                        var model = GameObjectManager.ChooseDisplayId(creatureInfo);
                        Global.ObjectMgr.GetCreatureModelRandomGender(ref model, creatureInfo);
                        displayId = model.CreatureDisplayId;
                    }

                    //some spell has one aura of mount and one of vehicle
                    foreach (var effect in SpellInfo.Effects)
                        if (effect.IsEffect(SpellEffectName.Summon) && effect.MiscValue == MiscValue)
                            displayId = 0;
                }

                target.Mount(displayId, vehicleId, creatureEntry);
            }

            // cast speed aura
            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            {
                var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(Amount);

                if (mountCapability != null)
                    target.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(this));
            }
        }
        else
        {
            if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                target.Dismount();

            //some mounts like Headless Horseman's Mount or broom stick are skill based spell
            // need to remove ALL arura related to mounts, this will stop client crash with broom stick
            // and never endless flying after using Headless Horseman's Mount
            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                target.RemoveAurasByType(AuraType.Mounted);

            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            {
                // remove speed aura
                var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(Amount);

                if (mountCapability != null)
                    target.RemoveAurasDueToSpell(mountCapability.ModSpellAuraID, target.GUID);
            }
        }
    }

    [AuraEffectHandler(AuraType.Fly)]
    private void HandleAuraAllowFlight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType) || target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                return;

        target.SetCanTransitionBetweenSwimAndFly(apply);

        if (target.SetCanFly(apply))
            if (!apply && !target.IsGravityDisabled)
                target.MotionMaster.MoveFall();
    }

    [AuraEffectHandler(AuraType.WaterWalk)]
    private void HandleAuraWaterWalk(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        target.SetWaterWalking(apply);
    }

    [AuraEffectHandler(AuraType.FeatherFall)]
    private void HandleAuraFeatherFall(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        target.SetFeatherFall(apply);

        // start fall from current height
        if (!apply && target.IsTypeId(TypeId.Player))
            target.AsPlayer.SetFallInformation(0, target.Location.Z);
    }

    [AuraEffectHandler(AuraType.Hover)]
    private void HandleAuraHover(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        target.SetHover(apply); //! Sets movementflags
    }

    [AuraEffectHandler(AuraType.WaterBreathing)]
    private void HandleWaterBreathing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        // update timers in client
        if (target.IsTypeId(TypeId.Player))
            target.AsPlayer.UpdateMirrorTimers();
    }

    [AuraEffectHandler(AuraType.ForceMoveForward)]
    private void HandleForceMoveForward(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetUnitFlag2(UnitFlags2.ForceMovement);
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

            target.RemoveUnitFlag2(UnitFlags2.ForceMovement);
        }
    }

    [AuraEffectHandler(AuraType.CanTurnWhileFalling)]
    private void HandleAuraCanTurnWhileFalling(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        target.SetCanTurnWhileFalling(apply);
    }

    [AuraEffectHandler(AuraType.IgnoreMovementForces)]
    private void HandleIgnoreMovementForces(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        target.SetIgnoreMovementForces(apply);
    }

    [AuraEffectHandler(AuraType.DisableInertia)]
    private void HandleDisableInertia(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        target.SetDisableInertia(apply);
    }

    /****************************/
    /***        THREAT        ***/
    /****************************/
    [AuraEffectHandler(AuraType.ModThreat)]
    private void HandleModThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        aurApp.Target.GetThreatManager().UpdateMySpellSchoolModifiers();
    }

    [AuraEffectHandler(AuraType.ModTotalThreat)]
    private void HandleAuraModTotalThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        if (!target.IsAlive || !target.IsTypeId(TypeId.Player))
            return;

        var caster = Caster;

        if (caster is { IsAlive: true })
            caster.GetThreatManager().UpdateMyTempModifiers();
    }

    [AuraEffectHandler(AuraType.ModTaunt)]
    private void HandleModTaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!target.IsAlive || !target.CanHaveThreatList)
            return;

        target.GetThreatManager().TauntUpdate();
    }

    [AuraEffectHandler(AuraType.ModDetaunt)]
    private void HandleModDetaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var caster = Caster;
        var target = aurApp.Target;

        if (!caster || !caster.IsAlive || !target.IsAlive || !caster.CanHaveThreatList)
            return;

        caster.GetThreatManager().TauntUpdate();
    }

    /*****************************/
    /***        CONTROL        ***/
    /*****************************/
    [AuraEffectHandler(AuraType.ModConfuse)]
    private void HandleModConfuse(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.SetControlled(apply, UnitState.Confused);

        if (apply)
            target.GetThreatManager().EvaluateSuppressed();
    }

    [AuraEffectHandler(AuraType.ModFear)]
    private void HandleModFear(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.SetControlled(apply, UnitState.Fleeing);
    }

    [AuraEffectHandler(AuraType.ModStun)]
    private void HandleAuraModStun(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.SetControlled(apply, UnitState.Stunned);

        if (apply)
            target.GetThreatManager().EvaluateSuppressed();
    }

    [AuraEffectHandler(AuraType.ModRoot)]
    [AuraEffectHandler(AuraType.ModRoot2)]
    private void HandleAuraModRoot(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.SetControlled(apply, UnitState.Root);
    }

    [AuraEffectHandler(AuraType.PreventsFleeing)]
    private void HandlePreventFleeing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        // Since patch 3.0.2 this mechanic no longer affects fear effects. It will ONLY prevent humanoids from fleeing due to low health.
        if (!apply || target.HasAuraType(AuraType.ModFear))
            return;

        // TODO: find a way to cancel fleeing for assistance.
        // Currently this will only stop creatures fleeing due to low health that could not find nearby allies to flee towards.
        target.SetControlled(false, UnitState.Fleeing);
    }

    [AuraEffectHandler(AuraType.ModRootDisableGravity)]
    private void HandleAuraModRootAndDisableGravity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.SetControlled(apply, UnitState.Root);

        // Do not remove DisableGravity if there are more than this auraEffect of that kind on the unit or if it's a creature with DisableGravity on its movement template.
        if (!apply && (target.HasAuraType(AuraType) || target.HasAuraType(AuraType.ModStunDisableGravity) || (target.IsCreature && target.AsCreature.MovementTemplate.Flight == CreatureFlightMovementType.DisableGravity)))
            return;

        if (target.SetDisableGravity(apply))
            if (!apply && !target.IsFlying)
                target.MotionMaster.MoveFall();
    }

    [AuraEffectHandler(AuraType.ModStunDisableGravity)]
    private void HandleAuraModStunAndDisableGravity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.SetControlled(apply, UnitState.Stunned);

        if (apply)
            target.GetThreatManager().EvaluateSuppressed();

        // Do not remove DisableGravity if there are more than this auraEffect of that kind on the unit or if it's a creature with DisableGravity on its movement template.
        if (!apply && (target.HasAuraType(AuraType) || target.HasAuraType(AuraType.ModStunDisableGravity) || (target.IsCreature && target.AsCreature.MovementTemplate.Flight == CreatureFlightMovementType.DisableGravity)))
            return;

        if (target.SetDisableGravity(apply))
            if (!apply && !target.IsFlying)
                target.MotionMaster.MoveFall();
    }

    /***************************/
    /***        CHARM        ***/
    /***************************/
    [AuraEffectHandler(AuraType.ModPossess)]
    private void HandleModPossess(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        var caster = Caster;

        // no support for posession AI yet
        if (caster != null && caster.IsTypeId(TypeId.Unit))
        {
            HandleModCharm(aurApp, mode, apply);

            return;
        }

        if (apply)
            target.SetCharmedBy(caster, CharmType.Possess, aurApp);
        else
            target.RemoveCharmedBy(caster);
    }

    [AuraEffectHandler(AuraType.ModPossessPet)]
    private void HandleModPossessPet(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var caster = Caster;

        if (caster == null || !caster.IsTypeId(TypeId.Player))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Unit) || !target.IsPet)
            return;

        var pet = target.AsPet;

        if (apply)
        {
            if (caster.AsPlayer.CurrentPet != pet)
                return;

            pet.SetCharmedBy(caster, CharmType.Possess, aurApp);
        }
        else
        {
            pet.RemoveCharmedBy(caster);

            if (!pet.Location.IsWithinDistInMap(caster, pet.Location.Map.VisibilityRange))
            {
                pet.Remove(PetSaveMode.NotInSlot, true);
            }
            else
            {
                // Reinitialize the pet bar or it will appear greyed out
                caster. // Reinitialize the pet bar or it will appear greyed out
                    AsPlayer.PetSpellInitialize();

                // TODO: remove this
                if (pet.Victim == null && !pet.GetCharmInfo().HasCommandState(CommandStates.Stay))
                    pet.MotionMaster.MoveFollow(caster, SharedConst.PetFollowDist, pet.FollowAngle);
            }
        }
    }

    [AuraEffectHandler(AuraType.ModCharm)]
    private void HandleModCharm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        var caster = Caster;

        if (apply)
            target.SetCharmedBy(caster, CharmType.Charm, aurApp);
        else
            target.RemoveCharmedBy(caster);
    }

    [AuraEffectHandler(AuraType.AoeCharm)]
    private void HandleCharmConvert(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        var caster = Caster;

        if (apply)
            target.SetCharmedBy(caster, CharmType.Convert, aurApp);
        else
            target.RemoveCharmedBy(caster);
    }

    /**
     * Such auras are applied from a caster(=player) to a vehicle.
     * This has been verified using spell #49256
     */
    [AuraEffectHandler(AuraType.ControlVehicle)]
    private void HandleAuraControlVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        if (!target.IsVehicle)
            return;

        var caster = Caster;

        if (caster == null || caster == target)
            return;

        if (apply)
        {
            // Currently spells that have base points  0 and DieSides 0 = "0/0" exception are pushed to -1,
            // however the idea of 0/0 is to ingore flag VEHICLE_SEAT_FLAG_CAN_ENTER_OR_EXIT and -1 checks for it,
            // so this break such spells or most of them.
            // Current formula about m_amount: effect base points + dieside - 1
            // TO DO: Reasearch more about 0/0 and fix it.
            caster._EnterVehicle(target.VehicleKit1, (sbyte)(Amount - 1), aurApp);
        }
        else
        {
            // Remove pending passengers before exiting vehicle - might cause an Uninstall
            target. // Remove pending passengers before exiting vehicle - might cause an Uninstall
                VehicleKit1.RemovePendingEventsForPassenger(caster);

            if (Id == 53111) // Devour Humanoid
            {
                Unit.Kill(target, caster);

                if (caster.IsTypeId(TypeId.Unit))
                    caster.AsCreature.DespawnOrUnsummon();
            }

            var seatChange = mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmount) // Seat change on the same direct vehicle
                             ||
                             target.HasAuraTypeWithCaster(AuraType.ControlVehicle, caster.GUID); // Seat change to a proxy vehicle (for example turret mounted on a siege engine)

            if (!seatChange)
                caster._ExitVehicle();
            else
                target.VehicleKit1.RemovePassenger(caster); // Only remove passenger from vehicle without launching exit movement or despawning the vehicle

            // some SPELL_AURA_CONTROL_VEHICLE auras have a dummy effect on the player - remove them
            caster.RemoveAura(Id);
        }
    }

    /*********************************************************/
    /***                  MODIFY SPEED                     ***/
    /*********************************************************/
    [AuraEffectHandler(AuraType.ModIncreaseSpeed)]
    [AuraEffectHandler(AuraType.ModSpeedAlways)]
    [AuraEffectHandler(AuraType.ModSpeedNotStack)]
    [AuraEffectHandler(AuraType.ModMinimumSpeed)]
    private void HandleAuraModIncreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        target.UpdateSpeed(UnitMoveType.Run);
    }

    [AuraEffectHandler(AuraType.ModIncreaseMountedSpeed)]
    [AuraEffectHandler(AuraType.ModMountedSpeedAlways)]
    [AuraEffectHandler(AuraType.ModMountedSpeedNotStack)]
    private void HandleAuraModIncreaseMountedSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        HandleAuraModIncreaseSpeed(aurApp, mode, apply);
    }

    [AuraEffectHandler(AuraType.ModIncreaseVehicleFlightSpeed)]
    [AuraEffectHandler(AuraType.ModIncreaseMountedFlightSpeed)]
    [AuraEffectHandler(AuraType.ModIncreaseFlightSpeed)]
    [AuraEffectHandler(AuraType.ModMountedFlightSpeedAlways)]
    [AuraEffectHandler(AuraType.ModVehicleSpeedAlways)]
    [AuraEffectHandler(AuraType.ModFlightSpeedNotStack)]
    private void HandleAuraModIncreaseFlightSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
            return;

        var target = aurApp.Target;

        if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            target.UpdateSpeed(UnitMoveType.Flight);

        //! Update ability to fly
        if (AuraType == AuraType.ModIncreaseMountedFlightSpeed)
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask) && (apply || (!target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !target.HasAuraType(AuraType.Fly))))
            {
                target.SetCanTransitionBetweenSwimAndFly(apply);

                if (target.SetCanFly(apply))
                    if (!apply && !target.IsGravityDisabled)
                        target.MotionMaster.MoveFall();
            }

            //! Someone should clean up these hacks and remove it from this function. It doesn't even belong here.
            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                //Players on flying mounts must be immune to polymorph
                if (target.IsTypeId(TypeId.Player))
                    target.ApplySpellImmune(Id, SpellImmunity.Mechanic, (uint)Mechanics.Polymorph, apply);

                // Dragonmaw Illusion (overwrite mount model, mounted aura already applied)
                if (apply && target.HasAuraEffect(42016, 0) && target.MountDisplayId != 0)
                    target.MountDisplayId = 16314;
            }
        }
    }

    [AuraEffectHandler(AuraType.ModIncreaseSwimSpeed)]
    private void HandleAuraModIncreaseSwimSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        target.UpdateSpeed(UnitMoveType.Swim);
    }

    [AuraEffectHandler(AuraType.ModDecreaseSpeed)]
    private void HandleAuraModDecreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        target.UpdateSpeed(UnitMoveType.Run);
        target.UpdateSpeed(UnitMoveType.Swim);
        target.UpdateSpeed(UnitMoveType.Flight);
        target.UpdateSpeed(UnitMoveType.RunBack);
        target.UpdateSpeed(UnitMoveType.SwimBack);
        target.UpdateSpeed(UnitMoveType.FlightBack);
    }

    [AuraEffectHandler(AuraType.UseNormalMovementSpeed)]
    private void HandleAuraModUseNormalSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.UpdateSpeed(UnitMoveType.Run);
        target.UpdateSpeed(UnitMoveType.Swim);
        target.UpdateSpeed(UnitMoveType.Flight);
    }

    [AuraEffectHandler(AuraType.ModMinimumSpeedRate)]
    private void HandleAuraModMinimumSpeedRate(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        target.UpdateSpeed(UnitMoveType.Run);
    }

    [AuraEffectHandler(AuraType.ModMovementForceMagnitude)]
    private void HandleModMovementForceMagnitude(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        aurApp.Target.UpdateMovementForcesModMagnitude();
    }

    /*********************************************************/
    /***                     IMMUNITY                      ***/
    /*********************************************************/
    [AuraEffectHandler(AuraType.MechanicImmunityMask)]
    private void HandleModMechanicImmunityMask(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
    }

    [AuraEffectHandler(AuraType.MechanicImmunity)]
    private void HandleModMechanicImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
    }

    [AuraEffectHandler(AuraType.EffectImmunity)]
    private void HandleAuraModEffectImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

        // when removing flag aura, handle flag drop
        // TODO: this should be handled in aura script for flag spells using AfterEffectRemove hook
        var player = target.AsPlayer;

        if (!apply && player != null && SpellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.StealthOrInvis))
        {
            if (player.InBattleground)
            {
                var bg = player.Battleground;

                if (bg)
                    bg.EventPlayerDroppedFlag(player);
            }
            else
            {
                Global.OutdoorPvPMgr.HandleDropFlag(player, SpellInfo.Id);
            }
        }
    }

    [AuraEffectHandler(AuraType.StateImmunity)]
    private void HandleAuraModStateImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
    }

    [AuraEffectHandler(AuraType.SchoolImmunity)]
    private void HandleAuraModSchoolImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

        if (SpellInfo.Mechanic == Mechanics.Banish)
        {
            if (apply)
            {
                target.AddUnitState(UnitState.Isolated);
            }
            else
            {
                var banishFound = false;
                var banishAuras = target.GetAuraEffectsByType(AuraType);

                foreach (var aurEff in banishAuras)
                    if (aurEff.SpellInfo.Mechanic == Mechanics.Banish)
                    {
                        banishFound = true;

                        break;
                    }

                if (!banishFound)
                    target.ClearUnitState(UnitState.Isolated);
            }
        }

        // TODO: should be changed to a proc script on flag spell (they have "Taken positive" proc flags in db2)
        {
            if (apply && MiscValue == (int)SpellSchoolMask.Normal)
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

            // remove all flag auras (they are positive, but they must be removed when you are immune)
            if (SpellInfo.HasAttribute(SpellAttr1.ImmunityPurgesEffect) && SpellInfo.HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);
        }

        if (apply)
        {
            target.SetUnitFlag(UnitFlags.Immune);
            target.GetThreatManager().EvaluateSuppressed();
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit
            if (target.HasAuraType(AuraType) || target.HasAuraType(AuraType.DamageImmunity))
                return;

            target.RemoveUnitFlag(UnitFlags.Immune);
        }
    }

    [AuraEffectHandler(AuraType.DamageImmunity)]
    private void HandleAuraModDmgImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

        if (apply)
        {
            target.SetUnitFlag(UnitFlags.Immune);
            target.GetThreatManager().EvaluateSuppressed();
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit
            if (target.HasAuraType(AuraType) || target.HasAuraType(AuraType.SchoolImmunity))
                return;

            target.RemoveUnitFlag(UnitFlags.Immune);
        }
    }

    [AuraEffectHandler(AuraType.DispelImmunity)]
    private void HandleAuraModDispelImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
    }

    /*********************************************************/
    /***                  MODIFY STATS                     ***/
    /*********************************************************/

    /********************************/
    /***        RESISTANCE        ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModResistance)]
    private void HandleAuraModResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        for (var x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
            if (Convert.ToBoolean(MiscValue & (1 << x)))
                target.HandleStatFlatModifier(UnitMods.ResistanceStart + x, UnitModifierFlatType.Total, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModBaseResistancePct)]
    private void HandleAuraModBaseResistancePCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        // only players have base stats
        if (!target.IsTypeId(TypeId.Player))
        {
            //pets only have base armor
            if (target.IsPet && Convert.ToBoolean(MiscValue & (int)SpellSchoolMask.Normal))
            {
                if (apply)
                {
                    target.ApplyStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, Amount);
                }
                else
                {
                    var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, (uint)SpellSchoolMask.Normal);
                    target.SetStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, amount);
                }
            }
        }
        else
        {
            for (var x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                if (Convert.ToBoolean(MiscValue & (1 << x)))
                {
                    if (apply)
                    {
                        target.ApplyStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, Amount);
                    }
                    else
                    {
                        var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, 1u << x);
                        target.SetStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, amount);
                    }
                }
        }
    }

    [AuraEffectHandler(AuraType.ModResistancePct)]
    private void HandleModResistancePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        for (var i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
            if (Convert.ToBoolean(MiscValue & (1 << i)))
            {
                var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModResistancePct, 1u << i);

                if (target.GetPctModifierValue(UnitMods.ResistanceStart + i, UnitModifierPctType.Total) == amount)
                    continue;

                target.SetStatPctModifier(UnitMods.ResistanceStart + i, UnitModifierPctType.Total, amount);
            }
    }

    [AuraEffectHandler(AuraType.ModBaseResistance)]
    private void HandleModBaseResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        // only players have base stats
        if (!target.IsTypeId(TypeId.Player))
        {
            //only pets have base stats
            if (target.IsPet && Convert.ToBoolean(MiscValue & (int)SpellSchoolMask.Normal))
                target.HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, Amount, apply);
        }
        else
        {
            for (var i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
                if (Convert.ToBoolean(MiscValue & (1 << i)))
                    target.HandleStatFlatModifier(UnitMods.ResistanceStart + i, UnitModifierFlatType.Total, Amount, apply);
        }
    }

    [AuraEffectHandler(AuraType.ModTargetResistance)]
    private void HandleModTargetResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        // applied to damage as HandleNoImmediateEffect in Unit.CalcAbsorbResist and Unit.CalcArmorReducedDamage

        // show armor penetration
        if (target.IsTypeId(TypeId.Player) && Convert.ToBoolean(MiscValue & (int)SpellSchoolMask.Normal))
            target.ApplyModTargetPhysicalResistance(AmountAsInt, apply);

        // show as spell penetration only full spell penetration bonuses (all resistances except armor and holy
        if (target.IsTypeId(TypeId.Player) && ((SpellSchoolMask)MiscValue & SpellSchoolMask.Spell) == SpellSchoolMask.Spell)
            target.ApplyModTargetResistance(AmountAsInt, apply);
    }

    /********************************/
    /***           STAT           ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModStat)]
    private void HandleAuraModStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        if (MiscValue < -2 || MiscValue > 4)
        {
            Log.Logger.Error("WARNING: Spell {0} effect {1} has an unsupported misc value ({2}) for SPELL_AURA_MOD_STAT ", Id, EffIndex, MiscValue);

            return;
        }

        var target = aurApp.Target;
        var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModStat, true, MiscValue);

        if (Math.Abs(spellGroupVal) >= Math.Abs(Amount))
            return;

        for (var i = Stats.Strength; i < Stats.Max; i++)
            // -1 or -2 is all stats (misc < -2 checked in function beginning)
            if (MiscValue < 0 || MiscValue == (int)i)
            {
                if (spellGroupVal != 0)
                {
                    target.HandleStatFlatModifier((UnitMods.StatStart + (int)i), UnitModifierFlatType.Total, (double)spellGroupVal, !apply);

                    if (target.IsTypeId(TypeId.Player) || target.IsPet)
                        target.UpdateStatBuffMod(i);
                }

                target.HandleStatFlatModifier(UnitMods.StatStart + (int)i, UnitModifierFlatType.Total, Amount, apply);

                if (target.IsTypeId(TypeId.Player) || target.IsPet)
                    target.UpdateStatBuffMod(i);
            }
    }

    [AuraEffectHandler(AuraType.ModPercentStat)]
    private void HandleModPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (MiscValue < -1 || MiscValue > 4)
        {
            Log.Logger.Error("WARNING: Misc Value for SPELL_AURA_MOD_PERCENT_STAT not valid");

            return;
        }

        // only players have base stats
        if (!target.IsTypeId(TypeId.Player))
            return;

        for (var i = (int)Stats.Strength; i < (int)Stats.Max; ++i)
            if (MiscValue == i || MiscValue == -1)
            {
                if (apply)
                {
                    target.ApplyStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, Amount);
                }
                else
                {
                    var amount = target.GetTotalAuraMultiplier(AuraType.ModPercentStat,
                                                               aurEff =>
                                                               {
                                                                   if (aurEff.MiscValue == i || aurEff.MiscValue == -1)
                                                                       return true;

                                                                   return false;
                                                               });

                    target.SetStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, amount);
                }
            }
    }

    [AuraEffectHandler(AuraType.ModSpellDamageOfStatPercent)]
    private void HandleModSpellDamagePercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        // Magic damage modifiers implemented in Unit.SpellDamageBonus
        // This information for client side use only
        // Recalculate bonus
        target.
            // Magic damage modifiers implemented in Unit.SpellDamageBonus
            // This information for client side use only
            // Recalculate bonus
            AsPlayer.UpdateSpellDamageAndHealingBonus();
    }

    [AuraEffectHandler(AuraType.ModSpellHealingOfStatPercent)]
    private void HandleModSpellHealingPercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        // Recalculate bonus
        target.
            // Recalculate bonus
            AsPlayer.UpdateSpellDamageAndHealingBonus();
    }

    [AuraEffectHandler(AuraType.ModHealingDone)]
    private void HandleModHealingDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        // implemented in Unit.SpellHealingBonus
        // this information is for client side only
        target.
            // implemented in Unit.SpellHealingBonus
            // this information is for client side only
            AsPlayer.UpdateSpellDamageAndHealingBonus();
    }

    [AuraEffectHandler(AuraType.ModHealingDonePercent)]
    private void HandleModHealingDonePct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var player = aurApp.Target.AsPlayer;

        if (player)
            player.UpdateHealingDonePercentMod();
    }

    [AuraEffectHandler(AuraType.ModTotalStatPercentage)]
    private void HandleModTotalPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        // save current health state
        double healthPct = target.HealthPct;
        var zeroHealth = !target.IsAlive;

        // players in corpse state may mean two different states:
        /// 1. player just died but did not release (in this case health == 0)
        /// 2. player is corpse running (ie ghost) (in this case health == 1)
        if (target.DeathState == DeathState.Corpse)
            zeroHealth = target.Health == 0;

        for (var i = (int)Stats.Strength; i < (int)Stats.Max; i++)
            if (Convert.ToBoolean(MiscValueB & 1 << i) || MiscValueB == 0) // 0 is also used for all stats
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModTotalStatPercentage,
                                                           aurEff =>
                                                           {
                                                               if ((aurEff.MiscValueB & 1 << i) != 0 || aurEff.MiscValueB == 0)
                                                                   return true;

                                                               return false;
                                                           });

                if (target.GetPctModifierValue(UnitMods.StatStart + i, UnitModifierPctType.Total) == amount)
                    continue;

                target.SetStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Total, amount);

                if (target.IsTypeId(TypeId.Player) || target.IsPet)
                    target.UpdateStatBuffMod((Stats)i);
            }

        // recalculate current HP/MP after applying aura modifications (only for spells with SPELL_ATTR0_ABILITY 0x00000010 flag)
        // this check is total bullshit i think
        if ((Convert.ToBoolean(MiscValueB & 1 << (int)Stats.Stamina) || MiscValueB == 0) && _spellInfo.HasAttribute(SpellAttr0.IsAbility))
            target.SetHealth(Math.Max(MathFunctions.CalculatePct(target.MaxHealth, healthPct), (zeroHealth ? 0 : 1L)));
    }

    [AuraEffectHandler(AuraType.ModExpertise)]
    private void HandleAuraModExpertise(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        target.AsPlayer.UpdateExpertise(WeaponAttackType.BaseAttack);
        target.AsPlayer.UpdateExpertise(WeaponAttackType.OffAttack);
    }

    // Increase armor by <AuraEffect.BasePoints> % of your <primary stat>
    [AuraEffectHandler(AuraType.ModArmorPctFromStat)]
    private void HandleModArmorPctFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        // only players have primary stats
        var player = aurApp.Target.AsPlayer;

        if (!player)
            return;

        player.UpdateArmor();
    }

    [AuraEffectHandler(AuraType.ModBonusArmor)]
    private void HandleModBonusArmor(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        aurApp.Target.HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModBonusArmorPct)]
    private void HandleModBonusArmorPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        aurApp.Target.UpdateArmor();
    }

    [AuraEffectHandler(AuraType.ModStatBonusPct)]
    private void HandleModStatBonusPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        if (MiscValue < -1 || MiscValue > 4)
        {
            Log.Logger.Error("WARNING: Misc Value for SPELL_AURA_MOD_STAT_BONUS_PCT not valid");

            return;
        }

        // only players have base stats
        if (!target.IsTypeId(TypeId.Player))
            return;

        for (var stat = Stats.Strength; stat < Stats.Max; ++stat)
            if (MiscValue == (int)stat || MiscValue == -1)
            {
                target.HandleStatFlatModifier(UnitMods.StatStart + (int)stat, UnitModifierFlatType.BasePCTExcludeCreate, Amount, apply);
                target.UpdateStatBuffMod(stat);
            }
    }

    [AuraEffectHandler(AuraType.OverrideSpellPowerByApPct)]
    private void HandleOverrideSpellPowerByAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target.AsPlayer;

        if (!target)
            return;

        target.ApplyModOverrideSpellPowerByAPPercent(AmountAsFloat, apply);
        target.UpdateSpellDamageAndHealingBonus();
    }

    [AuraEffectHandler(AuraType.OverrideAttackPowerBySpPct)]
    private void HandleOverrideAttackPowerBySpellPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target.AsPlayer;

        if (!target)
            return;

        target.ApplyModOverrideAPBySpellPowerPercent(AmountAsFloat, apply);
        target.UpdateAttackPowerAndDamage();
        target.UpdateAttackPowerAndDamage(true);
    }

    [AuraEffectHandler(AuraType.ModVersatility)]
    private void HandleModVersatilityByPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target)
        {
            target.SetVersatilityBonus((float)target.GetTotalAuraModifier(AuraType.ModVersatility));
            target.UpdateHealingDonePercentMod();
            target.UpdateVersatilityDamageDone();
        }
    }

    [AuraEffectHandler(AuraType.ModMaxPower)]
    private void HandleAuraModMaxPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        var power = (PowerType)MiscValue;
        var unitMod = (UnitMods)(UnitMods.PowerStart + (int)power);

        target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, Amount, apply);
    }

    /********************************/
    /***      HEAL & ENERGIZE     ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModPowerRegen)]
    private void HandleModPowerRegen(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        // Update manaregen value
        if (MiscValue == (int)PowerType.Mana)
            target.AsPlayer.UpdateManaRegen();
        else if (MiscValue == (int)PowerType.Runes)
            target.AsPlayer.UpdateAllRunesRegen();
        // other powers are not immediate effects - implemented in Player.Regenerate, Creature.Regenerate
    }

    [AuraEffectHandler(AuraType.ModPowerRegenPercent)]
    private void HandleModPowerRegenPCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        HandleModPowerRegen(aurApp, mode, apply);
    }

    [AuraEffectHandler(AuraType.ModManaRegenPct)]
    private void HandleModManaRegenPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        if (!target.IsPlayer)
            return;

        target.AsPlayer.UpdateManaRegen();
    }

    [AuraEffectHandler(AuraType.ModIncreaseHealth)]
    [AuraEffectHandler(AuraType.ModIncreaseHealth2)]
    [AuraEffectHandler(AuraType.ModMaxHealth)]
    private void HandleAuraModIncreaseHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        var amt = apply ? AmountAsLong : -AmountAsLong;

        if (amt < 0)
            target.ModifyHealth(Math.Max(1L - target.Health, amt));

        target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, Amount, apply);

        if (amt > 0)
            target.ModifyHealth(amt);
    }

    private void HandleAuraModIncreaseMaxHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        double percent = target.HealthPct;

        target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, Amount, apply);

        // refresh percentage
        if (target.Health > 0)
        {
            var newHealth = Math.Max(target.CountPctFromMaxHealth(percent), 1);
            target.SetHealth(newHealth);
        }
    }

    [AuraEffectHandler(AuraType.ModIncreaseEnergy)]
    private void HandleAuraModIncreaseEnergy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;
        var powerType = (PowerType)MiscValue;

        var unitMod = (UnitMods.PowerStart + (int)powerType);
        target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModIncreaseEnergyPercent)]
    private void HandleAuraModIncreaseEnergyPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;
        var powerType = (PowerType)MiscValue;

        var unitMod = UnitMods.PowerStart + (int)powerType;

        // Save old powers for further calculation
        var oldPower = target.GetPower(powerType);
        var oldMaxPower = target.GetMaxPower(powerType);

        // Handle aura effect for max power
        if (apply)
        {
            target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent,
                                                       aurEff =>
                                                       {
                                                           if (aurEff.MiscValue == (int)powerType)
                                                               return true;

                                                           return false;
                                                       });

            amount *= target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct,
                                                    aurEff =>
                                                    {
                                                        if (aurEff.MiscValue == (int)powerType)
                                                            return true;

                                                        return false;
                                                    });

            target.SetStatPctModifier(unitMod, UnitModifierPctType.Total, amount);
        }

        // Calculate the current power change
        var change = target.GetMaxPower(powerType) - oldMaxPower;
        change = (oldPower + change) - target.GetPower(powerType);
    }

    [AuraEffectHandler(AuraType.ModIncreaseHealthPercent)]
    private void HandleAuraModIncreaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        // Unit will keep hp% after MaxHealth being modified if unit is alive.
        double percent = target.HealthPct;

        if (apply)
        {
            target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent) * target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent2);
            target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, amount);
        }

        if (target.Health > 0)
        {
            var newHealth = Math.Max(MathFunctions.CalculatePct(target.MaxHealth, percent), 1);
            target.SetHealth(newHealth);
        }
    }

    [AuraEffectHandler(AuraType.ModBaseHealthPct)]
    private void HandleAuraIncreaseBaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModBaseHealthPct);
            target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, amount);
        }
    }

    [AuraEffectHandler(AuraType.ModBaseManaPct)]
    private void HandleAuraModIncreaseBaseManaPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.ApplyStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModBaseManaPct);
            target.SetStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, amount);
        }
    }

    [AuraEffectHandler(AuraType.ModManaCostPct)]
    private void HandleModManaCostPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        aurApp.Target.ApplyModManaCostMultiplier(AmountAsFloat / 100.0f, apply);
    }

    [AuraEffectHandler(AuraType.ModPowerDisplay)]
    private void HandleAuraModPowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.RealOrReapplyMask))
            return;

        if (MiscValue >= (int)PowerType.Max)
            return;

        if (apply)
            aurApp.Target.RemoveAurasByType(AuraType, ObjectGuid.Empty, Base);

        aurApp.Target.UpdateDisplayPower();
    }

    [AuraEffectHandler(AuraType.ModOverridePowerDisplay)]
    private void HandleAuraModOverridePowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var powerDisplay = CliDB.PowerDisplayStorage.LookupByKey(MiscValue);

        if (powerDisplay == null)
            return;

        var target = aurApp.Target;

        if (target.GetPowerIndex((PowerType)powerDisplay.ActualType) == (int)PowerType.Max)
            return;

        if (apply)
        {
            target.RemoveAurasByType(AuraType, ObjectGuid.Empty, Base);
            target.SetOverrideDisplayPowerId(powerDisplay.Id);
        }
        else
        {
            target.SetOverrideDisplayPowerId(0);
        }
    }

    [AuraEffectHandler(AuraType.ModMaxPowerPct)]
    private void HandleAuraModMaxPowerPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        if (!target.IsPlayer)
            return;

        var powerType = (PowerType)MiscValue;
        var unitMod = UnitMods.PowerStart + (int)powerType;

        // Save old powers for further calculation
        var oldPower = target.GetPower(powerType);
        var oldMaxPower = target.GetMaxPower(powerType);

        // Handle aura effect for max power
        if (apply)
        {
            target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct,
                                                       aurEff =>
                                                       {
                                                           if (aurEff.MiscValue == (int)powerType)
                                                               return true;

                                                           return false;
                                                       });

            amount *= target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent,
                                                    aurEff =>
                                                    {
                                                        if (aurEff.MiscValue == (int)powerType)
                                                            return true;

                                                        return false;
                                                    });

            target.SetStatPctModifier(unitMod, UnitModifierPctType.Total, amount);
        }

        // Calculate the current power change
        var change = target.GetMaxPower(powerType) - oldMaxPower;
        change = (oldPower + change) - target.GetPower(powerType);
        target.ModifyPower(powerType, change);
    }

    [AuraEffectHandler(AuraType.TriggerSpellOnHealthPct)]
    private void HandleTriggerSpellOnHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasFlag(AuraEffectHandleModes.Real) || !apply)
            return;

        var target = aurApp.Target;
        var thresholdPct = Amount;
        var triggerSpell = GetSpellEffectInfo().TriggerSpell;

        switch ((AuraTriggerOnHealthChangeDirection)MiscValue)
        {
            case AuraTriggerOnHealthChangeDirection.Above:
                if (!target.HealthAbovePct(thresholdPct))
                    return;

                break;
            case AuraTriggerOnHealthChangeDirection.Below:
                if (!target.HealthBelowPct(thresholdPct))
                    return;

                break;
            default:
                break;
        }

        target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
    }

    /********************************/
    /***          FIGHT           ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModParryPercent)]
    private void HandleAuraModParryPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        target.AsPlayer.UpdateParryPercentage();
    }

    [AuraEffectHandler(AuraType.ModDodgePercent)]
    private void HandleAuraModDodgePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        target.AsPlayer.UpdateDodgePercentage();
    }

    [AuraEffectHandler(AuraType.ModBlockPercent)]
    private void HandleAuraModBlockPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        target.AsPlayer.UpdateBlockPercentage();
    }

    [AuraEffectHandler(AuraType.InterruptRegen)]
    private void HandleAuraModRegenInterrupt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        if (!target.IsPlayer)
            return;

        target.AsPlayer.UpdateManaRegen();
    }

    [AuraEffectHandler(AuraType.ModWeaponCritPercent)]
    private void HandleAuraModWeaponCritPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target.AsPlayer;

        if (!target)
            return;

        target.UpdateAllWeaponDependentCritAuras();
    }

    [AuraEffectHandler(AuraType.ModSpellHitChance)]
    private void HandleModSpellHitChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (target.IsTypeId(TypeId.Player))
            target.AsPlayer.UpdateSpellHitChances();
        else
            target.ModSpellHitChance += (apply) ? Amount : (-Amount);
    }

    [AuraEffectHandler(AuraType.ModSpellCritChance)]
    private void HandleModSpellCritChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (target.IsTypeId(TypeId.Player))
            target.AsPlayer.UpdateSpellCritChance();
        else
            target.BaseSpellCritChance += (apply) ? Amount : -Amount;
    }

    [AuraEffectHandler(AuraType.ModCritPct)]
    private void HandleAuraModCritPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
        {
            target.BaseSpellCritChance += (apply) ? Amount : -Amount;

            return;
        }

        target.AsPlayer.UpdateAllWeaponDependentCritAuras();

        // included in Player.UpdateSpellCritChance calculation
        target.
            // included in Player.UpdateSpellCritChance calculation
            AsPlayer.UpdateSpellCritChance();
    }

    /********************************/
    /***         ATTACK SPEED     ***/
    /********************************/
    [AuraEffectHandler(AuraType.HasteSpells)]
    [AuraEffectHandler(AuraType.ModCastingSpeedNotStack)]
    private void HandleModCastingSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        // Do not apply such auras in normal way
        if (Amount >= 1000)
        {
            if (apply)
            {
                target.SetInstantCast(true);
            }
            else
            {
                // only SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK can have this high amount
                // it's some rare case that you have 2 auras like that, but just in case ;)

                var remove = true;
                var castingSpeedNotStack = target.GetAuraEffectsByType(AuraType.ModCastingSpeedNotStack);

                foreach (var aurEff in castingSpeedNotStack)
                    if (aurEff != this && aurEff.Amount >= 1000)
                    {
                        remove = false;

                        break;
                    }

                if (remove)
                    target.SetInstantCast(false);
            }

            return;
        }

        var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType);

        if (Math.Abs(spellGroupVal) >= Math.Abs(Amount))
            return;

        if (spellGroupVal != 0)
            target.ApplyCastTimePercentMod(spellGroupVal, !apply);

        target.ApplyCastTimePercentMod(Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModMeleeRangedHaste)]
    [AuraEffectHandler(AuraType.ModMeleeRangedHaste2)]
    private void HandleModMeleeRangedSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        //! ToDo: Haste auras with the same handler _CAN'T_ stack together
        var target = aurApp.Target;

        target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, Amount, apply);
        target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, Amount, apply);
        target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, Amount, apply);
    }

    [AuraEffectHandler(AuraType.MeleeSlow)]
    [AuraEffectHandler(AuraType.ModSpeedSlowAll)]
    private void HandleModCombatSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;
        var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.MeleeSlow);

        if (Math.Abs(spellGroupVal) >= Math.Abs(Amount))
            return;

        if (spellGroupVal != 0)
        {
            target.ApplyCastTimePercentMod(spellGroupVal, !apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, spellGroupVal, !apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, spellGroupVal, !apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, spellGroupVal, !apply);
        }

        target.ApplyCastTimePercentMod(Amount, apply);
        target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, Amount, apply);
        target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, Amount, apply);
        target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModAttackspeed)]
    private void HandleModAttackSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, Amount, apply);
        target.UpdateDamagePhysical(WeaponAttackType.BaseAttack);
    }

    [AuraEffectHandler(AuraType.ModMeleeHaste)]
    [AuraEffectHandler(AuraType.ModMeleeHaste2)]
    [AuraEffectHandler(AuraType.ModMeleeHaste3)]
    private void HandleModMeleeSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        //! ToDo: Haste auras with the same handler _CAN'T_ stack together
        var target = aurApp.Target;
        var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModMeleeHaste);

        if (Math.Abs(spellGroupVal) >= Math.Abs(Amount))
            return;

        if (spellGroupVal != 0)
        {
            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, spellGroupVal, !apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, spellGroupVal, !apply);
        }

        target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, Amount, apply);
        target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModRangedHaste)]
    private void HandleAuraModRangedHaste(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        //! ToDo: Haste auras with the same handler _CAN'T_ stack together
        var target = aurApp.Target;

        target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, Amount, apply);
    }

    /********************************/
    /***       COMBAT RATING      ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModRating)]
    private void HandleModRating(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        for (var rating = 0; rating < (int)CombatRating.Max; ++rating)
            if (Convert.ToBoolean(MiscValue & (1 << rating)))
                target.AsPlayer.ApplyRatingMod((CombatRating)rating, AmountAsInt, apply);
    }

    [AuraEffectHandler(AuraType.ModRatingPct)]
    private void HandleModRatingPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        // Just recalculate ratings
        for (var rating = 0; rating < (int)CombatRating.Max; ++rating)
            if (Convert.ToBoolean(MiscValue & (1 << rating)))
                target.AsPlayer.UpdateRating((CombatRating)rating);
    }

    /********************************/
    /***        ATTACK POWER      ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModAttackPower)]
    private void HandleAuraModAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        target.HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModRangedAttackPower)]
    private void HandleAuraModRangedAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if ((target.ClassMask & (uint)PlayerClass.ClassMaskWandUsers) != 0)
            return;

        target.HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModAttackPowerPct)]
    private void HandleAuraModAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        //UNIT_FIELD_ATTACK_POWER_MULTIPLIER = multiplier - 1
        if (apply)
        {
            target.ApplyStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModAttackPowerPct);
            target.SetStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, amount);
        }
    }

    [AuraEffectHandler(AuraType.ModRangedAttackPowerPct)]
    private void HandleAuraModRangedAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if ((target.ClassMask & (uint)PlayerClass.ClassMaskWandUsers) != 0)
            return;

        //UNIT_FIELD_RANGED_ATTACK_POWER_MULTIPLIER = multiplier - 1
        if (apply)
        {
            target.ApplyStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModRangedAttackPowerPct);
            target.SetStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, amount);
        }
    }

    /********************************/
    /***        DAMAGE BONUS      ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModDamageDone)]
    private void HandleModDamageDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        if ((MiscValue & (int)SpellSchoolMask.Normal) != 0)
            target.UpdateAllDamageDoneMods();

        // Magic damage modifiers implemented in Unit::SpellBaseDamageBonusDone
        // This information for client side use only
        var playerTarget = target.AsPlayer;

        if (playerTarget != null)
        {
            for (var i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(MiscValue & (1 << i)))
                {
                    if (Amount >= 0)
                        playerTarget.ApplyModDamageDonePos((SpellSchools)i, AmountAsInt, apply);
                    else
                        playerTarget.ApplyModDamageDoneNeg((SpellSchools)i, AmountAsInt, apply);
                }

            var pet = playerTarget.GetGuardianPet();

            if (pet)
                pet.UpdateAttackPowerAndDamage();
        }
    }

    [AuraEffectHandler(AuraType.ModDamagePercentDone)]
    private void HandleModDamagePercentDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        // also handles spell group stacks
        if (Convert.ToBoolean(MiscValue & (int)SpellSchoolMask.Normal))
            target.UpdateAllDamagePctDoneMods();

        var thisPlayer = target.AsPlayer;

        if (thisPlayer != null)
            for (var i = SpellSchools.Normal; i < SpellSchools.Max; ++i)
                if (Convert.ToBoolean(MiscValue & (1 << (int)i)))
                {
                    // only aura type modifying PLAYER_FIELD_MOD_DAMAGE_DONE_PCT
                    var amount = thisPlayer.GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, 1u << (int)i);
                    thisPlayer.SetModDamageDonePercent(i, (float)amount);
                }
    }

    [AuraEffectHandler(AuraType.ModOffhandDamagePct)]
    private void HandleModOffhandDamagePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target;

        // also handles spell group stacks
        target.UpdateDamagePctDoneMods(WeaponAttackType.OffAttack);
    }

    private void HandleShieldBlockValue(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
            return;

        var player = aurApp.Target.AsPlayer;

        if (player != null)
            player.HandleBaseModFlatValue(BaseModGroup.ShieldBlockValue, Amount, apply);
    }

    [AuraEffectHandler(AuraType.ModShieldBlockvaluePct)]
    private void HandleShieldBlockValuePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
            return;

        var target = aurApp.Target.AsPlayer;

        if (!target)
            return;

        if (apply)
        {
            target.ApplyBaseModPctValue(BaseModGroup.ShieldBlockValue, Amount);
        }
        else
        {
            var amount = target.GetTotalAuraMultiplier(AuraType.ModShieldBlockvaluePct);
            target.SetBaseModPctValue(BaseModGroup.ShieldBlockValue, amount);
        }
    }

    /********************************/
    /***        POWER COST        ***/
    /********************************/
    [AuraEffectHandler(AuraType.ModPowerCostSchool)]
    private void HandleModPowerCost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        // handled in SpellInfo::CalcPowerCost, this is only for client UI
        if ((MiscValueB & (1 << (int)PowerType.Mana)) == 0)
            return;

        var target = aurApp.Target;

        for (var i = 0; i < (int)SpellSchools.Max; ++i)
            if (Convert.ToBoolean(MiscValue & (1 << i)))
                target.ApplyModManaCostModifier((SpellSchools)i, AmountAsInt, apply);
    }

    [AuraEffectHandler(AuraType.ArenaPreparation)]
    private void HandleArenaPreparation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetUnitFlag(UnitFlags.Preparation);
        }
        else
        {
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

            target.RemoveUnitFlag(UnitFlags.Preparation);
        }

        target.ModifyAuraState(AuraStateType.ArenaPreparation, apply);
    }

    [AuraEffectHandler(AuraType.NoReagentUse)]
    private void HandleNoReagentUseAura(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player))
            return;

        FlagArray128 mask = new();
        var noReagent = target.GetAuraEffectsByType(AuraType.NoReagentUse);

        foreach (var eff in noReagent)
        {
            var effect = eff.GetSpellEffectInfo();

            if (effect != null)
                mask |= effect.SpellClassMask;
        }

        target.AsPlayer.SetNoRegentCostMask(mask);
    }

    /*********************************************************/
    /***                    OTHERS                         ***/
    /*********************************************************/
    [AuraEffectHandler(AuraType.Dummy)]
    private void HandleAuraDummy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Reapply)))
            return;

        var target = aurApp.Target;

        var caster = Caster;

        // pet auras
        if (target.TypeId == TypeId.Player && mode.HasAnyFlag(AuraEffectHandleModes.Real))
        {
            var petSpell = Global.SpellMgr.GetPetAura(Id, (byte)EffIndex);

            if (petSpell != null)
            {
                if (apply)
                    target.AsPlayer.AddPetAura(petSpell);
                else
                    target.AsPlayer.RemovePetAura(petSpell);
            }
        }

        if (mode.HasAnyFlag(AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply))
        {
            // AT APPLY
            if (apply)
                switch (Id)
                {
                    case 1515: // Tame beast
                        // FIX_ME: this is 2.0.12 threat effect replaced in 2.1.x by dummy aura, must be checked for correctness
                        if (caster != null && target.CanHaveThreatList)
                            target.GetThreatManager().AddThreat(caster, 10.0f);

                        break;
                    case 13139: // net-o-matic
                        // root to self part of (root_target.charge.root_self sequence
                        if (caster != null)
                            caster.CastSpell(caster, 13138, new CastSpellExtraArgs(this));

                        break;
                    case 34026: // kill command
                    {
                        Unit pet = target.GetGuardianPet();

                        if (pet == null)
                            break;

                        target.CastSpell(target, 34027, new CastSpellExtraArgs(this));

                        // set 3 stacks and 3 charges (to make all auras not disappear at once)
                        var owner_aura = target.GetAura(34027, CasterGuid);
                        var pet_aura = pet.GetAura(58914, CasterGuid);

                        if (owner_aura != null)
                        {
                            owner_aura.SetStackAmount((byte)owner_aura.SpellInfo.StackAmount);

                            if (pet_aura != null)
                            {
                                pet_aura.SetCharges(0);
                                pet_aura.SetStackAmount((byte)owner_aura.SpellInfo.StackAmount);
                            }
                        }

                        break;
                    }
                    case 37096: // Blood Elf Illusion
                    {
                        if (caster != null)
                        {
                            if (caster.Gender == Gender.Female)
                                caster.CastSpell(target, 37095, new CastSpellExtraArgs(this)); // Blood Elf Disguise
                            else
                                caster.CastSpell(target, 37093, new CastSpellExtraArgs(this));
                        }

                        break;
                    }
                    case 39850:                          // Rocket Blast
                        if (RandomHelper.randChance(20)) // backfire stun
                            target.CastSpell(target, 51581, new CastSpellExtraArgs(this));

                        break;
                    case 43873: // Headless Horseman Laugh
                        target.PlayDistanceSound(11965);

                        break;
                    case 46354: // Blood Elf Illusion
                        if (caster != null)
                        {
                            if (caster.Gender == Gender.Female)
                                caster.CastSpell(target, 46356, new CastSpellExtraArgs(this));
                            else
                                caster.CastSpell(target, 46355, new CastSpellExtraArgs(this));
                        }

                        break;
                    case 46361: // Reinforced Net
                        if (caster != null)
                            target.MotionMaster.MoveFall();

                        break;
                }
            // AT REMOVE
            else
                switch (_spellInfo.SpellFamilyName)
                {
                    case SpellFamilyNames.Generic:
                        switch (Id)
                        {
                            case 2584: // Waiting to Resurrect
                                // Waiting to resurrect spell cancel, we must remove player from resurrect queue
                                if (target.IsTypeId(TypeId.Player))
                                {
                                    var bg = target.AsPlayer.Battleground;

                                    if (bg)
                                        bg.RemovePlayerFromResurrectQueue(target.GUID);

                                    var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(target.Location.Map, target.Location.Zone);

                                    if (bf != null)
                                        bf.RemovePlayerFromResurrectQueue(target.GUID);
                                }

                                break;
                            case 36730: // Flame Strike
                                target.CastSpell(target, 36731, new CastSpellExtraArgs(this));

                                break;
                            case 43681: // Inactive
                            {
                                if (!target.IsTypeId(TypeId.Player) || aurApp.RemoveMode != AuraRemoveMode.Expire)
                                    return;

                                if (target.Location.Map.IsBattleground)
                                    target.AsPlayer.LeaveBattleground();

                                break;
                            }
                            case 42783: // Wrath of the Astromancer
                                target.CastSpell(target, (uint)Amount, new CastSpellExtraArgs(this));

                                break;
                            case 46308: // Burning Winds casted only at creatures at spawn
                                target.CastSpell(target, 47287, new CastSpellExtraArgs(this));

                                break;
                            case 52172: // Coyote Spirit Despawn Aura
                            case 60244: // Blood Parrot Despawn Aura
                                target.CastSpell((Unit)null, (uint)Amount, new CastSpellExtraArgs(this));

                                break;
                            case 91604: // Restricted Flight Area
                                if (aurApp.RemoveMode == AuraRemoveMode.Expire)
                                    target.CastSpell(target, 58601, new CastSpellExtraArgs(this));

                                break;
                        }

                        break;
                    case SpellFamilyNames.Deathknight:
                        // Summon Gargoyle (Dismiss Gargoyle at remove)
                        if (Id == 61777)
                            target.CastSpell(target, (uint)Amount, new CastSpellExtraArgs(this));

                        break;
                    default:
                        break;
                }
        }

        // AT APPLY & REMOVE

        switch (_spellInfo.SpellFamilyName)
        {
            case SpellFamilyNames.Generic:
            {
                if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                    break;

                switch (Id)
                {
                    // Recently Bandaged
                    case 11196:
                        target.ApplySpellImmune(Id, SpellImmunity.Mechanic, (uint)MiscValue, apply);

                        break;
                    // Unstable Power
                    case 24658:
                    {
                        uint spellId = 24659;

                        if (apply && caster != null)
                        {
                            var spell = Global.SpellMgr.GetSpellInfo(spellId, Base.CastDifficulty);

                            CastSpellExtraArgs args = new()
                            {
                                TriggerFlags = TriggerCastFlags.FullMask,
                                OriginalCaster = CasterGuid,
                                OriginalCastId = Base.CastId,
                                CastDifficulty = Base.CastDifficulty
                            };

                            for (uint i = 0; i < spell.StackAmount; ++i)
                                caster.CastSpell(target, spell.Id, args);

                            break;
                        }

                        target.RemoveAura(spellId);

                        break;
                    }
                    // Restless Strength
                    case 24661:
                    {
                        uint spellId = 24662;

                        if (apply && caster != null)
                        {
                            var spell = Global.SpellMgr.GetSpellInfo(spellId, Base.CastDifficulty);

                            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask)
                            {
                                OriginalCaster = CasterGuid,
                                OriginalCastId = Base.CastId,
                                CastDifficulty = Base.CastDifficulty
                            };

                            for (uint i = 0; i < spell.StackAmount; ++i)
                                caster.CastSpell(target, spell.Id, args);

                            break;
                        }

                        target.RemoveAura(spellId);

                        break;
                    }
                    // Tag Murloc
                    case 30877:
                    {
                        // Tag/untag Blacksilt Scout
                        target. // Tag/untag Blacksilt Scout
                                Entry = (uint)(apply ? 17654 : 17326);

                        break;
                    }
                    case 57819: // Argent Champion
                    case 57820: // Ebon Champion
                    case 57821: // Champion of the Kirin Tor
                    case 57822: // Wyrmrest Champion
                    {
                        if (!caster || !caster.IsTypeId(TypeId.Player))
                            break;

                        uint FactionID = 0;

                        if (apply)
                            switch (_spellInfo.Id)
                            {
                                case 57819:
                                    FactionID = 1106; // Argent Crusade

                                    break;
                                case 57820:
                                    FactionID = 1098; // Knights of the Ebon Blade

                                    break;
                                case 57821:
                                    FactionID = 1090; // Kirin Tor

                                    break;
                                case 57822:
                                    FactionID = 1091; // The Wyrmrest Accord

                                    break;
                            }

                        caster.AsPlayer.SetChampioningFaction(FactionID);

                        break;
                    }
                    // LK Intro VO (1)
                    case 58204:
                        if (target.IsTypeId(TypeId.Player))
                        {
                            // Play part 1
                            if (apply)
                                target.PlayDirectSound(14970, target.AsPlayer);
                            // continue in 58205
                            else
                                target.CastSpell(target, 58205, new CastSpellExtraArgs(this));
                        }

                        break;
                    // LK Intro VO (2)
                    case 58205:
                        if (target.IsTypeId(TypeId.Player))
                        {
                            // Play part 2
                            if (apply)
                                target.PlayDirectSound(14971, target.AsPlayer);
                            // Play part 3
                            else
                                target.PlayDirectSound(14972, target.AsPlayer);
                        }

                        break;
                }

                break;
            }
        }
    }

    [AuraEffectHandler(AuraType.ChannelDeathItem)]
    private void HandleChannelDeathItem(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        if (apply || aurApp.RemoveMode != AuraRemoveMode.Death)
            return;

        var caster = Caster;

        if (caster == null || !caster.IsTypeId(TypeId.Player))
            return;

        var plCaster = caster.AsPlayer;
        var target = aurApp.Target;

        // Item amount
        if (Amount <= 0)
            return;

        if (GetSpellEffectInfo().ItemType == 0)
            return;

        // Soul Shard
        if (GetSpellEffectInfo().ItemType == 6265)
            // Soul Shard only from units that grant XP or honor
            if (!plCaster.IsHonorOrXPTarget(target) ||
                (target.IsTypeId(TypeId.Unit) && !target.AsCreature.IsTappedBy(plCaster)))
                return;

        //Adding items
        var count = (uint)Amount;

        List<ItemPosCount> dest = new();
        var msg = plCaster.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, GetSpellEffectInfo().ItemType, count, out var noSpaceForCount);

        if (msg != InventoryResult.Ok)
        {
            count -= noSpaceForCount;
            plCaster.SendEquipError(msg, null, null, GetSpellEffectInfo().ItemType);

            if (count == 0)
                return;
        }

        var newitem = plCaster.StoreNewItem(dest, GetSpellEffectInfo().ItemType, true);

        if (newitem == null)
        {
            plCaster.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        plCaster.SendNewItem(newitem, count, true, true);
    }

    [AuraEffectHandler(AuraType.BindSight)]
    private void HandleBindSight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        var caster = Caster;

        if (caster == null || !caster.IsTypeId(TypeId.Player))
            return;

        caster.AsPlayer.SetViewpoint(target, apply);
    }

    [AuraEffectHandler(AuraType.ForceReaction)]
    private void HandleForceReaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        var player = target.AsPlayer;

        if (player == null)
            return;

        var factionId = (uint)MiscValue;
        var factionRank = (ReputationRank)Amount;

        player.ReputationMgr.ApplyForceReaction(factionId, factionRank, apply);
        player.ReputationMgr.SendForceReactions();

        // stop fighting at apply (if forced rank friendly) or at remove (if real rank friendly)
        if ((apply && factionRank >= ReputationRank.Friendly) || (!apply && player.GetReputationRank(factionId) >= ReputationRank.Friendly))
            player.StopAttackFaction(factionId);
    }

    [AuraEffectHandler(AuraType.Empathy)]
    private void HandleAuraEmpathy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!apply)
            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (target.HasAuraType(AuraType))
                return;

        if (target.CreatureType == CreatureType.Beast)
        {
            if (apply)
                target.SetDynamicFlag(UnitDynFlags.SpecialInfo);
            else
                target.RemoveDynamicFlag(UnitDynFlags.SpecialInfo);
        }
    }

    [AuraEffectHandler(AuraType.ModFaction)]
    private void HandleAuraModFaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.Faction = (uint)MiscValue;

            if (target.IsTypeId(TypeId.Player))
                target.RemoveUnitFlag(UnitFlags.PlayerControlled);
        }
        else
        {
            target.RestoreFaction();

            if (target.IsTypeId(TypeId.Player))
                target.SetUnitFlag(UnitFlags.PlayerControlled);
        }
    }

    [AuraEffectHandler(AuraType.LearnSpell)]
    private void HandleLearnSpell(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var player = aurApp.Target.AsPlayer;

        if (player == null)
            return;

        if (apply)
            player.LearnSpell((uint)MiscValue, true, 0, true);
        else
            player.RemoveSpell((uint)MiscValue, false, false, true);
    }

    [AuraEffectHandler(AuraType.ComprehendLanguage)]
    private void HandleComprehendLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetUnitFlag2(UnitFlags2.ComprehendLang);
        }
        else
        {
            if (target.HasAuraType(AuraType))
                return;

            target.RemoveUnitFlag2(UnitFlags2.ComprehendLang);
        }
    }

    [AuraEffectHandler(AuraType.ModAlternativeDefaultLanguage)]
    private void HandleModAlternativeDefaultLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.SetUnitFlag3(UnitFlags3.AlternativeDefaultLanguage);
        }
        else
        {
            if (target.HasAuraType(AuraType))
                return;

            target.RemoveUnitFlag3(UnitFlags3.AlternativeDefaultLanguage);
        }
    }

    [AuraEffectHandler(AuraType.Linked)]
    private void HandleAuraLinked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        var target = aurApp.Target;

        var triggeredSpellId = GetSpellEffectInfo().TriggerSpell;
        var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggeredSpellId, Base.CastDifficulty);

        if (triggeredSpellInfo == null)
            return;

        var caster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? Caster : target;

        if (!caster)
            return;

        if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
        {
            if (apply)
            {
                CastSpellExtraArgs args = new(this);

                if (Amount != 0) // If amount avalible cast with basepoints (Crypt Fever for example)
                    args.AddSpellMod(SpellValueMod.BasePoint0, Amount);

                caster.CastSpell(target, triggeredSpellId, args);
            }
            else
            {
                var casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? CasterGuid : target.GUID;
                target.RemoveAura(triggeredSpellId, casterGUID);
            }
        }
        else if (mode.HasAnyFlag(AuraEffectHandleModes.Reapply) && apply)
        {
            var casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? CasterGuid : target.GUID;
            // change the stack amount to be equal to stack amount of our aura
            var triggeredAura = target.GetAura(triggeredSpellId, casterGUID);

            if (triggeredAura != null)
                triggeredAura.ModStackAmount(Base.StackAmount - triggeredAura.StackAmount);
        }
    }

    [AuraEffectHandler(AuraType.TriggerSpellOnPowerPct)]
    private void HandleTriggerSpellOnPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real) || !apply)
            return;

        var target = aurApp.Target;

        var effectAmount = Amount;
        var triggerSpell = GetSpellEffectInfo().TriggerSpell;
        double powerAmountPct = MathFunctions.GetPctOf(target.GetPower((PowerType)MiscValue), target.GetMaxPower((PowerType)MiscValue));

        switch ((AuraTriggerOnPowerChangeDirection)MiscValueB)
        {
            case AuraTriggerOnPowerChangeDirection.Gain:
                if (powerAmountPct < effectAmount)
                    return;

                break;
            case AuraTriggerOnPowerChangeDirection.Loss:
                if (powerAmountPct > effectAmount)
                    return;

                break;
            default:
                break;
        }

        target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
    }

    [AuraEffectHandler(AuraType.TriggerSpellOnPowerAmount)]
    private void HandleTriggerSpellOnPowerAmount(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real) || !apply)
            return;

        var target = aurApp.Target;

        var effectAmount = Amount;
        var triggerSpell = GetSpellEffectInfo().TriggerSpell;
        double powerAmount = target.GetPower((PowerType)MiscValue);

        switch ((AuraTriggerOnPowerChangeDirection)MiscValueB)
        {
            case AuraTriggerOnPowerChangeDirection.Gain:
                if (powerAmount < effectAmount)
                    return;

                break;
            case AuraTriggerOnPowerChangeDirection.Loss:
                if (powerAmount > effectAmount)
                    return;

                break;
            default:
                break;
        }

        target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
    }

    [AuraEffectHandler(AuraType.TriggerSpellOnExpire)]
    private void HandleTriggerSpellOnExpire(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasFlag(AuraEffectHandleModes.Real) || apply || aurApp.RemoveMode != AuraRemoveMode.Expire)
            return;

        var caster = aurApp.Target;

        if (MiscValue > 0)
            caster = Caster;

        caster.CastSpell(aurApp.Target, GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(this));
    }

    [AuraEffectHandler(AuraType.OpenStable)]
    private void HandleAuraOpenStable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!target.IsTypeId(TypeId.Player) || !target.Location.IsInWorld)
            return;

        if (apply)
            target.AsPlayer.Session.SendStablePet(target.GUID);

        // client auto close stable dialog at !apply aura
    }

    [AuraEffectHandler(AuraType.ModFakeInebriate)]
    private void HandleAuraModFakeInebriation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            target.Visibility.InvisibilityDetect.AddFlag(InvisibilityType.Drunk);
            target.Visibility.InvisibilityDetect.AddValue(InvisibilityType.Drunk, AmountAsInt);

            var playerTarget = target.AsPlayer;

            if (playerTarget)
                playerTarget.ApplyModFakeInebriation(AmountAsInt, true);
        }
        else
        {
            var removeDetect = !target.HasAuraType(AuraType.ModFakeInebriate);

            target.Visibility.InvisibilityDetect.AddValue(InvisibilityType.Drunk, -AmountAsInt);

            var playerTarget = target.AsPlayer;

            if (playerTarget != null)
            {
                playerTarget.ApplyModFakeInebriation(AmountAsInt, false);

                if (removeDetect)
                    removeDetect = playerTarget.DrunkValue == 0;
            }

            if (removeDetect)
                target.Visibility.InvisibilityDetect.DelFlag(InvisibilityType.Drunk);
        }

        // call functions which may have additional effects after changing state of unit
        if (target.Location.IsInWorld)
            target.UpdateObjectVisibility();
    }

    [AuraEffectHandler(AuraType.OverrideSpells)]
    private void HandleAuraOverrideSpells(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null || !target.Location.IsInWorld)
            return;

        var overrideId = (uint)MiscValue;

        if (apply)
        {
            target.SetOverrideSpellsId(overrideId);
            var overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);

            if (overrideSpells != null)
                for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                {
                    var spellId = overrideSpells.Spells[i];

                    if (spellId != 0)
                        target.AddTemporarySpell(spellId);
                }
        }
        else
        {
            target.SetOverrideSpellsId(0);
            var overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);

            if (overrideSpells != null)
                for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                {
                    var spellId = overrideSpells.Spells[i];

                    if (spellId != 0)
                        target.RemoveTemporarySpell(spellId);
                }
        }
    }

    [AuraEffectHandler(AuraType.SetVehicleId)]
    private void HandleAuraSetVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (!target.Location.IsInWorld)
            return;

        var vehicleId = MiscValue;

        if (apply)
        {
            if (!target.CreateVehicleKit((uint)vehicleId, 0))
                return;
        }
        else if (target.VehicleKit1 != null)
        {
            target.RemoveVehicleKit();
        }

        if (!target.IsTypeId(TypeId.Player))
            return;

        if (apply)
            target.AsPlayer.SendOnCancelExpectedVehicleRideAura();
    }

    [AuraEffectHandler(AuraType.PreventResurrection)]
    private void HandlePreventResurrection(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
            target.RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
        else if (!target.Location.Map.Instanceable)
            target.SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
    }

    [AuraEffectHandler(AuraType.Mastery)]
    private void HandleMastery(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        target.UpdateMastery();
    }

    private void HandlePeriodicTriggerSpellAuraTick(Unit target, Unit caster)
    {
        var triggerSpellId = GetSpellEffectInfo().TriggerSpell;

        if (triggerSpellId == 0)
        {
            Log.Logger.Warning($"AuraEffect::HandlePeriodicTriggerSpellAuraTick: Spell {Id} [EffectIndex: {EffIndex}] does not have triggered spell.");

            return;
        }

        var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, Base.CastDifficulty);

        if (triggeredSpellInfo != null)
        {
            var triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? caster : target;

            if (triggerCaster != null)
            {
                triggerCaster.CastSpell(target, triggerSpellId, new CastSpellExtraArgs(this));
                Log.Logger.Debug("AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} Trigger {1}", Id, triggeredSpellInfo.Id);
            }
        }
        else
        {
            Log.Logger.Error("AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", Id, triggerSpellId, EffIndex);
        }
    }

    private void HandlePeriodicTriggerSpellWithValueAuraTick(Unit target, Unit caster)
    {
        var triggerSpellId = GetSpellEffectInfo().TriggerSpell;

        if (triggerSpellId == 0)
        {
            Log.Logger.Warning($"AuraEffect::HandlePeriodicTriggerSpellWithValueAuraTick: Spell {Id} [EffectIndex: {EffIndex}] does not have triggered spell.");

            return;
        }

        var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, Base.CastDifficulty);

        if (triggeredSpellInfo != null)
        {
            var triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(_spellInfo) ? caster : target;

            if (triggerCaster != null)
            {
                CastSpellExtraArgs args = new(this);

                foreach (var effect in triggeredSpellInfo.Effects)
                    args.AddSpellMod(SpellValueMod.BasePoint0 + effect.EffectIndex, Amount);

                triggerCaster.CastSpell(target, triggerSpellId, args);
                Log.Logger.Debug("AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} Trigger {1}", Id, triggeredSpellInfo.Id);
            }
        }
        else
        {
            Log.Logger.Error("AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", Id, triggerSpellId, EffIndex);
        }
    }

    private void HandlePeriodicDamageAurasTick(Unit target, Unit caster)
    {
        if (!target.IsAlive)
            return;

        if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(SpellInfo))
        {
            SendTickImmune(target, caster);

            return;
        }

        // Consecrate ticks can miss and will not show up in the combat log
        // dynobj auras must always have a caster
        if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
            caster.WorldObjectCombat.SpellHitResult(target, SpellInfo, false) != SpellMissInfo.None)
            return;

        CleanDamage cleanDamage = new(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

        var stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? Base.StackAmount : 1u;

        // ignore non positive values (can be result apply spellmods to aura damage
        var damage = Math.Max(Amount, 0);

        // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
        Global.ScriptMgr.ForEach<IUnitModifyPeriodicDamageAurasTick>(p => p.ModifyPeriodicDamageAurasTick(target, caster, ref damage));

        switch (AuraType)
        {
            case AuraType.PeriodicDamage:
            {
                if (caster != null)
                    damage = caster.SpellDamageBonusDone(target, SpellInfo, damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

                damage = target.SpellDamageBonusTaken(caster, SpellInfo, damage, DamageEffectType.DOT);

                // There is a Chance to make a Soul Shard when Drain soul does damage
                if (caster != null && SpellInfo.SpellFamilyName == SpellFamilyNames.Warlock && SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
                {
                    if (caster.IsTypeId(TypeId.Player) && caster.AsPlayer.IsHonorOrXPTarget(target))
                        caster.CastSpell(caster, 95810, new CastSpellExtraArgs(this));
                }
                else if (SpellInfo.SpellFamilyName == SpellFamilyNames.Generic)
                {
                    switch (Id)
                    {
                        case 70911: // Unbound Plague
                        case 72854: // Unbound Plague
                        case 72855: // Unbound Plague
                        case 72856: // Unbound Plague
                            damage *= Math.Pow(1.25f, _ticksDone);

                            break;
                        default:
                            break;
                    }
                }

                break;
            }
            case AuraType.PeriodicWeaponPercentDamage:
            {
                var attackType = SpellInfo.GetAttackType();

                damage = MathFunctions.CalculatePct(caster.CalculateDamage(attackType, false, true), Amount);

                // Add melee damage bonuses (also check for negative)
                if (caster != null)
                    damage = caster.MeleeDamageBonusDone(target, damage, attackType, DamageEffectType.DOT, SpellInfo);

                damage = target.MeleeDamageBonusTaken(caster, damage, attackType, DamageEffectType.DOT, SpellInfo);

                break;
            }
            case AuraType.PeriodicDamagePercent:
                // ceil obtained value, it may happen that 10 ticks for 10% damage may not kill owner
                damage = Math.Ceiling(MathFunctions.CalculatePct((double)target.MaxHealth, damage));
                damage = target.SpellDamageBonusTaken(caster, SpellInfo, damage, DamageEffectType.DOT);

                break;
            default:
                break;
        }

        var crit = RandomHelper.randChance(GetCritChanceFor(caster, target));

        if (crit)
            damage = Unit.SpellCriticalDamageBonus(caster, _spellInfo, damage, target);

        // Calculate armor mitigation
        if (Unit.IsDamageReducedByArmor(SpellInfo.GetSchoolMask(), SpellInfo))
        {
            var damageReducedArmor = Unit.CalcArmorReducedDamage(caster, target, damage, SpellInfo, SpellInfo.GetAttackType(), Base.CasterLevel);
            cleanDamage.MitigatedDamage += damage - damageReducedArmor;
            damage = damageReducedArmor;
        }

        if (!SpellInfo.HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
            if (GetSpellEffectInfo().IsTargetingArea || GetSpellEffectInfo().IsAreaAuraEffect || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) || SpellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
                damage = target.CalculateAOEAvoidance(damage, (uint)_spellInfo.SchoolMask, Base.CastItemGuid);

        var dmg = damage;

        if (!SpellInfo.HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers) && caster != null && caster.CanApplyResilience())
            Unit.ApplyResilience(target, ref dmg);

        damage = dmg;

        DamageInfo damageInfo = new(caster, target, damage, SpellInfo, SpellInfo.GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
        Unit.CalcAbsorbResist(damageInfo);
        damage = damageInfo.Damage;

        var absorb = damageInfo.Absorb;
        var resist = damageInfo.Resist;
        Unit.DealDamageMods(caster, target, ref damage, ref absorb);

        // Set trigger flag
        var procAttacker = new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic);
        var procVictim = new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic);
        var hitMask = damageInfo.HitMask;

        if (damage != 0)
        {
            hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
            procVictim.Or(ProcFlags.TakeAnyDamage);
        }

        var overkill = damage - target.Health;

        if (overkill < 0)
            overkill = 0;

        SpellPeriodicAuraLogInfo pInfo = new(this, damage, dmg, overkill, absorb, resist, 0.0f, crit);

        Unit.DealDamage(caster, target, damage, cleanDamage, DamageEffectType.DOT, SpellInfo.GetSchoolMask(), SpellInfo, true);

        Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, null, damageInfo, null);
        target.SendPeriodicAuraLog(pInfo);
    }

    private void HandlePeriodicHealthLeechAuraTick(Unit target, Unit caster)
    {
        if (!target.IsAlive)
            return;

        if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(SpellInfo))
        {
            SendTickImmune(target, caster);

            return;
        }

        // dynobj auras must always have a caster
        if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
            caster.WorldObjectCombat.SpellHitResult(target, SpellInfo, false) != SpellMissInfo.None)
            return;

        CleanDamage cleanDamage = new(0, 0, SpellInfo.GetAttackType(), MeleeHitOutcome.Normal);

        var stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? Base.StackAmount : 1u;

        // ignore negative values (can be result apply spellmods to aura damage
        var damage = Math.Max(Amount, 0);

        if (caster)
            damage = caster.SpellDamageBonusDone(target, SpellInfo, damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

        damage = target.SpellDamageBonusTaken(caster, SpellInfo, damage, DamageEffectType.DOT);

        var crit = RandomHelper.randChance(GetCritChanceFor(caster, target));

        if (crit)
            damage = Unit.SpellCriticalDamageBonus(caster, _spellInfo, damage, target);

        // Calculate armor mitigation
        if (Unit.IsDamageReducedByArmor(SpellInfo.GetSchoolMask(), SpellInfo))
        {
            var damageReducedArmor = Unit.CalcArmorReducedDamage(caster, target, damage, SpellInfo, SpellInfo.GetAttackType(), Base.CasterLevel);
            cleanDamage.MitigatedDamage += damage - damageReducedArmor;
            damage = damageReducedArmor;
        }

        if (!SpellInfo.HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
            if (GetSpellEffectInfo().IsTargetingArea || GetSpellEffectInfo().IsAreaAuraEffect || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) || SpellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
                damage = target.CalculateAOEAvoidance(damage, (uint)_spellInfo.SchoolMask, Base.CastItemGuid);

        var dmg = damage;

        if (!SpellInfo.HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers) && caster != null && caster.CanApplyResilience())
            Unit.ApplyResilience(target, ref dmg);

        damage = dmg;

        DamageInfo damageInfo = new(caster, target, damage, SpellInfo, SpellInfo.GetSchoolMask(), DamageEffectType.DOT, SpellInfo.GetAttackType());
        Unit.CalcAbsorbResist(damageInfo);

        var absorb = damageInfo.Absorb;
        var resist = damageInfo.Resist;

        // SendSpellNonMeleeDamageLog expects non-absorbed/non-resisted damage
        SpellNonMeleeDamage log = new(caster, target, SpellInfo, Base.SpellVisual, SpellInfo.GetSchoolMask(), Base.CastId)
        {
            Damage = damage,
            OriginalDamage = dmg,
            Absorb = absorb,
            Resist = resist,
            PeriodicLog = true
        };

        if (crit)
            log.HitInfo |= (int)SpellHitType.Crit;

        // Set trigger flag
        var procAttacker = new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic);
        var procVictim = new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic);
        var hitMask = damageInfo.HitMask;

        if (damage != 0)
        {
            hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
            procVictim.Or(ProcFlags.TakeAnyDamage);
        }

        var new_damage = Unit.DealDamage(caster, target, damage, cleanDamage, DamageEffectType.DOT, SpellInfo.GetSchoolMask(), SpellInfo, false);
        Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, null, damageInfo, null);

        // process caster heal from now on (must be in world)
        if (!caster || !caster.IsAlive)
            return;

        var gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

        var heal = caster.SpellHealingBonusDone(caster, SpellInfo, (new_damage * gainMultiplier), DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);
        heal = caster.SpellHealingBonusTaken(caster, SpellInfo, heal, DamageEffectType.DOT);

        HealInfo healInfo = new(caster, caster, heal, SpellInfo, SpellInfo.GetSchoolMask());
        caster.HealBySpell(healInfo);

        caster.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.EffectiveHeal * 0.5f, SpellInfo);
        Unit.ProcSkillsAndAuras(caster, caster, new ProcFlagsInit(ProcFlags.DealHelpfulPeriodic), new ProcFlagsInit(ProcFlags.TakeHelpfulPeriodic), ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, null, null, healInfo);

        caster.SendSpellNonMeleeDamageLog(log);
    }

    private void HandlePeriodicHealthFunnelAuraTick(Unit target, Unit caster)
    {
        if (caster == null || !caster.IsAlive || !target.IsAlive)
            return;

        if (target.HasUnitState(UnitState.Isolated))
        {
            SendTickImmune(target, caster);

            return;
        }

        var damage = Math.Max(Amount, 0);

        // do not kill health donator
        if (caster.Health < damage)
            damage = caster.Health - 1;

        if (damage == 0)
            return;

        caster.ModifyHealth(-damage);
        Log.Logger.Debug("PeriodicTick: donator {0} target {1} damage {2}.", caster.Entry, target.Entry, damage);

        var gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

        damage = damage * gainMultiplier;

        HealInfo healInfo = new(caster, target, damage, SpellInfo, SpellInfo.GetSchoolMask());
        caster.HealBySpell(healInfo);
        Unit.ProcSkillsAndAuras(caster, target, new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic), new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic), ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, ProcFlagsHit.Normal, null, null, healInfo);
    }

    private void HandlePeriodicHealAurasTick(Unit target, Unit caster)
    {
        if (!target.IsAlive)
            return;

        if (target.HasUnitState(UnitState.Isolated))
        {
            SendTickImmune(target, caster);

            return;
        }

        // don't regen when permanent aura target has full power
        if (Base.IsPermanent && target.IsFullHealth)
            return;

        var stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? Base.StackAmount : 1u;

        // ignore negative values (can be result apply spellmods to aura damage
        var damage = Math.Max(Amount, 0);

        if (AuraType == AuraType.ObsModHealth)
            damage = target.CountPctFromMaxHealth(damage);
        else if (caster != null)
            damage = caster.SpellHealingBonusDone(target, SpellInfo, damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

        damage = target.SpellHealingBonusTaken(caster, SpellInfo, damage, DamageEffectType.DOT);

        var crit = RandomHelper.randChance(GetCritChanceFor(caster, target));

        if (crit)
            damage = Unit.SpellCriticalHealingBonus(caster, _spellInfo, damage, target);

        Log.Logger.Debug("PeriodicTick: {0} (TypeId: {1}) heal of {2} (TypeId: {3}) for {4} health inflicted by {5}",
                         CasterGuid.ToString(),
                         Caster.TypeId,
                         target.GUID.ToString(),
                         target.TypeId,
                         damage,
                         Id);

        var heal = damage;

        HealInfo healInfo = new(caster, target, heal, SpellInfo, SpellInfo.GetSchoolMask());
        Unit.CalcHealAbsorb(healInfo);
        Unit.DealHeal(healInfo);

        SpellPeriodicAuraLogInfo pInfo = new(this, heal, damage, heal - healInfo.EffectiveHeal, healInfo.Absorb, 0, 0.0f, crit);
        target.SendPeriodicAuraLog(pInfo);

        if (caster != null)
            target.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.EffectiveHeal * 0.5f, SpellInfo);

        // %-based heal - does not proc auras
        if (AuraType == AuraType.ObsModHealth)
            return;

        var procAttacker = new ProcFlagsInit(ProcFlags.DealHelpfulPeriodic);
        var procVictim = new ProcFlagsInit(ProcFlags.TakeHelpfulPeriodic);
        var hitMask = crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;

        // ignore item heals
        if (Base.CastItemGuid.IsEmpty)
            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, null, null, healInfo);
    }

    private void HandlePeriodicManaLeechAuraTick(Unit target, Unit caster)
    {
        var powerType = (PowerType)MiscValue;

        if (caster == null || !caster.IsAlive || !target.IsAlive || target.DisplayPowerType != powerType)
            return;

        if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(SpellInfo))
        {
            SendTickImmune(target, caster);

            return;
        }

        if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
            caster.WorldObjectCombat.SpellHitResult(target, SpellInfo, false) != SpellMissInfo.None)
            return;

        // ignore negative values (can be result apply spellmods to aura damage
        var drainAmount = Math.Max(Amount, 0);

        double drainedAmount = -target.ModifyPower(powerType, -drainAmount);
        var gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

        SpellPeriodicAuraLogInfo pInfo = new(this, drainedAmount, drainAmount, 0, 0, 0, gainMultiplier, false);

        var gainAmount = drainedAmount * gainMultiplier;
        var gainedAmount = 0;

        if (gainAmount != 0)
        {
            gainedAmount = caster.ModifyPower(powerType, gainAmount);

            // energize is not modified by threat modifiers
            if (!SpellInfo.HasAttribute(SpellAttr4.NoHelpfulThreat))
                target.GetThreatManager().AddThreat(caster, gainedAmount * 0.5f, SpellInfo, true);
        }

        // Drain Mana
        if (caster.GetGuardianPet() != null && _spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && _spellInfo.SpellFamilyFlags[0].HasAnyFlag<uint>(0x00000010))
        {
            double manaFeedVal = 0;
            var aurEff = Base.GetEffect(1);

            if (aurEff != null)
                manaFeedVal = aurEff.Amount;

            if (manaFeedVal > 0)
            {
                var feedAmount = MathFunctions.CalculatePct(gainedAmount, manaFeedVal);

                CastSpellExtraArgs args = new(this);
                args.AddSpellMod(SpellValueMod.BasePoint0, feedAmount);
                caster.CastSpell(caster, 32554, args);
            }
        }

        target.SendPeriodicAuraLog(pInfo);
    }

    private void HandleObsModPowerAuraTick(Unit target, Unit caster)
    {
        PowerType powerType;

        if (MiscValue == (int)PowerType.All)
            powerType = target.DisplayPowerType;
        else
            powerType = (PowerType)MiscValue;

        if (!target.IsAlive || target.GetMaxPower(powerType) == 0)
            return;

        if (target.HasUnitState(UnitState.Isolated))
        {
            SendTickImmune(target, caster);

            return;
        }

        // don't regen when permanent aura target has full power
        if (Base.IsPermanent && target.GetPower(powerType) == target.GetMaxPower(powerType))
            return;

        // ignore negative values (can be result apply spellmods to aura damage
        var amount = Math.Max(Amount, 0) * target.GetMaxPower(powerType) / 100;

        SpellPeriodicAuraLogInfo pInfo = new(this, amount, amount, 0, 0, 0, 0.0f, false);

        var gain = target.ModifyPower(powerType, amount);

        if (caster != null)
            target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, SpellInfo, true);

        target.SendPeriodicAuraLog(pInfo);
    }

    private void HandlePeriodicEnergizeAuraTick(Unit target, Unit caster)
    {
        var powerType = (PowerType)MiscValue;

        if (!target.IsAlive || target.GetMaxPower(powerType) == 0)
            return;

        if (target.HasUnitState(UnitState.Isolated))
        {
            SendTickImmune(target, caster);

            return;
        }

        // don't regen when permanent aura target has full power
        if (Base.IsPermanent && target.GetPower(powerType) == target.GetMaxPower(powerType))
            return;

        // ignore negative values (can be result apply spellmods to aura damage
        var amount = Math.Max(Amount, 0);

        SpellPeriodicAuraLogInfo pInfo = new(this, amount, amount, 0, 0, 0, 0.0f, false);
        var gain = target.ModifyPower(powerType, amount);

        if (caster != null)
            target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, SpellInfo, true);

        target.SendPeriodicAuraLog(pInfo);
    }

    private void HandlePeriodicPowerBurnAuraTick(Unit target, Unit caster)
    {
        var powerType = (PowerType)MiscValue;

        if (caster == null || !target.IsAlive || target.DisplayPowerType != powerType)
            return;

        if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(SpellInfo))
        {
            SendTickImmune(target, caster);

            return;
        }

        // ignore negative values (can be result apply spellmods to aura damage
        var damage = Math.Max(Amount, 0);

        double gain = -target.ModifyPower(powerType, -damage);

        var dmgMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

        var spellProto = SpellInfo;

        // maybe has to be sent different to client, but not by SMSG_PERIODICAURALOG
        SpellNonMeleeDamage damageInfo = new(caster, target, spellProto, Base.SpellVisual, spellProto.SchoolMask, Base.CastId)
        {
            PeriodicLog = true
        };

        // no SpellDamageBonus for burn mana
        caster.CalculateSpellDamageTaken(damageInfo, gain * dmgMultiplier, spellProto);

        Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);

        // Set trigger flag
        var procAttacker = new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic);
        var procVictim = new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic);
        var hitMask = Unit.CreateProcHitMask(damageInfo, SpellMissInfo.None);
        var spellTypeMask = ProcFlagsSpellType.NoDmgHeal;

        if (damageInfo.Damage != 0)
        {
            procVictim.Or(ProcFlags.TakeAnyDamage);
            spellTypeMask |= ProcFlagsSpellType.Damage;
        }

        caster.DealSpellDamage(damageInfo, true);

        DamageInfo dotDamageInfo = new(damageInfo, DamageEffectType.DOT, WeaponAttackType.BaseAttack, hitMask);
        Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, spellTypeMask, ProcFlagsSpellPhase.Hit, hitMask, null, dotDamageInfo, null);

        caster.SendSpellNonMeleeDamageLog(damageInfo);
    }

    private bool CanPeriodicTickCrit()
    {
        if (SpellInfo.HasAttribute(SpellAttr2.CantCrit))
            return false;

        return true;
    }

    private double CalcPeriodicCritChance(Unit caster)
    {
        if (!caster || !CanPeriodicTickCrit())
            return 0.0f;

        var modOwner = caster.SpellModOwner;

        if (!modOwner)
            return 0.0f;

        var critChance = modOwner.SpellCritChanceDone(null, this, SpellInfo.GetSchoolMask(), SpellInfo.GetAttackType());

        return Math.Max(0.0f, critChance);
    }

    private void HandleBreakableCCAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var damageLeft = Amount - eventInfo.DamageInfo.Damage;

        if (damageLeft <= 0)
            aurApp.Target.RemoveAura(aurApp);
        else
            ChangeAmount(damageLeft);
    }

    private void HandleProcTriggerSpellAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var triggerCaster = aurApp.Target;
        var triggerTarget = eventInfo.ProcTarget;

        var triggerSpellId = GetSpellEffectInfo().TriggerSpell;

        if (triggerSpellId == 0)
        {
            Log.Logger.Warning($"AuraEffect::HandleProcTriggerSpellAuraProc: Spell {Id} [EffectIndex: {EffIndex}] does not have triggered spell.");

            return;
        }

        var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, Base.CastDifficulty);

        if (triggeredSpellInfo != null)
        {
            Log.Logger.Debug($"AuraEffect.HandleProcTriggerSpellAuraProc: Triggering spell {triggeredSpellInfo.Id} from aura {Id} proc");
            triggerCaster.CastSpell(triggerTarget, triggeredSpellInfo.Id, new CastSpellExtraArgs(this).SetTriggeringSpell(eventInfo.ProcSpell));
        }
        else if (triggerSpellId != 0 && AuraType != AuraType.Dummy)
        {
            Log.Logger.Error($"AuraEffect.HandleProcTriggerSpellAuraProc: Spell {Id} has non-existent spell {triggerSpellId} in EffectTriggered[{EffIndex}] and is therefore not triggered.");
        }
    }

    private void HandleProcTriggerSpellWithValueAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var triggerCaster = aurApp.Target;
        var triggerTarget = eventInfo.ProcTarget;

        var triggerSpellId = GetSpellEffectInfo().TriggerSpell;

        if (triggerSpellId == 0)
        {
            Log.Logger.Warning($"AuraEffect::HandleProcTriggerSpellAuraProc: Spell {Id} [EffectIndex: {EffIndex}] does not have triggered spell.");

            return;
        }

        var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, Base.CastDifficulty);

        if (triggeredSpellInfo != null)
        {
            CastSpellExtraArgs args = new(this);
            args.SetTriggeringSpell(eventInfo.ProcSpell);
            args.AddSpellMod(SpellValueMod.BasePoint0, Amount);
            triggerCaster.CastSpell(triggerTarget, triggerSpellId, args);
            Log.Logger.Debug("AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Triggering spell {0} with value {1} from aura {2} proc", triggeredSpellInfo.Id, Amount, Id);
        }
        else
        {
            Log.Logger.Error("AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Spell {GetId()} has non-existent spell {triggerSpellId} in EffectTriggered[{GetEffIndex()}] and is therefore not triggered.");
        }
    }

    private void HandleProcTriggerDamageAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
    {
        var target = aurApp.Target;
        var triggerTarget = eventInfo.ProcTarget;

        if (triggerTarget.HasUnitState(UnitState.Isolated) || triggerTarget.IsImmunedToDamage(SpellInfo))
        {
            SendTickImmune(triggerTarget, target);

            return;
        }

        SpellNonMeleeDamage damageInfo = new(target, triggerTarget, SpellInfo, Base.SpellVisual, SpellInfo.SchoolMask, Base.CastId);
        var damage = target.SpellDamageBonusDone(triggerTarget, SpellInfo, Amount, DamageEffectType.SpellDirect, GetSpellEffectInfo());
        damage = triggerTarget.SpellDamageBonusTaken(target, SpellInfo, damage, DamageEffectType.SpellDirect);
        target.CalculateSpellDamageTaken(damageInfo, damage, SpellInfo);
        Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);
        target.DealSpellDamage(damageInfo, true);
        target.SendSpellNonMeleeDamageLog(damageInfo);
    }

    [AuraEffectHandler(AuraType.ForceWeather)]
    private void HandleAuraForceWeather(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
            target.SendPacket(new WeatherPkt((WeatherState)MiscValue, 1.0f));
        else
            target.Location.Map.SendZoneWeather(target.Location.Zone, target);
    }

    [AuraEffectHandler(AuraType.EnableAltPower)]
    private void HandleEnableAltPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var altPowerId = MiscValue;
        var powerEntry = CliDB.UnitPowerBarStorage.LookupByKey(altPowerId);

        if (powerEntry == null)
            return;

        if (apply)
            aurApp.Target.SetMaxPower(PowerType.AlternatePower, (int)powerEntry.MaxPower);
        else
            aurApp.Target.SetMaxPower(PowerType.AlternatePower, 0);
    }

    [AuraEffectHandler(AuraType.ModSpellCategoryCooldown)]
    private void HandleModSpellCategoryCooldown(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var player = aurApp.Target.AsPlayer;

        if (player)
            player.SendSpellCategoryCooldowns();
    }

    [AuraEffectHandler(AuraType.ShowConfirmationPrompt)]
    [AuraEffectHandler(AuraType.ShowConfirmationPromptWithDifficulty)]
    private void HandleShowConfirmationPrompt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var player = aurApp.Target.AsPlayer;

        if (!player)
            return;

        if (apply)
            player.AddTemporarySpell(_effectInfo.TriggerSpell);
        else
            player.RemoveTemporarySpell(_effectInfo.TriggerSpell);
    }

    [AuraEffectHandler(AuraType.OverridePetSpecs)]
    private void HandleOverridePetSpecs(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var player = aurApp.Target.AsPlayer;

        if (!player)
            return;

        if (player.Class != PlayerClass.Hunter)
            return;

        var pet = player.CurrentPet;

        if (!pet)
            return;

        var currSpec = CliDB.ChrSpecializationStorage.LookupByKey(pet.Specialization);

        if (currSpec == null)
            return;

        pet.SetSpecialization(Global.DB2Mgr.GetChrSpecializationByIndex(apply ? PlayerClass.Max : 0, currSpec.OrderIndex).Id);
    }

    [AuraEffectHandler(AuraType.AllowUsingGameobjectsWhileMounted)]
    private void HandleAllowUsingGameobjectsWhileMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
            target.SetPlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
        else if (!target.HasAuraType(AuraType.AllowUsingGameobjectsWhileMounted))
            target.RemovePlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
    }

    [AuraEffectHandler(AuraType.PlayScene)]
    private void HandlePlayScene(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var player = aurApp.Target.AsPlayer;

        if (!player)
            return;

        if (apply)
            player.SceneMgr.PlayScene((uint)MiscValue);
        else
            player.SceneMgr.CancelSceneBySceneId((uint)MiscValue);
    }

    [AuraEffectHandler(AuraType.AreaTrigger)]
    private void HandleCreateAreaTrigger(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;

        if (apply)
        {
            AreaTrigger.CreateAreaTrigger((uint)MiscValue, Caster, target, SpellInfo, target.Location, Base.Duration, Base.SpellVisual, ObjectGuid.Empty, this);
        }
        else
        {
            var caster = Caster;

            if (caster)
                caster.RemoveAreaTrigger(this);
        }
    }

    [AuraEffectHandler(AuraType.PvpTalents)]
    private void HandleAuraPvpTalents(AuraApplication auraApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = auraApp.Target.AsPlayer;

        if (target)
        {
            if (apply)
                target.TogglePvpTalents(true);
            else if (!target.HasAuraType(AuraType.PvpTalents))
                target.TogglePvpTalents(false);
        }
    }

    [AuraEffectHandler(AuraType.LinkedSummon)]
    private void HandleLinkedSummon(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target;
        var triggerSpellInfo = Global.SpellMgr.GetSpellInfo(GetSpellEffectInfo().TriggerSpell, Base.CastDifficulty);

        if (triggerSpellInfo == null)
            return;

        // on apply cast summon spell
        if (apply)
        {
            CastSpellExtraArgs args = new(this)
            {
                CastDifficulty = triggerSpellInfo.Difficulty
            };

            target.CastSpell(target, triggerSpellInfo.Id, args);
        }
        // on unapply we need to search for and remove the summoned creature
        else
        {
            List<uint> summonedEntries = new();

            foreach (var spellEffectInfo in triggerSpellInfo.Effects)
                if (spellEffectInfo.IsEffect(SpellEffectName.Summon))
                {
                    var summonEntry = (uint)spellEffectInfo.MiscValue;

                    if (summonEntry != 0)
                        summonedEntries.Add(summonEntry);
                }

            // we don't know if there can be multiple summons for the same effect, so consider only 1 summon for each effect
            // most of the spells have multiple effects with the same summon spell id for multiple spawns, so right now it's safe to assume there's only 1 spawn per effect
            foreach (var summonEntry in summonedEntries)
            {
                var nearbyEntries = target.Location.GetCreatureListWithEntryInGrid(summonEntry);

                foreach (var creature in nearbyEntries)
                    if (creature.OwnerUnit == target)
                    {
                        creature.DespawnOrUnsummon();

                        break;
                    }
                    else
                    {
                        var tempSummon = creature.ToTempSummon();

                        if (tempSummon)
                            if (tempSummon.GetSummoner() == target)
                            {
                                tempSummon.DespawnOrUnsummon();

                                break;
                            }
                    }
            }
        }
    }

    [AuraEffectHandler(AuraType.SetFFAPvp)]
    private void HandleSetFFAPvP(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (!target)
            return;

        target.UpdatePvPState(true);
    }

    [AuraEffectHandler(AuraType.ModOverrideZonePvpType)]
    private void HandleModOverrideZonePVPType(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        if (apply)
            target.OverrideZonePvpType = (ZonePVPTypeOverride)MiscValue;
        else if (target.HasAuraType(AuraType.ModOverrideZonePvpType))
            target.OverrideZonePvpType = (ZonePVPTypeOverride)target.GetAuraEffectsByType(AuraType.ModOverrideZonePvpType).Last().MiscValue;
        else
            target.OverrideZonePvpType = ZonePVPTypeOverride.None;

        target.UpdateHostileAreaState(CliDB.AreaTableStorage.LookupByKey(target.Location.Zone));
        target.UpdatePvPState();
    }

    [AuraEffectHandler(AuraType.BattleGroundPlayerPositionFactional)]
    [AuraEffectHandler(AuraType.BattleGroundPlayerPosition)]
    private void HandleBattlegroundPlayerPosition(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var target = aurApp.Target.AsPlayer;

        if (target == null)
            return;

        var battlegroundMap = target.Location.Map.ToBattlegroundMap;

        if (battlegroundMap == null)
            return;

        var bg = battlegroundMap.GetBG();

        if (bg == null)
            return;

        if (apply)
        {
            BattlegroundPlayerPosition playerPosition = new()
            {
                Guid = target.GUID,
                ArenaSlot = (sbyte)MiscValue,
                Pos = target.Location
            };

            if (AuraType == AuraType.BattleGroundPlayerPositionFactional)
                playerPosition.IconID = target.EffectiveTeam == TeamFaction.Alliance ? BattlegroundConst.PlayerPositionIconHordeFlag : BattlegroundConst.PlayerPositionIconAllianceFlag;
            else if (AuraType == AuraType.BattleGroundPlayerPosition)
                playerPosition.IconID = target.EffectiveTeam == TeamFaction.Alliance ? BattlegroundConst.PlayerPositionIconAllianceFlag : BattlegroundConst.PlayerPositionIconHordeFlag;
            else
                Log.Logger.Warning($"Unknown aura effect {AuraType} handled by HandleBattlegroundPlayerPosition.");

            bg.AddPlayerPosition(playerPosition);
        }
        else
        {
            bg.RemovePlayerPosition(target.GUID);
        }
    }

    [AuraEffectHandler(AuraType.StoreTeleportReturnPoint)]
    private void HandleStoreTeleportReturnPoint(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var playerTarget = aurApp.Target.AsPlayer;

        if (playerTarget == null)
            return;

        if (apply)
            playerTarget.AddStoredAuraTeleportLocation(SpellInfo.Id);
        else if (!playerTarget.Session.IsLogingOut)
            playerTarget.RemoveStoredAuraTeleportLocation(SpellInfo.Id);
    }

    [AuraEffectHandler(AuraType.MountRestrictions)]
    private void HandleMountRestrictions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        aurApp.Target.UpdateMountCapability();
    }

    [AuraEffectHandler(AuraType.CosmeticMounted)]
    private void HandleCosmeticMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        if (apply)
            aurApp.Target.SetCosmeticMountDisplayId((uint)MiscValue);
        else
            aurApp.Target.SetCosmeticMountDisplayId(0); // set cosmetic mount to 0, even if multiple auras are active; tested with zandalari racial + divine steed

        var playerTarget = aurApp.Target.AsPlayer;

        if (playerTarget == null)
            return;

        playerTarget.SendMovementSetCollisionHeight(playerTarget.CollisionHeight, UpdateCollisionHeightReason.Force);
    }

    [AuraEffectHandler(AuraType.SuppressItemPassiveEffectBySpellLabel)]
    private void HandleSuppressItemPassiveEffectBySpellLabel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        // Refresh applications
        aurApp.Target.GetAuraQuery().HasLabel((uint)MiscValue).ForEachResult(aura => aura.ApplyForTargets());
    }

    [AuraEffectHandler(AuraType.ForceBeathBar)]
    private void HandleForceBreathBar(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
    {
        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
            return;

        var playerTarget = aurApp.Target.AsPlayer;

        if (playerTarget == null)
            return;

        playerTarget.Location.UpdatePositionData();
    }

    #endregion
}