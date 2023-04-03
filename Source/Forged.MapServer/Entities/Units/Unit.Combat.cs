// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Combat;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Units;

public partial class Unit
{
    public void AddExtraAttacks(uint count)
    {
        var targetGUID = LastDamagedTargetGuid;

        if (!targetGUID.IsEmpty)
        {
            var selection = Target;

            if (!selection.IsEmpty)
                targetGUID = selection; // Spell was cast directly (not triggered by aura)
            else
                return;
        }

        if (!_extraAttacksTargets.ContainsKey(targetGUID))
            _extraAttacksTargets[targetGUID] = 0;

        _extraAttacksTargets[targetGUID] += count;
    }

    public void ApplyAttackTimePercentMod(WeaponAttackType att, double val, bool apply)
    {
        var remainingTimePct = AttackTimer[(int)att] / (_baseAttackSpeed[(int)att] * ModAttackSpeedPct[(int)att]);

        if (val > 0.0f)
        {
            MathFunctions.ApplyPercentModFloatVar(ref ModAttackSpeedPct[(int)att], val, !apply);

            if (att == WeaponAttackType.BaseAttack)
                ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModHaste), (float)val, !apply);
            else if (att == WeaponAttackType.RangedAttack)
                ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModRangedHaste), (float)val, !apply);
        }
        else
        {
            MathFunctions.ApplyPercentModFloatVar(ref ModAttackSpeedPct[(int)att], -val, apply);

            if (att == WeaponAttackType.BaseAttack)
                ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModHaste), -(float)val, apply);
            else if (att == WeaponAttackType.RangedAttack)
                ApplyPercentModUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ModRangedHaste), -(float)val, apply);
        }

        UpdateAttackTimeField(att);
        AttackTimer[(int)att] = (uint)(_baseAttackSpeed[(int)att] * ModAttackSpeedPct[(int)att] * remainingTimePct);
    }

    public virtual void AtDisengage() { }

    public virtual void AtEngage(Unit target) { }

    public virtual void AtEnterCombat()
    {
        foreach (var pair in AppliedAuras)
            pair.Base.CallScriptEnterLeaveCombatHandlers(pair, true);

        var spell = GetCurrentSpell(CurrentSpellTypes.Generic);

        if (spell != null)
            if (spell.State == SpellState.Preparing && spell.SpellInfo.HasAttribute(SpellAttr0.NotInCombatOnlyPeaceful) && spell.SpellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Combat))
                InterruptNonMeleeSpells(false);

        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnteringCombat);
        UnitCombatHelpers.ProcSkillsAndAuras(this, null, new ProcFlagsInit(ProcFlags.EnterCombat), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
    }

    public virtual void AtExitCombat()
    {
        foreach (var pair in AppliedAuras)
            pair.Base.CallScriptEnterLeaveCombatHandlers(pair, false);

        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LeavingCombat);
    }
    public bool Attack(Unit victim, bool meleeAttack)
    {
        if (victim == null || victim.GUID == GUID)
            return false;

        // dead units can neither attack nor be attacked
        if (!IsAlive || !victim.Location.IsInWorld || !victim.IsAlive)
            return false;

        // player cannot attack in mount state
        if (IsTypeId(TypeId.Player) && IsMounted)
            return false;

        var creature = AsCreature;

        // creatures cannot attack while evading
        if (creature != null)
        {
            if (creature.IsInEvadeMode)
                return false;

            if (creature.CanMelee)
                meleeAttack = false;
        }

        // nobody can attack GM in GM-mode
        if (victim.IsTypeId(TypeId.Player))
        {
            if (victim.AsPlayer.IsGameMaster)
                return false;
        }
        else
        {
            if (victim.AsCreature.IsEvadingAttacks)
                return false;
        }

        // remove SPELL_AURA_MOD_UNATTACKABLE at attack (in case non-interruptible spells stun aura applied also that not let attack)
        if (HasAuraType(AuraType.ModUnattackable))
            RemoveAurasByType(AuraType.ModUnattackable);

        if (Attacking != null)
        {
            if (Attacking == victim)
            {
                // switch to melee attack from ranged/magic
                if (meleeAttack)
                {
                    if (!HasUnitState(UnitState.MeleeAttacking))
                    {
                        AddUnitState(UnitState.MeleeAttacking);
                        SendMeleeAttackStart(victim);

                        return true;
                    }
                }
                else if (HasUnitState(UnitState.MeleeAttacking))
                {
                    ClearUnitState(UnitState.MeleeAttacking);
                    SendMeleeAttackStop(victim);

                    return true;
                }

                return false;
            }

            // switch target
            InterruptSpell(CurrentSpellTypes.Melee);

            if (!meleeAttack)
                ClearUnitState(UnitState.MeleeAttacking);
        }

        Attacking?._removeAttacker(this);

        Attacking = victim;
        Attacking._addAttacker(this);

        // Set our target
        SetTarget(victim.GUID);

        if (meleeAttack)
            AddUnitState(UnitState.MeleeAttacking);

        if (creature != null && !ControlledByPlayer)
        {
            EngageWithTarget(victim); // ensure that anything we're attacking has threat

            creature.SendAIReaction(AiReaction.Hostile);
            creature.CallAssistance();

            // Remove emote state - will be restored on creature reset
            EmoteState = Emote.OneshotNone;
        }

        // delay offhand weapon attack by 50% of the base attack time
        if (HaveOffhandWeapon() && TypeId != TypeId.Player)
            SetAttackTimer(WeaponAttackType.OffAttack, Math.Max(GetAttackTimer(WeaponAttackType.OffAttack), GetAttackTimer(WeaponAttackType.BaseAttack) + MathFunctions.CalculatePct(GetBaseAttackTime(WeaponAttackType.BaseAttack), 50)));

        if (meleeAttack)
            SendMeleeAttackStart(victim);

        // Let the pet know we've started attacking someting. Handles melee attacks only
        // Spells such as auto-shot and others handled in WorldSession.HandleCastSpellOpcode
        if (IsTypeId(TypeId.Player))
            foreach (var controlled in Controlled)
            {
                var cControlled = controlled.AsCreature;

                var controlledAI = cControlled?.AI;

                controlledAI?.OwnerAttacked(victim);
            }

        return true;
    }

    public void AttackerStateUpdate(Unit victim, WeaponAttackType attType = WeaponAttackType.BaseAttack, bool extra = false)
    {
        if (HasUnitFlag(UnitFlags.Pacified))
            return;

        if (HasUnitState(UnitState.CannotAutoattack) && !extra)
            return;

        if (HasAuraType(AuraType.DisableAttackingExceptAbilities))
            return;

        if (!victim.IsAlive)
            return;

        if (attType is WeaponAttackType.BaseAttack or WeaponAttackType.OffAttack && !Location.IsWithinLOSInMap(victim))
            return;

        AtTargetAttacked(victim);
        RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Attacking);

        // ignore ranged case
        if (attType != WeaponAttackType.BaseAttack && attType != WeaponAttackType.OffAttack)
            return;

        if (!extra && _lastExtraAttackSpell != 0)
            _lastExtraAttackSpell = 0;

        // melee attack spell casted at main hand attack only - no normal melee dmg dealt
        if (attType == WeaponAttackType.BaseAttack && GetCurrentSpell(CurrentSpellTypes.Melee) != null && !extra)
        {
            CurrentSpells[CurrentSpellTypes.Melee].Cast();
        }
        else
        {
            // attack can be redirected to another target
            victim = GetMeleeHitRedirectTarget(victim);

            var meleeAttackOverrides = GetAuraEffectsByType(AuraType.OverrideAutoattackWithMeleeSpell);
            AuraEffect meleeAttackAuraEffect = null;
            uint meleeAttackSpellId = 0;

            if (attType == WeaponAttackType.BaseAttack)
            {
                if (!meleeAttackOverrides.Empty())
                {
                    meleeAttackAuraEffect = meleeAttackOverrides.First();
                    meleeAttackSpellId = meleeAttackAuraEffect.GetSpellEffectInfo().TriggerSpell;
                }
            }
            else
            {
                var auraEffect = meleeAttackOverrides.Find(aurEff => { return aurEff.GetSpellEffectInfo().MiscValue != 0; });

                if (auraEffect != null)
                {
                    meleeAttackAuraEffect = auraEffect;
                    meleeAttackSpellId = (uint)meleeAttackAuraEffect.GetSpellEffectInfo().MiscValue;
                }
            }

            if (meleeAttackAuraEffect == null)
            {
                CalculateMeleeDamage(victim, out var damageInfo, attType);
                // Send log damage message to client
                UnitCombatHelpers.CheckEvade(damageInfo.Attacker, victim, ref damageInfo.Damage, ref damageInfo.Absorb);

                if (TryGetAI(out var aI))
                    aI.OnMeleeAttack(damageInfo, attType, extra);

                ScriptManager.ForEach<IUnitOnMeleeAttack>(s => s.OnMeleeAttack(damageInfo, attType, extra));

                SendAttackStateUpdate(damageInfo);

                LastDamagedTargetGuid = victim.GUID;

                DealMeleeDamage(damageInfo, true);

                DamageInfo dmgInfo = new(damageInfo);
                UnitCombatHelpers.ProcSkillsAndAuras(damageInfo.Attacker, damageInfo.Target, damageInfo.ProcAttacker, damageInfo.ProcVictim, ProcFlagsSpellType.None, ProcFlagsSpellPhase.None, dmgInfo.HitMask, null, dmgInfo, null);

                Log.Logger.Debug("AttackerStateUpdate: {0} attacked {1} for {2} dmg, absorbed {3}, blocked {4}, resisted {5}.",
                                 GUID.ToString(),
                                 victim.GUID.ToString(),
                                 damageInfo.Damage,
                                 damageInfo.Absorb,
                                 damageInfo.Blocked,
                                 damageInfo.Resist);
            }
            else
            {
                SpellFactory.CastSpell(victim, meleeAttackSpellId, new CastSpellExtraArgs(meleeAttackAuraEffect));

                var hitInfo = HitInfo.AffectsVictim | HitInfo.NoAnimation;

                if (attType == WeaponAttackType.OffAttack)
                    hitInfo |= HitInfo.OffHand;

                SendAttackStateUpdate(hitInfo, victim, GetMeleeDamageSchoolMask(), 0, 0, 0, VictimState.Hit, 0);
            }
        }
    }

    public bool AttackStop()
    {
        if (Attacking == null)
            return false;

        var victim = Attacking;

        Attacking._removeAttacker(this);
        Attacking = null;

        // Clear our target
        SetTarget(ObjectGuid.Empty);

        ClearUnitState(UnitState.MeleeAttacking);

        InterruptSpell(CurrentSpellTypes.Melee);

        // reset only at real combat stop
        var creature = AsCreature;

        creature?.SetNoCallAssistance(false);

        SendMeleeAttackStop(victim);

        return true;
    }

    public void AtTargetAttacked(Unit target, bool canInitialAggro = true)
    {
        if (!target.IsEngaged && !canInitialAggro)
            return;

        target.EngageWithTarget(this);

        var targetOwner = target.CharmerOrOwner;

        targetOwner?.EngageWithTarget(this);

        var myPlayerOwner = CharmerOrOwnerPlayerOrPlayerItself;
        var targetPlayerOwner = target.CharmerOrOwnerPlayerOrPlayerItself;

        if (!myPlayerOwner || !targetPlayerOwner || myPlayerOwner.Duel != null && myPlayerOwner.Duel.Opponent == targetPlayerOwner)
            return;

        myPlayerOwner.UpdatePvP(true);
        myPlayerOwner.SetContestedPvP(targetPlayerOwner);
        myPlayerOwner.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
    }

    public double CalculateDamage(WeaponAttackType attType, bool normalized, bool addTotalPct)
    {
        double minDamage;
        double maxDamage;

        if (normalized || !addTotalPct)
        {
            CalculateMinMaxDamage(attType, normalized, addTotalPct, out minDamage, out maxDamage);

            if (IsInFeralForm && attType == WeaponAttackType.BaseAttack)
            {
                CalculateMinMaxDamage(WeaponAttackType.OffAttack, normalized, addTotalPct, out var minOffhandDamage, out var maxOffhandDamage);
                minDamage += minOffhandDamage;
                maxDamage += maxOffhandDamage;
            }
        }
        else
        {
            switch (attType)
            {
                case WeaponAttackType.RangedAttack:
                    minDamage = UnitData.MinRangedDamage;
                    maxDamage = UnitData.MaxRangedDamage;

                    break;
                case WeaponAttackType.BaseAttack:
                    minDamage = UnitData.MinDamage;
                    maxDamage = UnitData.MaxDamage;

                    if (IsInFeralForm)
                    {
                        minDamage += UnitData.MinOffHandDamage;
                        maxDamage += UnitData.MaxOffHandDamage;
                    }

                    break;
                case WeaponAttackType.OffAttack:
                    minDamage = UnitData.MinOffHandDamage;
                    maxDamage = UnitData.MaxOffHandDamage;

                    break;
                // Just for good manner
                default:
                    minDamage = 0.0f;
                    maxDamage = 0.0f;

                    break;
            }
        }

        minDamage = Math.Max(0.0f, minDamage);
        maxDamage = Math.Max(0.0f, maxDamage);

        if (minDamage > maxDamage)
            Extensions.Swap(ref minDamage, ref maxDamage);

        return RandomHelper.URand(minDamage, maxDamage);
    }

    public virtual bool CanUseAttackType(WeaponAttackType attacktype)
    {
        switch (attacktype)
        {
            case WeaponAttackType.BaseAttack:
                return !HasUnitFlag(UnitFlags.Disarmed);
            case WeaponAttackType.OffAttack:
                return !HasUnitFlag2(UnitFlags2.DisarmOffhand);
            case WeaponAttackType.RangedAttack:
                return !HasUnitFlag2(UnitFlags2.DisarmRanged);
            default:
                return true;
        }
    }

    public virtual bool CheckAttackFitToAuraRequirement(WeaponAttackType attackType, AuraEffect aurEff)
    {
        return true;
    }

    public void ClearInCombat()
    {
        _combatManager.EndAllCombat();
    }

    public void ClearInPetCombat()
    {
        RemoveUnitFlag(UnitFlags.PetInCombat);
        var owner = OwnerUnit;

        owner?.RemoveUnitFlag(UnitFlags.PetInCombat);
    }

    public void CombatStop(bool includingCast = false, bool mutualPvP = true)
    {
        if (includingCast && IsNonMeleeSpellCast(false))
            InterruptNonMeleeSpells(false);

        AttackStop();
        RemoveAllAttackers();

        if (IsTypeId(TypeId.Player))
            AsPlayer.SendAttackSwingCancelAttack(); // melee and ranged forced attack cancel

        if (mutualPvP)
        {
            ClearInCombat();
        }
        else
        {
            // vanish and brethren are weird
            _combatManager.EndAllPvECombat();
            _combatManager.SuppressPvPCombat();
        }
    }

    public void CombatStopWithPets(bool includingCast = false)
    {
        CombatStop(includingCast);

        foreach (var minion in Controlled)
            minion.CombatStop(includingCast);
    }

    public void EngageWithTarget(Unit enemy)
    {
        if (enemy == null)
            return;

        if (CanHaveThreatList)
            _threatManager.AddThreat(enemy, 0.0f, null, true, true);
        else
            SetInCombatWith(enemy);
    }

    public double GetApMultiplier(WeaponAttackType attType, bool normalized)
    {
        if (!IsTypeId(TypeId.Player) || (IsInFeralForm && !normalized))
            return GetBaseAttackTime(attType) / 1000.0f;

        var weapon = AsPlayer.GetWeaponForAttack(attType, true);

        if (!weapon)
            return 2.0f;

        if (!normalized)
            return weapon.Template.Delay / 1000.0f;

        switch ((ItemSubClassWeapon)weapon.Template.SubClass)
        {
            case ItemSubClassWeapon.Axe2:
            case ItemSubClassWeapon.Mace2:
            case ItemSubClassWeapon.Polearm:
            case ItemSubClassWeapon.Sword2:
            case ItemSubClassWeapon.Staff:
            case ItemSubClassWeapon.FishingPole:
                return 3.3f;
            case ItemSubClassWeapon.Axe:
            case ItemSubClassWeapon.Mace:
            case ItemSubClassWeapon.Sword:
            case ItemSubClassWeapon.Warglaives:
            case ItemSubClassWeapon.Exotic:
            case ItemSubClassWeapon.Exotic2:
            case ItemSubClassWeapon.Fist:
                return 2.4f;
            case ItemSubClassWeapon.Dagger:
                return 1.7f;
            case ItemSubClassWeapon.Thrown:
                return 2.0f;
            default:
                return weapon.Template.Delay / 1000.0f;
        }
    }

    public Unit GetAttackerForHelper()
    {
        if (!IsEngaged)
            return null;

        var victim = Victim;

        if (victim != null)
            if ((!IsPet && PlayerMovingMe1 == null) || IsInCombatWith(victim))
                return victim;

        var mgr = GetCombatManager();
        // pick arbitrary targets; our pvp combat > owner's pvp combat > our pve combat > owner's pve combat
        var owner = CharmerOrOwner;

        if (mgr.HasPvPCombat())
            return mgr.PvPCombatRefs.First().Value.GetOther(this);

        if (owner && (owner.GetCombatManager().HasPvPCombat()))
            return owner.GetCombatManager().PvPCombatRefs.First().Value.GetOther(owner);

        if (mgr.HasPvECombat())
            return mgr.PvECombatRefs.First().Value.GetOther(this);

        if (owner && (owner.GetCombatManager().HasPvECombat()))
            return owner.GetCombatManager().PvECombatRefs.First().Value.GetOther(owner);

        return null;
    }

    public uint GetAttackTimer(WeaponAttackType type)
    {
        return AttackTimer[(int)type];
    }

    public uint GetBaseAttackTime(WeaponAttackType att)
    {
        return _baseAttackSpeed[(int)att];
    }

    public uint GetLastExtraAttackSpell()
    {
        return _lastExtraAttackSpell;
    }

    public Unit GetMeleeHitRedirectTarget(Unit victim, SpellInfo spellInfo = null)
    {
        var interceptAuras = victim.GetAuraEffectsByType(AuraType.InterceptMeleeRangedAttacks);

        foreach (var i in interceptAuras)
        {
            var magnet = i.Caster;

            if (magnet != null)
                if (WorldObjectCombat.IsValidAttackTarget(magnet, spellInfo) && magnet.Location.IsWithinLOSInMap(this) && (spellInfo == null || (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk && spellInfo.CheckTarget(this, magnet, false) == SpellCastResult.SpellCastOk)))
                {
                    i.Base.DropCharge(AuraRemoveMode.Expire);

                    return magnet;
                }
        }

        return victim;
    }

    public float GetMeleeRange(Unit target)
    {
        var range = CombatReach + target.CombatReach + 4.0f / 3.0f;

        return Math.Max(range, SharedConst.NominalMeleeRange);
    }

    public double GetTotalAttackPowerValue(WeaponAttackType attType, bool includeWeapon = true)
    {
        if (attType == WeaponAttackType.RangedAttack)
        {
            double ap = UnitData.RangedAttackPower + UnitData.RangedAttackPowerModPos + UnitData.RangedAttackPowerModNeg;

            if (includeWeapon)
                ap += Math.Max(UnitData.MainHandWeaponAttackPower, UnitData.RangedWeaponAttackPower);

            if (ap < 0)
                return 0.0f;

            return ap * (1.0f + UnitData.RangedAttackPowerMultiplier);
        }
        else
        {
            double ap = UnitData.AttackPower + UnitData.AttackPowerModPos + UnitData.AttackPowerModNeg;

            if (includeWeapon)
            {
                if (attType == WeaponAttackType.BaseAttack)
                {
                    ap += Math.Max(UnitData.MainHandWeaponAttackPower, UnitData.RangedWeaponAttackPower);
                }
                else
                {
                    ap += UnitData.OffHandWeaponAttackPower;
                    ap /= 2;
                }
            }

            if (ap < 0)
                return 0.0f;

            return ap * (1.0f + UnitData.AttackPowerMultiplier);
        }
    }

    public double GetWeaponDamageRange(WeaponAttackType attType, WeaponDamageRange type)
    {
        if (attType == WeaponAttackType.OffAttack && !HaveOffhandWeapon())
            return 0.0f;

        return WeaponDamage[(int)attType][(int)type];
    }

    public void HandleProcExtraAttackFor(Unit victim, uint count)
    {
        while (count != 0)
        {
            --count;
            AttackerStateUpdate(victim, WeaponAttackType.BaseAttack, true);
        }
    }

    public bool IsAttackReady(WeaponAttackType type = WeaponAttackType.BaseAttack)
    {
        return AttackTimer[(int)type] == 0;
    }

    public bool IsEngagedBy(Unit who)
    {
        return CanHaveThreatList ? IsThreatenedBy(who) : IsInCombatWith(who);
    }

    public bool IsInCombatWith(Unit who)
    {
        return who != null && _combatManager.IsInCombatWith(who);
    }

    public bool IsSilenced(uint schoolMask)
    {
        return (UnitData.SilencedSchoolMask.Value & schoolMask) != 0;
    }

    public bool IsSilenced(SpellSchoolMask schoolMask)
    {
        return IsSilenced((uint)schoolMask);
    }

    public bool IsTargetableForAttack(bool checkFakeDeath = true)
    {
        if (!IsAlive)
            return false;

        if (HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible))
            return false;

        if (IsTypeId(TypeId.Player) && AsPlayer.IsGameMaster)
            return false;

        return !HasUnitState(UnitState.Unattackable) && (!checkFakeDeath || !HasUnitState(UnitState.Died));
    }

    public bool IsThreatenedBy(Unit who)
    {
        return who != null && _threatManager.IsThreatenedBy(who, true);
    }

    public bool IsWithinMeleeRange(Unit obj)
    {
        return IsWithinMeleeRangeAt(Location, obj);
    }

    public bool IsWithinMeleeRangeAt(Position pos, Unit obj)
    {
        if (!obj || !Location.IsInMap(obj) || !Location.InSamePhase(obj))
            return false;

        var dx = pos.X - obj.Location.X;
        var dy = pos.Y - obj.Location.Y;
        var dz = pos.Z - obj.Location.Z;
        var distsq = (dx * dx) + (dy * dy) + (dz * dz);

        var maxdist = GetMeleeRange(obj) + GetTotalAuraModifier(AuraType.ModAutoAttackRange);

        return distsq <= maxdist * maxdist;
    }

    public void KillSelf(bool durabilityLoss = true, bool skipSettingDeathState = false)
    {
        UnitCombatHelpers.Kill(this, this, durabilityLoss, skipSettingDeathState);
    }

    public virtual void OnCombatExit()
    {
        foreach (var aurApp in AppliedAuras)
            aurApp.Base.CallScriptEnterLeaveCombatHandlers(aurApp, false);
    }

    public void RemoveAllAttackers()
    {
        while (!AttackerList.Empty())
        {
            var iter = AttackerList.First();

            if (!iter.AttackStop())
            {
                Log.Logger.Error("WORLD: Unit has an attacker that isn't attacking it!");
                AttackerList.Remove(iter);
            }
        }
    }

    public void ReplaceAllSilencedSchoolMask(uint schoolMask)
    {
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SilencedSchoolMask), schoolMask);
    }

    public void ReplaceAllSilencedSchoolMask(SpellSchoolMask schoolMask)
    {
        ReplaceAllSilencedSchoolMask((uint)schoolMask);
    }

    public void ResetAttackTimer(WeaponAttackType type = WeaponAttackType.BaseAttack)
    {
        AttackTimer[(int)type] = (uint)(GetBaseAttackTime(type) * ModAttackSpeedPct[(int)type]);
    }

    public void SendAttackStateUpdate(HitInfo hitInfo, Unit target, SpellSchoolMask damageSchoolMask, double damage, double absorbDamage, double resist, VictimState targetState, uint blockedAmount)
    {
        CalcDamageInfo dmgInfo = new()
        {
            HitInfo = hitInfo,
            Attacker = this,
            Target = target,
            Damage = damage - absorbDamage - resist - blockedAmount,
            OriginalDamage = damage,
            DamageSchoolMask = (uint)damageSchoolMask,
            Absorb = absorbDamage,
            Resist = resist,
            TargetState = targetState,
            Blocked = blockedAmount
        };

        SendAttackStateUpdate(dmgInfo);
    }

    public void SendAttackStateUpdate(CalcDamageInfo damageInfo)
    {
        AttackerStateUpdate packet = new()
        {
            hitInfo = damageInfo.HitInfo,
            AttackerGUID = damageInfo.Attacker.GUID,
            VictimGUID = damageInfo.Target.GUID,
            Damage = (int)damageInfo.Damage,
            OriginalDamage = (int)damageInfo.OriginalDamage
        };

        var overkill = (int)(damageInfo.Damage - damageInfo.Target.Health);
        packet.OverDamage = (overkill < 0 ? -1 : overkill);

        SubDamage subDmg = new()
        {
            SchoolMask = (int)damageInfo.DamageSchoolMask, // School of sub damage
            FDamage = (float)damageInfo.Damage,            // sub damage
            Damage = (int)damageInfo.Damage,               // Sub Damage
            Absorbed = (int)damageInfo.Absorb,
            Resisted = (int)damageInfo.Resist
        };

        packet.SubDmg = subDmg;

        packet.VictimState = (byte)damageInfo.TargetState;
        packet.BlockAmount = (int)damageInfo.Blocked;
        packet.LogData.Initialize(damageInfo.Attacker);

        ContentTuningParams contentTuningParams = new();

        if (contentTuningParams.GenerateDataForUnits(damageInfo.Attacker, damageInfo.Target))
            packet.ContentTuning = contentTuningParams;

        SendCombatLogMessage(packet);
    }

    public void SendMeleeAttackStart(Unit victim)
    {
        AttackStart packet = new()
        {
            Attacker = GUID,
            Victim = victim.GUID
        };

        SendMessageToSet(packet, true);
    }

    public void SendMeleeAttackStop(Unit victim = null)
    {
        SendMessageToSet(new SAttackStop(this, victim), true);

        if (victim != null)
            Log.Logger.Information("{0} {1} stopped attacking {2} {3}",
                                   (IsTypeId(TypeId.Player) ? "Player" : "Creature"),
                                   GUID.ToString(),
                                   (victim.IsTypeId(TypeId.Player) ? "player" : "creature"),
                                   victim.GUID.ToString());
        else
            Log.Logger.Information("{0} {1} stopped attacking", (IsTypeId(TypeId.Player) ? "Player" : "Creature"), GUID.ToString());
    }

    public void SetAttackTimer(WeaponAttackType type, uint time)
    {
        AttackTimer[(int)type] = time;
    }

    public void SetBaseAttackTime(WeaponAttackType att, uint val)
    {
        _baseAttackSpeed[(int)att] = val;
        UpdateAttackTimeField(att);
    }

    public void SetBaseWeaponDamage(WeaponAttackType attType, WeaponDamageRange damageRange, double value)
    {
        WeaponDamage[(int)attType][(int)damageRange] = value;
    }

    public void SetCombatReach(float combatReach)
    {
        if (combatReach > 0.1f)
            combatReach = SharedConst.DefaultPlayerCombatReach;

        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CombatReach), combatReach);
    }

    public void SetInCombatWith(Unit enemy, bool addSecondUnitSuppressed = false)
    {
        if (enemy != null)
            _combatManager.SetInCombatWith(enemy, addSecondUnitSuppressed);
    }

    public void SetInCombatWithZone()
    {
        if (!CanHaveThreatList)
            return;

        var map = Location.Map;

        if (!map.IsDungeon)
        {
            Log.Logger.Error($"Creature entry {Entry} call SetInCombatWithZone for map (id: {map.Entry}) that isn't an instance.");

            return;
        }

        var players = map.Players;

        foreach (var player in players)
        {
            if (player.IsGameMaster)
                continue;

            if (player.IsAlive)
            {
                SetInCombatWith(player);
                player.SetInCombatWith(this);
                GetThreatManager().AddThreat(player, 0);
            }
        }
    }
    /// <summary>
    ///     enables / disables combat interaction of this unit
    /// </summary>
    public void SetIsCombatDisallowed(bool apply)
    {
        IsCombatDisallowed = apply;
    }

    public void SetLastExtraAttackSpell(uint spellId)
    {
        _lastExtraAttackSpell = spellId;
    }

    public void SetSilencedSchoolMask(uint schoolMask)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SilencedSchoolMask), schoolMask);
    }

    public void SetSilencedSchoolMask(SpellSchoolMask schoolMask)
    {
        SetSilencedSchoolMask((uint)schoolMask);
    }
    public virtual void SetTarget(ObjectGuid guid) { }

    public void StopAttackFaction(uint factionId)
    {
        var victim = Victim;

        if (victim != null)
            if (victim.WorldObjectCombat.GetFactionTemplateEntry().Faction == factionId)
            {
                AttackStop();

                if (IsNonMeleeSpellCast(false))
                    InterruptNonMeleeSpells(false);

                // melee and ranged forced attack cancel
                if (IsTypeId(TypeId.Player))
                    AsPlayer.SendAttackSwingCancelAttack();
            }

        var attackers = Attackers;

        for (var i = 0; i < attackers.Count;)
        {
            var unit = attackers[i];

            if (unit.WorldObjectCombat.GetFactionTemplateEntry().Faction == factionId)
            {
                unit.AttackStop();
                i = 0;
            }
            else
            {
                ++i;
            }
        }

        List<CombatReference> refsToEnd = new();

        foreach (var pair in _combatManager.PvECombatRefs)
            if (pair.Value.GetOther(this).WorldObjectCombat.GetFactionTemplateEntry().Faction == factionId)
                refsToEnd.Add(pair.Value);

        foreach (var refe in refsToEnd)
            refe.EndCombat();

        foreach (var minion in Controlled)
            minion.StopAttackFaction(factionId);
    }

    public void ValidateAttackersAndOwnTarget()
    {
        // iterate attackers
        List<Unit> toRemove = new();

        foreach (var attacker in Attackers)
            if (!attacker.WorldObjectCombat.IsValidAttackTarget(this))
                toRemove.Add(attacker);

        foreach (var attacker in toRemove)
            attacker.AttackStop();

        // remove our own victim
        var victim = Victim;

        if (victim != null)
            if (!WorldObjectCombat.IsValidAttackTarget(victim))
                AttackStop();
    }
    private void _addAttacker(Unit pAttacker)
    {
        AttackerList.Add(pAttacker);
    }

    private void _removeAttacker(Unit pAttacker)
    {
        AttackerList.Remove(pAttacker);
    }

    // TODO for melee need create structure as in
    private void CalculateMeleeDamage(Unit victim, out CalcDamageInfo damageInfo, WeaponAttackType attackType)
    {
        damageInfo = new CalcDamageInfo
        {
            Attacker = this,
            Target = victim,
            DamageSchoolMask = (uint)SpellSchoolMask.Normal,
            Damage = 0,
            OriginalDamage = 0,
            Absorb = 0,
            Resist = 0,
            Blocked = 0,
            HitInfo = 0,
            TargetState = 0,
            AttackType = attackType,
            ProcAttacker = new ProcFlagsInit(),
            ProcVictim = new ProcFlagsInit(),
            CleanDamage = 0,
            HitOutCome = MeleeHitOutcome.Evade
        };

        if (victim == null)
            return;

        if (!IsAlive || !victim.IsAlive)
            return;

        // Select HitInfo/procAttacker/procVictim Id based on attack type
        switch (attackType)
        {
            case WeaponAttackType.BaseAttack:
                damageInfo.ProcAttacker = new ProcFlagsInit(ProcFlags.DealMeleeSwing | ProcFlags.MainHandWeaponSwing);
                damageInfo.ProcVictim = new ProcFlagsInit(ProcFlags.TakeMeleeSwing);

                break;
            case WeaponAttackType.OffAttack:
                damageInfo.ProcAttacker = new ProcFlagsInit(ProcFlags.DealMeleeSwing | ProcFlags.OffHandWeaponSwing);
                damageInfo.ProcVictim = new ProcFlagsInit(ProcFlags.TakeMeleeSwing);
                damageInfo.HitInfo = HitInfo.OffHand;

                break;
            default:
                return;
        }

        // Physical Immune check
        if (damageInfo.Target.IsImmunedToDamage((SpellSchoolMask)damageInfo.DamageSchoolMask))
        {
            damageInfo.HitInfo |= HitInfo.NormalSwing;
            damageInfo.TargetState = VictimState.Immune;

            damageInfo.Damage = 0;
            damageInfo.CleanDamage = 0;

            return;
        }

        double damage = 0;
        damage += CalculateDamage(damageInfo.AttackType, false, true);
        // Add melee damage bonus
        damage = MeleeDamageBonusDone(damageInfo.Target, damage, damageInfo.AttackType, DamageEffectType.Direct, null, null, (SpellSchoolMask)damageInfo.DamageSchoolMask);
        damage = damageInfo.Target.MeleeDamageBonusTaken(this, damage, damageInfo.AttackType, DamageEffectType.Direct, null, (SpellSchoolMask)damageInfo.DamageSchoolMask);

        // Script Hook For CalculateMeleeDamage -- Allow scripts to change the Damage pre class mitigation calculations
        var t = damageInfo.Target;
        var a = damageInfo.Attacker;
        UnitCombatHelpers.ScaleDamage(a, t, ref damage);

        ScriptManager.ForEach<IUnitModifyMeleeDamage>(p => p.ModifyMeleeDamage(t, a, ref damage));

        // Calculate armor reduction
        if (UnitCombatHelpers.IsDamageReducedByArmor((SpellSchoolMask)damageInfo.DamageSchoolMask))
        {
            damageInfo.Damage = UnitCombatHelpers.CalcArmorReducedDamage(damageInfo.Attacker, damageInfo.Target, damage, null, damageInfo.AttackType);
            damageInfo.CleanDamage += damage - damageInfo.Damage;
        }
        else
        {
            damageInfo.Damage = damage;
        }

        damageInfo.HitOutCome = RollMeleeOutcomeAgainst(damageInfo.Target, damageInfo.AttackType);

        switch (damageInfo.HitOutCome)
        {
            case MeleeHitOutcome.Evade:
                damageInfo.HitInfo |= HitInfo.Miss | HitInfo.SwingNoHitSound;
                damageInfo.TargetState = VictimState.Evades;
                damageInfo.OriginalDamage = damageInfo.Damage;

                damageInfo.Damage = 0;
                damageInfo.CleanDamage = 0;

                return;
            case MeleeHitOutcome.Miss:
                damageInfo.HitInfo |= HitInfo.Miss;
                damageInfo.TargetState = VictimState.Intact;
                damageInfo.OriginalDamage = damageInfo.Damage;

                damageInfo.Damage = 0;
                damageInfo.CleanDamage = 0;

                break;
            case MeleeHitOutcome.Normal:
                damageInfo.TargetState = VictimState.Hit;
                damageInfo.OriginalDamage = damageInfo.Damage;

                break;
            case MeleeHitOutcome.Crit:
                damageInfo.HitInfo |= HitInfo.CriticalHit;
                damageInfo.TargetState = VictimState.Hit;
                // Crit bonus calc
                damageInfo.Damage *= 2;

                // Increase crit damage from SPELL_AURA_MOD_CRIT_DAMAGE_BONUS
                var mod = (GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, damageInfo.DamageSchoolMask) - 1.0f) * 100;

                if (mod != 0)
                    MathFunctions.AddPct(ref damageInfo.Damage, mod);

                damageInfo.OriginalDamage = damageInfo.Damage;

                break;
            case MeleeHitOutcome.Parry:
                damageInfo.TargetState = VictimState.Parry;
                damageInfo.CleanDamage += damageInfo.Damage;

                damageInfo.OriginalDamage = damageInfo.Damage;
                damageInfo.Damage = 0;

                break;
            case MeleeHitOutcome.Dodge:
                damageInfo.TargetState = VictimState.Dodge;
                damageInfo.CleanDamage += damageInfo.Damage;

                damageInfo.OriginalDamage = damageInfo.Damage;
                damageInfo.Damage = 0;

                break;
            case MeleeHitOutcome.Block:
                damageInfo.TargetState = VictimState.Hit;
                damageInfo.HitInfo |= HitInfo.Block;
                // 30% damage blocked, double blocked amount if block is critical
                damageInfo.Blocked = MathFunctions.CalculatePct(damageInfo.Damage, damageInfo.Target.GetBlockPercent(Level));

                if (damageInfo.Target.IsBlockCritical())
                    damageInfo.Blocked *= 2;

                damageInfo.OriginalDamage = damageInfo.Damage;
                damageInfo.Damage -= damageInfo.Blocked;
                damageInfo.CleanDamage += damageInfo.Blocked;

                break;
            case MeleeHitOutcome.Glancing:
                damageInfo.HitInfo |= HitInfo.Glancing;
                damageInfo.TargetState = VictimState.Hit;
                var leveldif = (int)victim.Level - (int)Level;

                if (leveldif > 3)
                    leveldif = 3;

                damageInfo.OriginalDamage = damageInfo.Damage;
                var reducePercent = 1.0f - leveldif * 0.1f;
                damageInfo.CleanDamage += damageInfo.Damage - (reducePercent * damageInfo.Damage);
                damageInfo.Damage = reducePercent * damageInfo.Damage;

                break;
            case MeleeHitOutcome.Crushing:
                damageInfo.HitInfo |= HitInfo.Crushing;
                damageInfo.TargetState = VictimState.Hit;
                // 150% normal damage
                damageInfo.Damage += (damageInfo.Damage / 2);
                damageInfo.OriginalDamage = damageInfo.Damage;

                break;
        }

        // Always apply HITINFO_AFFECTS_VICTIM in case its not a miss
        if (!damageInfo.HitInfo.HasAnyFlag(HitInfo.Miss))
            damageInfo.HitInfo |= HitInfo.AffectsVictim;

        var resilienceReduction = damageInfo.Damage;

        if (CanApplyResilience())
            UnitCombatHelpers.ApplyResilience(victim, ref resilienceReduction);

        resilienceReduction = damageInfo.Damage - resilienceReduction;
        damageInfo.Damage -= resilienceReduction;
        damageInfo.CleanDamage += resilienceReduction;

        // Calculate absorb resist
        if (damageInfo.Damage > 0)
        {
            damageInfo.ProcVictim.Or(ProcFlags.TakeAnyDamage);
            // Calculate absorb & resists
            DamageInfo dmgInfo = new(damageInfo);
            UnitCombatHelpers.CalcAbsorbResist(dmgInfo);
            damageInfo.Absorb = dmgInfo.Absorb;
            damageInfo.Resist = dmgInfo.Resist;

            if (damageInfo.Absorb != 0)
                damageInfo.HitInfo |= (damageInfo.Damage - damageInfo.Absorb == 0 ? HitInfo.FullAbsorb : HitInfo.PartialAbsorb);

            if (damageInfo.Resist != 0)
                damageInfo.HitInfo |= (damageInfo.Damage - damageInfo.Resist == 0 ? HitInfo.FullResist : HitInfo.PartialResist);

            damageInfo.Damage = dmgInfo.Damage;
        }
        else // Impossible get negative result but....
        {
            damageInfo.Damage = 0;
        }
    }

    private MeleeHitOutcome RollMeleeOutcomeAgainst(Unit victim, WeaponAttackType attType)
    {
        if (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsEvadingAttacks)
            return MeleeHitOutcome.Evade;

        // Miss chance based on melee
        var missChance = MeleeSpellMissChance(victim, attType, null) * 100.0f;

        // Critical hit chance
        var critChance = (GetUnitCriticalChanceAgainst(attType, victim) + GetTotalAuraModifier(AuraType.ModAutoAttackCritChance)) * 100.0f;

        var dodgeChance = GetUnitDodgeChance(attType, victim) * 100.0f;
        var blockChance = GetUnitBlockChance(victim) * 100.0f;
        var parryChance = GetUnitParryChance(attType, victim) * 100.0f;

        // melee attack table implementation
        // outcome priority:
        //   1. >    2. >    3. >       4. >    5. >   6. >       7. >  8.
        // MISS > DODGE > PARRY > GLANCING > BLOCK > CRIT > CRUSHING > HIT

        double sum = 0;
        var roll = RandomHelper.IRand(0, 9999);

        var attackerLevel = GetLevelForTarget(victim);
        var victimLevel = GetLevelForTarget(this);

        // check if attack comes from behind, nobody can parry or block if attacker is behind
        var canParryOrBlock = victim.Location.HasInArc((float)Math.PI, Location) || victim.HasAuraType(AuraType.IgnoreHitDirection);

        // only creatures can dodge if attacker is behind
        var canDodge = !victim.IsTypeId(TypeId.Player) || canParryOrBlock;

        // if victim is casting or cc'd it can't avoid attacks
        if (victim.IsNonMeleeSpellCast(false, false, true) || victim.HasUnitState(UnitState.Controlled))
        {
            canDodge = false;
            canParryOrBlock = false;
        }

        // 1. MISS
        var tmp = missChance;

        if (tmp > 0 && roll < (sum += tmp))
            return MeleeHitOutcome.Miss;

        // always crit against a sitting target (except 0 crit chance)
        if (victim.IsTypeId(TypeId.Player) && critChance > 0 && !victim.IsStandState)
            return MeleeHitOutcome.Crit;

        // 2. DODGE
        if (canDodge)
        {
            tmp = dodgeChance;

            if (tmp > 0 // check if unit _can_ dodge
                &&
                roll < (sum += tmp))
                return MeleeHitOutcome.Dodge;
        }

        // 3. PARRY
        if (canParryOrBlock)
        {
            tmp = parryChance;

            if (tmp > 0 // check if unit _can_ parry
                &&
                roll < (sum += tmp))
                return MeleeHitOutcome.Parry;
        }

        // 4. GLANCING
        // Max 40% chance to score a glancing blow against mobs that are higher level (can do only players and pets and not with ranged weapon)
        if ((IsTypeId(TypeId.Player) || IsPet) &&
            !victim.IsTypeId(TypeId.Player) &&
            !victim.IsPet &&
            attackerLevel + 3 < victimLevel)
        {
            // cap possible value (with bonuses > max skill)
            tmp = (int)(10 + 10 * (victimLevel - attackerLevel)) * 100;

            if (tmp > 0 && roll < (sum += tmp))
                return MeleeHitOutcome.Glancing;
        }

        // 5. BLOCK
        if (canParryOrBlock)
        {
            tmp = blockChance;

            if (tmp > 0 // check if unit _can_ block
                &&
                roll < (sum += tmp))
                return MeleeHitOutcome.Block;
        }

        // 6.CRIT
        tmp = critChance;

        if (tmp > 0 && roll < (sum += tmp))
            return MeleeHitOutcome.Crit;

        // 7. CRUSHING
        // mobs can score crushing blows if they're 4 or more levels above victim
        if (attackerLevel >= victimLevel + 4 &&
            // can be from by creature (if can) or from controlled player that considered as creature
            !ControlledByPlayer &&
            !(TypeId == TypeId.Unit && AsCreature.Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoCrushingBlows)))
        {
            // add 2% chance per level, min. is 15%
            tmp = (int)(attackerLevel - victimLevel * 1000 - 1500);

            if (roll < (sum += tmp))
            {
                Log.Logger.Debug("RollMeleeOutcomeAgainst: CRUSHING <{0}, {1})", sum - tmp, sum);

                return MeleeHitOutcome.Crushing;
            }
        }

        // 8. HIT
        return MeleeHitOutcome.Normal;
    }

    private void UpdateAttackTimeField(WeaponAttackType att)
    {
        switch (att)
        {
            case WeaponAttackType.BaseAttack:
            case WeaponAttackType.OffAttack:
                SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.AttackRoundBaseTime, (int)att), (uint)(_baseAttackSpeed[(int)att] * ModAttackSpeedPct[(int)att]));

                break;
            case WeaponAttackType.RangedAttack:
                SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.RangedAttackRoundBaseTime), (uint)(_baseAttackSpeed[(int)att] * ModAttackSpeedPct[(int)att]));

                break;
        }
    }
}