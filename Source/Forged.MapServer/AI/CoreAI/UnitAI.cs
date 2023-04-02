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
    private static readonly Dictionary<(uint id, Difficulty difficulty), AISpellInfoType> _aiSpellInfo = new();

    public UnitAI(Unit unit)
    {
        Me = unit;
    }

    protected Unit Me { get; private set; }

    private ThreatManager ThreatManager => Me.GetThreatManager();
    public static void FillAISpellInfo()
    {
        Global.SpellMgr.ForEachSpellInfo(spellInfo =>
        {
            AISpellInfoType AIInfo = new();

            if (spellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead))
                AIInfo.Condition = AICondition.Die;
            else if (spellInfo.IsPassive || spellInfo.Duration == -1)
                AIInfo.Condition = AICondition.Aggro;
            else
                AIInfo.Condition = AICondition.Combat;

            if (AIInfo.Cooldown.TotalMilliseconds < spellInfo.RecoveryTime)
                AIInfo.Cooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);

            if (spellInfo.GetMaxRange(false) != 0)
                foreach (var spellEffectInfo in spellInfo.Effects)
                {
                    var targetType = spellEffectInfo.TargetA.Target;

                    if (targetType == Targets.UnitTargetEnemy || targetType == Targets.DestTargetEnemy)
                    {
                        if (AIInfo.Target < AITarget.Victim)
                            AIInfo.Target = AITarget.Victim;
                    }
                    else if (targetType == Targets.UnitDestAreaEnemy)
                    {
                        if (AIInfo.Target < AITarget.Enemy)
                            AIInfo.Target = AITarget.Enemy;
                    }

                    if (spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                    {
                        if (targetType == Targets.UnitTargetEnemy)
                        {
                            if (AIInfo.Target < AITarget.Debuff)
                                AIInfo.Target = AITarget.Debuff;
                        }
                        else if (spellInfo.IsPositive)
                        {
                            if (AIInfo.Target < AITarget.Buff)
                                AIInfo.Target = AITarget.Buff;
                        }
                    }
                }

            AIInfo.RealCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime + spellInfo.StartRecoveryTime);
            AIInfo.MaxRange = spellInfo.GetMaxRange(false) * 3 / 4;

            AIInfo.Effects = 0;
            AIInfo.Targets = 0;

            foreach (var spellEffectInfo in spellInfo.Effects)
            {
                // Spell targets self.
                if (spellEffectInfo.TargetA.Target == Targets.UnitCaster)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.Self - 1);

                // Spell targets a single enemy.
                if (spellEffectInfo.TargetA.Target == Targets.UnitTargetEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.DestTargetEnemy)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.SingleEnemy - 1);

                // Spell targets AoE at enemy.
                if (spellEffectInfo.TargetA.Target == Targets.UnitSrcAreaEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.UnitDestAreaEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.SrcCaster ||
                    spellEffectInfo.TargetA.Target == Targets.DestDynobjEnemy)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.AoeEnemy - 1);

                // Spell targets an enemy.
                if (spellEffectInfo.TargetA.Target == Targets.UnitTargetEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.DestTargetEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.UnitSrcAreaEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.UnitDestAreaEnemy ||
                    spellEffectInfo.TargetA.Target == Targets.SrcCaster ||
                    spellEffectInfo.TargetA.Target == Targets.DestDynobjEnemy)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.AnyEnemy - 1);

                // Spell targets a single friend (or self).
                if (spellEffectInfo.TargetA.Target == Targets.UnitCaster ||
                    spellEffectInfo.TargetA.Target == Targets.UnitTargetAlly ||
                    spellEffectInfo.TargetA.Target == Targets.UnitTargetParty)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.SingleFriend - 1);

                // Spell targets AoE friends.
                if (spellEffectInfo.TargetA.Target == Targets.UnitCasterAreaParty ||
                    spellEffectInfo.TargetA.Target == Targets.UnitLastTargetAreaParty ||
                    spellEffectInfo.TargetA.Target == Targets.SrcCaster)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.AoeFriend - 1);

                // Spell targets any friend (or self).
                if (spellEffectInfo.TargetA.Target == Targets.UnitCaster ||
                    spellEffectInfo.TargetA.Target == Targets.UnitTargetAlly ||
                    spellEffectInfo.TargetA.Target == Targets.UnitTargetParty ||
                    spellEffectInfo.TargetA.Target == Targets.UnitCasterAreaParty ||
                    spellEffectInfo.TargetA.Target == Targets.UnitLastTargetAreaParty ||
                    spellEffectInfo.TargetA.Target == Targets.SrcCaster)
                    AIInfo.Targets |= 1 << ((int)SelectTargetType.AnyFriend - 1);

                // Make sure that this spell includes a damage effect.
                if (spellEffectInfo.Effect == SpellEffectName.SchoolDamage ||
                    spellEffectInfo.Effect == SpellEffectName.Instakill ||
                    spellEffectInfo.Effect == SpellEffectName.EnvironmentalDamage ||
                    spellEffectInfo.Effect == SpellEffectName.HealthLeech)
                    AIInfo.Effects |= 1 << ((int)SelectEffect.Damage - 1);

                // Make sure that this spell includes a healing effect (or an apply aura with a periodic heal).
                if (spellEffectInfo.Effect == SpellEffectName.Heal ||
                    spellEffectInfo.Effect == SpellEffectName.HealMaxHealth ||
                    spellEffectInfo.Effect == SpellEffectName.HealMechanical ||
                    (spellEffectInfo.Effect == SpellEffectName.ApplyAura && spellEffectInfo.ApplyAuraName == AuraType.PeriodicHeal))
                    AIInfo.Effects |= 1 << ((int)SelectEffect.Healing - 1);

                // Make sure that this spell applies an aura.
                if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
                    AIInfo.Effects |= 1 << ((int)SelectEffect.Aura - 1);
            }

            _aiSpellInfo[(spellInfo.Id, spellInfo.Difficulty)] = AIInfo;
        });
    }

    public static AISpellInfoType GetAISpellInfo(uint spellId, Difficulty difficulty)
    {
        return _aiSpellInfo.LookupByKey((spellId, difficulty));
    }

    public virtual void AttackStart(Unit victim)
    {
        if (victim != null && Me.Attack(victim, true))
        {
            // Clear distracted state on attacking
            if (Me.HasUnitState(UnitState.Distracted))
            {
                Me.ClearUnitState(UnitState.Distracted);
                Me.MotionMaster.Clear();
            }

            Me.MotionMaster.MoveChase(victim);
        }
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
    public virtual void DamageDealt(Unit victim, ref double damage, DamageEffectType damageType) { }

    public virtual void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null) { }

    public virtual void DoAction(int action) { }

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
                var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Me.Location.Map.DifficultyID);

                if (spellInfo != null)
                {
                    DefaultTargetSelector targetSelectorInner = new(Me, spellInfo.GetMaxRange(false), false, true, 0);

                    bool targetSelector(Unit candidate)
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

                    ;
                    target = SelectTarget(SelectTargetMethod.Random, 0, targetSelector);
                }

                break;
            }
            case AITarget.Ally:
            case AITarget.Buff:
                target = Me;

                break;
            case AITarget.Debuff:
            {
                var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Me.Location.Map.DifficultyID);

                if (spellInfo != null)
                {
                    var range = spellInfo.GetMaxRange(false);

                    DefaultTargetSelector targetSelectorInner = new(Me, range, false, true, -(int)spellId);

                    bool targetSelector(Unit candidate)
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

                    ;

                    if (!spellInfo.HasAuraInterruptFlag(SpellAuraInterruptFlags.NotVictim) && targetSelector(Me.Victim))
                        target = Me.Victim;
                    else
                        target = SelectTarget(SelectTargetMethod.Random, 0, targetSelector);
                }

                break;
            }
        }

        if (target != null)
            return Me.CastSpell(target, spellId, false);

        return SpellCastResult.BadTargets;
    }

    public SpellCastResult DoCast(Unit victim, uint spellId, CastSpellExtraArgs args = null)
    {
        args = args ?? new CastSpellExtraArgs();

        if (Me.HasUnitState(UnitState.Casting) && !args.TriggerFlags.HasAnyFlag(TriggerCastFlags.IgnoreCastInProgress))
            return SpellCastResult.SpellInProgress;

        return Me.CastSpell(victim, spellId, args);
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

        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Me.Location.Map.DifficultyID);

        if (spellInfo != null)
            if (Me.IsWithinCombatRange(Me.Victim, spellInfo.GetMaxRange(false)))
            {
                Me.CastSpell(Me.Victim, spellId, new CastSpellExtraArgs(Me.Location.Map.DifficultyID));
                Me.ResetAttackTimer();

                return true;
            }

        return false;
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

    public virtual void HealDone(Unit to, double addhealth) { }

    public virtual void HealReceived(Unit by, double addhealth) { }

    public virtual void InitializeAI()
    {
        if (!Me.IsDead)
            Reset();
    }

    // Called when the unit enters combat
    // (NOTE: Creature engage logic should NOT be here, but in JustEngagedWith, which happens once threat is established!)
    public virtual void JustEnteredCombat(Unit who) { }

    // Called when the unit leaves combat
    public virtual void JustExitedCombat() { }

    /// <summary>
    // Called when unit's charm state changes with isNew = false
    // Implementation should call me->ScheduleAIChange() if AI replacement is desired
    // If this call is made, AI will be replaced on the next tick
    // When replacement is made, OnCharmed is called with isNew = true
    /// </summary>
    /// <param name="apply"> </param>
    public virtual void OnCharmed(bool isNew)
    {
        if (!isNew)
            Me.ScheduleAIChange();
    }

    // Called when the unit is about to be removed from the world (despawn, grid unload, corpse disappearing, player logging out etc.)
    public virtual void OnDespawn() { }

    /// <summary>
    ///     Called when a GameInfo event starts or ends
    /// </summary>
    public virtual void OnGameEvent(bool start, ushort eventId) { }

    public virtual void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra) { }
    public virtual void Reset() { }

    /// <summary>
    ///     Select the best target (in
    ///     <targetType>
    ///         order) from the threat list that fulfill the following:
    ///         - Not among the first
    ///         <offset>
    ///             entries in
    ///             <targetType>
    ///                 order (or MAXTHREAT order, if
    ///                 <targetType>
    ///                     is RANDOM).
    ///                     - Within at most
    ///                     <dist>
    ///                         yards (if dist > 0.0f)
    ///                         - At least -
    ///                         <dist>
    ///                             yards away (if dist
    ///                             < 0.0f)
    ///                                 - Is a player ( if playerOnly= true)
    ///                                   - Not the current tank ( if withTank= false)
    ///                                   - Has aura with ID
    ///                             <aura>
    ///                                 (if aura > 0)
    ///                                 - Does not have aura with ID -<aura> (if aura < 0)
    /// </summary>
    public Unit SelectTarget(SelectTargetMethod targetType, uint offset = 0, float dist = 0.0f, bool playerOnly = false, bool withTank = true, int aura = 0)
    {
        return SelectTarget(targetType, offset, new DefaultTargetSelector(Me, dist, playerOnly, withTank, aura));
    }

    public Unit SelectTarget(SelectTargetMethod targetType, uint offset, ICheck<Unit> selector)
    {
        return SelectTarget(targetType, offset, selector.Invoke);
    }

    /// <summary>
    ///     Select the best target (in
    ///     <targetType>
    ///         order) satisfying
    ///         <predicate>
    ///             from the threat list.
    ///             If <offset> is nonzero, the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM) are skipped.
    /// </summary>
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
            SelectTargetMethod.Random                                                                                                        => targetList.SelectRandom(),
            _                                                                                                                                => null,
        };
    }

    /// <summary>
    ///     Select the best (up to)
    ///     <num>
    ///         targets (in
    ///         <targetType>
    ///             order) from the threat list that fulfill the following:
    ///             - Not among the first
    ///             <offset>
    ///                 entries in
    ///                 <targetType>
    ///                     order (or MAXTHREAT order, if
    ///                     <targetType>
    ///                         is RANDOM).
    ///                         - Within at most
    ///                         <dist>
    ///                             yards (if dist > 0.0f)
    ///                             - At least -
    ///                             <dist>
    ///                                 yards away (if dist
    ///                                 < 0.0f)
    ///                                     - Is a player ( if playerOnly= true)
    ///                                       - Not the current tank ( if withTank= false)
    ///                                       - Has aura with ID
    ///                                 <aura>
    ///                                     (if aura > 0)
    ///                                     - Does not have aura with ID -
    ///                                     <aura>
    ///                                         (if aura
    ///                                         < 0)
    ///                                             The resulting targets are stored in
    ///                                         <targetList> (which is cleared first).
    /// </summary>
    public List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset = 0, float dist = 0f, bool playerOnly = false, bool withTank = true, int aura = 0)
    {
        return SelectTargetList(num, targetType, offset, new DefaultTargetSelector(Me, dist, playerOnly, withTank, aura).Invoke);
    }

    /// <summary>
    ///     Select the best (up to)
    ///     <num>
    ///         targets (in
    ///         <targetType>
    ///             order) satisfying
    ///             <predicate>
    ///                 from the threat list and stores them in
    ///                 <targetList>
    ///                     (which is cleared first).
    ///                     If <offset> is nonzero, the first <offset> entries in <targetType> order (or MAXTHREAT order, if <targetType> is RANDOM) are skipped.
    /// </summary>
    public List<Unit> SelectTargetList(uint num, SelectTargetMethod targetType, uint offset, Func<Unit, bool> selector)
    {
        var targetList = new List<Unit>();

        var mgr = ThreatManager;

        // shortcut: we're gonna ignore the first <offset> elements, and there's at most <offset> elements, so we ignore them all - nothing to do here
        if (mgr.ThreatListSize <= offset)
            return targetList;

        if (targetType == SelectTargetMethod.MaxDistance || targetType == SelectTargetMethod.MinDistance)
        {
            foreach (var refe in mgr.SortedThreatList)
            {
                if (!refe.IsOnline)
                    continue;

                targetList.Add(refe.Victim);
            }
        }
        else
        {
            var currentVictim = mgr.CurrentVictim;

            if (currentVictim != null)
                targetList.Add(currentVictim);

            foreach (var refe in mgr.SortedThreatList)
            {
                if (!refe.IsOnline)
                    continue;

                var thisTarget = refe.Victim;

                if (thisTarget != currentVictim)
                    targetList.Add(thisTarget);
            }
        }

        // shortcut: the list isn't gonna get any larger
        if (targetList.Count <= offset)
        {
            targetList.Clear();

            return targetList;
        }

        // right now, list is unsorted for DISTANCE types - re-sort by MAXDISTANCE
        if (targetType == SelectTargetMethod.MaxDistance || targetType == SelectTargetMethod.MinDistance)
            SortByDistance(targetList, targetType == SelectTargetMethod.MinDistance);

        // now the list is MAX sorted, reverse for MIN types
        if (targetType == SelectTargetMethod.MinThreat)
            targetList.Reverse();

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
    public virtual void SetData(uint id, uint value) { }

    public virtual void SetGUID(ObjectGuid guid, int id = 0) { }

    public virtual bool ShouldSparWith(Unit target)
    {
        return false;
    }

    public virtual void SpellInterrupted(uint spellId, uint unTimeMs) { }

    public virtual void UpdateAI(uint diff) { }
    private void SortByDistance(List<Unit> targets, bool ascending)
    {
        targets.Sort(new ObjectDistanceOrderPred(Me, ascending));
    }
}