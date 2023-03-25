// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Combat;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.IUnit;
using Game.Spells;

namespace Game.Entities;

public partial class Unit
{
	// This value can be different from IsInCombat, for example:
	// - when a projectile spell is midair against a creature (combat on launch - threat+aggro on impact)
	// - when the creature has no targets left, but the AI has not yet ceased engaged logic
	public virtual bool IsEngaged => IsInCombat;

	public override float CombatReach => (float)UnitData.CombatReach;

	public bool IsInCombat => HasUnitFlag(UnitFlags.InCombat);

	public bool IsPetInCombat => HasUnitFlag(UnitFlags.PetInCombat);

	public bool CanHaveThreatList => _threatManager.CanHaveThreatList;

	public ObjectGuid Target => UnitData.Target;

	public Unit Victim => Attacking;

	public List<Unit> Attackers => AttackerList;

	public float BoundingRadius
	{
		get => UnitData.BoundingRadius;
		set => SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.BoundingRadius), value);
	}

	/// <summary>
	///  returns if the unit can't enter combat
	/// </summary>
	public bool IsCombatDisallowed => _isCombatDisallowed;

	public ObjectGuid LastDamagedTargetGuid
	{
		get => _lastDamagedTargetGuid;
		set => _lastDamagedTargetGuid = value;
	}

	public bool IsThreatened => !_threatManager.IsThreatListEmpty();

	public virtual void AtEnterCombat()
	{
		foreach (var pair in AppliedAuras)
			pair.Base.CallScriptEnterLeaveCombatHandlers(pair, true);

		var spell = GetCurrentSpell(CurrentSpellTypes.Generic);

		if (spell != null)
			if (spell.State == SpellState.Preparing && spell.SpellInfo.HasAttribute(SpellAttr0.NotInCombatOnlyPeaceful) && spell.SpellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Combat))
				InterruptNonMeleeSpells(false);

		RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.EnteringCombat);
		ProcSkillsAndAuras(this, null, new ProcFlagsInit(ProcFlags.EnterCombat), new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
	}

	public virtual void AtExitCombat()
	{
		foreach (var pair in AppliedAuras)
			pair.Base.CallScriptEnterLeaveCombatHandlers(pair, false);

		RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LeavingCombat);
	}

	public virtual void AtEngage(Unit target) { }

	public virtual void AtDisengage() { }

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

	public bool IsInCombatWith(Unit who)
	{
		return who != null && _combatManager.IsInCombatWith(who);
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

		var map = Map;

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

	public void EngageWithTarget(Unit enemy)
	{
		if (enemy == null)
			return;

		if (CanHaveThreatList)
			_threatManager.AddThreat(enemy, 0.0f, null, true, true);
		else
			SetInCombatWith(enemy);
	}

	public void ClearInCombat()
	{
		_combatManager.EndAllCombat();
	}

	public void ClearInPetCombat()
	{
		RemoveUnitFlag(UnitFlags.PetInCombat);
		var owner = OwnerUnit;

		if (owner != null)
			owner.RemoveUnitFlag(UnitFlags.PetInCombat);
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

	public virtual void OnCombatExit()
	{
		foreach (var aurApp in AppliedAuras)
			aurApp.Base.CallScriptEnterLeaveCombatHandlers(aurApp, false);
	}

	public bool IsEngagedBy(Unit who)
	{
		return CanHaveThreatList ? IsThreatenedBy(who) : IsInCombatWith(who);
	}

	public bool IsThreatenedBy(Unit who)
	{
		return who != null && _threatManager.IsThreatenedBy(who, true);
	}

	public bool IsSilenced(uint schoolMask)
	{
		return (UnitData.SilencedSchoolMask.GetValue() & schoolMask) != 0;
	}

	public bool IsSilenced(SpellSchoolMask schoolMask)
	{
		return IsSilenced((uint)schoolMask);
	}

	public void SetSilencedSchoolMask(uint schoolMask)
	{
		SetUpdateFieldFlagValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SilencedSchoolMask), schoolMask);
	}

	public void SetSilencedSchoolMask(SpellSchoolMask schoolMask)
	{
		SetSilencedSchoolMask((uint)schoolMask);
	}

	public void ReplaceAllSilencedSchoolMask(uint schoolMask)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.SilencedSchoolMask), schoolMask);
	}


	public void ReplaceAllSilencedSchoolMask(SpellSchoolMask schoolMask)
	{
		ReplaceAllSilencedSchoolMask((uint)schoolMask);
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

	public void ValidateAttackersAndOwnTarget()
	{
		// iterate attackers
		List<Unit> toRemove = new();

		foreach (var attacker in Attackers)
			if (!attacker.IsValidAttackTarget(this))
				toRemove.Add(attacker);

		foreach (var attacker in toRemove)
			attacker.AttackStop();

		// remove our own victim
		var victim = Victim;

		if (victim != null)
			if (!IsValidAttackTarget(victim))
				AttackStop();
	}

	public void StopAttackFaction(uint factionId)
	{
		var victim = Victim;

		if (victim != null)
			if (victim.GetFactionTemplateEntry().Faction == factionId)
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

			if (unit.GetFactionTemplateEntry().Faction == factionId)
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
			if (pair.Value.GetOther(this).GetFactionTemplateEntry().Faction == factionId)
				refsToEnd.Add(pair.Value);

		foreach (var refe in refsToEnd)
			refe.EndCombat();

		foreach (var minion in Controlled)
			minion.StopAttackFaction(factionId);
	}

	public void HandleProcExtraAttackFor(Unit victim, uint count)
	{
		while (count != 0)
		{
			--count;
			AttackerStateUpdate(victim, WeaponAttackType.BaseAttack, true);
		}
	}

	public void AddExtraAttacks(uint count)
	{
		var targetGUID = _lastDamagedTargetGuid;

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

	public bool Attack(Unit victim, bool meleeAttack)
	{
		if (victim == null || victim.GUID == GUID)
			return false;

		// dead units can neither attack nor be attacked
		if (!IsAlive || !victim.IsInWorld || !victim.IsAlive)
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

		if (Attacking != null)
			Attacking._removeAttacker(this);

		Attacking = victim;
		Attacking._addAttacker(this);

		// Set our target
		SetTarget(victim.GUID);

		if (meleeAttack)
			AddUnitState(UnitState.MeleeAttacking);

		if (creature != null && !IsControlledByPlayer)
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

				if (cControlled != null)
				{
					var controlledAI = cControlled.AI;

					if (controlledAI != null)
						controlledAI.OwnerAttacked(victim);
				}
			}

		return true;
	}

	public void SendMeleeAttackStart(Unit victim)
	{
		AttackStart packet = new();
		packet.Attacker = GUID;
		packet.Victim = victim.GUID;
		SendMessageToSet(packet, true);
	}

	public void SendMeleeAttackStop(Unit victim = null)
	{
		SendMessageToSet(new SAttackStop(this, victim), true);

		if (victim)
			Log.Logger.Information(
						"{0} {1} stopped attacking {2} {3}",
						(IsTypeId(TypeId.Player) ? "Player" : "Creature"),
						GUID.ToString(),
						(victim.IsTypeId(TypeId.Player) ? "player" : "creature"),
						victim.GUID.ToString());
		else
			Log.Logger.Information("{0} {1} stopped attacking", (IsTypeId(TypeId.Player) ? "Player" : "Creature"), GUID.ToString());
	}

	public virtual void SetTarget(ObjectGuid guid) { }

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

		if (creature != null)
			creature.SetNoCallAssistance(false);

		SendMeleeAttackStop(victim);

		return true;
	}

	public void SetLastExtraAttackSpell(uint spellId)
	{
		_lastExtraAttackSpell = spellId;
	}

	public uint GetLastExtraAttackSpell()
	{
		return _lastExtraAttackSpell;
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

	public void SetCombatReach(float combatReach)
	{
		if (combatReach > 0.1f)
			combatReach = SharedConst.DefaultPlayerCombatReach;

		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.CombatReach), combatReach);
	}

	public void ResetAttackTimer(WeaponAttackType type = WeaponAttackType.BaseAttack)
	{
		AttackTimer[(int)type] = (uint)(GetBaseAttackTime(type) * ModAttackSpeedPct[(int)type]);
	}

	public void SetAttackTimer(WeaponAttackType type, uint time)
	{
		AttackTimer[(int)type] = time;
	}

	public uint GetAttackTimer(WeaponAttackType type)
	{
		return AttackTimer[(int)type];
	}

	public bool IsAttackReady(WeaponAttackType type = WeaponAttackType.BaseAttack)
	{
		return AttackTimer[(int)type] == 0;
	}

	public uint GetBaseAttackTime(WeaponAttackType att)
	{
		return _baseAttackSpeed[(int)att];
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

		if ((attType == WeaponAttackType.BaseAttack || attType == WeaponAttackType.OffAttack) && !IsWithinLOSInMap(victim))
			return;

		AtTargetAttacked(victim, true);
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
				CheckEvade(damageInfo.Attacker, victim, ref damageInfo.Damage, ref damageInfo.Absorb);

				if (TryGetAI(out var aI))
					aI.OnMeleeAttack(damageInfo, attType, extra);

				Global.ScriptMgr.ForEach<IUnitOnMeleeAttack>(s => s.OnMeleeAttack(damageInfo, attType, extra));

				SendAttackStateUpdate(damageInfo);

				_lastDamagedTargetGuid = victim.GUID;

				DealMeleeDamage(damageInfo, true);

				DamageInfo dmgInfo = new(damageInfo);
				ProcSkillsAndAuras(damageInfo.Attacker, damageInfo.Target, damageInfo.ProcAttacker, damageInfo.ProcVictim, ProcFlagsSpellType.None, ProcFlagsSpellPhase.None, dmgInfo.HitMask, null, dmgInfo, null);

				Log.Logger.Debug(
							"AttackerStateUpdate: {0} attacked {1} for {2} dmg, absorbed {3}, blocked {4}, resisted {5}.",
							GUID.ToString(),
							victim.GUID.ToString(),
							damageInfo.Damage,
							damageInfo.Absorb,
							damageInfo.Blocked,
							damageInfo.Resist);
			}
			else
			{
				CastSpell(victim, meleeAttackSpellId, new CastSpellExtraArgs(meleeAttackAuraEffect));

				var hitInfo = HitInfo.AffectsVictim | HitInfo.NoAnimation;

				if (attType == WeaponAttackType.OffAttack)
					hitInfo |= HitInfo.OffHand;

				SendAttackStateUpdate(hitInfo, victim, GetMeleeDamageSchoolMask(), 0, 0, 0, VictimState.Hit, 0);
			}
		}
	}

	public void SetBaseWeaponDamage(WeaponAttackType attType, WeaponDamageRange damageRange, double value)
	{
		WeaponDamage[(int)attType][(int)damageRange] = value;
	}

	public Unit GetMeleeHitRedirectTarget(Unit victim, SpellInfo spellInfo = null)
	{
		var interceptAuras = victim.GetAuraEffectsByType(AuraType.InterceptMeleeRangedAttacks);

		foreach (var i in interceptAuras)
		{
			var magnet = i.Caster;

			if (magnet != null)
				if (IsValidAttackTarget(magnet, spellInfo) && magnet.IsWithinLOSInMap(this) && (spellInfo == null || (spellInfo.CheckExplicitTarget(this, magnet) == SpellCastResult.SpellCastOk && spellInfo.CheckTarget(this, magnet, false) == SpellCastResult.SpellCastOk)))
				{
					i.Base.DropCharge(AuraRemoveMode.Expire);

					return magnet;
				}
		}

		return victim;
	}

	public void SendAttackStateUpdate(HitInfo HitInfo, Unit target, SpellSchoolMask damageSchoolMask, double Damage, double AbsorbDamage, double Resist, VictimState TargetState, uint BlockedAmount)
	{
		CalcDamageInfo dmgInfo = new();
		dmgInfo.HitInfo = HitInfo;
		dmgInfo.Attacker = this;
		dmgInfo.Target = target;
		dmgInfo.Damage = Damage - AbsorbDamage - Resist - BlockedAmount;
		dmgInfo.OriginalDamage = Damage;
		dmgInfo.DamageSchoolMask = (uint)damageSchoolMask;
		dmgInfo.Absorb = AbsorbDamage;
		dmgInfo.Resist = Resist;
		dmgInfo.TargetState = TargetState;
		dmgInfo.Blocked = BlockedAmount;
		SendAttackStateUpdate(dmgInfo);
	}

	public void SendAttackStateUpdate(CalcDamageInfo damageInfo)
	{
		AttackerStateUpdate packet = new();
		packet.hitInfo = damageInfo.HitInfo;
		packet.AttackerGUID = damageInfo.Attacker.GUID;
		packet.VictimGUID = damageInfo.Target.GUID;
		packet.Damage = (int)damageInfo.Damage;
		packet.OriginalDamage = (int)damageInfo.OriginalDamage;
		var overkill = (int)(damageInfo.Damage - damageInfo.Target.Health);
		packet.OverDamage = (overkill < 0 ? -1 : overkill);

		SubDamage subDmg = new();
		subDmg.SchoolMask = (int)damageInfo.DamageSchoolMask; // School of sub damage
		subDmg.FDamage = (float)damageInfo.Damage;            // sub damage
		subDmg.Damage = (int)damageInfo.Damage;               // Sub Damage
		subDmg.Absorbed = (int)damageInfo.Absorb;
		subDmg.Resisted = (int)damageInfo.Resist;
		packet.SubDmg = subDmg;

		packet.VictimState = (byte)damageInfo.TargetState;
		packet.BlockAmount = (int)damageInfo.Blocked;
		packet.LogData.Initialize(damageInfo.Attacker);

		ContentTuningParams contentTuningParams = new();

		if (contentTuningParams.GenerateDataForUnits(damageInfo.Attacker, damageInfo.Target))
			packet.ContentTuning = contentTuningParams;

		SendCombatLogMessage(packet);
	}

	public void AtTargetAttacked(Unit target, bool canInitialAggro = true)
	{
		if (!target.IsEngaged && !canInitialAggro)
			return;

		target.EngageWithTarget(this);

		var targetOwner = target.CharmerOrOwner;

		if (targetOwner != null)
			targetOwner.EngageWithTarget(this);

		var myPlayerOwner = CharmerOrOwnerPlayerOrPlayerItself;
		var targetPlayerOwner = target.CharmerOrOwnerPlayerOrPlayerItself;

		if (myPlayerOwner && targetPlayerOwner && !(myPlayerOwner.Duel != null && myPlayerOwner.Duel.Opponent == targetPlayerOwner))
		{
			myPlayerOwner.UpdatePvP(true);
			myPlayerOwner.SetContestedPvP(targetPlayerOwner);
			myPlayerOwner.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
		}
	}

	public static void Kill(Unit attacker, Unit victim, bool durabilityLoss = true, bool skipSettingDeathState = false)
	{
		// Prevent killing unit twice (and giving reward from kill twice)
		if (victim.Health == 0)
			return;

		if (attacker != null && !attacker.IsInMap(victim))
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

		if (isRewardAllowed && creature)
		{
			foreach (var tapperGuid in creature.TapList)
			{
				var tapper = Global.ObjAccessor.GetPlayer(creature, tapperGuid);

				if (tapper != null)
					tappers.Add(tapper);
			}

			if (!creature.CanHaveLoot)
				isRewardAllowed = false;
		}

		// Exploit fix
		if (creature && creature.IsPet && creature.OwnerGUID.IsPlayer)
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
						PartyKillLog partyKillLog = new();
						partyKillLog.Player = player && tapperGroup.IsMember(player.GUID) ? player.GUID : tapper.GUID;
						partyKillLog.Victim = victim.GUID;
						partyKillLog.Write();

						tapperGroup.BroadcastPacket(partyKillLog, tapperGroup.GetMemberGroup(tapper.GUID) != 0);

						if (creature)
							tapperGroup.UpdateLooterGuid(creature, true);
					}
				}
				else
				{
					PartyKillLog partyKillLog = new();
					partyKillLog.Player = tapper.GUID;
					partyKillLog.Victim = victim.GUID;
					tapper.SendPacket(partyKillLog);
				}
			}

			// Generate loot before updating looter
			if (creature)
			{
				DungeonEncounterRecord dungeonEncounter = null;
				var instance = creature.InstanceScript;

				if (instance != null)
					dungeonEncounter = instance.GetBossDungeonEncounter(creature);

				if (creature.Map.IsDungeon)
				{
					if (dungeonEncounter != null)
					{
						creature.PersonalLoot = LootManager.GenerateDungeonEncounterPersonalLoot(dungeonEncounter.Id,
																								creature.LootId,
																								LootStorage.Creature,
																								LootType.Corpse,
																								creature,
																								creature.Template.MinGold,
																								creature.Template.MaxGold,
																								(ushort)creature.GetLootMode(),
																								creature.Map.GetDifficultyLootItemContext(),
																								tappers);
					}
					else if (!tappers.Empty())
					{
						var group = !groups.Empty() ? groups.First() : null;
						var looter = group ? Global.ObjAccessor.GetPlayer(creature, group.LooterGuid) : tappers[0];

						Loot loot = new(creature.Map, creature.GUID, LootType.Corpse, dungeonEncounter != null ? group : null);

						var lootid = creature.LootId;

						if (lootid != 0)
							loot.FillLoot(lootid, LootStorage.Creature, looter, dungeonEncounter != null, false, creature.GetLootMode(), creature.Map.GetDifficultyLootItemContext());

						if (creature.GetLootMode() > 0)
							loot.GenerateMoneyLoot(creature.Template.MinGold, creature.Template.MaxGold);

						if (group)
							loot.NotifyLootList(creature.Map);

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
						Loot loot = new(creature.Map, creature.GUID, LootType.Corpse, null);

						if (dungeonEncounter != null)
							loot.SetDungeonEncounterId(dungeonEncounter.Id);

						var lootid = creature.LootId;

						if (lootid != 0)
							loot.FillLoot(lootid, LootStorage.Creature, tapper, true, false, creature.GetLootMode(), creature.Map.GetDifficultyLootItemContext());

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
				ProcSkillsAndAuras(owner, victim, new ProcFlagsInit(ProcFlags.Kill), new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
		}

		if (!victim.IsCritter)
		{
			ProcSkillsAndAuras(attacker, victim, new ProcFlagsInit(ProcFlags.Kill), new ProcFlagsInit(ProcFlags.Heartbeat), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);

			foreach (var tapper in tappers)
				if (tapper.IsAtGroupRewardDistance(victim))
					ProcSkillsAndAuras(tapper, victim, new ProcFlagsInit(ProcFlags.None, ProcFlags2.TargetDies), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
		}

		// Proc auras on death - must be before aura/combat remove
		ProcSkillsAndAuras(victim, victim, new ProcFlagsInit(ProcFlags.None), new ProcFlagsInit(ProcFlags.Death), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);

		// update get killing blow achievements, must be done before setDeathState to be able to require auras on target
		// and before Spirit of Redemption as it also removes auras
		if (attacker != null)
		{
			var killerPlayer = attacker.CharmerOrOwnerPlayerOrPlayerItself;

			if (killerPlayer != null)
				killerPlayer.UpdateCriteria(CriteriaType.DeliveredKillingBlow, 1, 0, 0, victim);
		}

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

			if (pet != null && pet.IsAlive && pet.IsControlled)
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
			if ((durabilityLoss && player == null && !victim.AsPlayer.InBattleground) || (player != null && WorldConfig.GetBoolValue(WorldCfg.DurabilityLossInPvp)))
			{
				double baseLoss = WorldConfig.GetFloatValue(WorldCfg.RateDurabilityLossOnDeath);
				var loss = (uint)(baseLoss - (baseLoss * plrVictim.GetTotalAuraMultiplier(AuraType.ModDurabilityLoss)));
				Log.Logger.Debug("We are dead, losing {0} percent durability", loss);
				// Durability loss is calculated more accurately again for each item in Player.DurabilityLoss
				plrVictim.DurabilityLossAll(baseLoss, false);
				// durability lost message
				plrVictim.SendDurabilityLoss(plrVictim, loss);
			}

			// Call KilledUnit for creatures
			if (attacker != null && attacker.IsCreature && attacker.IsAIEnabled)
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

				if (creature.CanHaveLoot && LootStorage.Skinning.HaveLootFor(creature.Template.SkinLootId))
				{
					creature.SetDynamicFlag(UnitDynFlags.CanSkin);
					creature.SetUnitFlag(UnitFlags.Skinnable);
				}
			}

			// Call KilledUnit for creatures, this needs to be called after the lootable flag is set
			if (attacker != null && attacker.IsCreature && attacker.IsAIEnabled)
				attacker.AsCreature.AI.KilledUnit(victim);

			// Call creature just died function
			var ai = creature.AI;

			if (ai != null)
				ai.JustDied(attacker);

			var summon = creature.ToTempSummon();

			if (summon != null)
			{
				var summoner = summon.GetSummoner();

				if (summoner != null)
				{
					if (summoner.IsCreature)
						summoner.AsCreature.AI?.SummonedCreatureDies(creature, attacker);
					else if (summoner.IsGameObject)
						summoner.AsGameObject.AI?.SummonedCreatureDies(creature, attacker);
				}
			}
		}

		// outdoor pvp things, do these after setting the death state, else the player activity notify won't work... doh...
		// handle player kill only if not suicide (spirit of redemption for example)
		if (player != null && attacker != victim)
		{
			var pvp = player.GetOutdoorPvP();

			if (pvp != null)
				pvp.HandleKill(player, victim);

			var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.Map, player.Zone);

			if (bf != null)
				bf.HandleKill(player, victim);
		}

		// Battlegroundthings (do this at the end, so the death state flag will be properly set to handle in the bg.handlekill)
		if (player != null && player.InBattleground)
		{
			var bg = player.Battleground;

			if (bg)
			{
				var playerVictim = victim.AsPlayer;

				if (playerVictim)
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
					Global.ScriptMgr.ForEach<IPlayerOnPVPKill>(p => p.OnPVPKill(killerPlr, killedPlr));
				}
				else
				{
					var killedCre = victim.AsCreature;

					if (killedCre != null)
						Global.ScriptMgr.ForEach<IPlayerOnCreatureKill>(p => p.OnCreatureKill(killerPlr, killedCre));
				}
			}
			else
			{
				var killerCre = attacker.AsCreature;

				if (killerCre != null)
				{
					var killed = victim.AsPlayer;

					if (killed != null)
						Global.ScriptMgr.ForEach<IPlayerOnPlayerKilledByCreature>(p => p.OnPlayerKilledByCreature(killerCre, killed));
				}
			}
		}
	}

	public void KillSelf(bool durabilityLoss = true, bool skipSettingDeathState = false)
	{
		Kill(this, this, durabilityLoss, skipSettingDeathState);
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

	public double GetWeaponDamageRange(WeaponAttackType attType, WeaponDamageRange type)
	{
		if (attType == WeaponAttackType.OffAttack && !HaveOffhandWeapon())
			return 0.0f;

		return WeaponDamage[(int)attType][(int)type];
	}

	public double GetAPMultiplier(WeaponAttackType attType, bool normalized)
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

	public bool IsWithinMeleeRange(Unit obj)
	{
		return IsWithinMeleeRangeAt(Location, obj);
	}

	public bool IsWithinMeleeRangeAt(Position pos, Unit obj)
	{
		if (!obj || !IsInMap(obj) || !InSamePhase(obj))
			return false;

		var dx = pos.X - obj.Location.X;
		var dy = pos.Y - obj.Location.Y;
		var dz = pos.Z - obj.Location.Z;
		var distsq = (dx * dx) + (dy * dy) + (dz * dz);

		var maxdist = GetMeleeRange(obj) + GetTotalAuraModifier(AuraType.ModAutoAttackRange);

		return distsq <= maxdist * maxdist;
	}

	public float GetMeleeRange(Unit target)
	{
		var range = CombatReach + target.CombatReach + 4.0f / 3.0f;

		return Math.Max(range, SharedConst.NominalMeleeRange);
	}

	public void SetBaseAttackTime(WeaponAttackType att, uint val)
	{
		_baseAttackSpeed[(int)att] = val;
		UpdateAttackTimeField(att);
	}

	public virtual bool CheckAttackFitToAuraRequirement(WeaponAttackType attackType, AuraEffect aurEff)
	{
		return true;
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

	/// <summary>
	///  enables / disables combat interaction of this unit
	/// </summary>
	public void SetIsCombatDisallowed(bool apply)
	{
		_isCombatDisallowed = apply;
	}

	void _addAttacker(Unit pAttacker)
	{
		AttackerList.Add(pAttacker);
	}

	void _removeAttacker(Unit pAttacker)
	{
		AttackerList.Remove(pAttacker);
	}

	// TODO for melee need create structure as in
	void CalculateMeleeDamage(Unit victim, out CalcDamageInfo damageInfo, WeaponAttackType attackType)
	{
		damageInfo = new CalcDamageInfo();

		damageInfo.Attacker = this;
		damageInfo.Target = victim;

		damageInfo.DamageSchoolMask = (uint)SpellSchoolMask.Normal;
		damageInfo.Damage = 0;
		damageInfo.OriginalDamage = 0;
		damageInfo.Absorb = 0;
		damageInfo.Resist = 0;

		damageInfo.Blocked = 0;
		damageInfo.HitInfo = 0;
		damageInfo.TargetState = 0;

		damageInfo.AttackType = attackType;
		damageInfo.ProcAttacker = new ProcFlagsInit();
		damageInfo.ProcVictim = new ProcFlagsInit();
		damageInfo.CleanDamage = 0;
		damageInfo.HitOutCome = MeleeHitOutcome.Evade;

		if (victim == null)
			return;

		if (!IsAlive || !victim.IsAlive)
			return;

		// Select HitInfo/procAttacker/procVictim flag based on attack type
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
		ScaleDamage(a, t, ref damage);

		Global.ScriptMgr.ForEach<IUnitModifyMeleeDamage>(p => p.ModifyMeleeDamage(t, a, ref damage));

		// Calculate armor reduction
		if (IsDamageReducedByArmor((SpellSchoolMask)damageInfo.DamageSchoolMask))
		{
			damageInfo.Damage = CalcArmorReducedDamage(damageInfo.Attacker, damageInfo.Target, damage, null, damageInfo.AttackType);
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

			default:
				break;
		}

		// Always apply HITINFO_AFFECTS_VICTIM in case its not a miss
		if (!damageInfo.HitInfo.HasAnyFlag(HitInfo.Miss))
			damageInfo.HitInfo |= HitInfo.AffectsVictim;

		var resilienceReduction = damageInfo.Damage;

		if (CanApplyResilience())
			ApplyResilience(victim, ref resilienceReduction);

		resilienceReduction = damageInfo.Damage - resilienceReduction;
		damageInfo.Damage -= resilienceReduction;
		damageInfo.CleanDamage += resilienceReduction;

		// Calculate absorb resist
		if (damageInfo.Damage > 0)
		{
			damageInfo.ProcVictim.Or(ProcFlags.TakeAnyDamage);
			// Calculate absorb & resists
			DamageInfo dmgInfo = new(damageInfo);
			CalcAbsorbResist(dmgInfo);
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

	MeleeHitOutcome RollMeleeOutcomeAgainst(Unit victim, WeaponAttackType attType)
	{
		if (victim.IsTypeId(TypeId.Unit) && victim.AsCreature.IsEvadingAttacks)
			return MeleeHitOutcome.Evade;

		// Miss chance based on melee
		var miss_chance = (int)(MeleeSpellMissChance(victim, attType, null) * 100.0f);

		// Critical hit chance
		var crit_chance = (int)((GetUnitCriticalChanceAgainst(attType, victim) + GetTotalAuraModifier(AuraType.ModAutoAttackCritChance)) * 100.0f);

		var dodge_chance = (int)(GetUnitDodgeChance(attType, victim) * 100.0f);
		var block_chance = (int)(GetUnitBlockChance(attType, victim) * 100.0f);
		var parry_chance = (int)(GetUnitParryChance(attType, victim) * 100.0f);

		// melee attack table implementation
		// outcome priority:
		//   1. >    2. >    3. >       4. >    5. >   6. >       7. >  8.
		// MISS > DODGE > PARRY > GLANCING > BLOCK > CRIT > CRUSHING > HIT

		var sum = 0;
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
		var tmp = miss_chance;

		if (tmp > 0 && roll < (sum += tmp))
			return MeleeHitOutcome.Miss;

		// always crit against a sitting target (except 0 crit chance)
		if (victim.IsTypeId(TypeId.Player) && crit_chance > 0 && !victim.IsStandState)
			return MeleeHitOutcome.Crit;

		// 2. DODGE
		if (canDodge)
		{
			tmp = dodge_chance;

			if (tmp > 0 // check if unit _can_ dodge
				&&
				roll < (sum += tmp))
				return MeleeHitOutcome.Dodge;
		}

		// 3. PARRY
		if (canParryOrBlock)
		{
			tmp = parry_chance;

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
			tmp = block_chance;

			if (tmp > 0 // check if unit _can_ block
				&&
				roll < (sum += tmp))
				return MeleeHitOutcome.Block;
		}

		// 6.CRIT
		tmp = crit_chance;

		if (tmp > 0 && roll < (sum += tmp))
			return MeleeHitOutcome.Crit;

		// 7. CRUSHING
		// mobs can score crushing blows if they're 4 or more levels above victim
		if (attackerLevel >= victimLevel + 4 &&
			// can be from by creature (if can) or from controlled player that considered as creature
			!IsControlledByPlayer &&
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

	void UpdateAttackTimeField(WeaponAttackType att)
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
			default:
				break;

				;
		}
	}
}