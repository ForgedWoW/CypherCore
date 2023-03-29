// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class TargetInfo : TargetInfoBase
{
    public ObjectGuid TargetGuid;
    public ulong TimeDelay;
    public double Damage;
    public double Healing;

    public SpellMissInfo MissCondition;
    public SpellMissInfo ReflectResult;

    public bool IsAlive;
    public bool IsCrit;

    // info set at PreprocessTarget, used by DoTargetSpellHit
    public DiminishingGroup DrGroup;
    public int AuraDuration;
    public Dictionary<int, double> AuraBasePoints = new();
    public bool Positive = true;
    public UnitAura HitAura;

    private Unit _spellHitTarget; // changed for example by reflect
    private bool _enablePVP;      // need to enable PVP at DoDamageAndTriggers?

    public override void PreprocessTarget(Spell spell)
    {
        var unit = spell.Caster.GUID == TargetGuid ? spell.Caster.AsUnit : Global.ObjAccessor.GetUnit(spell.Caster, TargetGuid);

        if (unit == null)
            return;

        // Need init unitTarget by default unit (can changed in code on reflect)
        spell.UnitTarget = unit;

        // Reset damage/healing counter
        spell.DamageInEffects = Damage;
        spell.HealingInEffects = Healing;

        _spellHitTarget = null;

        if (MissCondition == SpellMissInfo.None || (MissCondition == SpellMissInfo.Block && !spell.SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked)))
            _spellHitTarget = unit;
        else if (MissCondition == SpellMissInfo.Reflect && ReflectResult == SpellMissInfo.None)
            _spellHitTarget = spell.Caster.AsUnit;

        if (spell.OriginalCaster && MissCondition != SpellMissInfo.Evade && !spell.OriginalCaster.WorldObjectCombat.IsFriendlyTo(unit) && (!spell.SpellInfo.IsPositive || spell.SpellInfo.HasEffect(SpellEffectName.Dispel)) && (spell.SpellInfo.HasInitialAggro || unit.IsEngaged))
            unit.SetInCombatWith(spell.OriginalCaster);

        // if target is flagged for pvp also flag caster if a player
        // but respect current pvp rules (buffing/healing npcs flagged for pvp only flags you if they are in combat)
        _enablePVP = (MissCondition == SpellMissInfo.None || spell.SpellInfo.HasAttribute(SpellAttr3.PvpEnabling)) && unit.IsPvP && (unit.IsInCombat || unit.IsCharmedOwnedByPlayerOrPlayer) && spell.Caster.IsPlayer; // need to check PvP state before spell effects, but act on it afterwards

        if (_spellHitTarget)
        {
            var missInfo = spell.PreprocessSpellHit(_spellHitTarget, this);

            if (missInfo != SpellMissInfo.None)
            {
                if (missInfo != SpellMissInfo.Miss)
                    spell.Caster.WorldObjectCombat.SendSpellMiss(unit, spell.SpellInfo.Id, missInfo);

                spell.DamageInEffects = 0;
                spell.HealingInEffects = 0;
                _spellHitTarget = null;
            }
        }

        // scripts can modify damage/healing for current target, save them
        Damage = spell.DamageInEffects;
        Healing = spell.HealingInEffects;
    }

    public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
    {
        var unit = spell.Caster.GUID == TargetGuid ? spell.Caster.AsUnit : Global.ObjAccessor.GetUnit(spell.Caster, TargetGuid);

        if (unit == null)
            return;

        // Need init unitTarget by default unit (can changed in code on reflect)
        // Or on missInfo != SPELL_MISS_NONE unitTarget undefined (but need in trigger subsystem)
        spell.UnitTarget = unit;
        spell.TargetMissInfo = MissCondition;

        // Reset damage/healing counter
        spell.DamageInEffects = Damage;
        spell.HealingInEffects = Healing;

        if (unit.IsAlive != IsAlive)
            return;

        if (spell.State == SpellState.Delayed && !spell.IsPositive && (GameTime.GetGameTimeMS() - TimeDelay) <= unit.LastSanctuaryTime)
            return; // No missinfo in that case

        if (_spellHitTarget)
            spell.DoSpellEffectHit(_spellHitTarget, spellEffectInfo, this);

        // scripts can modify damage/healing for current target, save them
        Damage = spell.DamageInEffects;
        Healing = spell.HealingInEffects;
    }

    public override void DoDamageAndTriggers(Spell spell)
    {
        var unit = spell.Caster.GUID == TargetGuid ? spell.Caster.AsUnit : Global.ObjAccessor.GetUnit(spell.Caster, TargetGuid);

        if (unit == null)
            return;

        // other targets executed before this one changed pointer
        spell.UnitTarget = unit;

        if (_spellHitTarget)
            spell.UnitTarget = _spellHitTarget;

        // Reset damage/healing counter
        spell.DamageInEffects = Damage;
        spell.HealingInEffects = Healing;

        // Get original caster (if exist) and calculate damage/healing from him data
        // Skip if m_originalCaster not available
        var caster = spell.OriginalCaster ? spell.OriginalCaster : spell.Caster.AsUnit;

        if (caster != null)
        {
            // Fill base trigger info
            var procAttacker = spell.ProcAttacker;
            var procVictim = spell.ProcVictim;
            var procSpellType = ProcFlagsSpellType.None;
            var hitMask = ProcFlagsHit.None;

            // Spells with this flag cannot trigger if effect is cast on self
            var canEffectTrigger = (!spell.SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs) || !spell.SpellInfo.HasAttribute(SpellAttr3.SuppressTargetProcs)) && spell.UnitTarget.CanProc;

            // Trigger info was not filled in Spell::prepareDataForTriggerSystem - we do it now
            if (canEffectTrigger && !procAttacker && !procVictim)
            {
                var positive = true;

                if (spell.DamageInEffects > 0)
                    positive = false;
                else if (spell.HealingInEffects == 0)
                    for (var i = 0; i < spell.SpellInfo.Effects.Count; ++i)
                    {
                        // in case of immunity, check all effects to choose correct procFlags, as none has technically hit
                        if (!Effects.Contains(i))
                            continue;

                        if (!spell.SpellInfo.IsPositiveEffect(i))
                        {
                            positive = false;

                            break;
                        }
                    }

                switch (spell.SpellInfo.DmgClass)
                {
                    case SpellDmgClass.None:
                    case SpellDmgClass.Magic:
                        if (spell.SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                        {
                            if (positive)
                            {
                                procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                                procVictim.Or(ProcFlags.TakeHelpfulPeriodic);
                            }
                            else
                            {
                                procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                                procVictim.Or(ProcFlags.TakeHarmfulPeriodic);
                            }
                        }
                        else if (spell.SpellInfo.HasAttribute(SpellAttr0.IsAbility))
                        {
                            if (positive)
                            {
                                procAttacker.Or(ProcFlags.DealHelpfulAbility);
                                procVictim.Or(ProcFlags.TakeHelpfulAbility);
                            }
                            else
                            {
                                procAttacker.Or(ProcFlags.DealHarmfulAbility);
                                procVictim.Or(ProcFlags.TakeHarmfulAbility);
                            }
                        }
                        else
                        {
                            if (positive)
                            {
                                procAttacker.Or(ProcFlags.DealHelpfulSpell);
                                procVictim.Or(ProcFlags.TakeHelpfulSpell);
                            }
                            else
                            {
                                procAttacker.Or(ProcFlags.DealHarmfulSpell);
                                procVictim.Or(ProcFlags.TakeHarmfulSpell);
                            }
                        }

                        break;
                }
            }

            // All calculated do it!
            // Do healing
            var hasHealing = false;
            DamageInfo spellDamageInfo = null;
            HealInfo healInfo = null;

            if (spell.HealingInEffects > 0)
            {
                hasHealing = true;
                var addhealth = spell.HealingInEffects;

                if (IsCrit)
                {
                    hitMask |= ProcFlagsHit.Critical;
                    addhealth = Unit.SpellCriticalHealingBonus(caster, spell.SpellInfo, addhealth, null);
                }
                else
                {
                    hitMask |= ProcFlagsHit.Normal;
                }

                healInfo = new HealInfo(caster, spell.UnitTarget, (uint)addhealth, spell.SpellInfo, spell.SpellInfo.GetSchoolMask());
                caster.HealBySpell(healInfo, IsCrit);
                spell.UnitTarget.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.EffectiveHeal * 0.5f, spell.SpellInfo);
                spell.HealingInEffects = (int)healInfo.EffectiveHeal;

                procSpellType |= ProcFlagsSpellType.Heal;
            }

            // Do damage
            var hasDamage = false;

            if (spell.DamageInEffects > 0)
            {
                hasDamage = true;
                // Fill base damage struct (unitTarget - is real spell target)
                SpellNonMeleeDamage damageInfo = new(caster, spell.UnitTarget, spell.SpellInfo, spell.SpellVisual, spell.SpellSchoolMask, spell.CastId);

                // Check damage immunity
                if (spell.UnitTarget.IsImmunedToDamage(spell.SpellInfo))
                {
                    hitMask = ProcFlagsHit.Immune;
                    spell.DamageInEffects = 0;

                    // no packet found in sniffs
                }
                else
                {
                    caster.LastDamagedTargetGuid = spell.UnitTarget.GUID;

                    // Add bonuses and fill damageInfo struct
                    caster.CalculateSpellDamageTaken(damageInfo, spell.DamageInEffects, spell.SpellInfo, spell.AttackType, IsCrit, MissCondition == SpellMissInfo.Block, spell);

                    var p = caster.AsPlayer;

                    if (p != null)
                        Global.ScriptMgr.ForEach<IPlayerOnDealDamage>(p.Class, d => d.OnDamage(p, spell.UnitTarget, ref damageInfo.Damage, spell.SpellInfo));

                    Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);

                    hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);
                    procVictim.Or(ProcFlags.TakeAnyDamage);

                    spell.DamageInEffects = (int)damageInfo.Damage;

                    caster.DealSpellDamage(damageInfo, true);

                    // Send log damage message to client
                    caster.SendSpellNonMeleeDamageLog(damageInfo);
                }

                // Do triggers for unit
                if (canEffectTrigger)
                {
                    spellDamageInfo = new DamageInfo(damageInfo, DamageEffectType.SpellDirect, spell.AttackType, hitMask);
                    procSpellType |= ProcFlagsSpellType.Damage;
                }
            }

            // Passive spell hits/misses or active spells only misses (only triggers)
            if (!hasHealing && !hasDamage)
            {
                // Fill base damage struct (unitTarget - is real spell target)
                SpellNonMeleeDamage damageInfo = new(caster, spell.UnitTarget, spell.SpellInfo, spell.SpellVisual, spell.SpellSchoolMask);
                hitMask |= Unit.CreateProcHitMask(damageInfo, MissCondition);

                // Do triggers for unit
                if (canEffectTrigger)
                {
                    spellDamageInfo = new DamageInfo(damageInfo, DamageEffectType.NoDamage, spell.AttackType, hitMask);
                    procSpellType |= ProcFlagsSpellType.NoDmgHeal;
                }

                // Failed Pickpocket, reveal rogue
                if (MissCondition == SpellMissInfo.Resist && spell.SpellInfo.HasAttribute(SpellCustomAttributes.PickPocket) && spell.UnitTarget.IsCreature)
                {
                    var unitCaster = spell.Caster.AsUnit;
                    unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);
                    spell.UnitTarget.AsCreature.EngageWithTarget(unitCaster);
                }
            }

            // Do triggers for unit
            if (canEffectTrigger)
            {
                if (spell.SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
                    procAttacker = new ProcFlagsInit();

                if (spell.SpellInfo.HasAttribute(SpellAttr3.SuppressTargetProcs))
                    procVictim = new ProcFlagsInit();

                Unit.ProcSkillsAndAuras(caster, spell.UnitTarget, procAttacker, procVictim, procSpellType, ProcFlagsSpellPhase.Hit, hitMask, spell, spellDamageInfo, healInfo);

                // item spells (spell hit of non-damage spell may also activate items, for example seal of corruption hidden hit)
                if (caster.IsPlayer && procSpellType.HasAnyFlag(ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal))
                    if (spell.SpellInfo.DmgClass == SpellDmgClass.Melee || spell.SpellInfo.DmgClass == SpellDmgClass.Ranged)
                        if (!spell.SpellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat) && !spell.SpellInfo.HasAttribute(SpellAttr4.SuppressWeaponProcs))
                            caster.AsPlayer.CastItemCombatSpell(spellDamageInfo);
            }

            // set hitmask for finish procs
            spell.HitMask |= hitMask;

            // Do not take combo points on dodge and miss
            if (MissCondition != SpellMissInfo.None && spell.NeedComboPoints && spell.Targets.UnitTargetGUID == TargetGuid)
                spell.NeedComboPoints = false;

            // _spellHitTarget can be null if spell is missed in DoSpellHitOnUnit
            if (MissCondition != SpellMissInfo.Evade && _spellHitTarget && !spell.Caster.WorldObjectCombat.IsFriendlyTo(unit) && (!spell.IsPositive || spell.SpellInfo.HasEffect(SpellEffectName.Dispel)))
            {
                var unitCaster = spell.Caster.AsUnit;

                if (unitCaster != null)
                {
                    unitCaster.AtTargetAttacked(unit, spell.SpellInfo.HasInitialAggro);

                    if (spell.SpellInfo.HasAttribute(SpellAttr6.TapsImmediately))
                    {
                        var targetCreature = unit.AsCreature;

                        if (targetCreature != null)
                            if (unitCaster.IsPlayer)
                                targetCreature.SetTappedBy(unitCaster);
                    }
                }

                if (!spell.SpellInfo.HasAttribute(SpellAttr3.DoNotTriggerTargetStand) && !unit.IsStandState)
                    unit.SetStandState(UnitStandStateType.Stand);
            }

            // Check for SPELL_ATTR7_INTERRUPT_ONLY_NONPLAYER
            if (MissCondition == SpellMissInfo.None && spell.SpellInfo.HasAttribute(SpellAttr7.InterruptOnlyNonplayer) && !unit.IsPlayer)
                caster.CastSpell(unit, 32747, new CastSpellExtraArgs(spell));
        }

        if (_spellHitTarget)
        {
            //AI functions
            var cHitTarget = _spellHitTarget.AsCreature;

            if (cHitTarget != null)
            {
                var hitTargetAI = cHitTarget.AI;

                if (hitTargetAI != null)
                    hitTargetAI.SpellHit(spell.Caster, spell.SpellInfo);
            }

            if (spell.Caster.IsCreature && spell.Caster.AsCreature.IsAIEnabled)
                spell.Caster.AsCreature.AI.SpellHitTarget(_spellHitTarget, spell.SpellInfo);
            else if (spell.Caster.IsGameObject && spell.Caster.AsGameObject.AI != null)
                spell.Caster.AsGameObject.AI.SpellHitTarget(_spellHitTarget, spell.SpellInfo);

            if (HitAura != null)
            {
                var aurApp = HitAura.GetApplicationOfTarget(_spellHitTarget.GUID);

                if (aurApp != null)
                {
                    var effMask = Effects.ToHashSet();
                    // only apply unapplied effects (for reapply case)
                    effMask.IntersectWith(aurApp.EffectsToApply);

                    for (var i = 0; i < spell.SpellInfo.Effects.Count; ++i)
                        if (effMask.Contains(i) && aurApp.HasEffect(i))
                            effMask.Remove(i);

                    if (effMask.Count != 0)
                        _spellHitTarget._ApplyAura(aurApp, effMask);
                }
            }

            // Needs to be called after dealing damage/healing to not remove breaking on damage auras
            spell.DoTriggersOnSpellHit(_spellHitTarget);
        }

        if (_enablePVP)
            spell.Caster.AsPlayer.UpdatePvP(true);

        spell.SpellAura = HitAura;
        spell.CallScriptAfterHitHandlers();
        spell.SpellAura = null;
    }
}