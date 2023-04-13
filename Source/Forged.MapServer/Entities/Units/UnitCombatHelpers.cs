// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.BattleFields;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Party;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Entities.Units;

public class UnitCombatHelpers
{
    private readonly BattleFieldManager _battleFieldManager;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly LootFactory _lootFactory;
    private readonly LootManager _lootManager;
    private readonly LootStoreBox _lootStorage;
    private readonly ObjectAccessor _objectAccessor;
    private readonly ScriptManager _scriptManager;
    public UnitCombatHelpers(ScriptManager scriptManager, LootFactory lootFactory, ObjectAccessor objectAccessor, LootStoreBox lootStorage,
                             LootManager lootManager, IConfiguration configuration, BattleFieldManager battleFieldManager, DB2Manager db2Manager)
    {
        _scriptManager = scriptManager;
        _lootFactory = lootFactory;
        _objectAccessor = objectAccessor;
        _lootStorage = lootStorage;
        _lootManager = lootManager;
        _configuration = configuration;
        _battleFieldManager = battleFieldManager;
        _db2Manager = db2Manager;
    }

    public void ApplyResilience(Unit victim, ref double damage)
    {
        // player mounted on multi-passenger mount is also classified as vehicle
        if (victim.IsVehicle && !victim.IsPlayer)
            return;

        Unit target = null;

        if (victim.IsPlayer)
        {
            target = victim;
        }
        else // victim->GetTypeId() == TYPEID_UNIT
        {
            var owner = victim.OwnerUnit;

            if (owner is { IsPlayer: true })
                target = owner;
        }

        if (target == null)
            return;

        damage -= target.GetDamageReduction(damage);
    }

    public void CalcAbsorbResist(DamageInfo damageInfo, Spell spell = null)
    {
        if (damageInfo.Victim == null || !damageInfo.Victim.IsAlive || damageInfo.Damage == 0)
            return;

        var resistedDamage = CalcSpellResistedDamage(damageInfo.Attacker, damageInfo.Victim, damageInfo.Damage, damageInfo.SchoolMask, damageInfo.SpellInfo);

        // Ignore Absorption Auras
        double auraAbsorbMod = 0f;

        var attacker = damageInfo.Attacker;

        if (attacker != null)
            auraAbsorbMod = attacker.GetMaxPositiveAuraModifierByMiscMask(AuraType.ModTargetAbsorbSchool, (uint)damageInfo.SchoolMask);

        MathFunctions.RoundToInterval(ref auraAbsorbMod, 0.0f, 100.0f);

        var absorbIgnoringDamage = MathFunctions.CalculatePct(damageInfo.Damage, auraAbsorbMod);

        spell?.CallScriptOnResistAbsorbCalculateHandlers(damageInfo, ref resistedDamage, ref absorbIgnoringDamage);

        damageInfo.ResistDamage(resistedDamage);

        // We're going to call functions which can modify content of the list during iteration over it's elements
        // Let's copy the list so we can prevent iterator invalidation
        var vSchoolAbsorbCopy = damageInfo.Victim.GetAuraEffectsByType(AuraType.SchoolAbsorb);
        vSchoolAbsorbCopy.Sort(new AbsorbAuraOrderPred());

        // absorb without mana cost
        for (var i = 0; i < vSchoolAbsorbCopy.Count && damageInfo.Damage > 0; ++i)
        {
            var absorbAurEff = vSchoolAbsorbCopy[i];

            // Check if aura was removed during iteration - we don't need to work on such auras
            var aurApp = absorbAurEff.Base.GetApplicationOfTarget(damageInfo.Victim.GUID);

            if (aurApp == null)
                continue;

            if ((absorbAurEff.MiscValue & (int)damageInfo.SchoolMask) == 0)
                continue;

            // get amount which can be still absorbed by the aura
            var currentAbsorb = absorbAurEff.Amount;

            // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
            if (currentAbsorb < 0)
                currentAbsorb = 0;

            if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                damageInfo.ModifyDamage(-absorbIgnoringDamage);

            var tempAbsorb = currentAbsorb;

            var defaultPrevented = false;

            absorbAurEff.Base.CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
            currentAbsorb = (int)tempAbsorb;

            if (!defaultPrevented)
            {
                // absorb must be smaller than the damage itself
                currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.Damage);

                damageInfo.AbsorbDamage(currentAbsorb);

                tempAbsorb = (uint)currentAbsorb;
                absorbAurEff.Base.CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

                // Check if our aura is using amount to count heal
                if (absorbAurEff.Amount >= 0)
                {
                    // Reduce shield amount
                    absorbAurEff.ChangeAmount(absorbAurEff.Amount - currentAbsorb);

                    // Aura cannot absorb anything more - remove it
                    if (absorbAurEff.Amount <= 0 && !absorbAurEff.Base.SpellInfo.HasAttribute(SpellAttr0.Passive))
                        absorbAurEff.Base.Remove(AuraRemoveMode.EnemySpell);
                }
            }

            if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                damageInfo.ModifyDamage(absorbIgnoringDamage);

            if (currentAbsorb != 0)
            {
                SpellAbsorbLog absorbLog = new()
                {
                    Attacker = damageInfo.Attacker?.GUID ?? ObjectGuid.Empty,
                    Victim = damageInfo.Victim.GUID,
                    Caster = absorbAurEff.Base.CasterGuid,
                    AbsorbedSpellID = damageInfo.SpellInfo?.Id ?? 0,
                    AbsorbSpellID = absorbAurEff.Id,
                    Absorbed = (int)currentAbsorb,
                    OriginalDamage = (uint)damageInfo.OriginalDamage
                };

                absorbLog.LogData.Initialize(damageInfo.Victim);
                damageInfo.Victim.SendCombatLogMessage(absorbLog);
            }
        }

        // absorb by mana cost
        var vManaShieldCopy = damageInfo.Victim.GetAuraEffectsByType(AuraType.ManaShield);

        foreach (var absorbAurEff in vManaShieldCopy)
        {
            if (damageInfo.Damage == 0)
                break;

            // Check if aura was removed during iteration - we don't need to work on such auras
            var aurApp = absorbAurEff.Base.GetApplicationOfTarget(damageInfo.Victim.GUID);

            if (aurApp == null)
                continue;

            // check damage school mask
            if (!Convert.ToBoolean(absorbAurEff.MiscValue & (int)damageInfo.SchoolMask))
                continue;

            // get amount which can be still absorbed by the aura
            var currentAbsorb = absorbAurEff.Amount;

            // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
            if (currentAbsorb < 0)
                currentAbsorb = 0;

            if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                damageInfo.ModifyDamage(-absorbIgnoringDamage);

            var tempAbsorb = currentAbsorb;

            var defaultPrevented = false;

            absorbAurEff.Base.CallScriptEffectManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref defaultPrevented);
            currentAbsorb = (int)tempAbsorb;

            if (!defaultPrevented)
            {
                // absorb must be smaller than the damage itself
                currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.Damage);

                var manaReduction = currentAbsorb;

                // lower absorb amount by talents
                var manaMultiplier = absorbAurEff.GetSpellEffectInfo().CalcValueMultiplier(absorbAurEff.Caster);

                if (manaMultiplier != 0)
                    manaReduction = (int)(manaReduction * manaMultiplier);

                var manaTaken = -damageInfo.Victim.ModifyPower(PowerType.Mana, -manaReduction);

                // take case when mana has ended up into account
                currentAbsorb = currentAbsorb != 0 ? currentAbsorb * (manaTaken / manaReduction) : 0;

                damageInfo.AbsorbDamage((uint)currentAbsorb);

                tempAbsorb = (uint)currentAbsorb;
                absorbAurEff.Base.CallScriptEffectAfterManaShieldHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb);

                // Check if our aura is using amount to count damage
                if (absorbAurEff.Amount >= 0)
                {
                    absorbAurEff.ChangeAmount(absorbAurEff.Amount - currentAbsorb);

                    if (absorbAurEff.Amount <= 0)
                        absorbAurEff.Base.Remove(AuraRemoveMode.EnemySpell);
                }
            }

            if (!absorbAurEff.SpellInfo.HasAttribute(SpellAttr6.AbsorbCannotBeIgnore))
                damageInfo.ModifyDamage(absorbIgnoringDamage);

            if (currentAbsorb != 0)
            {
                SpellAbsorbLog absorbLog = new()
                {
                    Attacker = damageInfo.Attacker?.GUID ?? ObjectGuid.Empty,
                    Victim = damageInfo.Victim.GUID,
                    Caster = absorbAurEff.Base.CasterGuid,
                    AbsorbedSpellID = damageInfo.SpellInfo?.Id ?? 0,
                    AbsorbSpellID = absorbAurEff.Id,
                    Absorbed = (int)currentAbsorb,
                    OriginalDamage = (uint)damageInfo.OriginalDamage
                };

                absorbLog.LogData.Initialize(damageInfo.Victim);
                damageInfo.Victim.SendCombatLogMessage(absorbLog);
            }
        }

        // split damage auras - only when not damaging self
        if (damageInfo.Victim != damageInfo.Attacker)
        {
            // We're going to call functions which can modify content of the list during iteration over it's elements
            // Let's copy the list so we can prevent iterator invalidation
            var vSplitDamagePctCopy = damageInfo.Victim.GetAuraEffectsByType(AuraType.SplitDamagePct);

            foreach (var itr in vSplitDamagePctCopy)
            {
                if (damageInfo.Damage == 0)
                    break;

                // Check if aura was removed during iteration - we don't need to work on such auras
                var aurApp = itr.Base.GetApplicationOfTarget(damageInfo.Victim.GUID);

                if (aurApp == null)
                    continue;

                // check damage school mask
                if (!Convert.ToBoolean(itr.MiscValue & (int)damageInfo.SchoolMask))
                    continue;

                // Damage can be splitted only if aura has an alive caster
                var caster = itr.Caster;

                if (caster == null || caster == damageInfo.Victim || !caster.Location.IsInWorld || !caster.IsAlive)
                    continue;

                var splitDamage = MathFunctions.CalculatePct(damageInfo.Damage, itr.Amount);

                itr.Base.CallScriptEffectSplitHandlers(itr, aurApp, damageInfo, ref splitDamage);

                // absorb must be smaller than the damage itself
                splitDamage = MathFunctions.RoundToInterval(ref splitDamage, 0, damageInfo.Damage);

                damageInfo.AbsorbDamage(splitDamage);

                // check if caster is immune to damage
                if (caster.IsImmunedToDamage(damageInfo.SchoolMask))
                {
                    damageInfo.Victim.WorldObjectCombat.SendSpellMiss(caster, itr.SpellInfo.Id, SpellMissInfo.Immune);

                    continue;
                }

                double splitAbsorb = 0;
                DealDamageMods(damageInfo.Attacker, caster, ref splitDamage, ref splitAbsorb);

                SpellNonMeleeDamage log = new(damageInfo.Attacker, caster, itr.SpellInfo, itr.Base.SpellVisual, damageInfo.SchoolMask, itr.Base.CastId);
                CleanDamage cleanDamage = new(splitDamage, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);
                splitDamage = DealDamage(damageInfo.Attacker, caster, splitDamage, cleanDamage, DamageEffectType.Direct, damageInfo.SchoolMask, itr.SpellInfo, false);
                log.Damage = splitDamage;
                log.OriginalDamage = splitDamage;
                log.Absorb = splitAbsorb;
                log.HitInfo |= (int)SpellHitType.Split;

                caster.SendSpellNonMeleeDamageLog(log);

                // break 'Fear' and similar auras
                ProcSkillsAndAuras(damageInfo.Attacker, caster, new ProcFlagsInit(), new ProcFlagsInit(ProcFlags.TakeHarmfulSpell), ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, ProcFlagsHit.None, null, damageInfo, null);
            }
        }
    }

    public double CalcArmorReducedDamage(Unit attacker, Unit victim, double damage, SpellInfo spellInfo, WeaponAttackType attackType = WeaponAttackType.Max, uint attackerLevel = 0)
    {
        double armor = victim.GetArmor();

        if (attacker != null)
        {
            armor *= victim.GetArmorMultiplierForTarget(attacker);

            // bypass enemy armor by SPELL_AURA_BYPASS_ARMOR_FOR_CASTER
            double armorBypassPct = 0;
            var reductionAuras = victim.GetAuraEffectsByType(AuraType.BypassArmorForCaster);

            foreach (var eff in reductionAuras)
                if (eff.CasterGuid == attacker.GUID)
                    armorBypassPct += eff.Amount;

            armor = MathFunctions.CalculatePct(armor, 100 - Math.Min(armorBypassPct, 100));

            // Ignore enemy armor by SPELL_AURA_MOD_TARGET_RESISTANCE aura
            armor += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)SpellSchoolMask.Normal);

            if (spellInfo != null)
            {
                var modOwner = attacker.SpellModOwner;

                modOwner?.ApplySpellMod(spellInfo, SpellModOp.TargetResistance, ref armor);
            }

            var resIgnoreAuras = attacker.GetAuraEffectsByType(AuraType.ModIgnoreTargetResist);

            foreach (var eff in resIgnoreAuras)
                if (eff.MiscValue.HasAnyFlag((int)SpellSchoolMask.Normal) && eff.IsAffectingSpell(spellInfo))
                    armor = (float)Math.Floor(MathFunctions.AddPct(ref armor, -eff.Amount));

            // Apply Player CR_ARMOR_PENETRATION rating
            if (attacker.IsPlayer)
            {
                var arpPct = attacker.AsPlayer.GetRatingBonusValue(CombatRating.ArmorPenetration);

                // no more than 100%
                MathFunctions.RoundToInterval(ref arpPct, 0.0f, 100.0f);

                double maxArmorPen;

                if (victim.GetLevelForTarget(attacker) < 60)
                    maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker);
                else
                    maxArmorPen = 400 + 85 * victim.GetLevelForTarget(attacker) + 4.5f * 85 * (victim.GetLevelForTarget(attacker) - 59);

                // Cap armor penetration to this number
                maxArmorPen = Math.Min((armor + maxArmorPen) / 3.0f, armor);
                // Figure out how much armor do we ignore
                armor -= MathFunctions.CalculatePct(maxArmorPen, arpPct);
            }
        }

        if (MathFunctions.fuzzyLe(armor, 0.0f))
            return damage;

        var attackerClass = PlayerClass.Warrior;

        if (attacker != null)
        {
            attackerLevel = attacker.GetLevelForTarget(victim);
            attackerClass = attacker.Class;
        }

        // Expansion and ContentTuningID necessary? Does Player get a ContentTuningID too ?
        var armorConstant = _db2Manager.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, attackerLevel, -2, 0, attackerClass);

        if (armor + armorConstant == 0)
            return damage;

        var mitigation = Math.Min(armor / (armor + armorConstant), 0.85f);

        return Math.Max(damage * (1.0f - mitigation), 0.0f);
    }

    public void CalcHealAbsorb(HealInfo healInfo)
    {
        if (healInfo.Heal == 0)
            return;

        // Need remove expired auras after
        var existExpired = false;

        // absorb without mana cost
        var vHealAbsorb = healInfo.Target.GetAuraEffectsByType(AuraType.SchoolHealAbsorb);

        for (var i = 0; i < vHealAbsorb.Count && healInfo.Heal > 0; ++i)
        {
            var absorbAurEff = vHealAbsorb[i];
            // Check if aura was removed during iteration - we don't need to work on such auras
            var aurApp = absorbAurEff.Base.GetApplicationOfTarget(healInfo.Target.GUID);

            if (aurApp == null)
                continue;

            if ((absorbAurEff.MiscValue & (int)healInfo.SchoolMask) == 0)
                continue;

            // get amount which can be still absorbed by the aura
            var currentAbsorb = absorbAurEff.Amount;

            // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
            if (currentAbsorb < 0)
                currentAbsorb = 0;

            var tempAbsorb = currentAbsorb;

            var defaultPrevented = false;

            absorbAurEff.Base.CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, healInfo, ref tempAbsorb, ref defaultPrevented);
            currentAbsorb = tempAbsorb;

            if (!defaultPrevented)
            {
                // absorb must be smaller than the heal itself
                currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, healInfo.Heal);

                healInfo.AbsorbHeal((uint)currentAbsorb);

                tempAbsorb = currentAbsorb;
                absorbAurEff.Base.CallScriptEffectAfterAbsorbHandlers(absorbAurEff, aurApp, healInfo, ref tempAbsorb);

                // Check if our aura is using amount to count heal
                if (absorbAurEff.Amount >= 0)
                {
                    // Reduce shield amount
                    absorbAurEff.ChangeAmount(absorbAurEff.Amount - currentAbsorb);

                    // Aura cannot absorb anything more - remove it
                    if (absorbAurEff.Amount <= 0)
                        existExpired = true;
                }
            }

            if (currentAbsorb == 0)
                continue;

            SpellHealAbsorbLog absorbLog = new()
            {
                Healer = healInfo.Healer?.GUID ?? ObjectGuid.Empty,
                Target = healInfo.Target.GUID,
                AbsorbCaster = absorbAurEff.Base.CasterGuid,
                AbsorbedSpellID = (int)(healInfo.SpellInfo?.Id ?? 0),
                AbsorbSpellID = (int)absorbAurEff.Id,
                Absorbed = (int)currentAbsorb,
                OriginalHeal = (int)healInfo.OriginalHeal
            };

            healInfo.Target.SendMessageToSet(absorbLog, true);
        }

        // Remove all expired absorb auras
        if (!existExpired)
            return;

        {
            for (var i = 0; i < vHealAbsorb.Count;)
            {
                var auraEff = vHealAbsorb[i];
                ++i;

                if (!(auraEff.Amount <= 0))
                    continue;

                var removedAuras = healInfo.Target.RemovedAurasCount;
                auraEff.Base.Remove(AuraRemoveMode.EnemySpell);

                if (removedAuras + 1 < healInfo.Target.RemovedAurasCount)
                    i = 0;
            }
        }
    }

    public bool CheckEvade(Unit attacker, Unit victim, ref double damage, ref double absorb)
    {
        if (victim is { IsAlive: true } && !victim.HasUnitState(UnitState.InFlight) && (!victim.IsTypeId(TypeId.Unit) || !victim.AsCreature.IsEvadingAttacks))
            return false;

        absorb += damage;
        damage = 0;

        return true;

    }

    public ProcFlagsHit CreateProcHitMask(SpellNonMeleeDamage damageInfo, SpellMissInfo missCondition)
    {
        var hitMask = ProcFlagsHit.None;

        // Check victim state
        if (missCondition != SpellMissInfo.None)
        {
            switch (missCondition)
            {
                case SpellMissInfo.Miss:
                    hitMask |= ProcFlagsHit.Miss;

                    break;
                case SpellMissInfo.Dodge:
                    hitMask |= ProcFlagsHit.Dodge;

                    break;
                case SpellMissInfo.Parry:
                    hitMask |= ProcFlagsHit.Parry;

                    break;
                case SpellMissInfo.Block:
                    // spells can't be partially blocked (it's damage can though)
                    hitMask |= ProcFlagsHit.Block | ProcFlagsHit.FullBlock;

                    break;
                case SpellMissInfo.Evade:
                    hitMask |= ProcFlagsHit.Evade;

                    break;
                case SpellMissInfo.Immune:
                case SpellMissInfo.Immune2:
                    hitMask |= ProcFlagsHit.Immune;

                    break;
                case SpellMissInfo.Deflect:
                    hitMask |= ProcFlagsHit.Deflect;

                    break;
                case SpellMissInfo.Absorb:
                    hitMask |= ProcFlagsHit.Absorb;

                    break;
                case SpellMissInfo.Reflect:
                    hitMask |= ProcFlagsHit.Reflect;

                    break;
                case SpellMissInfo.Resist:
                    hitMask |= ProcFlagsHit.FullResist;

                    break;
            }
        }
        else
        {
            // On block
            if (damageInfo.Blocked != 0)
            {
                hitMask |= ProcFlagsHit.Block;

                if (damageInfo.FullBlock)
                    hitMask |= ProcFlagsHit.FullBlock;
            }

            // On absorb
            if (damageInfo.Absorb != 0)
                hitMask |= ProcFlagsHit.Absorb;

            // Don't set hit/crit hitMask if damage is nullified
            var damageNullified = damageInfo.HitInfo.HasAnyFlag((int)HitInfo.FullAbsorb | (int)HitInfo.FullResist) || hitMask.HasAnyFlag(ProcFlagsHit.FullBlock);

            if (!damageNullified)
            {
                // On crit
                if (damageInfo.HitInfo.HasAnyFlag((int)SpellHitType.Crit))
                    hitMask |= ProcFlagsHit.Critical;
                else
                    hitMask |= ProcFlagsHit.Normal;
            }
            else if (damageInfo.HitInfo.HasAnyFlag((int)HitInfo.FullResist))
            {
                hitMask |= ProcFlagsHit.FullResist;
            }
        }

        return hitMask;
    }

    public double DealDamage(Unit attacker, Unit victim, double damage, CleanDamage cleanDamage = null, DamageEffectType damagetype = DamageEffectType.Direct, SpellSchoolMask damageSchoolMask = SpellSchoolMask.Normal, SpellInfo spellProto = null, bool durabilityLoss = true)
    {
        var damageDone = damage;
        var damageTaken = damage;

        if (attacker != null)
            damageTaken = damage / victim.GetHealthMultiplierForTarget(attacker);

        // call script hooks
        {
            var tmpDamage = damageTaken;

            victim.AI?.DamageTaken(attacker, ref tmpDamage, damagetype, spellProto);

            attacker?.AI?.DamageDealt(victim, ref tmpDamage, damagetype);

            // Hook for OnDamage Event
            _scriptManager.ForEach<IUnitOnDamage>(p => p.OnDamage(attacker, victim, ref tmpDamage));

            // if any script modified damage, we need to also apply the same modification to unscaled damage value
            if (tmpDamage != damageTaken)
            {
                if (attacker != null)
                    damageDone = (uint)(tmpDamage * victim.GetHealthMultiplierForTarget(attacker));
                else
                    damageDone = tmpDamage;

                damageTaken = tmpDamage;
            }
        }

        // Signal to pets that their owner was attacked - except when DOT.
        if (attacker != victim && damagetype != DamageEffectType.DOT)
            foreach (var controlled in victim.Controlled)
            {
                var cControlled = controlled.AsCreature;

                var controlledAI = cControlled?.AI;

                controlledAI?.OwnerAttackedBy(attacker);
            }

        var player = victim.AsPlayer;

        if (player != null && player.GetCommandStatus(PlayerCommandStates.God))
            return 0;

        if (damagetype != DamageEffectType.NoDamage)
        {
            // interrupting auras with SpellAuraInterruptFlags.Damage before checking !damage (absorbed damage breaks that type of auras)
            if (spellProto != null)
            {
                if (!spellProto.HasAttribute(SpellAttr4.ReactiveDamageProc))
                    victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Damage, spellProto);
            }
            else
            {
                victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Damage);
            }

            if (damageTaken == 0 && damagetype != DamageEffectType.DOT && cleanDamage != null && cleanDamage.AbsorbedDamage != 0)
                if (victim != attacker && victim.IsPlayer)
                {
                    var spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

                    if (spell != null)
                        if (spell.State == SpellState.Preparing && spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageAbsorb))
                            victim.InterruptNonMeleeSpells(false);
                }

            // We're going to call functions which can modify content of the list during iteration over it's elements
            // Let's copy the list so we can prevent iterator invalidation
            var vCopyDamageCopy = victim.GetAuraEffectsByType(AuraType.ShareDamagePct);

            // copy damage to casters of this aura
            foreach (var aura in vCopyDamageCopy)
            {
                // Check if aura was removed during iteration - we don't need to work on such auras
                if (!aura.Base.IsAppliedOnTarget(victim.GUID))
                    continue;

                // check damage school mask
                if ((aura.MiscValue & (int)damageSchoolMask) == 0)
                    continue;

                var shareDamageTarget = aura.Caster;

                if (shareDamageTarget == null)
                    continue;

                var spell = aura.SpellInfo;

                var share = MathFunctions.CalculatePct(damageDone, aura.Amount);

                // @todo check packets if damage is done by victim, or by attacker of victim
                DealDamageMods(attacker, shareDamageTarget, ref share);
                DealDamage(attacker, shareDamageTarget, share, null, DamageEffectType.NoDamage, spell.GetSchoolMask(), spell, false);
            }
        }

        // Rage from Damage made (only from direct weapon damage)
        if (attacker != null && cleanDamage?.AttackType is WeaponAttackType.BaseAttack or WeaponAttackType.OffAttack && damagetype == DamageEffectType.Direct && attacker != victim && attacker.DisplayPowerType == PowerType.Rage)
        {
            var rage = (uint)(attacker.GetBaseAttackTime(cleanDamage.AttackType) / 1000.0f * 1.75f);

            if (cleanDamage.AttackType == WeaponAttackType.OffAttack)
                rage /= 2;

            attacker.RewardRage(rage);
        }

        if (damageDone == 0)
            return 0;

        var health = (uint)victim.Health;

        // duel ends when player has 1 or less hp
        var duelHasEnded = false;
        var duelWasMounted = false;

        if (victim.IsPlayer && victim.AsPlayer.Duel != null && damageTaken >= health - 1)
        {
            if (attacker == null)
                return 0;

            // prevent kill only if killed in duel and killed by opponent or opponent controlled creature
            if (victim.AsPlayer.Duel.Opponent == attacker.GetControllingPlayer())
                damageTaken = health - 1;

            duelHasEnded = true;
        }
        else if (victim.TryGetAsCreature(out var creature) && damageTaken >= health && creature.StaticFlags.HasFlag(CreatureStaticFlags.UNKILLABLE))
        {
            damageTaken = health - 1;
        }
        else if (victim.IsVehicle && damageTaken >= health - 1 && victim.Charmer != null && victim.Charmer.IsTypeId(TypeId.Player))
        {
            var victimRider = victim.Charmer.AsPlayer;

            if (victimRider is { Duel: { IsMounted: true } })
            {
                if (attacker == null)
                    return 0;

                // prevent kill only if killed in duel and killed by opponent or opponent controlled creature
                if (victimRider.Duel.Opponent == attacker.GetControllingPlayer())
                    damageTaken = health - 1;

                duelWasMounted = true;
                duelHasEnded = true;
            }
        }

        if (attacker != null && attacker != victim)
        {
            var killer = attacker.AsPlayer;

            if (killer != null)
            {
                // in bg, count dmg if victim is also a player
                if (victim.IsPlayer)
                {
                    var bg = killer.Battleground;

                    bg?.UpdatePlayerScore(killer, ScoreType.DamageDone, (uint)damageDone);
                }

                killer.UpdateCriteria(CriteriaType.DamageDealt, health > damageDone ? damageDone : health, 0, 0, victim);
                killer.UpdateCriteria(CriteriaType.HighestDamageDone, damageDone);
            }
        }

        if (victim.IsPlayer)
            victim.AsPlayer.UpdateCriteria(CriteriaType.HighestDamageTaken, damageTaken);

        if (victim.TypeId != TypeId.Player && (!victim.ControlledByPlayer || victim.IsVehicle))
        {
            victim.AsCreature.SetTappedBy(attacker);

            if (attacker == null || attacker.ControlledByPlayer)
                victim.AsCreature.LowerPlayerDamageReq(health < damageTaken ? health : damageTaken);
        }

        var killed = false;
        var skipSettingDeathState = false;

        if (health <= damageTaken)
        {
            killed = true;

            if (victim.IsPlayer && victim != attacker)
                victim.AsPlayer.UpdateCriteria(CriteriaType.TotalDamageTaken, health);

            if (damagetype != DamageEffectType.NoDamage && damagetype != DamageEffectType.Self && victim.HasAuraType(AuraType.SchoolAbsorbOverkill))
            {
                var vAbsorbOverkill = victim.GetAuraEffectsByType(AuraType.SchoolAbsorbOverkill);
                DamageInfo damageInfo = new(attacker, victim, damageTaken, spellProto, damageSchoolMask, damagetype, cleanDamage?.AttackType ?? WeaponAttackType.BaseAttack);

                foreach (var absorbAurEff in vAbsorbOverkill)
                {
                    var baseAura = absorbAurEff.Base;
                    var aurApp = baseAura.GetApplicationOfTarget(victim.GUID);

                    if (aurApp == null)
                        continue;

                    if ((absorbAurEff.MiscValue & (int)damageInfo.SchoolMask) == 0)
                        continue;

                    // cannot absorb over limit
                    if (damageTaken >= victim.CountPctFromMaxHealth(100 + absorbAurEff.MiscValueB))
                        continue;

                    // get amount which can be still absorbed by the aura
                    var currentAbsorb = absorbAurEff.Amount;

                    // aura with infinite absorb amount - let the scripts handle absorbtion amount, set here to 0 for safety
                    if (currentAbsorb < 0)
                        currentAbsorb = 0;

                    var tempAbsorb = currentAbsorb;

                    // This aura type is used both by Spirit of Redemption (death not really prevented, must grant all credit immediately) and Cheat Death (death prevented)
                    // repurpose PreventDefaultAction for this
                    var deathFullyPrevented = false;

                    absorbAurEff.Base.CallScriptEffectAbsorbHandlers(absorbAurEff, aurApp, damageInfo, ref tempAbsorb, ref deathFullyPrevented);
                    currentAbsorb = tempAbsorb;

                    // absorb must be smaller than the damage itself
                    currentAbsorb = MathFunctions.RoundToInterval(ref currentAbsorb, 0, damageInfo.Damage);
                    damageInfo.AbsorbDamage(currentAbsorb);

                    if (deathFullyPrevented)
                        killed = false;

                    skipSettingDeathState = true;

                    if (currentAbsorb != 0)
                    {
                        SpellAbsorbLog absorbLog = new()
                        {
                            Attacker = attacker?.GUID ?? ObjectGuid.Empty,
                            Victim = victim.GUID,
                            Caster = baseAura.CasterGuid,
                            AbsorbedSpellID = spellProto?.Id ?? 0,
                            AbsorbSpellID = baseAura.Id,
                            Absorbed = (int)currentAbsorb,
                            OriginalDamage = (uint)damageInfo.OriginalDamage
                        };

                        absorbLog.LogData.Initialize(victim);
                        victim.SendCombatLogMessage(absorbLog);
                    }
                }

                damageTaken = damageInfo.Damage;
            }
        }

        if (spellProto != null && spellProto.HasAttribute(SpellAttr3.NoDurabilityLoss))
            durabilityLoss = false;

        if (killed)
        {
            Kill(attacker, victim, durabilityLoss, skipSettingDeathState);
        }
        else
        {
            if (victim.IsTypeId(TypeId.Player))
                victim.AsPlayer.UpdateCriteria(CriteriaType.TotalDamageTaken, damageTaken);

            victim.ModifyHealth(-(int)damageTaken);

            if (damagetype is DamageEffectType.Direct or DamageEffectType.SpellDirect)
                victim.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.NonPeriodicDamage, spellProto);

            if (!victim.IsTypeId(TypeId.Player))
            {
                // Part of Evade mechanics. DoT's and Thorns / Retribution Aura do not contribute to this
                if (damagetype != DamageEffectType.DOT && damageTaken > 0 && !victim.OwnerGUID.IsPlayer && (spellProto == null || !spellProto.HasAura(AuraType.DamageShield)))
                    victim.AsCreature.LastDamagedTime = GameTime.CurrentTime + SharedConst.MaxAggroResetTime;

                if (attacker != null && (spellProto == null || !spellProto.HasAttribute(SpellAttr4.NoHarmfulThreat)))
                    victim.GetThreatManager().AddThreat(attacker, damageTaken, spellProto);
            }
            else // victim is a player
            {
                // random durability for items (HIT TAKEN)
                if (durabilityLoss && _configuration.GetDefaultValue("DurabilityLossChance.Damage", 0.5f) > RandomHelper.randChance())
                {
                    var slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                    victim.AsPlayer.DurabilityPointLossForEquipSlot(slot);
                }
            }

            if (attacker is { IsPlayer: true })
                // random durability for items (HIT DONE)
                if (durabilityLoss && RandomHelper.randChance(_configuration.GetDefaultValue("DurabilityLossChance.Damage", 0.5f)))
                {
                    var slot = (byte)RandomHelper.IRand(0, EquipmentSlot.End - 1);
                    attacker.AsPlayer.DurabilityPointLossForEquipSlot(slot);
                }

            if (damagetype != DamageEffectType.NoDamage && damagetype != DamageEffectType.DOT)
            {
                if (victim != attacker && (spellProto == null || !(spellProto.HasAttribute(SpellAttr6.NoPushback) || spellProto.HasAttribute(SpellAttr7.NoPushbackOnDamage) || spellProto.HasAttribute(SpellAttr3.TreatAsPeriodic))))
                {
                    var spell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

                    if (spell is { State: SpellState.Preparing })
                    {
                        bool IsCastInterrupted()
                        {
                            if (damageTaken == 0)
                                return spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.ZeroDamageCancels);

                            if (victim.IsPlayer && spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageCancelsPlayerOnly))
                                return true;

                            if (spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamageCancels))
                                return true;

                            return false;
                        }

                        bool IsCastDelayed()
                        {
                            if (damageTaken == 0)
                                return false;

                            if (victim.IsPlayer && spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamagePushbackPlayerOnly))
                                return true;

                            if (spell.SpellInfo.InterruptFlags.HasAnyFlag(SpellInterruptFlags.DamagePushback))
                                return true;

                            return false;
                        }

                        if (IsCastInterrupted())
                            victim.InterruptNonMeleeSpells(false);
                        else if (IsCastDelayed())
                            spell.Delayed();
                    }
                }

                if (damageTaken != 0 && victim.IsPlayer)
                {
                    var spell1 = victim.GetCurrentSpell(CurrentSpellTypes.Channeled);

                    if (spell1 != null)
                        if (spell1.State == SpellState.Casting && spell1.SpellInfo.HasChannelInterruptFlag(SpellAuraInterruptFlags.DamageChannelDuration))
                            spell1.DelayedChannel();
                }
            }

            // last damage from duel opponent
            if (duelHasEnded)
            {
                var he = duelWasMounted ? victim.Charmer.AsPlayer : victim.AsPlayer;

                if (duelWasMounted) // In this case victim==mount
                    victim.SetHealth(1);
                else
                    he.SetHealth(1);

                he.Duel.Opponent.CombatStopWithPets(true);
                he.CombatStopWithPets(true);

                he.SpellFactory.CastSpell(he, 7267, true); // beg
                he.DuelComplete(DuelCompleteType.Won);
            }
        }

        // logging uses damageDone
        if (victim.IsPlayer)
        {
            player = victim.AsPlayer;
            _scriptManager.ForEach<IPlayerOnTakeDamage>(player.Class, a => a.OnPlayerTakeDamage(player, damageDone, damageSchoolMask));
        }

        // make player victims stand up automatically
        if (victim.StandState != 0 && victim.IsPlayer)
            victim.SetStandState(UnitStandStateType.Stand);

        if (player != null)
            victim.SaveDamageHistory(damageDone);

        return damageDone;
    }

    public void DealDamageMods(Unit attacker, Unit victim, ref double damage)
    {
        if (victim == null || !victim.IsAlive || victim.HasUnitState(UnitState.InFlight) || (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsInEvadeMode))
            damage = 0;
    }

    public void DealDamageMods(Unit attacker, Unit victim, ref double damage, ref double absorb)
    {
        if (!CheckEvade(attacker, victim, ref damage, ref absorb))
            ScaleDamage(attacker, victim, ref damage);
    }

    public void DealHeal(HealInfo healInfo)
    {
        uint gain = 0;
        var healer = healInfo.Healer;
        var victim = healInfo.Target;
        var addhealth = healInfo.Heal;

        var victimAI = victim.AI;

        victimAI?.HealReceived(healer, addhealth);

        var healerAI = healer?.AI;

        healerAI?.HealDone(victim, addhealth);

        if (addhealth != 0)
            gain = (uint)victim.ModifyHealth(addhealth);

        // Hook for OnHeal Event
        _scriptManager.ForEach<IUnitOnHeal>(p => p.OnHeal(healInfo, ref gain));

        var unit = healer;

        if (healer is { IsCreature: true, IsTotem: true })
            unit = healer.OwnerUnit;

        var bgPlayer = unit?.AsPlayer;

        if (bgPlayer != null)
        {
            var bg = bgPlayer.Battleground;

            bg?.UpdatePlayerScore(bgPlayer, ScoreType.HealingDone, gain);

            // use the actual gain, as the overheal shall not be counted, skip gain 0 (it ignored anyway in to criteria)
            if (gain != 0)
                bgPlayer.UpdateCriteria(CriteriaType.HealingDone, gain, 0, 0, victim);

            bgPlayer.UpdateCriteria(CriteriaType.HighestHealCast, (uint)addhealth);
        }

        var player = victim.AsPlayer;

        if (player != null)
        {
            player.UpdateCriteria(CriteriaType.TotalHealReceived, gain);
            player.UpdateCriteria(CriteriaType.HighestHealReceived, (uint)addhealth);
        }

        if (gain != 0)
            healInfo.SetEffectiveHeal(gain > 0 ? gain : 0u);
    }

    public bool IsDamageReducedByArmor(SpellSchoolMask schoolMask, SpellInfo spellInfo = null)
    {
        // only physical spells damage gets reduced by armor
        if ((schoolMask & SpellSchoolMask.Normal) == 0)
            return false;

        return spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.IgnoreArmor);
    }

    public void Kill(Unit attacker, Unit victim, bool durabilityLoss = true, bool skipSettingDeathState = false)
    {
        // Prevent killing unit twice (and giving reward from kill twice)
        if (victim.Health == 0)
            return;

        if (attacker != null && !attacker.Location.IsInMap(victim))
            attacker = null;

        // find player: owner of controlled `this` or `this` itself maybe
        Player player = null;

        if (attacker != null)
            player = attacker.CharmerOrOwnerPlayerOrPlayerItself;

        var creature = victim.AsCreature;

        var isRewardAllowed = attacker != victim;

        if (creature != null)
            isRewardAllowed = isRewardAllowed && !creature.TapList.Empty();

        List<Player> tappers = new();

        if (isRewardAllowed && creature != null)
        {
            foreach (var tapperGuid in creature.TapList)
            {
                var tapper = _objectAccessor.GetPlayer(creature, tapperGuid);

                if (tapper != null)
                    tappers.Add(tapper);
            }

            if (!creature.CanHaveLoot)
                isRewardAllowed = false;
        }

        // Exploit fix
        if (creature is { IsPet: true, OwnerGUID.IsPlayer: true })
            isRewardAllowed = false;

        // Reward player, his pets, and group/raid members
        // call kill spell proc event (before real die and combat stop to triggering auras removed at death/combat stop)
        if (isRewardAllowed)
        {
            HashSet<PlayerGroup> groups = new();

            foreach (var tapper in tappers)
            {
                var tapperGroup = tapper.Group;

                if (tapperGroup != null)
                {
                    if (groups.Add(tapperGroup))
                    {
                        PartyKillLog partyKillLog = new()
                        {
                            Player = player != null && tapperGroup.IsMember(player.GUID) ? player.GUID : tapper.GUID,
                            Victim = victim.GUID
                        };

                        partyKillLog.Write();

                        tapperGroup.BroadcastPacket(partyKillLog, tapperGroup.GetMemberGroup(tapper.GUID) != 0);

                        if (creature != null)
                            tapperGroup.UpdateLooterGuid(creature, true);
                    }
                }
                else
                {
                    PartyKillLog partyKillLog = new()
                    {
                        Player = tapper.GUID,
                        Victim = victim.GUID
                    };

                    tapper.SendPacket(partyKillLog);
                }
            }

            // Generate loot before updating looter
            if (creature != null)
            {
                DungeonEncounterRecord dungeonEncounter = null;
                var instance = creature.Location.InstanceScript;

                if (instance != null)
                    dungeonEncounter = instance.GetBossDungeonEncounter(creature);

                if (creature.Location.Map.IsDungeon)
                {
                    if (dungeonEncounter != null)
                    {
                        creature.PersonalLoot = _lootManager.GenerateDungeonEncounterPersonalLoot(dungeonEncounter.Id,
                                                                                                  creature.LootId,
                                                                                                  _lootStorage.Creature,
                                                                                                  LootType.Corpse,
                                                                                                  creature,
                                                                                                  creature.Template.MinGold,
                                                                                                  creature.Template.MaxGold,
                                                                                                  (ushort)creature.GetLootMode(),
                                                                                                  creature.Location.Map.GetDifficultyLootItemContext(),
                                                                                                  tappers);
                    }
                    else if (!tappers.Empty())
                    {
                        var group = !groups.Empty() ? groups.First() : null;
                        var looter = group != null ? _objectAccessor.GetPlayer(creature, group.LooterGuid) : tappers[0];

                        var loot = _lootFactory.GenerateLoot(creature.Location.Map, creature.GUID, LootType.Corpse, dungeonEncounter != null ? group : null);

                        var lootid = creature.LootId;

                        if (lootid != 0)
                            loot.FillLoot(lootid, LootStorageType.Creature, looter, dungeonEncounter != null, false, creature.GetLootMode(), creature.Location.Map.GetDifficultyLootItemContext());

                        if (creature.GetLootMode() > 0)
                            loot.GenerateMoneyLoot(creature.Template.MinGold, creature.Template.MaxGold);

                        if (group != null)
                            loot.NotifyLootList(creature.Location.Map);

                        if (loot != null)
                            creature.PersonalLoot[looter.GUID] = loot; // trash mob loot is personal, generated with round robin rules

                        // Update round robin looter only if the creature had loot
                        if (!loot.IsLooted())
                            foreach (var tapperGroup in groups)
                                tapperGroup.UpdateLooterGuid(creature);
                    }
                }
                else
                {
                    foreach (var tapper in tappers)
                    {
                        var loot = _lootFactory.GenerateLoot(creature.Location.Map, creature.GUID, LootType.Corpse);

                        if (dungeonEncounter != null)
                            loot.DungeonEncounterId = dungeonEncounter.Id;

                        var lootid = creature.LootId;

                        if (lootid != 0)
                            loot.FillLoot(lootid, LootStorageType.Creature, tapper, true, false, creature.GetLootMode(), creature.Location.Map.GetDifficultyLootItemContext());

                        if (creature.GetLootMode() > 0)
                            loot.GenerateMoneyLoot(creature.Template.MinGold, creature.Template.MaxGold);

                        if (loot != null)
                            creature.PersonalLoot[tapper.GUID] = loot;
                    }
                }
            }

            new KillRewarder(tappers.ToArray(), victim, false).Reward();
        }

        // Do KILL and KILLED procs. KILL proc is called only for the unit who landed the killing blow (and its owner - for pets and totems) regardless of who tapped the victim
        if (attacker != null && (attacker.IsPet || attacker.IsTotem))
        {
            // proc only once for victim
            var owner = attacker.OwnerUnit;

            if (owner != null)
                ProcSkillsAndAuras(owner, victim, new ProcFlagsInit(ProcFlags.Kill), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
        }

        if (!victim.IsCritter)
        {
            ProcSkillsAndAuras(attacker, victim, new ProcFlagsInit(ProcFlags.Kill), new ProcFlagsInit(ProcFlags.Heartbeat), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);

            foreach (var tapper in tappers)
                if (tapper.IsAtGroupRewardDistance(victim))
                    ProcSkillsAndAuras(tapper, victim, new ProcFlagsInit(ProcFlags.None, ProcFlags2.TargetDies), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
        }

        // Proc auras on death - must be before aura/combat remove
        ProcSkillsAndAuras(victim, victim, new ProcFlagsInit(), new ProcFlagsInit(ProcFlags.Death), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);

        // update get killing blow achievements, must be done before setDeathState to be able to require auras on target
        // and before Spirit of Redemption as it also removes auras
        var killerPlayer = attacker?.CharmerOrOwnerPlayerOrPlayerItself;

        killerPlayer?.UpdateCriteria(CriteriaType.DeliveredKillingBlow, 1, 0, 0, victim);

        if (!skipSettingDeathState)
        {
            Log.Logger.Debug("SET JUST_DIED");
            victim.SetDeathState(DeathState.JustDied);
        }

        // Inform pets (if any) when player kills target)
        // MUST come after victim.setDeathState(JUST_DIED); or pet next target
        // selection will get stuck on same target and break pet react state
        foreach (var tapper in tappers)
        {
            var pet = tapper.CurrentPet;

            if (pet is { IsAlive: true, IsControlled: true })
            {
                if (pet.IsAIEnabled)
                    pet.AI.KilledUnit(victim);
                else
                    Log.Logger.Error($"Pet doesn't have any AI in Unit.Kill() {pet.GetDebugInfo()}");
            }
        }

        // 10% durability loss on death
        var plrVictim = victim.AsPlayer;

        if (plrVictim != null)
        {
            // remember victim PvP death for corpse type and corpse reclaim delay
            // at original death (not at SpiritOfRedemtionTalent timeout)
            plrVictim.SetPvPDeath(player != null);

            // only if not player and not controlled by player pet. And not at BG
            if ((durabilityLoss && player == null && !victim.AsPlayer.InBattleground) || (player != null && _configuration.GetDefaultValue("DurabilityLoss.InPvP", false)))
            {
                double baseLoss = _configuration.GetDefaultValue("DurabilityLoss.OnDeath", 10.0f) / 100;
                var loss = (uint)(baseLoss - baseLoss * plrVictim.GetTotalAuraMultiplier(AuraType.ModDurabilityLoss));
                Log.Logger.Debug("We are dead, losing {0} percent durability", loss);
                // Durability loss is calculated more accurately again for each item in Player.DurabilityLoss
                plrVictim.DurabilityLossAll(baseLoss, false);
                // durability lost message
                plrVictim.SendDurabilityLoss(plrVictim, loss);
            }

            // Call KilledUnit for creatures
            if (attacker is { IsCreature: true, IsAIEnabled: true })
                attacker.AsCreature.AI.KilledUnit(victim);

            // last damage from non duel opponent or opponent controlled creature
            if (plrVictim.Duel != null)
            {
                plrVictim.Duel.Opponent.CombatStopWithPets(true);
                plrVictim.CombatStopWithPets(true);
                plrVictim.DuelComplete(DuelCompleteType.Interrupted);
            }
        }
        else // creature died
        {
            Log.Logger.Debug("DealDamageNotPlayer");

            if (!creature.IsPet)
            {
                // must be after setDeathState which resets dynamic flags
                if (!creature.IsFullyLooted)
                    creature.SetDynamicFlag(UnitDynFlags.Lootable);
                else
                    creature.AllLootRemovedFromCorpse();

                if (creature.CanHaveLoot && _lootStorage.Skinning.HaveLootFor(creature.Template.SkinLootId))
                {
                    creature.SetDynamicFlag(UnitDynFlags.CanSkin);
                    creature.SetUnitFlag(UnitFlags.Skinnable);
                }
            }

            // Call KilledUnit for creatures, this needs to be called after the lootable Id is set
            if (attacker is { IsCreature: true, IsAIEnabled: true })
                attacker.AsCreature.AI.KilledUnit(victim);

            // Call creature just died function
            var ai = creature.AI;

            ai?.JustDied(attacker);

            var summon = creature.ToTempSummon();

            var summoner = summon?.GetSummoner();

            if (summoner != null)
            {
                if (summoner.IsCreature)
                    summoner.AsCreature.AI?.SummonedCreatureDies(creature, attacker);
                else if (summoner.IsGameObject)
                    summoner.AsGameObject.AI?.SummonedCreatureDies(creature, attacker);
            }
        }

        // outdoor pvp things, do these after setting the death state, else the player activity notify won't work... doh...
        // handle player kill only if not suicide (spirit of redemption for example)
        if (player != null && attacker != victim)
        {
            var pvp = player.GetOutdoorPvP();

            pvp?.HandleKill(player, victim);

            var bf = _battleFieldManager.GetBattlefieldToZoneId(player.Location.Map, player.Location.Zone);

            bf?.HandleKill(player, victim);
        }

        // Battlegroundthings (do this at the end, so the death state Id will be properly set to handle in the bg.handlekill)
        if (player is { InBattleground: true })
        {
            var bg = player.Battleground;

            if (bg != null)
            {
                var playerVictim = victim.AsPlayer;

                if (playerVictim != null)
                    bg.HandleKillPlayer(playerVictim, player);
                else
                    bg.HandleKillUnit(victim.AsCreature, player);
            }
        }

        // achievement stuff
        if (attacker != null && victim.IsPlayer)
        {
            if (attacker.IsCreature)
                victim.AsPlayer.UpdateCriteria(CriteriaType.KilledByCreature, attacker.Entry);
            else if (attacker.IsPlayer && victim != attacker)
                victim.AsPlayer.UpdateCriteria(CriteriaType.KilledByPlayer, 1, (ulong)attacker.AsPlayer.EffectiveTeam);
        }

        // Hook for OnPVPKill Event
        if (attacker != null)
        {
            var killerPlr = attacker.AsPlayer;

            if (killerPlr != null)
            {
                var killedPlr = victim.AsPlayer;

                if (killedPlr != null)
                {
                    _scriptManager.ForEach<IPlayerOnPVPKill>(p => p.OnPVPKill(killerPlr, killedPlr));
                }
                else
                {
                    var killedCre = victim.AsCreature;

                    if (killedCre != null)
                        _scriptManager.ForEach<IPlayerOnCreatureKill>(p => p.OnCreatureKill(killerPlr, killedCre));
                }
            }
            else
            {
                var killerCre = attacker.AsCreature;

                if (killerCre != null)
                {
                    var killed = victim.AsPlayer;

                    if (killed != null)
                        _scriptManager.ForEach<IPlayerOnPlayerKilledByCreature>(p => p.OnPlayerKilledByCreature(killerCre, killed));
                }
            }
        }
    }
    public void ProcSkillsAndAuras(Unit actor, Unit actionTarget, ProcFlagsInit typeMaskActor, ProcFlagsInit typeMaskActionTarget, ProcFlagsSpellType spellTypeMask, ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
    {
        var attType = damageInfo?.AttackType ?? WeaponAttackType.BaseAttack;

        if (typeMaskActor && actor != null)
            actor.ProcSkillsAndReactives(false, actionTarget, typeMaskActor, hitMask, attType);

        if (typeMaskActionTarget != null && actionTarget != null)
            actionTarget.ProcSkillsAndReactives(true, actor, typeMaskActionTarget, hitMask, attType);

        actor?.TriggerAurasProcOnEvent(null, null, actionTarget, typeMaskActor, typeMaskActionTarget, spellTypeMask, spellPhaseMask, hitMask, spell, damageInfo, healInfo);
    }

    public void ScaleDamage(Unit attacker, Unit victim, ref double damage)
    {
        if (attacker != null)
            damage = damage * attacker.GetDamageMultiplierForTarget(victim);
    }
    public double SpellCriticalDamageBonus(Unit caster, SpellInfo spellProto, double damage, Unit victim = null)
    {
        // Calculate critical bonus
        var critBonus = damage * 2;
        double critMod = 0.0f;

        if (caster == null)
            return critBonus;

        critMod += (caster.GetTotalAuraMultiplierByMiscMask(AuraType.ModCritDamageBonus, (uint)spellProto.GetSchoolMask()) - 1.0f) * 100;

        if (critBonus != 0)
            MathFunctions.AddPct(ref critBonus, critMod);

        if (victim != null)
            MathFunctions.AddPct(ref critBonus, victim.GetTotalAuraModifier(AuraType.ModCriticalDamageTakenFromCaster, aurEff => aurEff.CasterGuid == caster.GUID));

        critBonus -= damage;

        // adds additional damage to critBonus (from talents)
        var modOwner = caster.SpellModOwner;

        modOwner?.ApplySpellMod(spellProto, SpellModOp.CritDamageAndHealing, ref critBonus);

        critBonus += damage;

        return critBonus;
    }

    public double SpellCriticalHealingBonus(Unit caster, SpellInfo spellProto, double damage, Unit victim)
    {
        // Calculate critical bonus
        var critBonus = damage;

        // adds additional damage to critBonus (from talents)
        var modOwner = caster?.SpellModOwner;

        modOwner?.ApplySpellMod(spellProto, SpellModOp.CritDamageAndHealing, ref critBonus);

        damage += critBonus;

        if (caster != null)
            damage = damage * caster.GetTotalAuraMultiplier(AuraType.ModCriticalHealingAmount);

        return damage;
    }
    private double CalcSpellResistedDamage(Unit attacker, Unit victim, double damage, SpellSchoolMask schoolMask, SpellInfo spellInfo)
    {
        // Magic damage, check for resists
        if (!Convert.ToBoolean(schoolMask & SpellSchoolMask.Magic))
            return 0;

        // Npcs can have holy resistance
        if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy) && victim.TypeId != TypeId.Unit)
            return 0;

        var averageResist = CalculateAverageResistReduction(attacker, schoolMask, victim, spellInfo);

        var discreteResistProbability = new double[11];

        if (averageResist <= 0.1f)
        {
            discreteResistProbability[0] = 1.0f - 7.5f * averageResist;
            discreteResistProbability[1] = 5.0f * averageResist;
            discreteResistProbability[2] = 2.5f * averageResist;
        }
        else
        {
            for (uint i = 0; i < 11; ++i)
                discreteResistProbability[i] = Math.Max(0.5f - 2.5f * Math.Abs(0.1f * i - averageResist), 0.0f);
        }

        var roll = RandomHelper.NextDouble();
        double probabilitySum = 0.0f;

        uint resistance = 0;

        for (; resistance < 11; ++resistance)
            if (roll < (probabilitySum += discreteResistProbability[resistance]))
                break;

        var damageResisted = damage * resistance / 10f;

        if (damageResisted > 0.0f) // if any damage was resisted
        {
            double ignoredResistance = 0;

            if (attacker != null)
                ignoredResistance += attacker.GetTotalAuraModifierByMiscMask(AuraType.ModIgnoreTargetResist, (int)schoolMask);

            ignoredResistance = Math.Min(ignoredResistance, 100);
            MathFunctions.ApplyPct(ref damageResisted, 100 - ignoredResistance);

            // Spells with melee and magic school mask, decide whether resistance or armor absorb is higher
            if (spellInfo != null && spellInfo.HasAttribute(SpellCustomAttributes.SchoolmaskNormalWithMagic))
            {
                var damageAfterArmor = CalcArmorReducedDamage(attacker, victim, damage, spellInfo, spellInfo.GetAttackType());
                var armorReduction = damage - damageAfterArmor;

                // pick the lower one, the weakest resistance counts
                damageResisted = Math.Min(damageResisted, armorReduction);
            }
        }

        damageResisted = Math.Max(damageResisted, 0.0f);

        return damageResisted;
    }

    private double CalculateAverageResistReduction(WorldObject caster, SpellSchoolMask schoolMask, Unit victim, SpellInfo spellInfo = null)
    {
        double victimResistance = victim.GetResistance(schoolMask);

        if (caster != null)
        {
            // pets inherit 100% of masters penetration
            var player = caster.SpellModOwner;

            if (player != null)
            {
                victimResistance += player.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
                victimResistance -= player.GetSpellPenetrationItemMod();
            }
            else
            {
                var unitCaster = caster.AsUnit;

                if (unitCaster != null)
                    victimResistance += unitCaster.GetTotalAuraModifierByMiscMask(AuraType.ModTargetResistance, (int)schoolMask);
            }
        }

        // holy resistance exists in pve and comes from level difference, ignore template values
        if (schoolMask.HasAnyFlag(SpellSchoolMask.Holy))
            victimResistance = 0.0f;

        // Chaos Bolt exception, ignore all target resistances (unknown attribute?)
        if (spellInfo is { SpellFamilyName: SpellFamilyNames.Warlock, Id: 116858 })
            victimResistance = 0.0f;

        victimResistance = Math.Max(victimResistance, 0.0f);

        // level-based resistance does not apply to binary spells, and cannot be overcome by spell penetration
        // gameobject caster -- should it have level based resistance?
        if (caster is { IsGameObject: false } && (spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.BinarySpell)))
            victimResistance += Math.Max((victim.GetLevelForTarget(caster) - caster.GetLevelForTarget(victim)) * 5.0f, 0.0f);

        uint bossLevel = 83;
        var bossResistanceConstant = 510.0f;
        var level = caster != null ? victim.GetLevelForTarget(caster) : victim.Level;
        float resistanceConstant;

        if (level == bossLevel)
            resistanceConstant = bossResistanceConstant;
        else
            resistanceConstant = level * 5.0f;

        return victimResistance / (victimResistance + resistanceConstant);
    }
}