// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Combat;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class UnitAI : IUnitAI
{
    private static readonly Dictionary<(uint id, Difficulty difficulty), AISpellInfoType> AISpellInfo = new();

    public UnitAI(Unit unit)
    {
        Me = unit;
    }

    protected Unit Me { get; }

    private ThreatManager ThreatManager => Me.GetThreatManager();

    public static void FillAISpellInfo(SpellManager spellManager)
    {
        spellManager.ForEachSpellInfo(spellInfo =>
        {
            AISpellInfoType aiInfo = new();

            if (spellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead))
                aiInfo.Condition = AICondition.Die;
            else if (spellInfo.IsPassive || spellInfo.Duration == -1)
                aiInfo.Condition = AICondition.Aggro;
            else
                aiInfo.Condition = AICondition.Combat;

            if (aiInfo.Cooldown.TotalMilliseconds < spellInfo.RecoveryTime)
                aiInfo.Cooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);

            if (spellInfo.GetMaxRange() != 0)
                foreach (var spellEffectInfo in spellInfo.Effects)
                {
                    var targetType = spellEffectInfo.TargetA.Target;

                    switch (targetType)
                    {
                        case Targets.UnitTargetEnemy or Targets.DestTargetEnemy:
                        {
                            if (aiInfo.Target < AITarget.Victim)
                                aiInfo.Target = AITarget.Victim;

                            break;
                        }
                        case Targets.UnitDestAreaEnemy:
                        {
                            if (aiInfo.Target < AITarget.Enemy)
                                aiInfo.Target = AITarget.Enemy;

                            break;
                        }
                    }

                    if (!spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                        continue;

                    if (targetType == Targets.UnitTargetEnemy)
                    {
                        if (aiInfo.Target < AITarget.Debuff)
                            aiInfo.Target = AITarget.Debuff;
                    }
                    else if (spellInfo.IsPositive)
                    {
                        if (aiInfo.Target < AITarget.Buff)
                            aiInfo.Target = AITarget.Buff;
                    }
                }

            aiInfo.RealCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime + spellInfo.StartRecoveryTime);
            aiInfo.MaxRange = spellInfo.GetMaxRange() * 3 / 4;

            aiInfo.Effects = 0;
            aiInfo.Targets = 0;

            foreach (var spellEffectInfo in spellInfo.Effects)
            {
                switch (spellEffectInfo.TargetA.Target)
                {
                    // Spell targets self.
                    case Targets.UnitCaster:
                        aiInfo.Targets |= 1 << ((int)SelectTargetType.Self - 1);

                        break;
                    // Spell targets a single enemy.
                    case Targets.UnitTargetEnemy:
                    case Targets.DestTargetEnemy:
                        aiInfo.Targets |= 1 << ((int)SelectTargetType.SingleEnemy - 1);

                        break;
                    // Spell targets AoE at enemy.
                    case Targets.UnitSrcAreaEnemy:
                    case Targets.UnitDestAreaEnemy:
                    case Targets.SrcCaster:
                    case Targets.DestDynobjEnemy:
                        aiInfo.Targets |= 1 << ((int)SelectTargetType.AoeEnemy - 1);

                        break;
                }

                // Spell targets an enemy.
                if (spellEffectInfo.TargetA.Target is Targets.UnitTargetEnemy or Targets.DestTargetEnemy or Targets.UnitSrcAreaEnemy or Targets.UnitDestAreaEnemy or Targets.SrcCaster or Targets.DestDynobjEnemy)
                    aiInfo.Targets |= 1 << ((int)SelectTargetType.AnyEnemy - 1);

                // Spell targets a single friend (or self).
                if (spellEffectInfo.TargetA.Target is Targets.UnitCaster or Targets.UnitTargetAlly or Targets.UnitTargetParty)
                    aiInfo.Targets |= 1 << ((int)SelectTargetType.SingleFriend - 1);

                // Spell targets AoE friends.
                if (spellEffectInfo.TargetA.Target is Targets.UnitCasterAreaParty or Targets.UnitLastTargetAreaParty or Targets.SrcCaster)
                    aiInfo.Targets |= 1 << ((int)SelectTargetType.AoeFriend - 1);

                // Spell targets any friend (or self).
                if (spellEffectInfo.TargetA.Target is Targets.UnitCaster or Targets.UnitTargetAlly or Targets.UnitTargetParty or Targets.UnitCasterAreaParty or Targets.UnitLastTargetAreaParty or Targets.SrcCaster)
                    aiInfo.Targets |= 1 << ((int)SelectTargetType.AnyFriend - 1);

                // Make sure that this spell includes a damage effect.
                if (spellEffectInfo.Effect is SpellEffectName.SchoolDamage or SpellEffectName.Instakill or SpellEffectName.EnvironmentalDamage or SpellEffectName.HealthLeech)
                    aiInfo.Effects |= 1 << ((int)SelectEffect.Damage - 1);

                // Make sure that this spell includes a healing effect (or an apply aura with a periodic heal).
                if (spellEffectInfo.Effect is SpellEffectName.Heal or SpellEffectName.HealMaxHealth or SpellEffectName.HealMechanical ||
                    (spellEffectInfo.Effect == SpellEffectName.ApplyAura && spellEffectInfo.ApplyAuraName == AuraType.PeriodicHeal))
                    aiInfo.Effects |= 1 << ((int)SelectEffect.Healing - 1);

                // Make sure that this spell applies an aura.
                if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
                    aiInfo.Effects |= 1 << ((int)SelectEffect.Aura - 1);
            }

            AISpellInfo[(spellInfo.Id, spellInfo.Difficulty)] = aiInfo;
        });
    }

    public static AISpellInfoType GetAISpellInfo(uint spellId, Difficulty difficulty)
    {
        return AISpellInfo.LookupByKey((spellId, difficulty));
    }

    public virtual void AttackStart(Unit victim)
    {
        if (victim == null || !Me.Attack(victim, true))
            return;

        // Clear distracted state on attacking
        if (Me.HasUnitState(UnitState.Distracted))
        {
            Me.ClearUnitState(UnitState.Distracted);
            Me.MotionMaster.Clear();
        }

        Me.MotionMaster.MoveChase(victim);
    }

    public void AttackStartCaster(Unit victim, float dist)
    {
        if (victim != null && Me.Attack(victim, false))
            Me.MotionMaster.MoveChase(victim, dist);
    }

    public virtual bool CanAIAttack(Unit victim)
    {
        return true;
    }

    // Called at any Damage to any victim (before damage apply)
    public virtual void DamageDealt(Unit victim, ref double damage, DamageEffectType damageType)
    { }

    public virtual void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    { }

    public virtual void DoAction(int action)
    { }

    public SpellCastResult DoCast(uint spellId)
    {
        Unit target = null;
        var aiTargetType = AITarget.Self;

        var info = GetAISpellInfo(spellId, Me.Location.Map.DifficultyID);

        if (info != null)
            aiTargetType = info.Target;

        switch (aiTargetType)
        {
            default:
            case AITarget.Self:
                target = Me;

                break;

            case AITarget.Victim:
                target = Me.Victim;

                break;

            case AITarget.Enemy:
            {
                var spellInfo = Me.SpellManager.GetSpellInfo(spellId, Me.Location.Map.DifficultyID);

                if (spellInfo != null)
                {
                    DefaultTargetSelector targetSelectorInner = new(Me, spellInfo.GetMaxRange(), false, true, 0);

                    bool TargetSelector(Unit candidate)
                    {
                        if (!candidate.IsPlayer)
                        {
                            if (spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                                return false;

                            if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && candidate.ControlledByPlayer)
                                return false;
                        }
                        else if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                        {
                            return false;
                        }

                        return targetSelectorInner.Invoke(candidate);
                    }

                    target = SelectTarget(SelectTargetMethod.Random, 0, TargetSelector);
                }

                break;
            }
            case AITarget.Ally:
            case AITarget.Buff:
                target = Me;

                break;

            case AITarget.Debuff:
            {
                var spellInfo = Me.SpellManager.GetSpellInfo(spellId, Me.Location.Map.DifficultyID);

                if (spellInfo != null)
                {
                    var range = spellInfo.GetMaxRange();

                    DefaultTargetSelector targetSelectorInner = new(Me, range, false, true, -(int)spellId);

                    bool TargetSelector(Unit candidate)
                    {
                        if (!candidate.IsPlayer)
                        {
                            if (spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                                return false;

                            if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && candidate.ControlledByPlayer)
                                return false;
                        }
                        else if (spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                        {
                            return false;
                        }

                        return targetSelectorInner.Invoke(candidate);
                    }

                    if (!spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.NotVictim) && TargetSelector(Me.Victim))
                        target = Me.Victim;
                    else
                        target = SelectTarget(SelectTargetMethod.Random, 0, TargetSelector);
                }

                break;
            }
        }

        return target != null ? Me.SpellFactory.CastSpell(target, spellId) : SpellCastResult.BadTargets;
    }

    public SpellCastResult DoCast(Unit victim, uint spellId, CastSpellExtraArgs args = null)
    {
        args ??= new CastSpellExtraArgs();

        if (Me.HasUnitState(UnitState.Casting) && !args.TriggerFlags.HasAnyFlag(TriggerCastFlags.IgnoreCastInProgress))
            return SpellCastResult.SpellInProgress;

        return Me.SpellFactory.CastSpell(victim, spellId, args);
    }

    public SpellCastResult DoCastAOE(uint spellId, CastSpellExtraArgs args = null)
    {
        return DoCast(null, spellId, args);
    }

    public SpellCastResult DoCastSelf(uint spellId, CastSpellExtraArgs args = null)
    {
        return DoCast(Me, spellId, args);
    }

    public SpellCastResult DoCastVictim(uint spellId, CastSpellExtraArgs args = null)
    {
        var victim = Me.Victim;

        if (victim != null)
            return DoCast(victim, spellId, args);

        return SpellCastResult.BadTargets;
    }

    public void DoMeleeAttackIfReady()
    {
        if (Me.HasUnitState(UnitState.Casting) || (Me.TryGetAsCreature(out var creature) && !creature.CanMelee))
            return;

        var victim = Me.Victim;

        if (!Me.IsWithinMeleeRange(victim))
            return;

        //Make sure our attack is ready and we aren't currently casting before checking distance
        if (Me.IsAttackReady())
        {
            Me.AttackerStateUpdate(victim);
            Me.ResetAttackTimer();
        }

        if (Me.HaveOffhandWeapon() && Me.IsAttackReady(WeaponAttackType.OffAttack))
        {
            Me.AttackerStateUpdate(victim, WeaponAttackType.OffAttack);
            Me.ResetAttackTimer(WeaponAttackType.OffAttack);
        }
    }

    public bool DoSpellAttackIfReady(uint spellId)
    {
        if (Me.HasUnitState(UnitState.Casting) || !Me.IsAttackReady())
            return true;

        var spellInfo = Me.SpellManager.GetSpellInfo(spellId, Me.Location.Map.DifficultyID);

        if (spellInfo == null)
            return false;

        if (!Me.IsWithinCombatRange(Me.Victim, spellInfo.GetMaxRange()))
            return false;

        Me.SpellFactory.CastSpell(Me.Victim, spellId, new CastSpellExtraArgs(Me.Location.Map.DifficultyID));
        Me.ResetAttackTimer();

        return true;
    }

    public virtual uint GetData(uint id = 0)
    {
        return 0;
    }

    public virtual string GetDebugInfo()
    {
        return $"Me: {(Me != null ? Me.GetDebugInfo() : "NULL")}";
    }

    public virtual ObjectGuid GetGUID(int id = 0)
    {
        return ObjectGuid.Empty;
    }

    public virtual void HealDone(Unit to, double addhealth)
    { }

    public virtual void HealReceived(Unit by, double addhealth)
    { }

    public virtual void InitializeAI()
    {
        if (!Me.IsDead)
            Reset();
    }

    // Called when the unit enters combat
    // (NOTE: Creature engage logic should NOT be here, but in JustEngagedWith, which happens once threat is established!)
    public virtual void JustEnteredCombat(Unit who)
    { }

    // Called when the unit leaves combat
    public virtual void JustExitedCombat()
    { }

    public virtual void OnCharmed(bool isNew)
    {
        if (!isNew)
            Me.ScheduleAIChange();
    }

    // Called when the unit is about to be removed from the world (despawn, grid unload, corpse disappearing, player logging out etc.)
    public virtual void OnDespawn()
    { }

    /// <summary>
    ///     Called when a GameInfo event starts or ends
    /// </summary>
    public virtual void OnGameEvent(bool start, ushort eventId)
    { }

    public virtual void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra)
    { }

    public virtual void Reset()
    { }

    public Unit SelectTarget(SelectTargetMethod targetType, uint offset = 0, float dist = 0.0f, bool playerOnly = false, bool withTank = true, int aura = 0)
    {
        return SelectTarget(targetType, offset, new DefaultTargetSelector(Me, dist, playerOnly, withTank, aura));
    }

    public Unit SelectTarget(SelectTargetMethod targetType, uint offset, ICheck<Unit> selector)
    {
        return SelectTarget(targetType, offset, selector.Invoke);
    }

    public Unit SelectTarget(SelectTargetMethod targetType, uint offset, Func<Unit, bool> selector)
    {
        var mgr = ThreatManager;

        // shortcut: if we ignore the first <offset> elements, and there are at most <offset> elements, then we ignore ALL elements
        if (mgr.ThreatListSize <= offset)
            return null;

        var targetList = SelectTargetList((uint)mgr.ThreatListSize, targetType, offset, selector);

        // maybe nothing fulfills the predicate
        if (targetList.Empty())
            return null;

        return targetType switch
        {
            SelectTargetMethod.MaxThreat or SelectTargetMethod.MinThreat or SelectTargetMethod.MaxDistance or SelectTargetMethod.MinDistance => targetList[0],
            SelectTargetMethod.Random => targetList.SelectRandom(),
            _ => null,
        };
    }

    public List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset = 0, float dist = 0f, bool playerOnly = false, bool withTank = true, int aura = 0)
    {
        return SelectTargetList(num, targetType, offset, new DefaultTargetSelector(Me, dist, playerOnly, withTank, aura).Invoke);
    }

    public List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset, Func<Unit, bool> selector)
    {
        var targetList = new List<Unit>();

        var mgr = ThreatManager;

        // shortcut: we're gonna ignore the first <offset> elements, and there's at most <offset> elements, so we ignore them all - nothing to do here
        if (mgr.ThreatListSize <= offset)
            return targetList;

        if (targetType is SelectTargetMethod.MaxDistance or SelectTargetMethod.MinDistance)
        {
            targetList.AddRange(from refe in mgr.SortedThreatList where refe.IsOnline select refe.Victim);
        }
        else
        {
            var currentVictim = mgr.CurrentVictim;

            if (currentVictim != null)
                targetList.Add(currentVictim);

            targetList.AddRange(from refe in mgr.SortedThreatList where refe.IsOnline select refe.Victim into thisTarget where thisTarget != currentVictim select thisTarget);
        }

        // shortcut: the list isn't gonna get any larger
        if (targetList.Count <= offset)
        {
            targetList.Clear();

            return targetList;
        }

        switch (targetType)
        {
            // right now, list is unsorted for DISTANCE types - re-sort by MAXDISTANCE
            case SelectTargetMethod.MaxDistance or SelectTargetMethod.MinDistance:
                SortByDistance(targetList, targetType == SelectTargetMethod.MinDistance);

                break;
            // now the list is MAX sorted, reverse for MIN types
            case SelectTargetMethod.MinThreat:
                targetList.Reverse();

                break;
        }

        // ignore the first <offset> elements
        while (offset != 0)
        {
            targetList.RemoveAt(0);
            --offset;
        }

        // then finally filter by predicate
        targetList.RemoveAll(unit => !selector(unit));

        if (targetList.Count <= num)
            return targetList;

        if (targetType == SelectTargetMethod.Random)
            targetList = targetList.SelectRandom(num).ToList();
        else
            targetList.Resize(num);

        return targetList;
    }

    public virtual void SetData(uint id, uint value)
    { }

    public virtual void SetGUID(ObjectGuid guid, int id = 0)
    { }

    public virtual bool ShouldSparWith(Unit target)
    {
        return false;
    }

    public virtual void SpellInterrupted(uint spellId, uint unTimeMs)
    { }

    public virtual void UpdateAI(uint diff)
    { }

    private void SortByDistance(List<Unit> targets, bool ascending)
    {
        targets.Sort(new ObjectDistanceOrderPred(Me, ascending));
    }
}