// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.CombatLog;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Objects;

public class WorldObjectCombat
{
    private readonly WorldObject _worldObject;

    public WorldObjectCombat(WorldObject worldObject)
    {
        _worldObject = worldObject;
    }

    public virtual uint GetCastSpellXSpellVisualId(SpellInfo spellInfo)
    {
        return spellInfo.GetSpellXSpellVisualId(_worldObject);
    }

    public float GetSpellMaxRangeForTarget(Unit target, SpellInfo spellInfo)
    {
        if (spellInfo.RangeEntry == null)
            return 0.0f;

        if (spellInfo.RangeEntry.RangeMax[0] == spellInfo.RangeEntry.RangeMax[1])
            return spellInfo.GetMaxRange();

        if (!target)
            return spellInfo.GetMaxRange(true);

        return spellInfo.GetMaxRange(!IsHostileTo(target));
    }

    public float GetSpellMinRangeForTarget(Unit target, SpellInfo spellInfo)
    {
        if (spellInfo.RangeEntry == null)
            return 0.0f;

        if (spellInfo.RangeEntry.RangeMin[0] == spellInfo.RangeEntry.RangeMin[1])
            return spellInfo.GetMinRange();

        if (!target)
            return spellInfo.GetMinRange(true);

        return spellInfo.GetMinRange(!IsHostileTo(target));
    }

    public void ModSpellCastTime(SpellInfo spellInfo, ref int castTime, Spell spell = null)
    {
        if (spellInfo == null || castTime < 0)
            return;

        // called from caster
        var modOwner = _worldObject.SpellModOwner;

        modOwner?.ApplySpellMod(spellInfo, SpellModOp.ChangeCastTime, ref castTime, spell);

        var unitCaster = _worldObject.AsUnit;

        if (!unitCaster)
            return;

        if (unitCaster.IsPlayer && unitCaster.AsPlayer.GetCommandStatus(PlayerCommandStates.Casttime))
            castTime = 0;
        else if (!(spellInfo.HasAttribute(SpellAttr0.IsAbility) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill) || spellInfo.HasAttribute(SpellAttr3.IgnoreCasterModifiers)) && ((_worldObject.IsPlayer && spellInfo.SpellFamilyName != 0) || _worldObject.IsCreature))
            castTime = unitCaster.CanInstantCast ? 0 : (int)(castTime * unitCaster.UnitData.ModCastingSpeed);
        else if (spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && !spellInfo.HasAttribute(SpellAttr2.AutoRepeat))
            castTime = (int)(castTime * unitCaster.ModAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
        else if (_worldObject.SpellManager.IsPartOfSkillLine(SkillType.Cooking, spellInfo.Id) && unitCaster.HasAura(67556)) // cooking with Chef Hat.
            castTime = 500;
    }

    public void ModSpellDurationTime(SpellInfo spellInfo, ref int duration, Spell spell = null)
    {
        if (spellInfo == null || duration < 0)
            return;

        if (spellInfo.IsChanneled && !spellInfo.HasAttribute(SpellAttr5.SpellHasteAffectsPeriodic))
            return;

        // called from caster
        var modOwner = _worldObject.SpellModOwner;

        modOwner?.ApplySpellMod(spellInfo, SpellModOp.ChangeCastTime, ref duration, spell);

        var unitCaster = _worldObject.AsUnit;

        if (!unitCaster)
            return;

        if (!(spellInfo.HasAttribute(SpellAttr0.IsAbility) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill) || spellInfo.HasAttribute(SpellAttr3.IgnoreCasterModifiers)) &&
            ((_worldObject.IsPlayer && spellInfo.SpellFamilyName != 0) || _worldObject.IsCreature))
            duration = (int)(duration * unitCaster.UnitData.ModCastingSpeed);
        else if (spellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) && !spellInfo.HasAttribute(SpellAttr2.AutoRepeat))
            duration = (int)(duration * unitCaster.ModAttackSpeedPct[(int)WeaponAttackType.RangedAttack]);
    }

    public virtual double MeleeSpellMissChance(Unit victim, WeaponAttackType attType, SpellInfo spellInfo)
    {
        return 0.0f;
    }

    public virtual SpellMissInfo MeleeSpellHitResult(Unit victim, SpellInfo spellInfo)
    {
        return SpellMissInfo.None;
    }

    public SpellMissInfo SpellHitResult(Unit victim, SpellInfo spellInfo, bool canReflect = false)
    {
        // Check for immune
        if (victim.IsImmunedToSpell(spellInfo, _worldObject))
            return SpellMissInfo.Immune;

        // Damage immunity is only checked if the spell has damage effects, this immunity must not prevent aura apply
        // returns SPELL_MISS_IMMUNE in that case, for other spells, the SMSG_SPELL_GO must show hit
        if (spellInfo.HasOnlyDamageEffects && victim.IsImmunedToDamage(spellInfo))
            return SpellMissInfo.Immune;

        // All positive spells can`t miss
        // @todo client not show miss log for this spells - so need find info for this in dbc and use it!
        if (spellInfo.IsPositive && !IsHostileTo(victim)) // prevent from affecting enemy by "positive" spell
            return SpellMissInfo.None;

        if (_worldObject == victim)
            return SpellMissInfo.None;

        // Return evade for units in evade mode
        if (victim.IsCreature && victim.AsCreature.IsEvadingAttacks)
            return SpellMissInfo.Evade;

        // Try victim reflect spell
        if (canReflect)
        {
            var reflectchance = victim.GetTotalAuraModifier(AuraType.ReflectSpells);
            reflectchance += victim.GetTotalAuraModifierByMiscMask(AuraType.ReflectSpellsSchool, (int)spellInfo.GetSchoolMask());

            if (reflectchance > 0 && RandomHelper.randChance(reflectchance))
                return SpellMissInfo.Reflect;
        }

        if (spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
            return SpellMissInfo.None;

        switch (spellInfo.DmgClass)
        {
            case SpellDmgClass.Ranged:
            case SpellDmgClass.Melee:
                return MeleeSpellHitResult(victim, spellInfo);
            case SpellDmgClass.None:
                return SpellMissInfo.None;
            case SpellDmgClass.Magic:
                return MagicSpellHitResult(victim, spellInfo);
        }

        return SpellMissInfo.None;
    }

    public void SendSpellMiss(Unit target, uint spellID, SpellMissInfo missInfo)
    {
        SpellMissLog spellMissLog = new()
        {
            SpellID = spellID,
            Caster = _worldObject.GUID
        };

        spellMissLog.Entries.Add(new SpellLogMissEntry(target.GUID, (byte)missInfo));
        _worldObject.SendMessageToSet(spellMissLog, true);
    }

    public FactionTemplateRecord GetFactionTemplateEntry()
    {
        var factionId = _worldObject.Faction;
        var entry = _worldObject.CliDB.FactionTemplateStorage.LookupByKey(factionId);

        if (entry == null)
            switch (_worldObject.TypeId)
            {
                case TypeId.Player:
                    Log.Logger.Error($"Player {_worldObject.AsPlayer.GetName()} has invalid faction (faction template id) #{factionId}");

                    break;
                case TypeId.Unit:
                    Log.Logger.Error($"Creature (template id: {_worldObject.AsCreature.Template.Entry}) has invalid faction (faction template Id) #{factionId}");

                    break;
                case TypeId.GameObject:
                    if (factionId != 0) // Gameobjects may have faction template id = 0
                        Log.Logger.Error($"GameObject (template id: {_worldObject.AsGameObject.Template.entry}) has invalid faction (faction template Id) #{factionId}");

                    break;
                default:
                    Log.Logger.Error($"Object (name={_worldObject.GetName()}, type={_worldObject.TypeId}) has invalid faction (faction template Id) #{factionId}");

                    break;
            }

        return entry;
    }

    public ReputationRank GetReactionTo(WorldObject target)
    {
        // always friendly to self
        if (_worldObject == target)
            return ReputationRank.Friendly;

        bool IsAttackableBySummoner(Unit me, ObjectGuid targetGuid)
        {
            if (!me)
                return false;

            var tempSummon = me.ToTempSummon();

            if (tempSummon == null || tempSummon.SummonPropertiesRecord == null)
                return false;

            if (tempSummon.SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.AttackableBySummoner) && targetGuid == tempSummon.GetSummonerGUID())
                return true;

            return false;
        }

        if (IsAttackableBySummoner(_worldObject.AsUnit, target.GUID) || IsAttackableBySummoner(target.AsUnit, _worldObject.GUID))
            return ReputationRank.Neutral;

        // always friendly to charmer or owner
        if (_worldObject.CharmerOrOwnerOrSelf == target.CharmerOrOwnerOrSelf)
            return ReputationRank.Friendly;

        var selfPlayerOwner = _worldObject.AffectingPlayer;
        var targetPlayerOwner = target.AffectingPlayer;

        // check forced reputation to support SPELL_AURA_FORCE_REACTION
        if (selfPlayerOwner)
        {
            var targetFactionTemplateEntry = target.WorldObjectCombat.GetFactionTemplateEntry();

            if (targetFactionTemplateEntry != null)
            {
                var repRank = selfPlayerOwner.ReputationMgr.GetForcedRankIfAny(targetFactionTemplateEntry);

                if (repRank != ReputationRank.None)
                    return repRank;
            }
        }
        else if (targetPlayerOwner)
        {
            var selfFactionTemplateEntry = GetFactionTemplateEntry();

            if (selfFactionTemplateEntry != null)
            {
                var repRank = targetPlayerOwner.ReputationMgr.GetForcedRankIfAny(selfFactionTemplateEntry);

                if (repRank != ReputationRank.None)
                    return repRank;
            }
        }

        var unit = _worldObject.AsUnit ?? selfPlayerOwner;
        var targetUnit = target.AsUnit ?? targetPlayerOwner;

        if (unit && unit.HasUnitFlag(UnitFlags.PlayerControlled))
            if (targetUnit && targetUnit.HasUnitFlag(UnitFlags.PlayerControlled))
            {
                if (selfPlayerOwner && targetPlayerOwner)
                {
                    // always friendly to other unit controlled by player, or to the player himself
                    if (selfPlayerOwner == targetPlayerOwner)
                        return ReputationRank.Friendly;

                    // duel - always hostile to opponent
                    if (selfPlayerOwner.Duel != null && selfPlayerOwner.Duel.Opponent == targetPlayerOwner && selfPlayerOwner.Duel.State == DuelState.InProgress)
                        return ReputationRank.Hostile;

                    // same group - checks dependant only on our faction - skip FFA_PVP for example
                    if (selfPlayerOwner.IsInRaidWith(targetPlayerOwner))
                        return ReputationRank.Friendly; // return true to allow config option AllowTwoSide.Interaction.Group to work
                    // however client seems to allow mixed group parties, because in 13850 client it works like:
                    // return GetFactionReactionTo(GetFactionTemplateEntry(), target);
                }

                // check FFA_PVP
                if (unit.IsFFAPvP && targetUnit.IsFFAPvP)
                    return ReputationRank.Hostile;

                if (selfPlayerOwner)
                {
                    var targetFactionTemplateEntry = targetUnit.WorldObjectCombat.GetFactionTemplateEntry();

                    if (targetFactionTemplateEntry != null)
                    {
                        var repRank = selfPlayerOwner.ReputationMgr.GetForcedRankIfAny(targetFactionTemplateEntry);

                        if (repRank != ReputationRank.None)
                            return repRank;

                        if (!selfPlayerOwner.HasUnitFlag2(UnitFlags2.IgnoreReputation))
                        {
                            var targetFactionEntry = _worldObject.CliDB.FactionStorage.LookupByKey(targetFactionTemplateEntry.Faction);

                            if (targetFactionEntry != null)
                                if (targetFactionEntry.CanHaveReputation())
                                {
                                    // check contested flags
                                    if ((targetFactionTemplateEntry.Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0 && selfPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
                                        return ReputationRank.Hostile;

                                    // if faction has reputation, hostile state depends only from AtWar state
                                    if (selfPlayerOwner.ReputationMgr.IsAtWar(targetFactionEntry))
                                        return ReputationRank.Hostile;

                                    return ReputationRank.Friendly;
                                }
                        }
                    }
                }
            }

        // do checks dependant only on our faction
        return GetFactionReactionTo(GetFactionTemplateEntry(), target);
    }

    public ReputationRank GetFactionReactionTo(FactionTemplateRecord factionTemplateEntry, WorldObject target)
    {
        // always neutral when no template entry found
        if (factionTemplateEntry == null)
            return ReputationRank.Neutral;

        var targetFactionTemplateEntry = target.WorldObjectCombat.GetFactionTemplateEntry();

        if (targetFactionTemplateEntry == null)
            return ReputationRank.Neutral;

        var targetPlayerOwner = target.AffectingPlayer;

        if (targetPlayerOwner != null)
        {
            // check contested flags
            if ((factionTemplateEntry.Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0 && targetPlayerOwner.HasPlayerFlag(PlayerFlags.ContestedPVP))
                return ReputationRank.Hostile;

            var repRank = targetPlayerOwner.ReputationMgr.GetForcedRankIfAny(factionTemplateEntry);

            if (repRank != ReputationRank.None)
                return repRank;

            if (target.IsUnit && !target.AsUnit.HasUnitFlag2(UnitFlags2.IgnoreReputation))
            {
                var factionEntry = _worldObject.CliDB.FactionStorage.LookupByKey(factionTemplateEntry.Faction);

                if (factionEntry != null)
                    if (factionEntry.CanHaveReputation())
                    {
                        // CvP case - check reputation, don't allow state higher than neutral when at war
                        var repRank1 = targetPlayerOwner.ReputationMgr.GetRank(factionEntry);

                        if (targetPlayerOwner.ReputationMgr.IsAtWar(factionEntry))
                            repRank1 = (ReputationRank)Math.Min((int)ReputationRank.Neutral, (int)repRank1);

                        return repRank1;
                    }
            }
        }

        // common faction based check
        if (factionTemplateEntry.IsHostileTo(targetFactionTemplateEntry))
            return ReputationRank.Hostile;

        if (factionTemplateEntry.IsFriendlyTo(targetFactionTemplateEntry))
            return ReputationRank.Friendly;

        if (targetFactionTemplateEntry.IsFriendlyTo(factionTemplateEntry))
            return ReputationRank.Friendly;

        if ((factionTemplateEntry.Flags & (ushort)FactionTemplateFlags.HostileByDefault) != 0)
            return ReputationRank.Hostile;

        // neutral by default
        return ReputationRank.Neutral;
    }

    public bool IsHostileTo(WorldObject target)
    {
        return GetReactionTo(target) <= ReputationRank.Hostile;
    }

    public bool IsFriendlyTo(WorldObject target)
    {
        return GetReactionTo(target) >= ReputationRank.Friendly;
    }

    public bool IsNeutralToAll()
    {
        var myFaction = GetFactionTemplateEntry();

        if (myFaction.Faction == 0)
            return true;

        var rawFaction = _worldObject.CliDB.FactionStorage.LookupByKey(myFaction.Faction);

        if (rawFaction is { ReputationIndex: >= 0 })
            return false;

        return myFaction.IsNeutralToAll();
    }

    public void SendPlaySpellVisual(WorldObject target, uint spellVisualId, ushort missReason, ushort reflectStatus, float travelSpeed, bool speedAsTime = false, float launchDelay = 0)
    {
        PlaySpellVisual playSpellVisual = new()
        {
            Source = _worldObject.GUID,
            Target = target.GUID,
            TargetPosition = target.Location,
            SpellVisualID = spellVisualId,
            TravelSpeed = travelSpeed,
            MissReason = missReason,
            ReflectStatus = reflectStatus,
            SpeedAsTime = speedAsTime,
            LaunchDelay = launchDelay
        };

        _worldObject.SendMessageToSet(playSpellVisual, true);
    }

    public void SendPlaySpellVisualKit(uint id, uint type, uint duration)
    {
        PlaySpellVisualKit playSpellVisualKit = new()
        {
            Unit = _worldObject.GUID,
            KitRecID = id,
            KitType = type,
            Duration = duration
        };

        _worldObject.SendMessageToSet(playSpellVisualKit, true);
    }

    public bool IsValidAttackTarget(WorldObject target, SpellInfo bySpell = null)
    {
        // some positive spells can be casted at hostile target
        var isPositiveSpell = bySpell is { IsPositive: true };

        // can't attack self (spells can, attribute check)
        if (bySpell == null && _worldObject == target)
            return false;

        // can't attack unattackable units
        var unitTarget = target.AsUnit;

        if (unitTarget != null && unitTarget.HasUnitState(UnitState.Unattackable))
            return false;

        // can't attack GMs
        if (target.IsPlayer && target.AsPlayer.IsGameMaster)
            return false;

        var unit = _worldObject.AsUnit;

        // visibility checks (only units)
        if (unit != null)
            // can't attack invisible
            if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.IgnorePhaseShift))
                if (!unit.Visibility.CanSeeOrDetect(target, bySpell is { IsAffectingArea: true }))
                    return false;

        // can't attack dead
        if ((bySpell == null || !bySpell.IsAllowingDeadTarget) && unitTarget is { IsAlive: false })
            return false;

        // can't attack untargetable
        if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable)) && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable2))
            return false;

        if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.Uninteractible))
            return false;

        var playerAttacker = _worldObject.AsPlayer;

        if (playerAttacker != null)
            if (playerAttacker.HasPlayerFlag(PlayerFlags.Uber))
                return false;

        // check flags
        if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.OnTaxi | UnitFlags.NotAttackable1))
            return false;

        var unitOrOwner = unit;
        var go = _worldObject.AsGameObject;

        if (go?.GoType == GameObjectTypes.Trap)
            unitOrOwner = go.OwnerUnit;

        // ignore immunity flags when assisting
        if (unitOrOwner != null && unitTarget != null && !(isPositiveSpell && bySpell.HasAttribute(SpellAttr6.CanAssistImmunePc)))
        {
            if (!unitOrOwner.HasUnitFlag(UnitFlags.PlayerControlled) && unitTarget.IsImmuneToNPC())
                return false;

            if (!unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) && unitOrOwner.IsImmuneToNPC())
                return false;

            if (bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
            {
                if (unitOrOwner.HasUnitFlag(UnitFlags.PlayerControlled) && unitTarget.IsImmuneToPc())
                    return false;

                if (unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) && unitOrOwner.IsImmuneToPc())
                    return false;
            }
        }

        // CvC case - can attack each other only when one of them is hostile
        if (unit != null && !unit.HasUnitFlag(UnitFlags.PlayerControlled) && unitTarget != null && !unitTarget.HasUnitFlag(UnitFlags.PlayerControlled))
            return IsHostileTo(unitTarget) || unitTarget.WorldObjectCombat.IsHostileTo(_worldObject);

        // Traps without owner or with NPC owner versus Creature case - can attack to creature only when one of them is hostile
        if (go?.GoType == GameObjectTypes.Trap)
        {
            var goOwner = go.OwnerUnit;

            if (goOwner == null || !goOwner.HasUnitFlag(UnitFlags.PlayerControlled))
                if (unitTarget != null && !unitTarget.HasUnitFlag(UnitFlags.PlayerControlled))
                    return IsHostileTo(unitTarget) || unitTarget.WorldObjectCombat.IsHostileTo(_worldObject);
        }

        // PvP, PvC, CvP case
        // can't attack friendly targets
        if (IsFriendlyTo(target) || target.WorldObjectCombat.IsFriendlyTo(_worldObject))
            return false;

        var playerAffectingAttacker = unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled) ? _worldObject.AffectingPlayer : go != null ? _worldObject.AffectingPlayer : null;
        var playerAffectingTarget = unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) ? unitTarget.AffectingPlayer : null;

        // Not all neutral creatures can be attacked (even some unfriendly faction does not react aggresive to you, like Sporaggar)
        if ((playerAffectingAttacker && !playerAffectingTarget) || (!playerAffectingAttacker && playerAffectingTarget))
        {
            var player = playerAffectingAttacker ? playerAffectingAttacker : playerAffectingTarget;
            var creature = playerAffectingAttacker ? unitTarget : unit;

            if (creature != null)
            {
                if (creature.IsContestedGuard() && player != null && player.HasPlayerFlag(PlayerFlags.ContestedPVP))
                    return true;

                var factionTemplate = creature.WorldObjectCombat.GetFactionTemplateEntry();

                if (factionTemplate != null && player != null && player.ReputationMgr.GetForcedRankIfAny(factionTemplate) == ReputationRank.None)
                {
                    var factionEntry = _worldObject.CliDB.FactionStorage.LookupByKey(factionTemplate.Faction);

                    if (factionEntry != null)
                    {
                        var repState = player.ReputationMgr.GetState(factionEntry);

                        if (repState != null)
                            if (!repState.Flags.HasFlag(ReputationFlags.AtWar))
                                return false;
                    }
                }
            }
        }

        var creatureAttacker = _worldObject.AsCreature;

        if (creatureAttacker && creatureAttacker.Template.TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit))
            return false;

        if (playerAffectingAttacker && playerAffectingTarget)
            if (playerAffectingAttacker.Duel != null && playerAffectingAttacker.Duel.Opponent == playerAffectingTarget && playerAffectingAttacker.Duel.State == DuelState.InProgress)
                return true;

        // PvP case - can't attack when attacker or target are in sanctuary
        // however, 13850 client doesn't allow to attack when one of the unit's has sanctuary flag and is pvp
        if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PlayerControlled) && unitOrOwner != null && unitOrOwner.HasUnitFlag(UnitFlags.PlayerControlled) && (unitTarget.IsInSanctuary || unitOrOwner.IsInSanctuary))
            return false;

        // additional checks - only PvP case
        if (playerAffectingAttacker && playerAffectingTarget)
        {
            if (playerAffectingTarget.IsPvP || (bySpell != null && bySpell.HasAttribute(SpellAttr5.IgnoreAreaEffectPvpCheck)))
                return true;

            if (playerAffectingAttacker.IsFFAPvP && playerAffectingTarget.IsFFAPvP)
                return true;

            return playerAffectingAttacker.HasPvpFlag(UnitPVPStateFlags.Unk1) ||
                   playerAffectingTarget.HasPvpFlag(UnitPVPStateFlags.Unk1);
        }

        return true;
    }

    public bool IsValidAssistTarget(WorldObject target, SpellInfo bySpell = null, bool spellCheck = true)
    {
        // some negative spells can be casted at friendly target
        var isNegativeSpell = bySpell is { IsPositive: false };

        // can assist to self
        if (_worldObject == target)
            return true;

        // can't assist unattackable units
        var unitTarget = target.AsUnit;

        if (unitTarget && unitTarget.HasUnitState(UnitState.Unattackable))
            return false;

        // can't assist GMs
        if (target.IsPlayer && target.AsPlayer.IsGameMaster)
            return false;

        // can't assist own vehicle or passenger
        var unit = _worldObject.AsUnit;

        if (unit && unitTarget && unit.Vehicle)
        {
            if (unit.IsOnVehicle(unitTarget))
                return false;

            if (unit.VehicleBase.IsOnVehicle(unitTarget))
                return false;
        }

        // can't assist invisible
        if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.IgnorePhaseShift)) && !_worldObject.Visibility.CanSeeOrDetect(target, bySpell is { IsAffectingArea: true }))
            return false;

        // can't assist dead
        if ((bySpell == null || !bySpell.IsAllowingDeadTarget) && unitTarget && !unitTarget.IsAlive)
            return false;

        // can't assist untargetable
        if ((bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanTargetUntargetable)) && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable2))
            return false;

        if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.Uninteractible))
            return false;

        // check flags for negative spells
        if (isNegativeSpell && unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.OnTaxi | UnitFlags.NotAttackable1))
            return false;

        if (isNegativeSpell || bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanAssistImmunePc))
        {
            if (unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled))
            {
                if (bySpell == null || !bySpell.HasAttribute(SpellAttr8.AttackIgnoreImmuneToPCFlag))
                    if (unitTarget != null && unitTarget.IsImmuneToPc())
                        return false;
            }
            else
            {
                if (unitTarget != null && unitTarget.IsImmuneToNPC())
                    return false;
            }
        }

        // can't assist non-friendly targets
        if (GetReactionTo(target) < ReputationRank.Neutral && target.WorldObjectCombat.GetReactionTo(_worldObject) < ReputationRank.Neutral && (!_worldObject.AsCreature || !_worldObject.AsCreature.Template.TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit)))
            return false;

        // PvP case
        if (unitTarget != null && unitTarget.HasUnitFlag(UnitFlags.PlayerControlled))
        {
            if (unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled))
            {
                var selfPlayerOwner = _worldObject.AffectingPlayer;
                var targetPlayerOwner = unitTarget.AffectingPlayer;

                if (selfPlayerOwner != null && targetPlayerOwner != null)
                    // can't assist player which is dueling someone
                    if (selfPlayerOwner != targetPlayerOwner && targetPlayerOwner.Duel != null)
                        return false;

                // can't assist player in ffa_pvp zone from outside
                if (unitTarget.IsFFAPvP && !unit.IsFFAPvP)
                    return false;

                // can't assist player out of sanctuary from sanctuary if has pvp enabled
                if (unitTarget.IsPvP)
                    if (unit.IsInSanctuary && !unitTarget.IsInSanctuary)
                        return false;
            }
        }
        // PvC case - player can assist creature only if has specific type flags
        // !target.HasFlag(UNIT_FIELD_FLAGS, UnitFlags.PvpAttackable) &&
        else if (unit != null && unit.HasUnitFlag(UnitFlags.PlayerControlled))
        {
            if (bySpell == null || !bySpell.HasAttribute(SpellAttr6.CanAssistImmunePc))
                if (unitTarget is { IsPvP: false })
                {
                    var creatureTarget = target.AsCreature;

                    if (creatureTarget != null)
                        return (creatureTarget.Template.TypeFlags.HasFlag(CreatureTypeFlags.TreatAsRaidUnit) || creatureTarget.Template.TypeFlags.HasFlag(CreatureTypeFlags.CanAssist));
                }
        }

        return true;
    }

    public Unit GetMagicHitRedirectTarget(Unit victim, SpellInfo spellInfo)
    {
        // Patch 1.2 notes: Spell Reflection no longer reflects abilities
        if (spellInfo.HasAttribute(SpellAttr0.IsAbility) || spellInfo.HasAttribute(SpellAttr1.NoRedirection) || spellInfo.HasAttribute(SpellAttr0.NoImmunities))
            return victim;

        var magnetAuras = victim.GetAuraEffectsByType(AuraType.SpellMagnet);

        foreach (var aurEff in magnetAuras)
        {
            var magnet = aurEff.Base.Caster;

            if (magnet != null)
                if (spellInfo.CheckExplicitTarget(_worldObject, magnet) == SpellCastResult.SpellCastOk && IsValidAttackTarget(magnet, spellInfo))
                {
                    // @todo handle this charge drop by proc in cast phase on explicit target
                    if (spellInfo.HasHitDelay)
                    {
                        // Set up missile speed based delay
                        var hitDelay = spellInfo.LaunchDelay;

                        if (spellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                            hitDelay += spellInfo.Speed;
                        else if (spellInfo.Speed > 0.0f)
                            hitDelay += Math.Max(victim.Location.GetDistance(_worldObject), 5.0f) / spellInfo.Speed;

                        var delay = (uint)Math.Floor(hitDelay * 1000.0f);
                        // Schedule charge drop
                        aurEff.Base.DropChargeDelayed(delay, AuraRemoveMode.Expire);
                    }
                    else
                    {
                        aurEff.Base.DropCharge(AuraRemoveMode.Expire);
                    }

                    return magnet;
                }
        }

        return victim;
    }

    private SpellMissInfo MagicSpellHitResult(Unit victim, SpellInfo spellInfo)
    {
        // Can`t miss on dead target (on skinning for example)
        if (!victim.IsAlive && !victim.IsPlayer)
            return SpellMissInfo.None;

        if (spellInfo.HasAttribute(SpellAttr3.NoAvoidance))
            return SpellMissInfo.None;

        double missChance;

        if (spellInfo.HasAttribute(SpellAttr7.NoAttackMiss))
        {
            missChance = 0.0f;
        }
        else
        {
            var schoolMask = spellInfo.GetSchoolMask();
            // PvP - PvE spell misschances per leveldif > 2
            var lchance = victim.IsPlayer ? 7 : 11;
            var thisLevel = _worldObject.GetLevelForTarget(victim);

            if (_worldObject.IsCreature && _worldObject.AsCreature.IsTrigger)
                thisLevel = Math.Max(thisLevel, spellInfo.SpellLevel);

            var leveldif = (int)(victim.GetLevelForTarget(_worldObject) - thisLevel);
            var levelBasedHitDiff = leveldif;

            // Base hit chance from attacker and victim levels
            double modHitChance;

            if (levelBasedHitDiff >= 0)
            {
                if (!victim.IsPlayer)
                {
                    modHitChance = 94 - 3 * Math.Min(levelBasedHitDiff, 3);
                    levelBasedHitDiff -= 3;
                }
                else
                {
                    modHitChance = 96 - Math.Min(levelBasedHitDiff, 2);
                    levelBasedHitDiff -= 2;
                }

                if (levelBasedHitDiff > 0)
                    modHitChance -= lchance * Math.Min(levelBasedHitDiff, 7);
            }
            else
            {
                modHitChance = 97 - levelBasedHitDiff;
            }

            // Spellmod from SpellModOp::HitChance
            var modOwner = _worldObject.SpellModOwner;

            modOwner?.ApplySpellMod(spellInfo, SpellModOp.HitChance, ref modHitChance);

            // Spells with SPELL_ATTR3_IGNORE_HIT_RESULT will ignore target's avoidance effects
            if (!spellInfo.HasAttribute(SpellAttr3.AlwaysHit))
                // Chance hit from victim SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE auras
                modHitChance += victim.GetTotalAuraModifierByMiscMask(AuraType.ModAttackerSpellHitChance, (int)schoolMask);

            var hitChance = modHitChance;
            // Increase hit chance from attacker SPELL_AURA_MOD_SPELL_HIT_CHANCE and attacker ratings
            var unit = _worldObject.AsUnit;

            if (unit != null)
                hitChance += unit.ModSpellHitChance;

            MathFunctions.RoundToInterval(ref hitChance, 0.0f, 100.0f);

            missChance = 100.0f - hitChance;
        }

        var tmp = missChance * 100.0f;

        var rand = RandomHelper.IRand(0, 9999);

        if (tmp > 0 && rand < tmp)
            return SpellMissInfo.Miss;

        // Chance resist mechanic (select max value from every mechanic spell effect)
        var resistChance = victim.GetMechanicResistChance(spellInfo) * 100;

        // Roll chance
        if (resistChance > 0 && rand < (tmp += resistChance))
            return SpellMissInfo.Resist;

        // cast by caster in front of victim
        if (!victim.HasUnitState(UnitState.Controlled) && (victim.Location.HasInArc(MathF.PI, _worldObject.Location) || victim.HasAuraType(AuraType.IgnoreHitDirection)))
        {
            var deflectChance = victim.GetTotalAuraModifier(AuraType.DeflectSpells) * 100;

            if (deflectChance > 0 && rand < tmp + deflectChance)
                return SpellMissInfo.Deflect;
        }

        return SpellMissInfo.None;
    }
}