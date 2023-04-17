// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Forged.MapServer.Spells.Events;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Entities.Units;

public partial class Unit
{
    public void AddAura(UnitAura aura, Unit caster)
    {
        _ownedAuras.Add(aura);

        RemoveNoStackAurasDueToAura(aura);

        if (aura.IsRemoved)
            return;

        aura.IsSingleTarget = caster != null && aura.SpellInfo.IsSingleTarget();

        if (!aura.IsSingleTarget)
            return;

        // register single target aura
        if (caster == null)
            return;

        caster.SingleCastAuras.Add(aura);

        Queue<Aura> aurasSharingLimit = new();

        // remove other single target auras
        foreach (var scAura in caster.SingleCastAuras.Where(scAura => scAura != aura && scAura.IsSingleTargetWith(aura)))
            aurasSharingLimit.Enqueue(scAura);

        var maxOtherAuras = aura.SpellInfo.MaxAffectedTargets - 1;

        while (aurasSharingLimit.Count > maxOtherAuras)
        {
            aurasSharingLimit.Peek().Remove();
            aurasSharingLimit.Dequeue();
        }
    }

    /// <summary>
    ///     Will add the aura to the unit. If the aura exists and it has a stack amount, a stack will be added up to the max stack amount.
    /// </summary>
    /// <param name="spellId"> Spell id of the aura to add </param>
    /// <returns> The aura and its applications. </returns>
    public Aura AddAura(uint spellId)
    {
        return AddAura(spellId, this);
    }

    public Aura AddAura(uint spellId, Unit target)
    {
        if (target == null)
            return null;

        var spellInfo = SpellManager.GetSpellInfo(spellId, Location.Map.DifficultyID);

        if (spellInfo == null)
            return null;

        return AddAura(spellInfo, SpellConst.MaxEffects, target);
    }

    public Aura AddAura(SpellInfo spellInfo, HashSet<int> effMask, Unit target)
    {
        if (spellInfo == null)
            return null;

        if (!target.IsAlive && !spellInfo.IsPassive && !spellInfo.HasAttribute(SpellAttr2.AllowDeadTarget))
            return null;

        if (target.IsImmunedToSpell(spellInfo, this))
            return null;

        foreach (var spellEffectInfo in spellInfo.Effects)
        {
            if (!effMask.Contains(spellEffectInfo.EffectIndex))
                continue;

            if (target.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, this))
                effMask.Remove(spellEffectInfo.EffectIndex);
        }

        if (effMask.Count == 0)
            return null;

        var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, Location.MapId, spellInfo.Id, Location.Map.GenerateLowGuid(HighGuid.Cast));

        AuraCreateInfo createInfo = new(castId, spellInfo, Location.Map.DifficultyID, effMask, target);
        createInfo.Caster = this;

        var aura = Aura.TryRefreshStackOrCreate(createInfo);

        if (aura == null)
            return null;

        aura.ApplyForTargets();

        return aura;
    }

    public void ApplyAllAuraStatMods()
    {
        foreach (var i in AppliedAuras)
            i.Base.HandleAllEffects(i, AuraEffectHandleModes.Stat, true);
    }

    public void ApplyAura(AuraApplication aurApp, int effIndex)
    {
        ApplyAura(aurApp,
                  new HashSet<int>()
                  {
                      effIndex
                  });
    }

    // handles effects of aura application
    // should be done after registering aura in lists
    public void ApplyAura(AuraApplication aurApp, HashSet<int> effMask)
    {
        var aura = aurApp.Base;

        RemoveNoStackAurasDueToAura(aura);

        if (aurApp.HasRemoveMode)
            return;

        // Update target aura state Id
        var aState = aura.SpellInfo.GetAuraState();

        if (aState != 0)
        {
            var aStateMask = 1u << ((int)aState - 1);

            // force update so the new caster registers it
            if (aStateMask.HasAnyFlag((uint)AuraStateType.PerCasterAuraStateMask) && (UnitData.AuraState & aStateMask) != 0)
            {
                Values.ModifyValue(UnitData).ModifyValue(UnitData.AuraState);
                ForceUpdateFieldChange();
            }
            else
                ModifyAuraState(aState, true);
        }

        if (aurApp.HasRemoveMode)
            return;

        // Sitdown on apply aura req seated
        if (aura.SpellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing) && !IsSitState)
            SetStandState(UnitStandStateType.Sit);

        var caster = aura.Caster;

        if (aurApp.HasRemoveMode)
            return;

        aura.HandleAuraSpecificMods(aurApp, caster, true, false);

        // apply effects of the aura
        foreach (var effect in aurApp.Base.AuraEffects)
            if (effMask.Contains(effect.Key) && !aurApp.HasRemoveMode)
                aurApp._HandleEffect(effect.Key, true);

        var player = AsPlayer;

        if (player == null)
            return;

        if (ConditionManager.IsSpellUsedInSpellClickConditions(aurApp.Base.Id))
            player.UpdateVisibleGameobjectsOrSpellClicks();
    }

    public void ApplyAuraEffect(Aura aura, int effIndex)
    {
        var aurApp = aura.GetApplicationOfTarget(GUID);

        if (aurApp.EffectMask.Count == 0)
            ApplyAura(aurApp, effIndex);
        else
            aurApp._HandleEffect(effIndex, true);
    }

    public void ApplyCastTimePercentMod(double val, bool apply)
    {
        ApplyCastTimePercentMod((float)val, apply);
    }

    public void ApplyCastTimePercentMod(float val, bool apply)
    {
        if (val > 0.0f)
        {
            ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModCastingSpeed), val, !apply);
            ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModSpellHaste), val, !apply);
            ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModHasteRegen), val, !apply);
        }
        else
        {
            ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModCastingSpeed), -val, apply);
            ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModSpellHaste), -val, apply);
            ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModHasteRegen), -val, apply);
        }
    }

    public void ApplyDiminishingAura(DiminishingGroup group, bool apply)
    {
        // Checking for existing in the table
        var diminish = _diminishing[(int)group];

        if (apply)
            ++diminish.Stack;
        else if (diminish.Stack != 0)
        {
            --diminish.Stack;

            // Remember time after last aura from group removed
            if (diminish.Stack == 0)
                diminish.HitTime = GameTime.CurrentTimeMS;
        }
    }

    public bool ApplyDiminishingToDuration(SpellInfo auraSpellInfo, ref int duration, WorldObject caster, DiminishingLevels previousLevel)
    {
        var group = auraSpellInfo.DiminishingReturnsGroupForSpell;

        if (duration == -1 || group == DiminishingGroup.None)
            return true;

        var limitDuration = auraSpellInfo.DiminishingReturnsLimitDuration;

        // test pet/charm masters instead pets/charmeds
        var targetOwner = CharmerOrOwner;
        var casterOwner = caster.CharmerOrOwner;

        if (limitDuration > 0 && duration > limitDuration)
        {
            var target = targetOwner ?? this;
            var source = casterOwner ?? caster;

            if (target.IsAffectedByDiminishingReturns && source.IsPlayer)
                duration = limitDuration;
        }

        var mod = 1.0f;

        switch (group)
        {
            case DiminishingGroup.Taunt:
                if (IsTypeId(TypeId.Unit) && AsCreature.Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.ObeysTauntDiminishingReturns))
                {
                    var diminish = previousLevel;

                    switch (diminish)
                    {
                        case DiminishingLevels.Level1:
                            break;

                        case DiminishingLevels.Level2:
                            mod = 0.65f;

                            break;

                        case DiminishingLevels.Level3:
                            mod = 0.4225f;

                            break;

                        case DiminishingLevels.Level4:
                            mod = 0.274625f;

                            break;

                        case DiminishingLevels.TauntImmune:
                            mod = 0.0f;

                            break;
                    }
                }

                break;

            case DiminishingGroup.AOEKnockback:
                if (auraSpellInfo.DiminishingReturnsGroupType == DiminishingReturnsType.All ||
                    (auraSpellInfo.DiminishingReturnsGroupType == DiminishingReturnsType.Player &&
                     (targetOwner?.IsAffectedByDiminishingReturns ?? IsAffectedByDiminishingReturns)))
                {
                    var diminish = previousLevel;

                    switch (diminish)
                    {
                        case DiminishingLevels.Level1:
                            break;

                        case DiminishingLevels.Level2:
                            mod = 0.5f;

                            break;
                    }
                }

                break;

            default:
                if (auraSpellInfo.DiminishingReturnsGroupType == DiminishingReturnsType.All ||
                    (auraSpellInfo.DiminishingReturnsGroupType == DiminishingReturnsType.Player &&
                     (targetOwner?.IsAffectedByDiminishingReturns ?? IsAffectedByDiminishingReturns)))
                {
                    var diminish = previousLevel;

                    switch (diminish)
                    {
                        case DiminishingLevels.Level1:
                            break;

                        case DiminishingLevels.Level2:
                            mod = 0.5f;

                            break;

                        case DiminishingLevels.Level3:
                            mod = 0.25f;

                            break;

                        case DiminishingLevels.Immune:
                            mod = 0.0f;

                            break;
                    }
                }

                break;
        }

        duration = (int)(duration * mod);

        return duration != 0;
    }

    public void ApplySpellImmune(uint spellId, SpellImmunity op, Mechanics type, bool apply)
    {
        ApplySpellImmune(spellId, op, (uint)type, apply);
    }

    public void ApplySpellImmune(uint spellId, SpellImmunity op, SpellSchoolMask type, bool apply)
    {
        ApplySpellImmune(spellId, op, (uint)type, apply);
    }

    public void ApplySpellImmune(uint spellId, SpellImmunity op, AuraType type, bool apply)
    {
        ApplySpellImmune(spellId, op, (uint)type, apply);
    }

    public void ApplySpellImmune(uint spellId, SpellImmunity op, SpellEffectName type, bool apply)
    {
        ApplySpellImmune(spellId, op, (uint)type, apply);
    }

    public void ApplySpellImmune(uint spellId, SpellImmunity op, uint type, bool apply)
    {
        if (apply)
            _spellImmune[op].Add(type, spellId);
        else
        {
            var bounds = _spellImmune[op].LookupByKey(type);

            foreach (var spell in bounds)
                if (spell == spellId)
                {
                    _spellImmune[op].Remove(type, spell);

                    break;
                }
        }
    }

    public uint BuildAuraStateUpdateForTarget(Unit target)
    {
        var auraStates = UnitData.AuraState & ~(uint)AuraStateType.PerCasterAuraStateMask;

        foreach (var state in _auraStateAuras.KeyValueList)
            if (Convert.ToBoolean((1 << (int)state.Key - 1) & (uint)AuraStateType.PerCasterAuraStateMask))
                if (state.Value.Base.CasterGuid == target.GUID)
                    auraStates |= (uint)(1 << (int)state.Key - 1);

        return auraStates;
    }

    public void CalculateSpellDamageTaken(SpellNonMeleeDamage damageInfo, double damage, SpellInfo spellInfo, WeaponAttackType attackType = WeaponAttackType.BaseAttack, bool crit = false, bool blocked = false, Spell spell = null)
    {
        if (damage < 0)
            return;

        var victim = damageInfo.Target;

        if (victim == null || !victim.IsAlive)
            return;

        var damageSchoolMask = damageInfo.SchoolMask;

        // Spells with SPELL_ATTR4_IGNORE_DAMAGE_TAKEN_MODIFIERS ignore resilience because their damage is based off another spell's damage.
        if (!spellInfo.HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
        {
            if (UnitCombatHelpers.IsDamageReducedByArmor(damageSchoolMask, spellInfo))
                damage = (int)UnitCombatHelpers.CalcArmorReducedDamage(damageInfo.Attacker, victim, damage, spellInfo, attackType);

            // Per-school calc
            switch (spellInfo.DmgClass)
            {
                // Melee and Ranged Spells
                case SpellDmgClass.Ranged:
                case SpellDmgClass.Melee:
                {
                    if (crit)
                    {
                        damageInfo.HitInfo |= (int)SpellHitType.Crit;

                        // Calculate crit bonus
                        var critBonus = damage;
                        // Apply crit_damage bonus for melee spells
                        var modOwner = SpellModOwner;

                        modOwner?.ApplySpellMod(spellInfo, SpellModOp.CritDamageAndHealing, ref critBonus);

                        damage += critBonus;

                        // Increase crit damage from SPELL_AURA_MOD_CRIT_DAMAGE_BONUS
                        var critPctDamageMod = (GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, (uint)spellInfo.GetSchoolMask()) - 1.0f) * 100;

                        if (critPctDamageMod != 0)
                            MathFunctions.AddPct(ref damage, critPctDamageMod);
                    }

                    // Spell weapon based damage CAN BE crit & blocked at same time
                    if (blocked)
                    {
                        // double blocked amount if block is critical
                        var value = victim.GetBlockPercent(Level);

                        if (victim.IsBlockCritical())
                            value *= 2; // double blocked percent

                        damageInfo.Blocked = MathFunctions.CalculatePct(damage, value);

                        if (damage <= damageInfo.Blocked)
                        {
                            damageInfo.Blocked = damage;
                            damageInfo.FullBlock = true;
                        }

                        damage -= damageInfo.Blocked;
                    }

                    if (CanApplyResilience())
                        UnitCombatHelpers.ApplyResilience(victim, ref damage);

                    break;
                }
                // Magical Attacks
                case SpellDmgClass.None:
                case SpellDmgClass.Magic:
                {
                    // If crit add critical bonus
                    if (crit)
                    {
                        damageInfo.HitInfo |= (int)SpellHitType.Crit;
                        damage = UnitCombatHelpers.SpellCriticalDamageBonus(this, spellInfo, damage, victim);
                    }

                    if (CanApplyResilience())
                        UnitCombatHelpers.ApplyResilience(victim, ref damage);

                    break;
                }
            }
        }

        // Script Hook For CalculateSpellDamageTaken -- Allow scripts to change the Damage post class mitigation calculations
        ScriptManager.ForEach<IUnitModifySpellDamageTaken>(p => p.ModifySpellDamageTaken(damageInfo.Target, damageInfo.Attacker, ref damage, spellInfo));

        // Calculate absorb resist
        if (damage < 0)
            damage = 0;

        damageInfo.Damage = damage;
        damageInfo.OriginalDamage = damage;
        DamageInfo dmgInfo = new(damageInfo, DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack, ProcFlagsHit.None);
        UnitCombatHelpers.CalcAbsorbResist(dmgInfo, spell);
        damageInfo.Absorb = dmgInfo.Absorb;
        damageInfo.Resist = dmgInfo.Resist;

        if (damageInfo.Absorb != 0)
            damageInfo.HitInfo |= damageInfo.Damage - damageInfo.Absorb == 0 ? (int)HitInfo.FullAbsorb : (int)HitInfo.PartialAbsorb;

        if (damageInfo.Resist != 0)
            damageInfo.HitInfo |= damageInfo.Damage - damageInfo.Resist == 0 ? (int)HitInfo.FullResist : (int)HitInfo.PartialResist;

        damageInfo.Damage = dmgInfo.Damage;
    }

    public bool CanCastSpellWhileMoving(SpellInfo spellInfo)
    {
        if (HasAuraTypeWithAffectMask(AuraType.CastWhileWalking, spellInfo))
            return true;

        if (HasAuraType(AuraType.CastWhileWalkingAll))
            return true;

        foreach (var label in spellInfo.Labels)
            if (HasAuraTypeWithMiscvalue(AuraType.CastWhileWalkingBySpellLabel, (int)label))
                return true;

        return false;
    }

    public void CastStop(uint exceptSpellid = 0)
    {
        for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
            if (CurrentSpells.TryGetValue(i, out var spell) && spell != null && spell.SpellInfo.Id != exceptSpellid)
                InterruptSpell(i, false);
    }

    public void CastWithDelay(TimeSpan delay, Unit target, uint spellId, bool triggered)
    {
        Events.AddEvent(new DelayedCastEvent(this, target, spellId, new CastSpellExtraArgs(triggered)), delay);
    }

    public void CastWithDelay(TimeSpan delay, Unit target, uint spellId, CastSpellExtraArgs args)
    {
        Events.AddEvent(new DelayedCastEvent(this, target, spellId, args), delay);
    }

    public void DealSpellDamage(SpellNonMeleeDamage damageInfo, bool durabilityLoss)
    {
        var victim = damageInfo?.Target;

        if (victim == null)
            return;

        if (!victim.IsAlive || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsEvadingAttacks))
            return;

        if (damageInfo.Spell == null)
        {
            Log.Logger.Debug("Unit.DealSpellDamage has no spell");

            return;
        }

        // Call default DealDamage
        CleanDamage cleanDamage = new(damageInfo.CleanDamage, damageInfo.Absorb, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
        damageInfo.Damage = UnitCombatHelpers.DealDamage(this, victim, damageInfo.Damage, cleanDamage, DamageEffectType.SpellDirect, damageInfo.SchoolMask, damageInfo.Spell, durabilityLoss);
    }

    public void DelayOwnedAuras(uint spellId, ObjectGuid caster, int delaytime)
    {
        var range = _ownedAuras.Query().HasSpellId(spellId).HasCasterGuid(caster).GetResults();

        foreach (var aura in range)
        {
            if (aura.Duration < delaytime)
                aura.SetDuration(0);
            else
                aura.SetDuration(aura.Duration - delaytime);

            // update for out of range group members (on 1 slot use)
            aura.SetNeedClientUpdateForTargets();
        }
    }

    public void DeleteRemovedAuras()
    {
        RemovedAurasCount = 0;
    }

    public void EnergizeBySpell(Unit victim, SpellInfo spellInfo, double damage, PowerType powerType)
    {
        EnergizeBySpell(victim, spellInfo, (int)damage, powerType);
    }

    public void EnergizeBySpell(Unit victim, SpellInfo spellInfo, int damage, PowerType powerType)
    {
        var gain = victim.ModifyPower(powerType, damage, false);
        var overEnergize = damage - gain;

        victim.GetThreatManager().ForwardThreatForAssistingMe(this, (float)damage / 2, spellInfo, true);
        SendEnergizeSpellLog(victim, spellInfo.Id, gain, overEnergize, powerType);
    }

    public Spell FindCurrentSpellBySpellId(uint spellID)
    {
        foreach (var spell in CurrentSpells.Values)
        {
            if (spell == null)
                continue;

            if (spell.SpellInfo.Id == spellID)
                return spell;
        }

        return null;
    }

    public void FinishSpell(CurrentSpellTypes spellType, SpellCastResult result = SpellCastResult.SpellCastOk)
    {
        var spell = GetCurrentSpell(spellType);

        if (spell == null)
            return;

        if (spellType == CurrentSpellTypes.Channeled)
            spell.SendChannelUpdate(0);

        spell.Finish(result);
    }

    public AuraApplicationCollection.AuraApplicationQuery GetAppliedAurasQuery()
    {
        return _appliedAuras.Query();
    }

    public Aura GetAura<T>(T spellId) where T : struct, Enum
    {
        return GetAura(Convert.ToUInt32(spellId));
    }

    public Aura GetAura<T>(T spellId, ObjectGuid casterGUID, ObjectGuid itemCasterGUID = default) where T : struct, Enum
    {
        return GetAura(Convert.ToUInt32(spellId), casterGUID, itemCasterGUID);
    }

    public Aura GetAura(uint spellId)
    {
        var aurApp = GetAuraApplication(spellId)?.FirstOrDefault();

        return aurApp?.Base;
    }

    public Aura GetAura(uint spellId, ObjectGuid casterGUID, ObjectGuid itemCasterGUID = default)
    {
        var aurApp = GetAuraApplication(spellId, casterGUID, itemCasterGUID);

        return aurApp?.Base;
    }

    public IEnumerable<AuraApplication> GetAuraApplication(uint spellId)
    {
        return _appliedAuras.Query().HasSpellId(spellId).GetResults();
    }

    public AuraApplication GetAuraApplication(uint spellId, ObjectGuid casterGUID, ObjectGuid itemCasterGUID = default)
    {
        return _appliedAuras.Query()
                            .HasSpellId(spellId)
                            .HasCasterGuid(casterGUID)
                            .HasCastItemGuid(itemCasterGUID)
                            .GetResults()
                            .FirstOrDefault();
    }

    public uint GetAuraCount(uint spellId)
    {
        uint count = 0;

        foreach (var aura in _appliedAuras.Query().HasSpellId(spellId).GetResults())
            if (aura.Base.StackAmount == 0)
                ++count;
            else
                count += aura.Base.StackAmount;

        return count;
    }

    public AuraEffect GetAuraEffect(uint spellId, int effIndex, ObjectGuid casterGUID = default)
    {
        return _appliedAuras.Query()
                            .HasSpellId(spellId)
                            .HasEffectIndex(effIndex)
                            .HasCasterGuid(casterGUID)
                            .GetResults()
                            .FirstOrDefault()
                            ?.Base
                            ?.GetEffect(effIndex);
    }

    // spell mustn't have familyflags
    public AuraEffect GetAuraEffect(AuraType type, SpellFamilyNames family, FlagArray128 familyFlag, ObjectGuid casterGUID = default)
    {
        var auras = GetAuraEffectsByType(type);

        foreach (var aura in auras)
        {
            var spell = aura.SpellInfo;

            if (spell.SpellFamilyName == family && spell.SpellFamilyFlags & familyFlag)
            {
                if (!casterGUID.IsEmpty && aura.CasterGuid != casterGUID)
                    continue;

                return aura;
            }
        }

        return null;
    }

    public double GetAuraEffectAmount(uint spellId, byte effIndex)
    {
        var aurEff = GetAuraEffect(spellId, effIndex);

        if (aurEff != null)
            return aurEff.Amount;

        return 0;
    }

    public AuraEffect GetAuraEffectOfRankedSpell(uint spellId, int effIndex, ObjectGuid casterGUID = default)
    {
        var rankSpell = SpellManager.GetFirstSpellInChain(spellId);

        while (rankSpell != 0)
        {
            var aurEff = GetAuraEffect(rankSpell, effIndex, casterGUID);

            if (aurEff != null)
                return aurEff;

            rankSpell = SpellManager.GetNextSpellInChain(rankSpell);
        }

        return null;
    }

    public List<AuraEffect> GetAuraEffectsByType(AuraType type)
    {
        return _modAuras.LookupByKey(type);
    }

    public Aura GetAuraOfRankedSpell(uint spellId)
    {
        var aurApp = GetAuraApplicationOfRankedSpell(spellId);

        return aurApp?.Base;
    }

    public AuraCollection.AuraQuery GetAuraQuery()
    {
        return _ownedAuras.Query();
    }

    public virtual SpellInfo GetCastSpellInfo(SpellInfo spellInfo)
    {
        SpellInfo FindMatchingAuraEffectIn(AuraType type)
        {
            foreach (var auraEffect in GetAuraEffectsByType(type))
            {
                var matches = auraEffect.MiscValue != 0 ? auraEffect.MiscValue == spellInfo.Id : auraEffect.IsAffectingSpell(spellInfo);

                if (matches)
                {
                    var info = SpellManager.GetSpellInfo((uint)auraEffect.Amount, Location.Map.DifficultyID);

                    if (info != null)
                        return info;
                }
            }

            return null;
        }

        var newInfo = FindMatchingAuraEffectIn(AuraType.OverrideActionbarSpells);

        if (newInfo != null)
            return newInfo;

        newInfo = FindMatchingAuraEffectIn(AuraType.OverrideActionbarSpellsTriggered);

        if (newInfo != null)
            return newInfo;

        return spellInfo;
    }

    public override uint GetCastSpellXSpellVisualId(SpellInfo spellInfo)
    {
        var visualOverrides = GetAuraEffectsByType(AuraType.OverrideSpellVisual);

        foreach (var effect in visualOverrides)
            if (effect.MiscValue == spellInfo.Id)
            {
                var visualSpell = SpellManager.GetSpellInfo((uint)effect.MiscValueB, Location.Map.DifficultyID);

                if (visualSpell != null)
                {
                    spellInfo = visualSpell;

                    break;
                }
            }

        return base.GetCastSpellXSpellVisualId(spellInfo);
    }

    public Spell GetCurrentSpell(CurrentSpellTypes spellType)
    {
        return CurrentSpells.LookupByKey(spellType);
    }

    public int GetCurrentSpellCastTime(uint spellID)
    {
        var spell = FindCurrentSpellBySpellId(spellID);

        if (spell != null)
            return spell.CastTime;

        return 0;
    }

    public DiminishingLevels GetDiminishing(DiminishingGroup group)
    {
        var diminish = _diminishing[(int)group];

        if (diminish.HitCount == 0)
            return DiminishingLevels.Level1;

        // If last spell was cast more than 18 seconds ago - reset level.
        if (diminish.Stack == 0 && Time.GetMSTimeDiffToNow(diminish.HitTime) > 18 * Time.IN_MILLISECONDS)
            return DiminishingLevels.Level1;

        return diminish.HitCount;
    }

    public uint GetDiseasesByCaster(ObjectGuid casterGUID, bool remove = false)
    {
        AuraType[] diseaseAuraTypes =
        {
            AuraType.PeriodicDamage, // Frost Fever and Blood Plague
            AuraType.Linked          // Crypt Fever and Ebon Plague
        };

        uint diseases = 0;

        foreach (var aType in diseaseAuraTypes)
        {
            if (aType == AuraType.None)
                break;

            if (!_modAuras.TryGetValue(aType, out var auras))
                continue;

            for (var i = auras.Count - 1; i >= 0; i--)
            {
                var eff = auras[i];

                // Get auras with disease dispel type by caster
                if (eff.SpellInfo.Dispel != DispelType.Disease || eff.CasterGuid != casterGUID)
                    continue;

                ++diseases;

                if (!remove)
                    continue;

                RemoveAura(eff.Id, eff.CasterGuid);
                i = 0;
            }
        }

        return diseases;
    }

    public List<DispelableAura> GetDispellableAuraList(WorldObject caster, uint dispelMask, bool isReflect = false)
    {
        List<DispelableAura> dispelList = new();

        foreach (var aura in _ownedAuras.Query().IsPassive().GetResults())
        {
            var aurApp = aura.GetApplicationOfTarget(GUID);

            if (aurApp == null)
                continue;

            if (Convert.ToBoolean(aura.SpellInfo.GetDispelMask() & dispelMask))
            {
                // do not remove positive auras if friendly target
                //               negative auras if non-friendly
                // unless we're reflecting (dispeller eliminates one of it's benefitial buffs)
                if (isReflect != (aurApp.IsPositive == WorldObjectCombat.IsFriendlyTo(caster)))
                    continue;

                // 2.4.3 Patch Notes: "Dispel effects will no longer attempt to remove effects that have 100% dispel resistance."
                var chance = aura.CalcDispelChance(this, !WorldObjectCombat.IsFriendlyTo(caster));

                if (chance == 0)
                    continue;

                // The charges / stack amounts don't count towards the total number of auras that can be dispelled.
                // Ie: A dispel on a target with 5 stacks of Winters Chill and a Polymorph has 1 / (1 + 1) . 50% chance to dispell
                // Polymorph instead of 1 / (5 + 1) . 16%.
                var dispelCharges = aura.SpellInfo.HasAttribute(SpellAttr7.DispelCharges);
                var charges = dispelCharges ? aura.Charges : aura.StackAmount;

                if (charges > 0)
                    dispelList.Add(new DispelableAura(aura, chance, charges));
            }
        }

        return dispelList;
    }

    public double GetHighestExclusiveSameEffectSpellGroupValue(AuraEffect aurEff, AuraType auraType, bool checkMiscValue = false, int miscValue = 0)
    {
        double val = 0;
        var spellGroupList = SpellManager.GetSpellSpellGroupMapBounds(aurEff.SpellInfo.GetFirstRankSpell().Id);

        foreach (var spellGroup in spellGroupList)
            if (SpellManager.GetSpellGroupStackRule(spellGroup) == SpellGroupStackRule.ExclusiveSameEffect)
            {
                var auraEffList = GetAuraEffectsByType(auraType);

                foreach (var auraEffect in auraEffList)
                    if (aurEff != auraEffect &&
                        (!checkMiscValue || auraEffect.MiscValue == miscValue) &&
                        SpellManager.IsSpellMemberOfSpellGroup(auraEffect.SpellInfo.Id, spellGroup))
                        // absolute value only
                        if (Math.Abs(val) < Math.Abs(auraEffect.Amount))
                            val = auraEffect.Amount;
            }

        return val;
    }

    public double GetMaxNegativeAuraModifier(AuraType auraType)
    {
        return GetMaxNegativeAuraModifier(auraType, _ => true);
    }

    public double GetMaxNegativeAuraModifier(AuraType auraType, Func<AuraEffect, bool> predicate)
    {
        var mTotalAuraList = GetAuraEffectsByType(auraType);

        if (mTotalAuraList.Empty())
            return 0;

        double modifier = 0;

        foreach (var aurEff in mTotalAuraList)
            if (predicate(aurEff))
                modifier = Math.Min(modifier, aurEff.Amount);

        return modifier;
    }

    public double GetMaxNegativeAuraModifierByMiscMask(AuraType auraType, uint miscMask)
    {
        return GetMaxNegativeAuraModifier(auraType, aurEff => (aurEff.MiscValue & miscMask) != 0);
    }

    public double GetMaxNegativeAuraModifierByMiscValue(AuraType auraType, int miscValue)
    {
        return GetMaxNegativeAuraModifier(auraType, aurEff => aurEff.MiscValue == miscValue);
    }

    public double GetMaxPositiveAuraModifier(AuraType auraType)
    {
        return GetMaxPositiveAuraModifier(auraType, _ => true);
    }

    public double GetMaxPositiveAuraModifier(AuraType auraType, Func<AuraEffect, bool> predicate)
    {
        var mTotalAuraList = GetAuraEffectsByType(auraType);

        if (mTotalAuraList.Empty())
            return 0;

        double modifier = 0;

        foreach (var aurEff in mTotalAuraList)
            if (predicate(aurEff))
                modifier = Math.Max(modifier, aurEff.Amount);

        return modifier;
    }

    public double GetMaxPositiveAuraModifierByMiscMask(AuraType auraType, uint miscMask, AuraEffect except = null)
    {
        return GetMaxPositiveAuraModifier(auraType, aurEff => except != aurEff && (aurEff.MiscValue & miscMask) != 0);
    }

    public ushort GetMaxSkillValueForLevel(Unit target = null)
    {
        return (ushort)(target != null ? GetLevelForTarget(target) : Level * 5);
    }

    public Aura GetOwnedAura(uint spellId)
    {
        return _ownedAuras.Query().HasSpellId(spellId).GetResults().FirstOrDefault();
    }

    public Aura GetOwnedAura(uint spellId, ObjectGuid casterGUID, ObjectGuid itemCasterGUID = default, Aura except = null)
    {
        return _ownedAuras.Query()
                          .HasSpellId(spellId)
                          .HasCasterGuid(casterGUID)
                          .AlsoMatches(aura => (itemCasterGUID.IsEmpty || aura.CastItemGuid == itemCasterGUID) &&
                                               (except == null || except != aura))
                          .GetResults()
                          .FirstOrDefault();
    }

    public double GetTotalAuraModifier(AuraType auraType)
    {
        return GetTotalAuraModifier(auraType, _ => true);
    }

    public double GetTotalAuraModifier(AuraType auraType, Func<AuraEffect, bool> predicate)
    {
        Dictionary<SpellGroup, double> sameEffectSpellGroup = new();
        double modifier = 0;

        var mTotalAuraList = GetAuraEffectsByType(auraType);

        foreach (var aurEff in mTotalAuraList)
            if (predicate(aurEff))
                // Check if the Aura Effect has a the Same Effect Stack Rule and if so, use the highest amount of that SpellGroup
                // If the Aura Effect does not have this Stack Rule, it returns false so we can add to the multiplier as usual
                if (!SpellManager.AddSameEffectStackRuleSpellGroups(aurEff.SpellInfo, auraType, aurEff.Amount, sameEffectSpellGroup))
                    modifier += aurEff.Amount;

        // Add the highest of the Same Effect Stack Rule SpellGroups to the accumulator
        foreach (var pair in sameEffectSpellGroup)
            modifier += pair.Value;

        return modifier;
    }

    public double GetTotalAuraModifierByMiscMask(AuraType auraType, int miscMask)
    {
        return GetTotalAuraModifier(auraType, aurEff => (aurEff.MiscValue & miscMask) != 0);
    }

    public double GetTotalAuraModifierByMiscValue(AuraType auraType, int miscValue)
    {
        return GetTotalAuraModifier(auraType, aurEff => aurEff.MiscValue == miscValue);
    }

    public double GetTotalAuraModValue(UnitMods unitMod)
    {
        if (unitMod >= UnitMods.End)
        {
            Log.Logger.Error("attempt to access non-existing UnitMods in GetTotalAuraModValue()!");

            return 0.0f;
        }

        var value = MathFunctions.CalculatePct(GetFlatModifierValue(unitMod, UnitModifierFlatType.Base), Math.Max(GetFlatModifierValue(unitMod, UnitModifierFlatType.BasePCTExcludeCreate), -100.0f));
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Base);
        value += GetFlatModifierValue(unitMod, UnitModifierFlatType.Total);
        value *= GetPctModifierValue(unitMod, UnitModifierPctType.Total);

        return value;
    }

    public double GetTotalAuraMultiplier(AuraType auraType)
    {
        return GetTotalAuraMultiplier(auraType, _ => true);
    }

    public double GetTotalAuraMultiplier(AuraType auraType, Func<AuraEffect, bool> predicate)
    {
        var mTotalAuraList = GetAuraEffectsByType(auraType);

        if (mTotalAuraList.Empty())
            return 1.0f;

        Dictionary<SpellGroup, double> sameEffectSpellGroup = new();
        double multiplier = 1.0f;

        foreach (var aurEff in mTotalAuraList)
            if (predicate(aurEff))
                // Check if the Aura Effect has a the Same Effect Stack Rule and if so, use the highest amount of that SpellGroup
                // If the Aura Effect does not have this Stack Rule, it returns false so we can add to the multiplier as usual
                if (!SpellManager.AddSameEffectStackRuleSpellGroups(aurEff.SpellInfo, auraType, aurEff.Amount, sameEffectSpellGroup))
                    MathFunctions.AddPct(ref multiplier, aurEff.Amount);

        // Add the highest of the Same Effect Stack Rule SpellGroups to the multiplier
        foreach (var pair in sameEffectSpellGroup)
            MathFunctions.AddPct(ref multiplier, pair.Value);

        return multiplier;
    }

    public double GetTotalAuraMultiplierByMiscMask(AuraType auraType, uint miscMask)
    {
        return GetTotalAuraMultiplier(auraType, aurEff => (aurEff.MiscValue & miscMask) != 0);
    }

    public double GetTotalAuraMultiplierByMiscValue(AuraType auraType, int miscValue)
    {
        return GetTotalAuraMultiplier(auraType, aurEff => aurEff.MiscValue == miscValue);
    }

    public void HandleSpellClick(Unit clicker, sbyte seatId = -1)
    {
        var spellClickHandled = false;

        var spellClickEntry = VehicleKit?.GetCreatureEntry() ?? Entry;
        var flags = VehicleKit != null ? TriggerCastFlags.IgnoreCasterMountedOrOnVehicle : TriggerCastFlags.None;

        var clickBounds = ObjectManager.GetSpellClickInfoMapBounds(spellClickEntry);

        foreach (var clickInfo in clickBounds)
        {
            //! First check simple relations from clicker to clickee
            if (!clickInfo.IsFitToRequirements(clicker, this))
                continue;

            //! Check database conditions
            if (!ConditionManager.IsObjectMeetingSpellClickConditions(spellClickEntry, clickInfo.SpellId, clicker, this))
                continue;

            var caster = Convert.ToBoolean(clickInfo.CastFlags & (byte)SpellClickCastFlags.CasterClicker) ? clicker : this;
            var target = Convert.ToBoolean(clickInfo.CastFlags & (byte)SpellClickCastFlags.TargetClicker) ? clicker : this;
            var origCasterGUID = Convert.ToBoolean(clickInfo.CastFlags & (byte)SpellClickCastFlags.OrigCasterOwner) ? OwnerGUID : clicker.GUID;

            var spellEntry = SpellManager.GetSpellInfo(clickInfo.SpellId, caster.Location.Map.DifficultyID);
            // if (!spellEntry) should be checked at npc_spellclick load

            if (seatId > -1)
            {
                byte i = 0;
                var valid = false;

                foreach (var spellEffectInfo in spellEntry.Effects)
                {
                    if (spellEffectInfo.ApplyAuraName == AuraType.ControlVehicle)
                    {
                        valid = true;

                        break;
                    }

                    ++i;
                }

                if (!valid)
                {
                    Log.Logger.Error("Spell {0} specified in npc_spellclick_spells is not a valid vehicle enter aura!", clickInfo.SpellId);

                    continue;
                }

                if (Location.IsInMap(caster))
                {
                    CastSpellExtraArgs args = new(flags)
                    {
                        OriginalCaster = origCasterGUID
                    };

                    args.AddSpellMod(SpellValueMod.BasePoint0 + i, seatId + 1);
                    caster.SpellFactory.CastSpell(target, clickInfo.SpellId, args);
                }
                else // This can happen during Player._LoadAuras
                {
                    Dictionary<int, double> bp = new();

                    foreach (var spellEffectInfo in spellEntry.Effects)
                        bp[spellEffectInfo.EffectIndex] = spellEffectInfo.BasePoints;

                    bp[i] = seatId;

                    AuraCreateInfo createInfo = new(ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, Location.MapId, spellEntry.Id, Location.Map.GenerateLowGuid(HighGuid.Cast)), spellEntry, Location.Map.DifficultyID, SpellConst.MaxEffects, this);
                    createInfo.Caster = clicker;
                    createInfo.SetBaseAmount(bp);
                    createInfo.CasterGuid = origCasterGUID;

                    Aura.TryRefreshStackOrCreate(createInfo);
                }
            }
            else
            {
                if (Location.IsInMap(caster))
                    caster.SpellFactory.CastSpell(target, spellEntry.Id, new CastSpellExtraArgs().SetOriginalCaster(origCasterGUID));
                else
                {
                    AuraCreateInfo createInfo = new(ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, Location.MapId, spellEntry.Id, Location.Map.GenerateLowGuid(HighGuid.Cast)), spellEntry, Location.Map.DifficultyID, SpellConst.MaxEffects, this);
                    createInfo.Caster = clicker;
                    createInfo.CasterGuid = origCasterGUID;

                    Aura.TryRefreshStackOrCreate(createInfo);
                }
            }

            spellClickHandled = true;
        }

        var creature = AsCreature;

        if (creature != null && creature.IsAIEnabled)
            creature.AI.OnSpellClick(clicker, ref spellClickHandled);
    }

    public bool HasAura<T>(T spellId) where T : struct, Enum
    {
        return GetAuraApplication(Convert.ToUInt32(spellId)).Any();
    }

    public bool HasAura(uint spellId)
    {
        return GetAuraApplication(spellId).Any();
    }

    public bool HasAura(uint spellId, ObjectGuid casterGUID, ObjectGuid itemCasterGUID = default)
    {
        return GetAuraApplication(spellId, casterGUID, itemCasterGUID) != null;
    }

    public bool HasAuraEffect(uint spellId, int effIndex, ObjectGuid casterGUID = default)
    {
        return _appliedAuras.Query().HasSpellId(spellId).HasEffectIndex(effIndex).HasCasterGuid(casterGUID).GetResults().Any();
    }

    public bool HasAuraState(AuraStateType flag, SpellInfo spellProto = null, Unit caster = null)
    {
        if (caster != null)
        {
            if (spellProto != null)
                if (caster.HasAuraTypeWithAffectMask(AuraType.AbilityIgnoreAurastate, spellProto))
                    return true;

            // Check per caster aura state
            // If aura with aurastate by caster not found return false
            if (Convert.ToBoolean((1 << (int)flag) & (int)AuraStateType.PerCasterAuraStateMask))
            {
                var range = _auraStateAuras.LookupByKey(flag);

                foreach (var auraApp in range)
                    if (auraApp.Base.CasterGuid == caster.GUID)
                        return true;

                return false;
            }
        }

        return (UnitData.AuraState & (1 << ((int)flag - 1))) != 0;
    }

    public bool HasAuraType(AuraType auraType)
    {
        return !_modAuras.LookupByKey(auraType).Empty();
    }

    public bool HasAuraTypeWithAffectMask(AuraType auraType, SpellInfo affectedSpell)
    {
        foreach (var auraEffect in GetAuraEffectsByType(auraType))
            if (auraEffect.IsAffectingSpell(affectedSpell))
                return true;

        return false;
    }

    public bool HasAuraTypeWithCaster(AuraType auraType, ObjectGuid caster)
    {
        foreach (var auraEffect in GetAuraEffectsByType(auraType))
            if (caster == auraEffect.CasterGuid)
                return true;

        return false;
    }

    public bool HasAuraTypeWithFamilyFlags(AuraType auraType, uint familyName, FlagArray128 familyFlags)
    {
        foreach (var aura in GetAuraEffectsByType(auraType))
            if (aura.SpellInfo.SpellFamilyName == (SpellFamilyNames)familyName && aura.SpellInfo.SpellFamilyFlags & familyFlags)
                return true;

        return false;
    }

    public bool HasAuraTypeWithMiscvalue(AuraType auraType, int miscvalue)
    {
        foreach (var auraEffect in GetAuraEffectsByType(auraType))
            if (miscvalue == auraEffect.MiscValue)
                return true;

        return false;
    }

    public bool HasAuraTypeWithTriggerSpell(AuraType auratype, uint triggerSpell)
    {
        foreach (var aura in GetAuraEffectsByType(auratype))
            if (aura.GetSpellEffectInfo().TriggerSpell == triggerSpell)
                return true;

        return false;
    }

    public bool HasAuraTypeWithValue(AuraType auraType, int value)
    {
        foreach (var auraEffect in GetAuraEffectsByType(auraType))
            if (value == auraEffect.Amount)
                return true;

        return false;
    }

    public bool HasAuraWithMechanic(ulong mechanicMask)
    {
        foreach (var pair in AppliedAuras)
        {
            var spellInfo = pair.Base.SpellInfo;

            if (spellInfo.Mechanic != 0 && Convert.ToBoolean(mechanicMask & (1ul << (int)spellInfo.Mechanic)))
                return true;

            foreach (var spellEffectInfo in spellInfo.Effects)
                if (spellEffectInfo != null && pair.HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.IsEffect() && spellEffectInfo.Mechanic != 0)
                    if ((mechanicMask & (1ul << (int)spellEffectInfo.Mechanic)) != 0)
                        return true;
        }

        return false;
    }

    public bool HasBreakableByDamageAuraType(AuraType type, uint excludeAura = 0)
    {
        var auras = GetAuraEffectsByType(type);

        foreach (var eff in auras)
            if ((excludeAura == 0 || excludeAura != eff.SpellInfo.Id) && //Avoid self interrupt of channeled Crowd Control spells like Seduction
                eff.SpellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.Damage))
                return true;

        return false;
    }

    public bool HasBreakableByDamageCrowdControlAura(Unit excludeCasterChannel = null)
    {
        uint excludeAura = 0;
        var currentChanneledSpell = excludeCasterChannel?.GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (currentChanneledSpell != null)
            excludeAura = currentChanneledSpell.SpellInfo.Id; //Avoid self interrupt of channeled Crowd Control spells like Seduction

        return HasBreakableByDamageAuraType(AuraType.ModConfuse, excludeAura) || HasBreakableByDamageAuraType(AuraType.ModFear, excludeAura) || HasBreakableByDamageAuraType(AuraType.ModStun, excludeAura) || HasBreakableByDamageAuraType(AuraType.ModRoot, excludeAura) || HasBreakableByDamageAuraType(AuraType.ModRoot2, excludeAura) || HasBreakableByDamageAuraType(AuraType.Transform, excludeAura);
    }

    public bool HasNegativeAuraWithInterruptFlag(SpellAuraInterruptFlags flag, ObjectGuid guid = default)
    {
        if (!HasInterruptFlag(flag))
            return false;

        foreach (var aura in _interruptableAuras)
            if (!aura.IsPositive && aura.Base.SpellInfo.HasAuraInterruptFlag(flag) && (guid.IsEmpty || aura.Base.CasterGuid == guid))
                return true;

        return false;
    }

    public bool HasNegativeAuraWithInterruptFlag(SpellAuraInterruptFlags2 flag, ObjectGuid guid = default)
    {
        if (!HasInterruptFlag(flag))
            return false;

        foreach (var aura in _interruptableAuras)
            if (!aura.IsPositive && aura.Base.SpellInfo.HasAuraInterruptFlag(flag) && (guid.IsEmpty || aura.Base.CasterGuid == guid))
                return true;

        return false;
    }

    public virtual bool HasSpell(uint spellId)
    {
        return false;
    }

    public virtual bool HasSpellFocus(Spell focusSpell = null)
    {
        return false;
    }

    public bool HasStrongerAuraWithDr(SpellInfo auraSpellInfo, Unit caster)
    {
        var diminishGroup = auraSpellInfo.DiminishingReturnsGroupForSpell;
        var level = GetDiminishing(diminishGroup);

        foreach (var aura in _appliedAuras.Query().HasDiminishGroup(diminishGroup).GetResults())
        {
            var existingDuration = aura.Base.Duration;
            var newDuration = auraSpellInfo.MaxDuration;
            ApplyDiminishingToDuration(auraSpellInfo, ref newDuration, caster, level);

            if (newDuration > 0 && newDuration < existingDuration)
                return true;
        }

        return false;
    }

    public bool HasVisibleAura(AuraApplication aurApp)
    {
        return _visibleAuras.Contains(aurApp);
    }

    public double HealBySpell(HealInfo healInfo, bool critical = false)
    {
        // calculate heal absorb and reduce healing
        UnitCombatHelpers.CalcHealAbsorb(healInfo);
        UnitCombatHelpers.DealHeal(healInfo);

        SendHealSpellLog(healInfo, critical);

        return healInfo.EffectiveHeal;
    }

    public void IncrDiminishing(SpellInfo auraSpellInfo)
    {
        var group = auraSpellInfo.DiminishingReturnsGroupForSpell;
        var currentLevel = GetDiminishing(group);
        var maxLevel = auraSpellInfo.DiminishingReturnsMaxLevel;

        if (currentLevel < maxLevel)
            _diminishing[(int)group].HitCount = currentLevel + 1;
    }

    // Interrupts
    public bool InterruptNonMeleeSpells(bool withDelayed, uint spellID = 0, bool withInstant = true)
    {
        var retval = false;

        // generic spells are interrupted if they are not finished or delayed
        if (GetCurrentSpell(CurrentSpellTypes.Generic) != null && (spellID == 0 || CurrentSpells[CurrentSpellTypes.Generic].SpellInfo.Id == spellID))
            if (InterruptSpell(CurrentSpellTypes.Generic, withDelayed, withInstant) != null)
                retval = true;

        // autorepeat spells are interrupted if they are not finished or delayed
        if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null && (spellID == 0 || CurrentSpells[CurrentSpellTypes.AutoRepeat].SpellInfo.Id == spellID))
            if (InterruptSpell(CurrentSpellTypes.AutoRepeat, withDelayed, withInstant) != null)
                retval = true;

        // channeled spells are interrupted if they are not finished, even if they are delayed
        if (GetCurrentSpell(CurrentSpellTypes.Channeled) == null || (spellID != 0 && CurrentSpells[CurrentSpellTypes.Channeled].SpellInfo.Id != spellID))
            return retval;

        if (InterruptSpell(CurrentSpellTypes.Channeled) != null)
            retval = true;

        return retval;
    }

    public Spell InterruptSpell(CurrentSpellTypes spellType, bool withDelayed = true, bool withInstant = true, Spell interruptingSpell = null)
    {
        Log.Logger.Debug("Interrupt spell for unit {0}", Entry);
        var spell = CurrentSpells.LookupByKey(spellType);

        if (spell == null || (!withDelayed && spell.State == SpellState.Delayed) || (!withInstant && spell.CastTime <= 0 && spell.State != SpellState.Casting))
            return null;

        // for example, do not let self-stun aura interrupt itself
        if (!spell.IsInterruptable)
            return null;

        // send autorepeat cancel message for autorepeat spells
        if (spellType == CurrentSpellTypes.AutoRepeat)
            if (IsTypeId(TypeId.Player))
                AsPlayer.SendAutoRepeatCancel(this);

        if (spell.State != SpellState.Finished)
            spell.Cancel();
        else
        {
            CurrentSpells[spellType] = null;
            spell.SetReferencedFromCurrent(false);
        }

        if (IsCreature && IsAIEnabled)
            AsCreature.AI.OnSpellFailed(spell.SpellInfo);

        ScriptManager.ForEach<IUnitSpellInterrupted>(s => s.SpellInterrupted(spell, interruptingSpell));

        return spell;
    }

    public bool IsHighestExclusiveAura(Aura aura, bool removeOtherAuraApplications = false)
    {
        foreach (var aurEff in aura.AuraEffects)
            if (!IsHighestExclusiveAuraEffect(aura.SpellInfo, aurEff.Value.AuraType, aurEff.Value.Amount, aura.AuraEffects.Keys.ToHashSet(), removeOtherAuraApplications))
                return false;

        return true;
    }

    public bool IsHighestExclusiveAuraEffect(SpellInfo spellInfo, AuraType auraType, double effectAmount, HashSet<int> auraEffectMask, bool removeOtherAuraApplications = false)
    {
        var auras = GetAuraEffectsByType(auraType);

        foreach (var existingAurEff in auras)
            if (SpellManager.CheckSpellGroupStackRules(spellInfo, existingAurEff.SpellInfo) == SpellGroupStackRule.ExclusiveHighest)
            {
                var diff = Math.Abs(effectAmount) - Math.Abs(existingAurEff.Amount);
                var effMask = auraEffectMask.ToMask();
                var baseMask = existingAurEff.Base.AuraEffects.Keys.ToMask();

                if (diff == 0)
                    diff = spellInfo.Effects.Aggregate(diff, (current, spellEff) => current + (((effMask & (1 << spellEff.EffectIndex)) >> spellEff.EffectIndex) - ((baseMask & (1 << spellEff.EffectIndex)) >> spellEff.EffectIndex)));

                switch (diff)
                {
                    case > 0:
                    {
                        var auraBase = existingAurEff.Base;

                        // no removing of area auras from the original owner, as that completely cancels them
                        if (removeOtherAuraApplications && (!auraBase.IsArea() || auraBase.Owner != this))
                        {
                            var aurApp = existingAurEff.Base.GetApplicationOfTarget(GUID);

                            if (aurApp != null)
                                RemoveAura(aurApp);
                        }

                        break;
                    }
                    case < 0:
                        return false;
                }
            }

        return true;
    }

    public bool IsImmunedToDamage(SpellSchoolMask schoolMask)
    {
        if (schoolMask == SpellSchoolMask.None)
            return false;

        // If m_immuneToSchool type contain this school type, IMMUNE damage.
        var schoolImmunityMask = SchoolImmunityMask;

        if (((SpellSchoolMask)schoolImmunityMask & schoolMask) == schoolMask) // We need to be immune to all types
            return true;

        // If m_immuneToDamage type contain magic, IMMUNE damage.
        var damageImmunityMask = DamageImmunityMask;

        return ((SpellSchoolMask)damageImmunityMask & schoolMask) == schoolMask; // We need to be immune to all types
    }

    public bool IsImmunedToDamage(SpellInfo spellInfo)
    {
        if (spellInfo == null)
            return false;

        // for example 40175
        if (spellInfo.HasAttribute(SpellAttr0.NoImmunities) && spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
            return false;

        if (spellInfo.HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) || spellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
            return false;

        var schoolMask = (uint)spellInfo.GetSchoolMask();

        if (schoolMask != 0)
        {
            // If m_immuneToSchool type contain this school type, IMMUNE damage.
            uint schoolImmunityMask = 0;
            var schoolList = _spellImmune[SpellImmunity.School];

            foreach (var pair in schoolList.KeyValueList)
                if (Convert.ToBoolean(pair.Key & schoolMask) && !spellInfo.CanPierceImmuneAura(SpellManager.GetSpellInfo(pair.Value, Location.Map.DifficultyID)))
                    schoolImmunityMask |= pair.Key;

            // // We need to be immune to all types
            if ((schoolImmunityMask & schoolMask) == schoolMask)
                return true;

            // If m_immuneToDamage type contain magic, IMMUNE damage.
            var damageImmunityMask = DamageImmunityMask;

            if ((damageImmunityMask & schoolMask) == schoolMask) // We need to be immune to all types
                return true;
        }

        return false;
    }

    public bool IsImmunedToSpell(SpellInfo spellInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
    {
        if (spellInfo == null)
            return false;

        bool HasImmunity(MultiMap<uint, uint> container, uint key)
        {
            var range = container.LookupByKey(key);

            if (!requireImmunityPurgesEffectAttribute)
                return !range.Empty();

            return range.Any(entry =>
            {
                var immunitySourceSpell = SpellManager.GetSpellInfo(entry);

                return immunitySourceSpell != null && immunitySourceSpell.HasAttribute(SpellAttr1.ImmunityPurgesEffect);
            });
        }

        // Single spell immunity.
        var idList = _spellImmune[SpellImmunity.Id];

        if (HasImmunity(idList, spellInfo.Id))
            return true;

        if (spellInfo.HasAttribute(SpellAttr0.NoImmunities))
            return false;

        var dispel = (uint)spellInfo.Dispel;

        if (dispel != 0)
        {
            var dispelList = _spellImmune[SpellImmunity.Dispel];

            if (HasImmunity(dispelList, dispel))
                return true;
        }

        // Spells that don't have effectMechanics.
        var mechanic = (uint)spellInfo.Mechanic;

        if (mechanic != 0)
        {
            var mechanicList = _spellImmune[SpellImmunity.Mechanic];

            if (HasImmunity(mechanicList, mechanic))
                return true;
        }

        var immuneToAllEffects = true;

        foreach (var spellEffectInfo in spellInfo.Effects)
        {
            // State/effect immunities applied by aura expect full spell immunity
            // Ignore effects with mechanic, they are supposed to be checked separately
            if (!spellEffectInfo.IsEffect())
                continue;

            if (!IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute))
            {
                immuneToAllEffects = false;

                break;
            }

            if (spellInfo.HasAttribute(SpellAttr4.NoPartialImmunity))
                return true;
        }

        if (immuneToAllEffects) //Return immune only if the target is immune to all spell effects.
            return true;

        var schoolMask = (uint)spellInfo.GetSchoolMask();

        if (schoolMask != 0)
        {
            uint schoolImmunityMask = 0;
            var schoolList = _spellImmune[SpellImmunity.School];

            foreach (var pair in schoolList.KeyValueList)
            {
                if ((pair.Key & schoolMask) == 0)
                    continue;

                var immuneSpellInfo = SpellManager.GetSpellInfo(pair.Value, Location.Map.DifficultyID);

                if (requireImmunityPurgesEffectAttribute)
                    if (immuneSpellInfo == null || !immuneSpellInfo.HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                        continue;

                // Consider the school immune if any of these conditions are not satisfied.
                // In case of no immuneSpellInfo, ignore that condition and check only the other conditions
                if ((immuneSpellInfo != null && !immuneSpellInfo.IsPositive) || !spellInfo.IsPositive || caster == null || !WorldObjectCombat.IsFriendlyTo(caster))
                    if (!spellInfo.CanPierceImmuneAura(immuneSpellInfo))
                        schoolImmunityMask |= pair.Key;
            }

            if ((schoolImmunityMask & schoolMask) == schoolMask)
                return true;
        }

        return false;
    }

    public virtual bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
    {
        if (spellInfo == null)
            return false;

        if (spellInfo.HasAttribute(SpellAttr0.NoImmunities))
            return false;

        bool HasImmunity(MultiMap<uint, uint> container, uint key)
        {
            var range = container.LookupByKey(key);

            if (!requireImmunityPurgesEffectAttribute)
                return !range.Empty();

            return range.Any(entry =>
            {
                var immunitySourceSpell = SpellManager.GetSpellInfo(entry);

                return immunitySourceSpell != null && immunitySourceSpell.HasAttribute(SpellAttr1.ImmunityPurgesEffect);
            });
        }

        // If m_immuneToEffect type contain this effect type, IMMUNE effect.
        var effectList = _spellImmune[SpellImmunity.Effect];

        if (HasImmunity(effectList, (uint)spellEffectInfo.Effect))
            return true;

        var mechanic = (uint)spellEffectInfo.Mechanic;

        if (mechanic != 0)
        {
            var mechanicList = _spellImmune[SpellImmunity.Mechanic];

            if (HasImmunity(mechanicList, mechanic))
                return true;
        }

        var aura = spellEffectInfo.ApplyAuraName;

        if (aura == 0)
            return false;

        if (!spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
        {
            var list = _spellImmune[SpellImmunity.State];

            if (HasImmunity(list, (uint)aura))
                return true;
        }

        if (spellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
            return false;

        // Check for immune to application of harmful magical effects
        var immuneAuraApply = GetAuraEffectsByType(AuraType.ModImmuneAuraApplySchool);

        foreach (var auraEffect in immuneAuraApply)
            if (Convert.ToBoolean(auraEffect.MiscValue & (int)spellInfo.GetSchoolMask()) &&                                                // Check school
                ((caster != null && !WorldObjectCombat.IsFriendlyTo(caster)) || !spellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))) // Harmful
                return true;

        return false;
    }

    /// <summary>
    ///     Check if our current channel spell has attribute SPELL_ATTR5_CAN_CHANNEL_WHEN_MOVING
    /// </summary>
    public virtual bool IsMovementPreventedByCasting()
    {
        // can always move when not casting
        if (!HasUnitState(UnitState.Casting))
            return false;

        var spell = GetCurrentSpell(CurrentSpellTypes.Generic);

        if (spell != null)
            if (CanCastSpellWhileMoving(spell.SpellInfo))
                return false;

        // channeled spells during channel stage (after the initial cast timer) allow movement with a specific spell attribute
        spell = CurrentSpells.LookupByKey(CurrentSpellTypes.Channeled);

        if (spell == null)
            return true;

        if (spell.State != SpellState.Finished && spell.IsChannelActive)
            if (spell.SpellInfo.IsMoveAllowedChannel || CanCastSpellWhileMoving(spell.SpellInfo))
                return false;

        // prohibit movement for all other spell casts
        return true;
    }

    public bool IsNonMeleeSpellCast(bool withDelayed, bool skipChanneled = false, bool skipAutorepeat = false, bool isAutoshoot = false, bool skipInstant = true)
    {
        // We don't do loop here to explicitly show that melee spell is excluded.
        // Maybe later some special spells will be excluded too.

        // generic spells are cast when they are not finished and not delayed
        var currentSpell = GetCurrentSpell(CurrentSpellTypes.Generic);

        if (currentSpell != null &&
            currentSpell.State != SpellState.Finished &&
            (withDelayed || currentSpell.State != SpellState.Delayed))
            if (!skipInstant || currentSpell.CastTime != 0)
                if (!isAutoshoot || !currentSpell.SpellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
                    return true;

        currentSpell = GetCurrentSpell(CurrentSpellTypes.Channeled);

        // channeled spells may be delayed, but they are still considered cast
        if (!skipChanneled &&
            currentSpell != null &&
            currentSpell.State != SpellState.Finished)
            if (!isAutoshoot || !currentSpell.SpellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
                return true;

        currentSpell = GetCurrentSpell(CurrentSpellTypes.AutoRepeat);

        // autorepeat spells may be finished or delayed, but they are still considered cast
        if (!skipAutorepeat && currentSpell != null)
            return true;

        return false;
    }

    public AuraEffect IsScriptOverriden(SpellInfo spell, int script)
    {
        var auras = GetAuraEffectsByType(AuraType.OverrideClassScripts);

        foreach (var eff in auras)
            if (eff.MiscValue == script)
                if (eff.IsAffectingSpell(spell))
                    return eff;

        return null;
    }

    // Melee based spells hit result calculations
    public SpellMissInfo MeleeSpellHitResult(Unit victim, SpellInfo spellInfo)
    {
        if (spellInfo.HasAttribute(SpellAttr3.NoAvoidance))
            return SpellMissInfo.None;

        var attType = WeaponAttackType.BaseAttack;

        // Check damage class instead of attack type to correctly handle judgements
        // - they are meele, but can't be dodged/parried/deflected because of ranged dmg class
        if (spellInfo.DmgClass == SpellDmgClass.Ranged)
            attType = WeaponAttackType.RangedAttack;

        var roll = RandomHelper.IRand(0, 9999);

        var missChance = MeleeSpellMissChance(victim, attType, spellInfo) * 100.0f;
        // Roll miss
        var tmp = missChance;

        if (roll < tmp)
            return SpellMissInfo.Miss;

        // Chance resist mechanic
        var resistChance = victim.GetMechanicResistChance(spellInfo) * 100;
        tmp += resistChance;

        if (roll < tmp)
            return SpellMissInfo.Resist;

        // Same spells cannot be parried/dodged
        if (spellInfo.HasAttribute(SpellAttr0.NoActiveDefense))
            return SpellMissInfo.None;

        var canDodge = !spellInfo.HasAttribute(SpellAttr7.NoAttackDodge);
        var canParry = !spellInfo.HasAttribute(SpellAttr7.NoAttackParry);
        var canBlock = true;

        // if victim is casting or cc'd it can't avoid attacks
        if (victim.IsNonMeleeSpellCast(false, false, true) || victim.HasUnitState(UnitState.Controlled))
        {
            canDodge = false;
            canParry = false;
            canBlock = false;
        }

        // Ranged attacks can only miss, resist and deflect and get blocked
        if (attType == WeaponAttackType.RangedAttack)
        {
            canParry = false;
            canDodge = false;

            // only if in front
            if (!victim.HasUnitState(UnitState.Controlled) && (victim.Location.HasInArc(MathFunctions.PI, Location) || victim.HasAuraType(AuraType.IgnoreHitDirection)))
            {
                var deflectChance = victim.GetTotalAuraModifier(AuraType.DeflectSpells) * 100;
                tmp += deflectChance;

                if (roll < tmp)
                    return SpellMissInfo.Deflect;
            }
        }

        // Check for attack from behind
        if (!victim.Location.HasInArc(MathFunctions.PI, Location))
        {
            if (!victim.HasAuraType(AuraType.IgnoreHitDirection))
            {
                // Can`t dodge from behind in PvP (but its possible in PvE)
                if (victim.IsTypeId(TypeId.Player))
                    canDodge = false;

                // Can`t parry or block
                canParry = false;
                canBlock = false;
            }
            else // Only deterrence as of 3.3.5
            {
                if (spellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget))
                    canParry = false;
            }
        }

        // Ignore combat result aura
        var ignore = GetAuraEffectsByType(AuraType.IgnoreCombatResult);

        foreach (var aurEff in ignore)
        {
            if (!aurEff.IsAffectingSpell(spellInfo))
                continue;

            switch ((MeleeHitOutcome)aurEff.MiscValue)
            {
                case MeleeHitOutcome.Dodge:
                    canDodge = false;

                    break;

                case MeleeHitOutcome.Block:
                    canBlock = false;

                    break;

                case MeleeHitOutcome.Parry:
                    canParry = false;

                    break;

                default:
                    Log.Logger.Debug("Spell {0} SPELL_AURA_IGNORE_COMBAT_RESULT has unhandled state {1}", aurEff.Id, aurEff.MiscValue);

                    break;
            }
        }

        if (canDodge)
        {
            // Roll dodge
            var dodgeChance = GetUnitDodgeChance(attType, victim) * 100.0f;

            if (dodgeChance < 0)
                dodgeChance = 0;

            if (roll < (tmp += dodgeChance))
                return SpellMissInfo.Dodge;
        }

        if (canParry)
        {
            // Roll parry
            var parryChance = GetUnitParryChance(attType, victim) * 100.0f;

            if (parryChance < 0)
                parryChance = 0;

            tmp += parryChance;

            if (roll < tmp)
                return SpellMissInfo.Parry;
        }

        if (!canBlock)
            return SpellMissInfo.None;

        var blockChance = GetUnitBlockChance(victim) * 100.0f;

        if (blockChance < 0)
            blockChance = 0;

        tmp += blockChance;

        return roll < tmp ? SpellMissInfo.Block : SpellMissInfo.None;
    }

    public void ModifyAuraState(AuraStateType flag, bool apply)
    {
        var mask = 1u << ((int)flag - 1);

        if (apply)
        {
            if ((UnitData.AuraState & mask) != 0)
                return;

            SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AuraState), mask);

            if (IsTypeId(TypeId.Player))
            {
                var spList = AsPlayer.GetSpellMap();

                foreach (var spell in spList)
                {
                    if (spell.Value.State == PlayerSpellState.Removed || spell.Value.Disabled)
                        continue;

                    var spellInfo = SpellManager.GetSpellInfo(spell.Key);

                    if (spellInfo is not { IsPassive: true })
                        continue;

                    if (spellInfo.CasterAuraState == flag)
                        SpellFactory.CastSpell(this, spell.Key, true);
                }
            }
            else if (IsPet)
            {
                var pet = AsPet;

                foreach (var spell in pet.Spells)
                {
                    if (spell.Value.State == PetSpellState.Removed)
                        continue;

                    var spellInfo = SpellManager.GetSpellInfo(spell.Key);

                    if (spellInfo is not { IsPassive: true })
                        continue;

                    if (spellInfo.CasterAuraState == flag)
                        SpellFactory.CastSpell(this, spell.Key, true);
                }
            }
        }
        else
        {
            if ((UnitData.AuraState & mask) != 0)
            {
                RemoveUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.AuraState), mask);

                _appliedAuras.Query()
                             .HasCasterGuid(GUID)
                             .HasCasterAuraState(flag)
                             .AlsoMatches(app => app.Base.SpellInfo.IsPassive || flag != AuraStateType.Enraged)
                             .Execute(RemoveAura);
            }
        }
    }

    public void ProcSkillsAndReactives(bool isVictim, Unit procTarget, ProcFlagsInit typeMask, ProcFlagsHit hitMask, WeaponAttackType attType)
    {
        // Player is loaded now - do not allow passive spell casts to proc
        if (IsPlayer && AsPlayer.Session.PlayerLoading)
            return;

        // For melee/ranged based attack need update skills and set some Aura states if victim present
        if (typeMask.HasFlag(ProcFlags.MeleeBasedTriggerMask) && procTarget != null)
            // If exist crit/parry/dodge/block need update aura state (for victim and attacker)
            if (hitMask.HasAnyFlag(ProcFlagsHit.Critical | ProcFlagsHit.Parry | ProcFlagsHit.Dodge | ProcFlagsHit.Block))
                // for victim
                if (isVictim)
                {
                    // if victim and dodge attack
                    if (hitMask.HasAnyFlag(ProcFlagsHit.Dodge))
                        // Update AURA_STATE on dodge
                        if (Class != PlayerClass.Rogue) // skip Rogue Riposte
                        {
                            ModifyAuraState(AuraStateType.Defensive, true);
                            StartReactiveTimer(ReactiveType.Defense);
                        }

                    // if victim and parry attack
                    if (hitMask.HasAnyFlag(ProcFlagsHit.Parry))
                    {
                        ModifyAuraState(AuraStateType.Defensive, true);
                        StartReactiveTimer(ReactiveType.Defense);
                    }

                    // if and victim block attack
                    if (hitMask.HasAnyFlag(ProcFlagsHit.Block))
                    {
                        ModifyAuraState(AuraStateType.Defensive, true);
                        StartReactiveTimer(ReactiveType.Defense);
                    }
                }
    }

    public void RegisterAuraEffect(AuraEffect aurEff, bool apply)
    {
        if (apply)
            _modAuras.Add(aurEff.AuraType, aurEff);
        else
            _modAuras.Remove(aurEff.AuraType, aurEff);
    }

    public void RemoveAllAuras()
    {
        // this may be a dead loop if some events on aura remove will continiously apply aura on remove
        // we want to have all auras removed, so use your brain when linking events
        for (var counter = 0; !_appliedAuras.Empty() || !_ownedAuras.Empty(); counter++)
        {
            foreach (var aurAppIter in _appliedAuras.AuraApplications)
                UnapplyAura(aurAppIter, AuraRemoveMode.Default);

            foreach (var aurIter in _ownedAuras.Auras)
                RemoveOwnedAura(aurIter);

            const int maxIteration = 50;

            // give this loop a few tries, if there are still auras then log as much information as possible
            if (counter >= maxIteration)
            {
                StringBuilder sstr = new();
                sstr.AppendLine($"Unit::RemoveAllAuras() iterated {maxIteration} times already but there are still {_appliedAuras.Count} m_appliedAuras and {_ownedAuras.Count} m_ownedAuras. Details:");
                sstr.AppendLine(GetDebugInfo());

                if (!_appliedAuras.Empty())
                {
                    sstr.AppendLine("m_appliedAuras:");

                    foreach (var auraAppPair in _appliedAuras.AuraApplications)
                        sstr.AppendLine(auraAppPair.GetDebugInfo());
                }

                if (!_ownedAuras.Empty())
                {
                    sstr.AppendLine("m_ownedAuras:");

                    foreach (var auraPair in _ownedAuras.Auras)
                        sstr.AppendLine(auraPair.GetDebugInfo());
                }

                Log.Logger.Error(sstr.ToString());

                break;
            }
        }
    }

    public void RemoveAllAurasOnDeath()
    {
        // used just after dieing to remove all visible auras
        // and disable the mods for the passive ones
        _appliedAuras.Query().IsDeathPersistant(false).IsPassive(false).Execute(UnapplyAura, AuraRemoveMode.Death);
        _ownedAuras.Query().IsDeathPersistant(false).IsPassive(false).Execute(RemoveOwnedAura, AuraRemoveMode.Death);
    }

    public void RemoveAllAurasRequiringDeadTarget()
    {
        _appliedAuras.Query().IsPassive(false).IsRequiringDeadTarget().Execute(UnapplyAura);
        _ownedAuras.Query().IsPassive(false).IsRequiringDeadTarget().Execute(RemoveOwnedAura);
    }

    public void RemoveAllAuraStatMods()
    {
        foreach (var i in AppliedAuras)
            i.Base.HandleAllEffects(i, AuraEffectHandleModes.Stat, false);
    }

    public void RemoveAllGroupBuffsFromCaster(ObjectGuid casterGUID)
    {
        _ownedAuras.Query().HasCasterGuid(casterGUID).IsGroupBuff().Execute(RemoveOwnedAura);
    }

    public void RemoveAppliedAuras(Func<AuraApplication, bool> check, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        AppliedAuras.CallOnMatch((pair) => check(pair), (pair) => RemoveAura(pair, removeMode));
    }

    public void RemoveArenaAuras()
    {
        // in join, remove positive buffs, on end, remove negative
        // used to remove positive visible auras in arenas
        RemoveAppliedAuras(aurApp =>
        {
            var aura = aurApp.Base;

            return (!aura.SpellInfo.HasAttribute(SpellAttr4.AllowEnteringArena) // don't remove stances, shadowform, pally/hunter auras
                    &&
                    !aura.IsPassive // don't remove passive auras
                    &&
                    (aurApp.IsPositive || !aura.SpellInfo.HasAttribute(SpellAttr3.AllowAuraWhileDead))) || // not negative death persistent auras
                   aura.SpellInfo.HasAttribute(SpellAttr5.RemoveEnteringArena);                            // special marker, always remove
        });
    }

    public void RemoveAura(KeyValuePair<uint, AuraApplication> appMap, AuraRemoveMode mode = AuraRemoveMode.Default)
    {
        RemoveAura(appMap.Value, mode);
    }

    public void RemoveAura(uint spellId)
    {
        _appliedAuras.Query().HasSpellId(spellId).Execute(RemoveAura);
    }

    public void RemoveAura<T>(T spellId) where T : struct, Enum
    {
        RemoveAura(Convert.ToUInt32(spellId));
    }

    public void RemoveAura<T>(T spellId, ObjectGuid caster, AuraRemoveMode removeMode = AuraRemoveMode.Default) where T : struct, Enum
    {
        RemoveAura(Convert.ToUInt32(spellId), caster, removeMode);
    }

    public void RemoveAura(uint spellId, ObjectGuid caster, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        _appliedAuras.Query().HasSpellId(spellId).HasCasterGuid(caster).Execute(RemoveAura, removeMode);
    }

    public void RemoveAura(AuraApplication aurApp, AuraRemoveMode mode = AuraRemoveMode.Default)
    {
        if (aurApp == null)
            return;

        // we've special situation here, RemoveAura called while during aura removal
        // this kind of call is needed only when aura effect removal handler
        // or event triggered by it expects to remove
        // not yet removed effects of an aura
        if (aurApp.HasRemoveMode)
        {
            // remove remaining effects of an aura
            foreach (var eff in aurApp.Base.AuraEffects)
                if (aurApp.HasEffect(eff.Key))
                    aurApp._HandleEffect(eff.Key, false);

            return;
        }

        // no need to remove
        if (aurApp.Base.GetApplicationOfTarget(GUID) != aurApp || aurApp.Base.IsRemoved)
            return;

        RemoveAuraBase(aurApp, mode);
    }

    public void RemoveAura(Aura aura, AuraRemoveMode mode = AuraRemoveMode.Default)
    {
        if (aura.IsRemoved)
            return;

        var aurApp = aura.GetApplicationOfTarget(GUID);

        if (aurApp != null)
            RemoveAura(aurApp, mode);
    }

    public void RemoveAuraApplicationCount(uint spellId, ushort count = 1)
    {
        _ownedAuras.Query().HasSpellId(spellId).ForEachResult(aura => aura.ModStackAmount(-count));
    }

    public void RemoveAuraBase(AuraApplication aurApp, AuraRemoveMode mode = AuraRemoveMode.Default)
    {
        // Do not remove aura which is already being removed
        if (aurApp.HasRemoveMode)
            return;

        var aura = aurApp.Base;
        UnapplyAura(aurApp, mode);

        // Remove aura - for Area and Target auras
        if (aura.Owner == this)
            aura.Remove(mode);
    }

    public void RemoveAuraFromStack(uint spellId, ObjectGuid casterGUID = default, AuraRemoveMode removeMode = AuraRemoveMode.Default, ushort num = 1)
    {
        _ownedAuras.Query()
                   .HasSpellId(spellId)
                   .HasCasterGuid(casterGUID)
                   .HasAuraType(AuraObjectType.Unit)
                   .ForEachResult(aura => aura.ModStackAmount(-num, removeMode));
    }

    public void RemoveAurasByShapeShift()
    {
        ulong mechanicMask = (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Root);

        AppliedAuras
            .CallOnMatch((auraApp) =>
                         {
                             var aura = auraApp.Base;

                             if ((aura.SpellInfo.GetAllEffectsMechanicMask() & mechanicMask) != 0 && !aura.SpellInfo.HasAttribute(SpellCustomAttributes.AuraCC))
                                 return true;

                             return false;
                         },
                         (auraApp) => RemoveAura(auraApp));
    }

    public void RemoveAurasByType(AuraType auraType, ObjectGuid casterGUID = default, Aura except = null, bool negative = true, bool positive = true)
    {
        if (_modAuras.TryGetValue(auraType, out var auras))
            for (var i = auras.Count - 1; i >= 0; i--)
            {
                var aura = auras[i].Base;
                var aurApp = aura.GetApplicationOfTarget(GUID);

                if (aura != except && (casterGUID.IsEmpty || aura.CasterGuid == casterGUID) && ((negative && !aurApp.IsPositive) || (positive && aurApp.IsPositive)))
                {
                    var removedAuras = RemovedAurasCount;
                    RemoveAura(aurApp);

                    if (RemovedAurasCount > removedAuras + 1)
                        i = 0;
                }
            }
    }

    public void RemoveAurasByType(AuraType auraType, Func<AuraApplication, bool> check, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        if (_modAuras.TryGetValue(auraType, out var auras))
            for (var i = auras.Count - 1; i >= 0; i--)
            {
                var aura = auras[i].Base;
                var aurApp = aura.GetApplicationOfTarget(GUID);

                if (check(aurApp))
                {
                    var removedAuras = RemovedAurasCount;
                    RemoveAura(aurApp, removeMode);

                    if (RemovedAurasCount > removedAuras + 1)
                        i = 0;
                }
            }
    }

    public void RemoveAurasDueToItemSpell(uint spellId, ObjectGuid castItemGuid)
    {
        _appliedAuras.Query().HasSpellId(spellId).HasCastItemGuid(castItemGuid).Execute(RemoveAura);
    }

    public void RemoveAurasDueToSpell(uint spellId, ObjectGuid casterGUID, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        _appliedAuras.Query().HasSpellId(spellId).HasCasterGuid(casterGUID).Execute(RemoveAura, removeMode);
    }

    public void RemoveAurasDueToSpellByDispel(uint spellId, uint dispellerSpellId, ObjectGuid casterGUID, WorldObject dispeller, byte chargesRemoved = 1)
    {
        var aura = _ownedAuras.Query().HasSpellId(spellId).HasCasterGuid(casterGUID).GetResults().FirstOrDefault();

        if (aura != null)
        {
            DispelInfo dispelInfo = new(dispeller, dispellerSpellId, chargesRemoved);

            // Call OnDispel hook on AuraScript
            aura.CallScriptDispel(dispelInfo);

            if (aura.SpellInfo.HasAttribute(SpellAttr7.DispelCharges))
                aura.ModCharges(-dispelInfo.GetRemovedCharges(), AuraRemoveMode.EnemySpell);
            else
                aura.ModStackAmount(-dispelInfo.GetRemovedCharges(), AuraRemoveMode.EnemySpell);

            // Call AfterDispel hook on AuraScript
            aura.CallScriptAfterDispel(dispelInfo);
        }
    }

    public void RemoveAurasDueToSpellBySteal(uint spellId, ObjectGuid casterGUID, WorldObject stealer, int stolenCharges = 1)
    {
        foreach (var aura in _ownedAuras.Query().HasSpellId(spellId).HasCasterGuid(casterGUID).GetResults())
        {
            Dictionary<int, double> damage = new();
            Dictionary<int, double> baseDamage = new();
            var effMask = new HashSet<int>();
            uint recalculateMask = 0;
            var caster = aura.Caster;

            foreach (var effect in aura.AuraEffects)
            {
                var index = effect.Value.EffIndex;
                baseDamage[index] = effect.Value.BaseAmount;
                damage[index] = effect.Value.Amount;
                effMask.Add(index);

                if (effect.Value.CanBeRecalculated())
                    recalculateMask |= 1u << index;
            }

            var stealCharge = aura.SpellInfo.HasAttribute(SpellAttr7.DispelCharges);
            // Cast duration to unsigned to prevent permanent aura's such as Righteous Fury being permanently added to caster
            var dur = (uint)Math.Min(2u * Time.MINUTE * Time.IN_MILLISECONDS, aura.Duration);

            var unitStealer = stealer.AsUnit;

            if (unitStealer != null)
            {
                var oldAura = unitStealer.GetAura(aura.Id, aura.CasterGuid);

                if (oldAura != null)
                {
                    if (stealCharge)
                        oldAura.ModCharges(stolenCharges);
                    else
                        oldAura.ModStackAmount(stolenCharges);

                    oldAura.SetDuration((int)dur);
                }
                else
                {
                    // single target state must be removed before aura creation to preserve existing single target aura
                    if (aura.IsSingleTarget)
                        aura.UnregisterSingleTarget();

                    AuraCreateInfo createInfo = new(aura.CastId, aura.SpellInfo, aura.CastDifficulty, effMask, stealer);
                    createInfo.CasterGuid = aura.CasterGuid;
                    createInfo.SetBaseAmount(baseDamage);

                    var newAura = Aura.TryRefreshStackOrCreate(createInfo);

                    if (newAura != null)
                    {
                        // created aura must not be single target aura, so stealer won't loose it on recast
                        if (newAura.IsSingleTarget)
                        {
                            newAura.UnregisterSingleTarget();
                            // bring back single target aura status to the old aura
                            aura.IsSingleTarget = true;
                            caster.SingleCastAuras.Add(aura);
                        }

                        // FIXME: using aura.GetMaxDuration() maybe not blizzlike but it fixes stealing of spells like Innervate
                        newAura.SetLoadedState(aura.MaxDuration, (int)dur, stealCharge ? stolenCharges : aura.Charges, (byte)stolenCharges, recalculateMask, damage);
                        newAura.ApplyForTargets();
                    }
                }
            }

            if (stealCharge)
                aura.ModCharges(-stolenCharges, AuraRemoveMode.EnemySpell);
            else
                aura.ModStackAmount(-stolenCharges, AuraRemoveMode.EnemySpell);

            return;
        }
    }

    public void RemoveAurasOnEvade()
    {
        if (IsCharmedOwnedByPlayerOrPlayer) // if it is a player owned creature it should not remove the aura
            return;

        // don't remove vehicle auras, passengers aren't supposed to drop off the vehicle
        // don't remove clone caster on evade (to be verified)
        bool EvadeAuraCheck(Aura aura)
        {
            if (aura.HasEffectType(AuraType.ControlVehicle))
                return false;

            if (aura.HasEffectType(AuraType.CloneCaster))
                return false;

            return !aura.SpellInfo.HasAttribute(SpellAttr1.AuraStaysAfterCombat);
        }

        bool EvadeAuraApplicationCheck(AuraApplication aurApp)
        {
            return EvadeAuraCheck(aurApp.Base);
        }

        RemoveAppliedAuras(EvadeAuraApplicationCheck);
        RemoveOwnedAuras(EvadeAuraCheck);
    }

    public void RemoveAurasWithAttribute(SpellAttr0 flags)
    {
        switch (flags)
        {
            case SpellAttr0.OnlyIndoors:
                _appliedAuras.Query().OnlyIndoors().Execute(RemoveAura);

                break;

            case SpellAttr0.OnlyOutdoors:
                _appliedAuras.Query().OnlyOutdoors().Execute(RemoveAura);

                break;

            default:
                foreach (var app in _appliedAuras.AuraApplications)
                    if (app.Base.SpellInfo.HasAttribute(flags))
                        RemoveAura(app);

                break;
        }
    }

    public void RemoveAurasWithFamily(SpellFamilyNames family, FlagArray128 familyFlag, ObjectGuid casterGUID)
    {
        _appliedAuras.Query()
                     .HasCasterGuid(casterGUID)
                     .HasSpellFamily(family)
                     .AlsoMatches(a => a.Base.SpellInfo.SpellFamilyFlags & familyFlag)
                     .Execute(RemoveAura);
    }

    public void RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags flag, SpellInfo source = null)
    {
        if (!HasInterruptFlag(flag))
            return;

        // interrupt auras
        for (var i = 0; i < _interruptableAuras.Count; i++)
        {
            var aura = _interruptableAuras[i].Base;

            if (aura.SpellInfo.HasAuraInterruptFlag(flag) && (source == null || aura.Id != source.Id) && !IsInterruptFlagIgnoredForSpell(flag, this, aura.SpellInfo, source))
            {
                var removedAuras = RemovedAurasCount;
                RemoveAura(aura, AuraRemoveMode.Interrupt);

                if (RemovedAurasCount > removedAuras + 1)
                    i = 0;
            }
        }

        // interrupt channeled spell
        var spell = GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (spell != null)
            if (spell.State == SpellState.Casting && spell.SpellInfo.HasChannelInterruptFlag(flag) && (source == null || spell.SpellInfo.Id != source.Id) && !IsInterruptFlagIgnoredForSpell(flag, this, spell.SpellInfo, source))
                InterruptNonMeleeSpells(false);

        UpdateInterruptMask();
    }

    public void RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2 flag, SpellInfo source = null)
    {
        if (!HasInterruptFlag(flag))
            return;

        // interrupt auras
        for (var i = 0; i < _interruptableAuras.Count; i++)
        {
            var aura = _interruptableAuras[i].Base;

            if (!aura.SpellInfo.HasAuraInterruptFlag(flag) || (source != null && aura.Id == source.Id) || IsInterruptFlagIgnoredForSpell(flag, this, aura.SpellInfo, source))
                continue;

            var removedAuras = RemovedAurasCount;
            RemoveAura(aura, AuraRemoveMode.Interrupt);

            if (RemovedAurasCount > removedAuras + 1)
                i = 0;
        }

        // interrupt channeled spell
        var spell = GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (spell != null)
            if (spell.State == SpellState.Casting && spell.SpellInfo.HasChannelInterruptFlag(flag) && (source == null || spell.SpellInfo.Id != source.Id) && !IsInterruptFlagIgnoredForSpell(flag, this, spell.SpellInfo, source))
                InterruptNonMeleeSpells(false);

        UpdateInterruptMask();
    }

    public void RemoveAurasWithMechanic(ulong mechanicMaskToRemove, AuraRemoveMode removeMode = AuraRemoveMode.Default, uint exceptSpellId = 0, bool withEffectMechanics = false)
    {
        List<Aura> aurasToUpdateTargets = new();

        RemoveAppliedAuras(aurApp =>
                           {
                               var aura = aurApp.Base;

                               if (exceptSpellId != 0 && aura.Id == exceptSpellId)
                                   return false;

                               var appliedMechanicMask = aura.SpellInfo.GetSpellMechanicMaskByEffectMask(aurApp.EffectMask);

                               if ((appliedMechanicMask & mechanicMaskToRemove) == 0)
                                   return false;

                               // spell mechanic matches required mask for removal
                               if (((1ul << (int)aura.SpellInfo.Mechanic) & mechanicMaskToRemove) != 0 || withEffectMechanics)
                                   return true;

                               // effect mechanic matches required mask for removal - don't remove, only update targets
                               aurasToUpdateTargets.Add(aura);

                               return false;
                           },
                           removeMode);

        foreach (var aura in aurasToUpdateTargets)
        {
            aura.UpdateTargetMap(aura.Caster);

            // Fully remove the aura if all effects were removed
            if (!aura.IsPassive && aura.Owner == this && aura.GetApplicationOfTarget(GUID) == null)
                aura.Remove(removeMode);
        }
    }

    public void RemoveMovementImpairingAuras(bool withRoot)
    {
        if (withRoot)
            RemoveAurasWithMechanic(1 << (int)Mechanics.Root, AuraRemoveMode.Default, 0, true);

        RemoveAurasWithMechanic(1 << (int)Mechanics.Snare);
    }

    public void RemoveNotOwnSingleTargetAuras(bool onPhaseChange = false)
    {
        // Iterate m_ownedAuras - aura is marked as single target in Unit::AddAura (and pushed to m_ownedAuras).
        // m_appliedAuras will NOT contain the aura before first Unit::Update after adding it to m_ownedAuras.
        // Quickly removing such an aura will lead to it not being unregistered from caster's single cast auras container
        // leading to assertion failures if the aura was cast on a player that can
        // (and is changing map at the point where this function is called).
        // Such situation occurs when player is logging in inside an instance and fails the entry check for any reason.
        // The aura that was loaded from db (indirectly, via linked casts) gets removed before it has a chance
        // to register in m_appliedAuras
        foreach (var aura in _ownedAuras.Query().HasCasterGuid(GUID).IsSingleTarget().GetResults())
            if (onPhaseChange)
                RemoveOwnedAura(aura.Id, aura);
            else
            {
                var caster = aura.Caster;

                if (caster == null || !caster.Location.InSamePhase(this))
                    RemoveOwnedAura(aura.Id, aura);
            }

        // single target auras at other targets
        for (var i = 0; i < SingleCastAuras.Count; i++)
        {
            var aura = SingleCastAuras[i];

            if (aura.OwnerAsUnit != this && (!onPhaseChange || !aura.OwnerAsUnit.Location.InSamePhase(this)))
                aura.Remove();
        }
    }

    public void RemoveOwnedAura(KeyValuePair<uint, Aura> keyValuePair, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        RemoveOwnedAura(keyValuePair.Key, keyValuePair.Value, removeMode);
    }

    // All aura base removes should go through this function!
    public void RemoveOwnedAura(uint spellId, Aura aura, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        if (aura is { IsRemoved: true })
        {
            if (_ownedAuras.Contains(aura))
                _ownedAuras.Remove(aura);

            return;
        }

        _ownedAuras.Remove(aura);

        // Unregister single target aura
        if (aura.IsSingleTarget)
            aura.UnregisterSingleTarget();

        aura._Remove(removeMode);
    }

    public void RemoveOwnedAura(uint spellId, ObjectGuid casterGUID = default, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        _ownedAuras.Query()
                   .HasSpellId(spellId)
                   .HasCasterGuid(casterGUID)
                   .Execute(RemoveOwnedAura);
    }

    public void RemoveOwnedAura(Aura auraToRemove, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        if (auraToRemove.IsRemoved)
            return;

        if (removeMode == AuraRemoveMode.None)
        {
            Log.Logger.Error("Unit.RemoveOwnedAura() called with unallowed removeMode AURA_REMOVE_NONE, spellId {0}", auraToRemove.Id);

            return;
        }

        var spellId = auraToRemove.Id;

        if (_ownedAuras.Contains(auraToRemove))
            RemoveOwnedAura(spellId, auraToRemove, removeMode);
    }

    public void RemoveOwnedAuras(Func<Aura, bool> check, AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        _ownedAuras.Auras.CallOnMatch((aura) => check(aura), (aura) => RemoveOwnedAura(aura.Id, aura, removeMode));
    }

    public void RemoveVisibleAura(AuraApplication aurApp)
    {
        lock (_visibleAurasToUpdate)
        {
            _visibleAuras.Remove(aurApp);
            _visibleAurasToUpdate.Remove(aurApp);
            UpdateAuraForGroup();
        }
    }

    public void SendCancelSpellVisual(uint id)
    {
        var cancelSpellVisual = new CancelSpellVisual
        {
            Source = GUID,
            SpellVisualID = id
        };

        SendMessageToSet(cancelSpellVisual, true);
    }

    public void SendCancelSpellVisualKit(uint id)
    {
        var cancelSpellVisualKit = new CancelSpellVisualKit
        {
            Source = GUID,
            SpellVisualKitID = id
        };

        SendMessageToSet(cancelSpellVisualKit, true);
    }

    public void SendEnergizeSpellLog(Unit victim, uint spellId, int amount, int overEnergize, PowerType powerType)
    {
        SpellEnergizeLog data = new()
        {
            CasterGUID = GUID,
            TargetGUID = victim.GUID,
            SpellID = spellId,
            Type = powerType,
            Amount = amount,
            OverEnergize = overEnergize
        };

        data.LogData.Initialize(victim);

        SendCombatLogMessage(data);
    }

    public void SendPeriodicAuraLog(SpellPeriodicAuraLogInfo info)
    {
        var aura = info.AuraEff;

        SpellPeriodicAuraLog data = new()
        {
            TargetGUID = GUID,
            CasterGUID = aura.CasterGuid,
            SpellID = aura.Id
        };

        data.LogData.Initialize(this);

        SpellPeriodicAuraLog.SpellLogEffect spellLogEffect = new()
        {
            Effect = (uint)aura.AuraType,
            Amount = (uint)info.Damage,
            OriginalDamage = (int)info.OriginalDamage,
            OverHealOrKill = (uint)info.OverDamage,
            SchoolMaskOrPower = (uint)aura.SpellInfo.GetSchoolMask(),
            AbsorbedOrAmplitude = (uint)info.Absorb,
            Resisted = (uint)info.Resist,
            Crit = info.Critical
        };

        // @todo: implement debug info

        ContentTuningParams contentTuningParams = new();
        var caster = ObjectAccessor.GetUnit(this, aura.CasterGuid);

        if (caster != null && contentTuningParams.GenerateDataForUnits(caster, this))
            spellLogEffect.ContentTuning = contentTuningParams;

        data.Effects.Add(spellLogEffect);

        SendCombatLogMessage(data);
    }

    public void SendPlaySpellVisual(Unit target, uint spellVisualId, ushort missReason, ushort reflectStatus, float travelSpeed, bool speedAsTime, float launchDelay)
    {
        var playSpellVisual = new PlaySpellVisual
        {
            Source = GUID,
            Target = target.GUID,
            TargetPosition = target.Location,
            SpellVisualID = spellVisualId,
            TravelSpeed = travelSpeed,
            MissReason = missReason,
            ReflectStatus = reflectStatus,
            SpeedAsTime = speedAsTime,
            LaunchDelay = launchDelay
        };

        SendMessageToSet(playSpellVisual, true);
    }

    public void SendPlaySpellVisual(in Position targetPosition, uint spellVisualId, ushort missReason, ushort reflectStatus, float travelSpeed, bool speedAsTime, float launchDelay)
    {
        var playSpellVisual = new PlaySpellVisual
        {
            Source = GUID,
            TargetPosition = targetPosition,
            SpellVisualID = spellVisualId,
            TravelSpeed = travelSpeed,
            MissReason = missReason,
            ReflectStatus = reflectStatus,
            SpeedAsTime = speedAsTime,
            LaunchDelay = launchDelay
        };

        SendMessageToSet(playSpellVisual, true);
    }

    public void SendSpellDamageImmune(Unit target, uint spellId, bool isPeriodic)
    {
        SpellOrDamageImmune spellOrDamageImmune = new()
        {
            CasterGUID = GUID,
            VictimGUID = target.GUID,
            SpellID = spellId,
            IsPeriodic = isPeriodic
        };

        SendMessageToSet(spellOrDamageImmune, true);
    }

    public void SendSpellInstakillLog(uint spellId, Unit caster, Unit target = null)
    {
        SpellInstakillLog spellInstakillLog = new()
        {
            Caster = caster.GUID,
            Target = target?.GUID ?? caster.GUID,
            SpellID = spellId
        };

        SendMessageToSet(spellInstakillLog, false);
    }

    public void SendSpellNonMeleeDamageLog(SpellNonMeleeDamage log)
    {
        SpellNonMeleeDamageLog packet = new()
        {
            Me = log.Target.GUID,
            CasterGUID = log.Attacker.GUID,
            CastID = log.CastId,
            SpellID = (int)(log.Spell?.Id ?? 0),
            Visual = log.SpellVisual,
            Damage = (int)log.Damage,
            OriginalDamage = (int)log.OriginalDamage
        };

        if (log.Damage > log.PreHitHealth)
            packet.Overkill = (int)(log.Damage - log.PreHitHealth);
        else
            packet.Overkill = -1;

        packet.SchoolMask = (byte)log.SchoolMask;
        packet.Absorbed = (int)log.Absorb;
        packet.Resisted = (int)log.Resist;
        packet.ShieldBlock = (int)log.Blocked;
        packet.Periodic = log.PeriodicLog;
        packet.Flags = log.HitInfo;

        ContentTuningParams contentTuningParams = new();

        if (contentTuningParams.GenerateDataForUnits(log.Attacker, log.Target))
            packet.ContentTuning = contentTuningParams;

        SendCombatLogMessage(packet);
    }

    public void SetAuraStack(uint spellId, Unit target, uint stack)
    {
        var aura = target.GetAura(spellId, GUID);

        if (aura == null)
            aura = AddAura(spellId, target);

        if (aura != null && stack != 0)
            aura.SetStackAmount((byte)stack);
    }

    public void SetCurrentCastSpell(Spell pSpell)
    {
        var cSpellType = pSpell.CurrentContainer;
        var currentSpell = GetCurrentSpell(cSpellType);

        if (pSpell == currentSpell) // avoid breaking self
            return;

        // special breakage effects:
        switch (cSpellType)
        {
            case CurrentSpellTypes.Generic:
            {
                InterruptSpell(CurrentSpellTypes.Generic, false);

                // generic spells always break channeled not delayed spells
                if (GetCurrentSpell(CurrentSpellTypes.Channeled) != null && !GetCurrentSpell(CurrentSpellTypes.Channeled).SpellInfo.HasAttribute(SpellAttr5.AllowActionsDuringChannel))
                    InterruptSpell(CurrentSpellTypes.Channeled, false);

                // autorepeat breaking
                if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null)
                    // break autorepeat if not Auto Shot
                    if (CurrentSpells[CurrentSpellTypes.AutoRepeat].SpellInfo.Id != 75)
                        InterruptSpell(CurrentSpellTypes.AutoRepeat);

                if (pSpell.SpellInfo.CalcCastTime() > 0)
                    AddUnitState(UnitState.Casting);

                break;
            }
            case CurrentSpellTypes.Channeled:
            {
                // channel spells always break generic non-delayed and any channeled spells
                InterruptSpell(CurrentSpellTypes.Generic, false);
                InterruptSpell(CurrentSpellTypes.Channeled);

                // it also does break autorepeat if not Auto Shot
                if (GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null &&
                    CurrentSpells[CurrentSpellTypes.AutoRepeat].SpellInfo.Id != 75)
                    InterruptSpell(CurrentSpellTypes.AutoRepeat);

                AddUnitState(UnitState.Casting);

                break;
            }
            case CurrentSpellTypes.AutoRepeat:
            {
                if (currentSpell is { State: SpellState.Idle })
                    currentSpell.State = SpellState.Finished;

                // only Auto Shoot does not break anything
                if (pSpell.SpellInfo.Id != 75)
                {
                    // generic autorepeats break generic non-delayed and channeled non-delayed spells
                    InterruptSpell(CurrentSpellTypes.Generic, false);
                    InterruptSpell(CurrentSpellTypes.Channeled, false);
                }

                break;
            }
        }

        // current spell (if it is still here) may be safely deleted now
        currentSpell?.SetReferencedFromCurrent(false);

        // set new current spell
        CurrentSpells[cSpellType] = pSpell;
        pSpell.SetReferencedFromCurrent(true);

        pSpell.SelfContainer = CurrentSpells[pSpell.CurrentContainer];
    }

    public void SetInstantCast(bool set)
    {
        CanInstantCast = set;
    }

    public void SetVisibleAura(AuraApplication aurApp)
    {
        lock (_visibleAurasToUpdate)
        {
            _visibleAuras.Add(aurApp);
            _visibleAurasToUpdate.Add(aurApp);
            UpdateAuraForGroup();
        }
    }

    public void SetVisibleAuraUpdate(AuraApplication aurApp)
    {
        lock (_visibleAurasToUpdate)
            _visibleAurasToUpdate.Add(aurApp);
    }

    public double SpellBaseDamageBonusDone(SpellSchoolMask schoolMask)
    {
        if (TryGetAsPlayer(out var thisPlayer))
        {
            float overrideSp = thisPlayer.ActivePlayerData.OverrideSpellPowerByAPPercent;

            if (overrideSp > 0.0f)
                return (int)(MathFunctions.CalculatePct(GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), overrideSp) + 0.5f);
        }

        var doneAdvertisedBenefit = GetTotalAuraModifierByMiscMask(AuraType.ModDamageDone, (int)schoolMask);

        if (!IsTypeId(TypeId.Player))
            return doneAdvertisedBenefit;

        // Base value
        doneAdvertisedBenefit += (int)AsPlayer.GetBaseSpellPowerBonus();

        // Check if we are ever using mana - PaperDollFrame.lua
        if (GetPowerIndex(PowerType.Mana) != (uint)PowerType.Max)
            doneAdvertisedBenefit += Math.Max(0, (int)GetStat(Stats.Intellect)); // spellpower from intellect

        // Damage bonus from stats
        var mDamageDoneOfStatPercent = GetAuraEffectsByType(AuraType.ModSpellDamageOfStatPercent);

        foreach (var eff in mDamageDoneOfStatPercent)
            if (Convert.ToBoolean(eff.MiscValue & (int)schoolMask))
            {
                // stat used stored in miscValueB for this aura
                var usedStat = (Stats)eff.MiscValueB;
                doneAdvertisedBenefit += (int)MathFunctions.CalculatePct(GetStat(usedStat), eff.Amount);
            }

        return doneAdvertisedBenefit;
    }

    public double SpellBaseHealingBonusDone(SpellSchoolMask schoolMask)
    {
        var thisPlayer = AsPlayer;

        if (thisPlayer != null)
        {
            float overrideSp = thisPlayer.ActivePlayerData.OverrideSpellPowerByAPPercent;

            if (overrideSp > 0.0f)
                return MathFunctions.CalculatePct(GetTotalAttackPowerValue(WeaponAttackType.BaseAttack), overrideSp) + 0.5f;
        }

        var advertisedBenefit = GetTotalAuraModifier(AuraType.ModHealingDone,
                                                     aurEff =>
                                                     {
                                                         if (aurEff.MiscValue == 0 || (aurEff.MiscValue & (int)schoolMask) != 0)
                                                             return true;

                                                         return false;
                                                     });

        // Healing bonus of spirit, intellect and strength
        if (IsTypeId(TypeId.Player))
        {
            // Base value
            advertisedBenefit += AsPlayer.GetBaseSpellPowerBonus();

            // Check if we are ever using mana - PaperDollFrame.lua
            if (GetPowerIndex(PowerType.Mana) != (uint)PowerType.Max)
                advertisedBenefit += Math.Max(0, GetStat(Stats.Intellect)); // spellpower from intellect

            // Healing bonus from stats
            var mHealingDoneOfStatPercent = GetAuraEffectsByType(AuraType.ModSpellHealingOfStatPercent);

            foreach (var i in mHealingDoneOfStatPercent)
            {
                // stat used dependent from misc value (stat index)
                var usedStat = (Stats)i.GetSpellEffectInfo().MiscValue;
                advertisedBenefit += MathFunctions.CalculatePct(GetStat(usedStat), i.Amount);
            }
        }

        return advertisedBenefit;
    }

    public double SpellCritChanceDone(Spell spell, AuraEffect aurEff, SpellSchoolMask schoolMask, WeaponAttackType attackType = WeaponAttackType.BaseAttack)
    {
        var spellInfo = spell != null ? spell.SpellInfo : aurEff.SpellInfo;

        //! Mobs can't crit with spells. (Except player controlled)
        if (IsCreature && SpellModOwner == null)
            return 0.0f;

        // not critting spell
        if (spell != null && !spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
            return 0.0f;

        double critChance = 0.0f;

        switch (spellInfo.DmgClass)
        {
            case SpellDmgClass.Magic:
            {
                if (schoolMask.HasAnyFlag(SpellSchoolMask.Normal))
                    critChance = 0.0f;
                // For other schools
                else if (IsTypeId(TypeId.Player))
                    critChance = AsPlayer.ActivePlayerData.SpellCritPercentage;
                else
                    critChance = BaseSpellCritChance;

                break;
            }
            case SpellDmgClass.Melee:
            case SpellDmgClass.Ranged:
                critChance += GetUnitCriticalChanceDone(attackType);

                break;

            case SpellDmgClass.None:
            default:
                return 0f;
        }

        // percent done
        // only players use intelligence for critical chance computations
        var modOwner = SpellModOwner;

        modOwner?.ApplySpellMod(spellInfo, SpellModOp.CritChance, ref critChance);

        return Math.Max(critChance, 0.0f);
    }

    public double SpellCritChanceTaken(Unit caster, Spell spell, AuraEffect aurEff, SpellSchoolMask schoolMask, double doneChance, WeaponAttackType attackType = WeaponAttackType.BaseAttack)
    {
        var spellInfo = spell != null ? spell.SpellInfo : aurEff.SpellInfo;

        // not critting spell
        if (spell != null && !spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
            return 0.0f;

        var critChance = doneChance;

        switch (spellInfo.DmgClass)
        {
            case SpellDmgClass.Magic:
            {
                // taken
                if (!spellInfo.IsPositive)
                    // Modify critical chance by victim SPELL_AURA_MOD_ATTACKER_SPELL_AND_WEAPON_CRIT_CHANCE
                    critChance += GetTotalAuraModifier(AuraType.ModAttackerSpellAndWeaponCritChance);

                if (caster != null)
                {
                    // scripted (increase crit chance ... against ... target by x%
                    var mOverrideClassScript = caster.GetAuraEffectsByType(AuraType.OverrideClassScripts);

                    foreach (var auraEffect in mOverrideClassScript)
                    {
                        if (!auraEffect.IsAffectingSpell(spellInfo))
                            continue;

                        switch (auraEffect.MiscValue)
                        {
                            case 911: // Shatter
                                if (HasAuraState(AuraStateType.Frozen, spellInfo, this))
                                {
                                    critChance *= 1.5f;
                                    var eff = auraEffect.Base.GetEffect(1);

                                    if (eff != null)
                                        critChance += eff.Amount;
                                }

                                break;
                        }
                    }

                    // Custom crit by class
                    switch (spellInfo.SpellFamilyName)
                    {
                        case SpellFamilyNames.Rogue:
                            // Shiv-applied poisons can't crit
                            if (caster.FindCurrentSpellBySpellId(5938) != null)
                                critChance = 0.0f;

                            break;
                    }

                    // Spell crit suppression
                    if (IsCreature)
                    {
                        var levelDiff = (int)(GetLevelForTarget(this) - caster.Level);
                        critChance -= levelDiff * 1.0f;
                    }
                }

                break;
            }
            case SpellDmgClass.Melee:
            case SpellDmgClass.Ranged:
            {
                if (caster != null)
                    critChance += GetUnitCriticalChanceTaken(caster, attackType, critChance);

                break;
            }
            case SpellDmgClass.None:
            default:
                return 0f;
        }

        // for this types the bonus was already added in GetUnitCriticalChance, do not add twice
        if (caster != null && spellInfo.DmgClass != SpellDmgClass.Melee && spellInfo.DmgClass != SpellDmgClass.Ranged)
        {
            critChance += GetTotalAuraModifier(AuraType.ModCritChanceForCasterWithAbilities, a => a.CasterGuid == caster.GUID && a.IsAffectingSpell(spellInfo));

            critChance += GetTotalAuraModifier(AuraType.ModCritChanceForCaster, a => a.CasterGuid == caster.GUID);

            critChance += caster.GetTotalAuraModifier(AuraType.ModCritChanceVersusTargetHealth, a => !HealthBelowPct(a.MiscValueB));

            var tempSummon = caster.ToTempSummon();

            if (tempSummon != null)
                critChance += GetTotalAuraModifier(AuraType.ModCritChanceForCasterPet, a => a.CasterGuid == tempSummon.SummonerGUID);
        }

        // call script handlers
        if (spell != null)
            spell.CallScriptCalcCritChanceHandlers(this, ref critChance);
        else
            aurEff.Base.CallScriptEffectCalcCritChanceHandlers(aurEff, aurEff.Base.GetApplicationOfTarget(GUID), this, ref critChance);

        return Math.Max(critChance, 0.0f);
    }

    public double SpellDamageBonusDone(Unit victim, SpellInfo spellProto, double pdamage, DamageEffectType damagetype, SpellEffectInfo spellEffectInfo, uint stack = 1, Spell spell = null)
    {
        if (spellProto == null || victim == null || damagetype == DamageEffectType.Direct)
            return pdamage;

        // Some spells don't benefit from done mods
        if (spellProto.HasAttribute(SpellAttr3.IgnoreCasterModifiers))
            return pdamage;

        // For totems get damage bonus from owner
        if (IsTypeId(TypeId.Unit) && IsTotem)
        {
            var owner = OwnerUnit;

            if (owner != null)
                return owner.SpellDamageBonusDone(victim, spellProto, pdamage, damagetype, spellEffectInfo, stack, spell);
        }

        double doneTotal = 0;
        var doneTotalMod = SpellDamagePctDone(victim, spellProto, damagetype, spellEffectInfo, spell);

        // Done fixed damage bonus auras
        var doneAdvertisedBenefit = SpellBaseDamageBonusDone(spellProto.GetSchoolMask());
        // modify spell power by victim's SPELL_AURA_MOD_DAMAGE_TAKEN auras (eg Amplify/Dampen Magic)
        doneAdvertisedBenefit += victim.GetTotalAuraModifierByMiscMask(AuraType.ModDamageTaken, (int)spellProto.GetSchoolMask());

        // Pets just add their bonus damage to their spell damage
        // note that their spell damage is just gain of their own auras
        if (HasUnitTypeMask(UnitTypeMask.Guardian))
            doneAdvertisedBenefit += ((Guardian)this).GetBonusDamage();

        // Check for table values
        if (spellEffectInfo.BonusCoefficientFromAp > 0.0f)
        {
            var apCoeffMod = spellEffectInfo.BonusCoefficientFromAp;

            if (SpellModOwner != null)
            {
                apCoeffMod *= 100.0f;
                SpellModOwner.ApplySpellMod(spellProto, SpellModOp.BonusCoefficient, ref apCoeffMod);
                apCoeffMod /= 100.0f;
            }

            var attType = WeaponAttackType.BaseAttack;

            if (spellProto.IsRangedWeaponSpell && spellProto.DmgClass != SpellDmgClass.Melee)
                attType = WeaponAttackType.RangedAttack;

            if (spellProto.HasAttribute(SpellAttr3.RequiresOffHandWeapon) && !spellProto.HasAttribute(SpellAttr3.RequiresMainHandWeapon))
                attType = WeaponAttackType.OffAttack;

            var aPbonus = victim.GetTotalAuraModifier(attType != WeaponAttackType.RangedAttack ? AuraType.MeleeAttackPowerAttackerBonus : AuraType.RangedAttackPowerAttackerBonus);
            aPbonus += GetTotalAttackPowerValue(attType);
            doneTotal += (int)(stack * apCoeffMod * aPbonus);
        }
        else
        {
            // No bonus damage for SPELL_DAMAGE_CLASS_NONE class spells by default
            if (spellProto.DmgClass == SpellDmgClass.None)
                return Math.Max(pdamage * doneTotalMod, 0.0f);
        }

        // Default calculation
        var coeff = spellEffectInfo.BonusCoefficient;

        if (doneAdvertisedBenefit != 0)
        {
            spell?.ForEachSpellScript<ISpellCalculateBonusCoefficient>(a => coeff = a.CalcBonusCoefficient(coeff));

            var modOwner1 = SpellModOwner;

            if (modOwner1 != null)
            {
                coeff *= 100.0f;
                modOwner1.ApplySpellMod(spellProto, SpellModOp.BonusCoefficient, ref coeff);
                coeff /= 100.0f;
            }

            doneTotal += doneAdvertisedBenefit * coeff * stack;
        }

        var tmpDamage = (pdamage + doneTotal) * doneTotalMod;
        // apply spellmod to Done damage (flat and pct)
        var modOwner = SpellModOwner;

        modOwner?.ApplySpellMod(spellProto, damagetype == DamageEffectType.DOT ? SpellModOp.PeriodicHealingAndDamage : SpellModOp.HealingAndDamage, ref tmpDamage);

        return Math.Max(tmpDamage, 0.0f);
    }

    public double SpellDamageBonusTaken(Unit caster, SpellInfo spellProto, double pdamage, DamageEffectType damagetype)
    {
        if (spellProto == null || damagetype == DamageEffectType.Direct)
            return pdamage;

        double takenTotalMod = 1.0f;

        // Mod damage from spell mechanic
        var mechanicMask = spellProto.GetAllEffectsMechanicMask();

        if (mechanicMask != 0)
            takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModMechanicDamageTakenPercent,
                                                    aurEff =>
                                                    {
                                                        if ((mechanicMask & (1ul << aurEff.MiscValue)) != 0)
                                                            return true;

                                                        return false;
                                                    });

        var cheatDeath = GetAuraEffect(45182, 0);

        if (cheatDeath != null)
            if (cheatDeath.MiscValue.HasAnyFlag((int)SpellSchoolMask.Normal))
                MathFunctions.AddPct(ref takenTotalMod, cheatDeath.Amount);

        // Spells with SPELL_ATTR4_IGNORE_DAMAGE_TAKEN_MODIFIERS should only benefit from mechanic damage mod auras.
        if (!spellProto.HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
        {
            // Versatility
            var modOwner = SpellModOwner;

            if (modOwner != null)
            {
                // only 50% of SPELL_AURA_MOD_VERSATILITY for damage reduction
                var versaBonus = modOwner.GetTotalAuraModifier(AuraType.ModVersatility) / 2.0f;
                MathFunctions.AddPct(ref takenTotalMod, -(modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageTaken) + versaBonus));
            }

            // from positive and negative SPELL_AURA_MOD_DAMAGE_PERCENT_TAKEN
            // multiplicative bonus, for example Dispersion + Shadowform (0.10*0.85=0.085)
            takenTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentTaken, (uint)spellProto.GetSchoolMask());

            // From caster spells
            if (caster != null)
            {
                takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSchoolMaskDamageFromCaster, aurEff => { return aurEff.CasterGuid == caster.GUID && (aurEff.MiscValue & (int)spellProto.GetSchoolMask()) != 0; });

                takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModSpellDamageFromCaster, aurEff => { return aurEff.CasterGuid == caster.GUID && aurEff.IsAffectingSpell(spellProto); });

                takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageTakenFromCasterByLabel, aurEff => { return aurEff.CasterGuid == caster.GUID && spellProto.HasLabel((uint)aurEff.MiscValue); });
            }

            if (damagetype == DamageEffectType.DOT)
                takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModPeriodicDamageTaken, aurEff => (aurEff.MiscValue & (uint)spellProto.GetSchoolMask()) != 0);
        }

        // Sanctified Wrath (bypass damage reduction)
        if (caster != null && takenTotalMod < 1.0f)
        {
            var damageReduction = 1.0f - takenTotalMod;
            var casterIgnoreResist = caster.GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);

            foreach (var aurEff in casterIgnoreResist)
            {
                if ((aurEff.MiscValue & (int)spellProto.GetSchoolMask()) == 0)
                    continue;

                MathFunctions.AddPct(ref damageReduction, -aurEff.Amount);
            }

            takenTotalMod = 1.0f - damageReduction;
        }

        var tmpDamage = pdamage * takenTotalMod;

        return Math.Max(tmpDamage, 0.0f);
    }

    public double SpellDamagePctDone(Unit victim, SpellInfo spellProto, DamageEffectType damagetype, SpellEffectInfo spellEffectInfo, Spell spell = null)
    {
        if (spellProto == null || victim == null || damagetype == DamageEffectType.Direct)
            return 1.0f;

        // Some spells don't benefit from done mods
        if (spellProto.HasAttribute(SpellAttr3.IgnoreCasterModifiers))
            return 1.0f;

        // Some spells don't benefit from pct done mods
        if (spellProto.HasAttribute(SpellAttr6.IgnoreCasterDamageModifiers))
            return 1.0f;

        // For totems get damage bonus from owner
        if (IsCreature && IsTotem)
        {
            var owner = OwnerUnit;

            if (owner != null)
                return owner.SpellDamagePctDone(victim, spellProto, damagetype, spellEffectInfo, spell);
        }

        // Done total percent damage auras
        double doneTotalMod = 1.0f;

        // Pet damage?
        if (IsTypeId(TypeId.Unit) && !IsPet)
            doneTotalMod *= AsCreature.GetSpellDamageMod(AsCreature.Template.Rank);

        // Versatility
        var modOwner = SpellModOwner;

        if (modOwner != null)
            MathFunctions.AddPct(ref doneTotalMod, modOwner.GetRatingBonusValue(CombatRating.VersatilityDamageDone) + modOwner.GetTotalAuraModifier(AuraType.ModVersatility));

        double maxModDamagePercentSchool = 0.0f;

        if (TryGetAsPlayer(out var thisPlayer))
        {
            for (var i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean((int)spellProto.GetSchoolMask() & (1 << i)))
                    maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, thisPlayer.ActivePlayerData.ModDamageDonePercent[i]);
        }
        else
            maxModDamagePercentSchool = GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, (uint)spellProto.GetSchoolMask());

        doneTotalMod *= maxModDamagePercentSchool;

        var creatureTypeMask = victim.CreatureTypeMask;

        doneTotalMod *= GetTotalAuraMultiplierByMiscMask(AuraType.ModDamageDoneVersus, creatureTypeMask);

        // bonus against aurastate
        doneTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageDoneVersusAurastate,
                                               aurEff =>
                                               {
                                                   if (victim.HasAuraState((AuraStateType)aurEff.MiscValue))
                                                       return true;

                                                   return false;
                                               });

        // Add SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC percent bonus
        if (spellEffectInfo.Mechanic != 0)
            MathFunctions.AddPct(ref doneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellEffectInfo.Mechanic));
        else if (spellProto.Mechanic != 0)
            MathFunctions.AddPct(ref doneTotalMod, GetTotalAuraModifierByMiscValue(AuraType.ModDamageDoneForMechanic, (int)spellProto.Mechanic));

        spell?.ForEachSpellScript<ISpellCalculateMultiplier>(a => doneTotalMod = a.CalcMultiplier(doneTotalMod));

        // Custom scripted damage. Need to figure out how to move this.
        if (spellProto.SpellFamilyName == SpellFamilyNames.Warlock)
            // Shadow Bite (30% increase from each dot)
            if (spellProto.SpellFamilyFlags[1].HasAnyFlag<uint>(0x00400000) && IsPet)
            {
                var count = victim.GetDoTsByCaster(OwnerGUID);

                if (count != 0)
                    MathFunctions.AddPct(ref doneTotalMod, 30 * count);
            }

        return doneTotalMod;
    }

    public double SpellHealingBonusDone(Unit victim, SpellInfo spellProto, double healamount, DamageEffectType damagetype, SpellEffectInfo spellEffectInfo, uint stack = 1, Spell spell = null)
    {
        // For totems get healing bonus from owner (statue isn't totem in fact)
        if (IsTypeId(TypeId.Unit) && IsTotem)
            if (TryGetAsUnit(out var owner))
                return owner.SpellHealingBonusDone(victim, spellProto, healamount, damagetype, spellEffectInfo, stack, spell);

        // No bonus healing for potion spells
        if (spellProto.SpellFamilyName == SpellFamilyNames.Potion)
            return healamount;

        double doneTotal = 0;
        var doneTotalMod = SpellHealingPctDone(victim, spellProto, spell);

        // done scripted mod (take it from owner)
        var owner1 = OwnerUnit ?? this;
        var mOverrideClassScript = owner1.GetAuraEffectsByType(AuraType.OverrideClassScripts);

        foreach (var aurEff in mOverrideClassScript)
        {
            if (!aurEff.IsAffectingSpell(spellProto))
                continue;

            switch (aurEff.MiscValue)
            {
                case 3736: // Hateful Totem of the Third Wind / Increased Lesser Healing Wave / LK Arena (4/5/6) Totem of the Third Wind / Savage Totem of the Third Wind
                    doneTotal += aurEff.Amount;

                    break;
            }
        }

        // Done fixed damage bonus auras
        var doneAdvertisedBenefit = SpellBaseHealingBonusDone(spellProto.GetSchoolMask());
        // modify spell power by victim's SPELL_AURA_MOD_HEALING auras (eg Amplify/Dampen Magic)
        doneAdvertisedBenefit += victim.GetTotalAuraModifierByMiscMask(AuraType.ModHealing, (int)spellProto.GetSchoolMask());

        // Pets just add their bonus damage to their spell damage
        // note that their spell damage is just gain of their own auras
        if (HasUnitTypeMask(UnitTypeMask.Guardian))
            doneAdvertisedBenefit += ((Guardian)this).GetBonusDamage();

        // Check for table values
        var coeff = spellEffectInfo.BonusCoefficient;

        if (spellEffectInfo.BonusCoefficientFromAp > 0.0f)
        {
            var attType = spellProto.IsRangedWeaponSpell && spellProto.DmgClass != SpellDmgClass.Melee ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack;
            var aPbonus = victim.GetTotalAuraModifier(attType == WeaponAttackType.BaseAttack ? AuraType.MeleeAttackPowerAttackerBonus : AuraType.RangedAttackPowerAttackerBonus);
            aPbonus += GetTotalAttackPowerValue(attType);

            doneTotal += spellEffectInfo.BonusCoefficientFromAp * stack * aPbonus;
        }
        else if (coeff <= 0.0f) // no AP and no SP coefs, skip
            // No bonus healing for SPELL_DAMAGE_CLASS_NONE class spells by default
            if (spellProto.DmgClass == SpellDmgClass.None)
                return Math.Max(healamount * doneTotalMod, 0.0f);

        // Default calculation
        if (doneAdvertisedBenefit != 0)
        {
            spell?.ForEachSpellScript<ISpellCalculateBonusCoefficient>(a => coeff = a.CalcBonusCoefficient(coeff));

            if (SpellModOwner != null)
            {
                coeff *= 100.0f;
                SpellModOwner.ApplySpellMod(spellProto, SpellModOp.BonusCoefficient, ref coeff);
                coeff /= 100.0f;
            }

            doneTotal += (int)(doneAdvertisedBenefit * coeff * stack);
        }

        foreach (var otherSpellEffectInfo in spellProto.Effects)
        {
            doneTotal = otherSpellEffectInfo.ApplyAuraName switch
            {
                // Bonus healing does not apply to these spells
                AuraType.PeriodicLeech        => 0,
                AuraType.PeriodicHealthFunnel => 0,
                _                             => doneTotal
            };

            if (otherSpellEffectInfo.IsEffect(SpellEffectName.HealthLeech))
                doneTotal = 0;
        }

        var heal = (healamount + doneTotal) * doneTotalMod;

        // apply spellmod to Done amount

        SpellModOwner?.ApplySpellMod(spellProto, damagetype == DamageEffectType.DOT ? SpellModOp.PeriodicHealingAndDamage : SpellModOp.HealingAndDamage, ref heal);

        return Math.Max(heal, 0.0f);
    }

    public double SpellHealingBonusTaken(Unit caster, SpellInfo spellProto, double healamount, DamageEffectType damagetype)
    {
        double takenTotalMod = 1.0f;

        // Healing taken percent
        var minval = GetMaxNegativeAuraModifier(AuraType.ModHealingPct);

        if (minval != 0)
            MathFunctions.AddPct(ref takenTotalMod, minval);

        var maxval = GetMaxPositiveAuraModifier(AuraType.ModHealingPct);

        if (maxval != 0)
            MathFunctions.AddPct(ref takenTotalMod, maxval);

        // Nourish cast
        if (spellProto.SpellFamilyName == SpellFamilyNames.Druid && spellProto.SpellFamilyFlags[1].HasAnyFlag(0x2000000u))
            // Rejuvenation, Regrowth, Lifebloom, or Wild Growth
            if (GetAuraEffect(AuraType.PeriodicHeal, SpellFamilyNames.Druid, new FlagArray128(0x50, 0x4000010)) != null)
                // increase healing by 20%
                takenTotalMod *= 1.2f;

        if (damagetype == DamageEffectType.DOT)
        {
            // Healing over time taken percent
            var minvalHot = GetMaxNegativeAuraModifier(AuraType.ModHotPct);

            if (minvalHot != 0)
                MathFunctions.AddPct(ref takenTotalMod, minvalHot);

            var maxvalHot = GetMaxPositiveAuraModifier(AuraType.ModHotPct);

            if (maxvalHot != 0)
                MathFunctions.AddPct(ref takenTotalMod, maxvalHot);
        }

        if (caster != null)
        {
            takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModHealingReceived, aurEff => caster.GUID == aurEff.CasterGuid && aurEff.IsAffectingSpell(spellProto));

            takenTotalMod *= GetTotalAuraMultiplier(AuraType.ModHealingTakenFromCaster, aurEff => aurEff.CasterGuid == caster.GUID);
        }

        var heal = healamount * takenTotalMod;

        return Math.Max(heal, 0.0f);
    }

    public double SpellHealingPctDone(Unit victim, SpellInfo spellProto, Spell spell = null)
    {
        // For totems get healing bonus from owner
        if (IsCreature && IsTotem)
        {
            var owner = OwnerUnit;

            if (owner != null)
                return owner.SpellHealingPctDone(victim, spellProto);
        }

        // Some spells don't benefit from done mods
        if (spellProto.HasAttribute(SpellAttr3.IgnoreCasterModifiers))
            return 1.0f;

        // Some spells don't benefit from done mods
        if (spellProto.HasAttribute(SpellAttr6.IgnoreHealingModifiers))
            return 1.0f;

        // No bonus healing for potion spells
        if (spellProto.SpellFamilyName == SpellFamilyNames.Potion)
            return 1.0f;

        var thisPlayer = AsPlayer;

        if (thisPlayer != null)
        {
            double maxModDamagePercentSchool = 0.0f;

            for (var i = 0; i < (int)SpellSchools.Max; ++i)
                if (((int)spellProto.GetSchoolMask() & (1 << i)) != 0)
                    maxModDamagePercentSchool = Math.Max(maxModDamagePercentSchool, thisPlayer.ActivePlayerData.ModHealingDonePercent[i]);

            return maxModDamagePercentSchool;
        }

        double doneTotalMod = 1.0f;

        // bonus against aurastate
        doneTotalMod *= GetTotalAuraMultiplier(AuraType.ModDamageDoneVersusAurastate, aurEff => { return victim.HasAuraState((AuraStateType)aurEff.MiscValue); });

        // Healing done percent
        doneTotalMod *= GetTotalAuraMultiplier(AuraType.ModHealingDonePercent);

        // bonus from missing health of target
        var healthPctDiff = 100.0f - victim.HealthPct;

        foreach (var healingDonePctVsTargetHealth in GetAuraEffectsByType(AuraType.ModHealingDonePctVersusTargetHealth))
            if (healingDonePctVsTargetHealth.IsAffectingSpell(spellProto))
                MathFunctions.AddPct(ref doneTotalMod, MathFunctions.CalculatePct((float)healingDonePctVsTargetHealth.Amount, healthPctDiff));

        spell?.ForEachSpellScript<ISpellCalculateMultiplier>(a => doneTotalMod = a.CalcMultiplier(doneTotalMod));

        return doneTotalMod;
    }

    public void TriggerAurasProcOnEvent(List<AuraApplication> myProcAuras, List<AuraApplication> targetProcAuras, Unit actionTarget, ProcFlagsInit typeMaskActor, ProcFlagsInit typeMaskActionTarget, ProcFlagsSpellType spellTypeMask, ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
    {
        // prepare data for self trigger
        ProcEventInfo myProcEventInfo = new(this, actionTarget, actionTarget, typeMaskActor, spellTypeMask, spellPhaseMask, hitMask, spell, damageInfo, healInfo);
        List<Tuple<HashSet<int>, AuraApplication>> myAurasTriggeringProc = new();

        if (typeMaskActor)
        {
            GetProcAurasTriggeredOnEvent(myAurasTriggeringProc, myProcAuras, myProcEventInfo);

            // needed for example for Cobra Strikes, pet does the attack, but aura is on owner
            var modOwner = SpellModOwner;

            if (modOwner != null)
                if (modOwner != this && spell != null)
                {
                    List<AuraApplication> modAuras = new();

                    foreach (var itr in modOwner.AppliedAuras)
                        if (spell.AppliedMods.Contains(itr.Base))
                            modAuras.Add(itr);

                    modOwner.GetProcAurasTriggeredOnEvent(myAurasTriggeringProc, modAuras, myProcEventInfo);
                }
        }

        // prepare data for target trigger
        ProcEventInfo targetProcEventInfo = new(this, actionTarget, this, typeMaskActionTarget, spellTypeMask, spellPhaseMask, hitMask, spell, damageInfo, healInfo);
        List<Tuple<HashSet<int>, AuraApplication>> targetAurasTriggeringProc = new();

        if (typeMaskActionTarget != null && actionTarget != null)
            actionTarget.GetProcAurasTriggeredOnEvent(targetAurasTriggeringProc, targetProcAuras, targetProcEventInfo);

        TriggerAurasProcOnEvent(myProcEventInfo, myAurasTriggeringProc);

        if (typeMaskActionTarget != null && actionTarget != null)
            actionTarget.TriggerAurasProcOnEvent(targetProcEventInfo, targetAurasTriggeringProc);
    }

    public bool TryGetAura<T>(T spellId, out Aura aura) where T : struct, Enum
    {
        aura = GetAura(spellId);

        return aura != null;
    }

    public bool TryGetAura(uint spellId, out Aura aura)
    {
        aura = GetAura(spellId);

        return aura != null;
    }

    public bool TryGetAuraEffect(uint spellId, int effIndex, ObjectGuid casterGUID, out AuraEffect auraEffect)
    {
        auraEffect = GetAuraEffect(spellId, effIndex, casterGUID);

        return auraEffect != null;
    }

    public bool TryGetAuraEffect(uint spellId, int effIndex, out AuraEffect auraEffect)
    {
        auraEffect = GetAuraEffect(spellId, effIndex);

        return auraEffect != null;
    }

    public Aura TryStackingOrRefreshingExistingAura(AuraCreateInfo createInfo)
    {
        // Check if these can stack anyway
        if (createInfo.CasterGuid.IsEmpty && !createInfo.SpellInfo.IsStackableOnOneSlotWithDifferentCasters)
            createInfo.CasterGuid = createInfo.Caster.GUID;

        // passive and Incanter's Absorption and auras with different type can stack with themselves any number of times
        if (createInfo.SpellInfo.IsMultiSlotAura)
            return null;

        // check if cast item changed
        var castItemGUID = createInfo.CastItemGuid;

        // find current aura from spell and change it's stackamount, or refresh it's duration
        var foundAura = GetOwnedAura(createInfo.SpellInfo.Id, createInfo.SpellInfo.IsStackableOnOneSlotWithDifferentCasters ? ObjectGuid.Empty : createInfo.CasterGuid, createInfo.SpellInfo.HasAttribute(SpellCustomAttributes.EnchantProc) ? castItemGUID : ObjectGuid.Empty);

        if (foundAura == null)
            return null;

        // effect masks do not match
        // extremely rare case
        // let's just recreate aura
        if (!createInfo.AuraEffectMask.SetEquals(foundAura.AuraEffects.Keys))
            return null;

        // update basepoints with new values - effect amount will be recalculated in ModStackAmount
        foreach (var spellEffectInfo in createInfo.SpellInfo.Effects)
        {
            var auraEff = foundAura.GetEffect(spellEffectInfo.EffectIndex);

            if (auraEff == null)
                continue;

            auraEff.BaseAmount = createInfo.BaseAmount != null ? createInfo.BaseAmount[spellEffectInfo.EffectIndex] : spellEffectInfo.BasePoints;
        }

        // correct cast item guid if needed
        if (castItemGUID != foundAura.CastItemGuid)
        {
            foundAura.CastItemGuid = castItemGUID;
            foundAura.CastItemId = createInfo.CastItemId;
            foundAura.CastItemLevel = createInfo.CastItemLevel;
        }

        // try to increase stack amount
        foundAura.ModStackAmount(1, AuraRemoveMode.Default, createInfo.ResetPeriodicTimer);

        return foundAura;
    }

    // removes aura application from lists and unapplies effects
    public void UnapplyAura(AuraApplication aurApp, AuraRemoveMode removeMode)
    {
        var check = aurApp.Base.GetApplicationOfTarget(GUID);

        if (check == null)
            return; // The user logged out

        if (check != aurApp)
        {
            Log.Logger.Error($"Tried to remove aura app with spell ID: {aurApp.Base.SpellInfo.Id} that does not match. GetApplicationOfTarget: {aurApp.Base.GetApplicationOfTarget(GUID).Guid} AuraApp: {aurApp.Guid}");

            return;
        }

        //Check if aura was already removed, if so just return.
        if (!_appliedAuras.Remove(aurApp))
            return;

        aurApp.RemoveMode = removeMode;
        var aura = aurApp.Base;
        Log.Logger.Debug("Aura {0} now is remove mode {1}", aura.Id, removeMode);

        ++RemovedAurasCount;

        var caster = aura.Caster;

        if (aura.SpellInfo.HasAnyAuraInterruptFlag)
        {
            _interruptableAuras.Remove(aurApp);
            UpdateInterruptMask();
        }

        var auraStateFound = false;
        var auraState = aura.SpellInfo.GetAuraState();

        if (auraState != 0)
        {
            var canBreak = false;
            // Get mask of all aurastates from remaining auras
            var list = _auraStateAuras.LookupByKey(auraState);

            for (var i = 0; i < list.Count && !(auraStateFound && canBreak);)
            {
                if (list[i] == aurApp)
                {
                    _auraStateAuras.Remove(auraState, list[i]);
                    list = _auraStateAuras.LookupByKey(auraState);
                    i = 0;
                    canBreak = true;

                    continue;
                }

                auraStateFound = true;
                ++i;
            }
        }

        aurApp.Remove();
        aura.UnapplyForTarget(this, caster, aurApp);

        // remove effects of the spell - needs to be done after removing aura from lists
        foreach (var effect in aurApp.Base.AuraEffects)
            if (aurApp.HasEffect(effect.Key))
                aurApp._HandleEffect(effect.Key, false);

        // all effect mustn't be applied
        // Cypher.Assert(aurApp.EffectMask.Count == 0);

        // Remove totem at next update if totem loses its aura
        if (aurApp.RemoveMode == AuraRemoveMode.Expire && IsTypeId(TypeId.Unit) && IsTotem)
            if (ToTotem().GetSpell() == aura.Id && ToTotem().GetTotemType() == TotemType.Passive)
                ToTotem().SetDeathState(DeathState.JustDied);

        // Remove aurastates only if needed and were not found
        if (auraState != 0)
        {
            if (!auraStateFound)
                ModifyAuraState(auraState, false);
            else
            {
                // update for casters, some shouldn't 'see' the aura state
                var aStateMask = 1u << ((int)auraState - 1);

                if ((aStateMask & (uint)AuraStateType.PerCasterAuraStateMask) != 0)
                {
                    Values.ModifyValue(UnitData).ModifyValue(UnitData.AuraState);
                    ForceUpdateFieldChange();
                }
            }
        }

        aura.HandleAuraSpecificMods(aurApp, caster, false, false);

        var player = AsPlayer;

        if (player == null)
            return;

        if (ConditionManager.IsSpellUsedInSpellClickConditions(aurApp.Base.Id))
            player.UpdateVisibleGameobjectsOrSpellClicks();
    }

    public void UpdateEmpowerState(EmpowerState state, uint exceptSpellid = 0)
    {
        for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
            if (CurrentSpells.TryGetValue(i, out var spell) && spell != null && spell.SpellInfo.Id == exceptSpellid)
                spell.SetEmpowerState(state);
    }

    public void UpdateInterruptMask()
    {
        _interruptMask = SpellAuraInterruptFlags.None;
        _interruptMask2 = SpellAuraInterruptFlags2.None;

        foreach (var aurApp in _interruptableAuras)
        {
            _interruptMask |= aurApp.Base.SpellInfo.AuraInterruptFlags;
            _interruptMask2 |= aurApp.Base.SpellInfo.AuraInterruptFlags2;
        }

        var spell = GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (spell is { State: SpellState.Casting })
        {
            _interruptMask |= spell.SpellInfo.ChannelInterruptFlags;
            _interruptMask2 |= spell.SpellInfo.ChannelInterruptFlags2;
        }
    }

    private AuraApplication GetAuraApplicationOfRankedSpell(uint spellId)
    {
        var rankSpell = SpellManager.GetFirstSpellInChain(spellId);

        while (rankSpell != 0)
        {
            var aurApp = GetAuraApplication(rankSpell)?.FirstOrDefault();

            if (aurApp != null)
                return aurApp;

            rankSpell = SpellManager.GetNextSpellInChain(rankSpell);
        }

        return null;
    }

    private uint GetDoTsByCaster(ObjectGuid casterGUID)
    {
        AuraType[] diseaseAuraTypes =
        {
            AuraType.PeriodicDamage, AuraType.PeriodicDamagePercent, AuraType.None
        };

        uint dots = 0;

        foreach (var aura in diseaseAuraTypes)
        {
            if (aura == AuraType.None)
                break;

            var auras = GetAuraEffectsByType(aura);

            foreach (var eff in auras)
                // Get auras by caster
                if (eff.CasterGuid == casterGUID)
                    ++dots;
        }

        return dots;
    }

    private void GetProcAurasTriggeredOnEvent(List<Tuple<HashSet<int>, AuraApplication>> aurasTriggeringProc, List<AuraApplication> procAuras, ProcEventInfo eventInfo)
    {
        var now = GameTime.Now;

        void ProcessAuraApplication(AuraApplication aurApp)
        {
            var procEffectMask = aurApp.Base.GetProcEffectMask(aurApp, eventInfo, now);

            if (procEffectMask.Count != 0)
            {
                aurApp.Base.PrepareProcToTrigger(aurApp, eventInfo, now);
                aurasTriggeringProc.Add(Tuple.Create(procEffectMask, aurApp));
            }
            else
            {
                if (aurApp.Base.SpellInfo.HasAttribute(SpellAttr0.ProcFailureBurnsCharge))
                {
                    var procEntry = SpellManager.GetSpellProcEntry(aurApp.Base.SpellInfo);

                    if (procEntry != null)
                    {
                        aurApp.Base.PrepareProcChargeDrop(procEntry, eventInfo);
                        aurApp.Base.ConsumeProcCharges(procEntry);
                    }
                }

                if (aurApp.Base.SpellInfo.HasAttribute(SpellAttr2.ProcCooldownOnFailure))
                {
                    var procEntry = SpellManager.GetSpellProcEntry(aurApp.Base.SpellInfo);

                    if (procEntry != null)
                        aurApp.Base.AddProcCooldown(procEntry, now);
                }
            }
        }

        // use provided list of auras which can proc
        if (procAuras != null)
            foreach (var aurApp in procAuras)
                ProcessAuraApplication(aurApp);
        // or generate one on our own
        else
            foreach (var aura in AppliedAuras)
                ProcessAuraApplication(aura);
    }

    private SpellSchools GetSpellSchoolByAuraGroup(UnitMods unitMod)
    {
        return unitMod switch
        {
            UnitMods.ResistanceHoly   => SpellSchools.Holy,
            UnitMods.ResistanceFire   => SpellSchools.Fire,
            UnitMods.ResistanceNature => SpellSchools.Nature,
            UnitMods.ResistanceFrost  => SpellSchools.Frost,
            UnitMods.ResistanceShadow => SpellSchools.Shadow,
            UnitMods.ResistanceArcane => SpellSchools.Arcane,
            _                         => SpellSchools.Normal
        };
    }

    private bool IsInterruptFlagIgnoredForSpell(SpellAuraInterruptFlags flag, Unit unit, SpellInfo auraSpellInfo, SpellInfo interruptSource)
    {
        switch (flag)
        {
            case SpellAuraInterruptFlags.Moving:
                return unit.CanCastSpellWhileMoving(auraSpellInfo);

            case SpellAuraInterruptFlags.Action:
            case SpellAuraInterruptFlags.ActionDelayed:
                if (interruptSource != null)
                {
                    if (interruptSource.HasAttribute(SpellAttr1.AllowWhileStealthed) && auraSpellInfo.Dispel == DispelType.Stealth)
                        return true;

                    if (interruptSource.HasAttribute(SpellAttr2.AllowWhileInvisible) && auraSpellInfo.Dispel == DispelType.Invisibility)
                        return true;
                }

                break;
        }

        return false;
    }

    private bool IsInterruptFlagIgnoredForSpell(SpellAuraInterruptFlags2 flag, Unit unit, SpellInfo auraSpellInfo, SpellInfo interruptSource)
    {
        return false;
    }

    private void RemoveAreaAurasDueToLeaveWorld()
    {
        // make sure that all area auras not applied on self are removed
        foreach (var pair in _ownedAuras.Auras)
        {
            var appMap = pair.ApplicationMap;

            foreach (var aurApp in appMap.Values.ToList())
            {
                var target = aurApp.Target;

                if (target == this)
                    continue;

                target.RemoveAura(aurApp);
                // things linked on aura remove may apply new area aura - so start from the beginning
            }
        }

        // remove area auras owned by others
        _appliedAuras.AuraApplications.CallOnMatch((pair) => pair.Base.Owner != this, (pair) => RemoveAura(pair));
    }

    private void RemoveNoStackAurasDueToAura(Aura aura)
    {
        var spellProto = aura.SpellInfo;

        // passive spell special case (only non stackable with ranks)
        if (spellProto.IsPassiveStackableWithRanks)
            return;

        if (!IsHighestExclusiveAura(aura))
        {
            aura.Remove();

            return;
        }

        _appliedAuras.AuraApplications.CallOnMatch((app) => !aura.CanStackWith(app.Base), (app) => RemoveAura(app));
    }

    private void SendHealSpellLog(HealInfo healInfo, bool critical = false)
    {
        SpellHealLog spellHealLog = new()
        {
            TargetGUID = healInfo.Target.GUID,
            CasterGUID = healInfo.Healer.GUID,
            SpellID = healInfo.SpellInfo.Id,
            Health = (uint)healInfo.Heal,
            OriginalHeal = (int)healInfo.OriginalHeal,
            OverHeal = (uint)(healInfo.Heal - healInfo.EffectiveHeal),
            Absorbed = (uint)healInfo.Absorb,
            Crit = critical
        };

        spellHealLog.LogData.Initialize(healInfo.Target);
        SendCombatLogMessage(spellHealLog);
    }

    private void SetCantProc(bool apply)
    {
        if (apply)
            ++ProcDeep;
        else
            --ProcDeep;
    }

    private void TriggerAurasProcOnEvent(ProcEventInfo eventInfo, List<Tuple<HashSet<int>, AuraApplication>> aurasTriggeringProc)
    {
        var triggeringSpell = eventInfo.ProcSpell;
        var disableProcs = triggeringSpell is { IsProcDisabled: true };

        if (disableProcs)
            SetCantProc(true);

        foreach (var (procEffectMask, aurApp) in aurasTriggeringProc)
        {
            if (aurApp.RemoveMode != 0)
                continue;

            aurApp.Base.TriggerProcOnEvent(procEffectMask, aurApp, eventInfo);
        }

        if (disableProcs)
            SetCantProc(false);
    }

    private void UpdateAuraForGroup()
    {
        var player = AsPlayer;

        if (player != null)
        {
            if (player.Group != null)
                player.SetGroupUpdateFlag(GroupUpdateFlags.Auras);
        }
        else if (IsPet)
        {
            var pet = AsPet;

            if (pet.IsControlled)
                pet.GroupUpdateFlag = GroupUpdatePetFlags.Auras;
        }
    }
}