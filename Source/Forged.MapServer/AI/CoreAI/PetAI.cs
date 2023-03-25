// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Movement;
using Game.Spells;

namespace Game.AI;

public class PetAI : CreatureAI
{
	readonly List<ObjectGuid> _allySet = new();
	uint _updateAlliesTimer;

	public PetAI(Creature creature) : base(creature)
	{
		UpdateAllies();
	}

	public override void UpdateAI(uint diff)
	{
		if (!Me.IsAlive || Me.GetCharmInfo() == null)
			return;

		var owner = Me.CharmerOrOwner;

		if (_updateAlliesTimer <= diff)
			// UpdateAllies self set update timer
			UpdateAllies();
		else
			_updateAlliesTimer -= diff;

		if (Me.Victim && Me.Victim.IsAlive)
		{
			// is only necessary to stop casting, the pet must not exit combat
			if (!Me.GetCurrentSpell(CurrentSpellTypes.Channeled) && // ignore channeled spells (Pin, Seduction)
				(Me.Victim && Me.Victim.HasBreakableByDamageCrowdControlAura(Me)))
			{
				Me.InterruptNonMeleeSpells(false);

				return;
			}

			if (NeedToStop())
			{
				Log.Logger.Verbose($"PetAI::UpdateAI: AI stopped attacking {Me.GUID}");
				StopAttack();

				return;
			}

			// Check before attacking to prevent pets from leaving stay position
			if (Me.GetCharmInfo().HasCommandState(CommandStates.Stay))
			{
				if (Me.GetCharmInfo().IsCommandAttack() || (Me.GetCharmInfo().IsAtStay() && Me.IsWithinMeleeRange(Me.Victim)))
					DoMeleeAttackIfReady();
			}
			else
			{
				DoMeleeAttackIfReady();
			}
		}
		else
		{
			if (Me.HasReactState(ReactStates.Aggressive) || Me.GetCharmInfo().IsAtStay())
			{
				// Every update we need to check targets only in certain cases
				// Aggressive - Allow auto select if owner or pet don't have a target
				// Stay - Only pick from pet or owner targets / attackers so targets won't run by
				//   while chasing our owner. Don't do auto select.
				// All other cases (ie: defensive) - Targets are assigned by DamageTaken(), OwnerAttackedBy(), OwnerAttacked(), etc.
				var nextTarget = SelectNextTarget(Me.HasReactState(ReactStates.Aggressive));

				if (nextTarget)
					AttackStart(nextTarget);
				else
					HandleReturnMovement();
			}
			else
			{
				HandleReturnMovement();
			}
		}

		// Autocast (casted only in combat or persistent spells in any state)
		if (!Me.HasUnitState(UnitState.Casting))
		{
			List<Tuple<Unit, Spell>> targetSpellStore = new();

			for (byte i = 0; i < Me.PetAutoSpellSize; ++i)
			{
				var spellID = Me.GetPetAutoSpellOnPos(i);

				if (spellID == 0)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo(spellID, Me.Map.DifficultyID);

				if (spellInfo == null)
					continue;

				if (Me.SpellHistory.HasGlobalCooldown(spellInfo))
					continue;

				// check spell cooldown
				if (!Me.SpellHistory.IsReady(spellInfo))
					continue;

				if (spellInfo.IsPositive)
				{
					if (spellInfo.CanBeUsedInCombat)
						// Check if we're in combat or commanded to attack
						if (!Me.IsInCombat && !Me.GetCharmInfo().IsCommandAttack())
							continue;

					Spell spell = new(Me, spellInfo, TriggerCastFlags.None);
					var spellUsed = false;

					// Some spells can target enemy or friendly (DK Ghoul's Leap)
					// Check for enemy first (pet then owner)
					var target = Me.GetAttackerForHelper();

					if (!target && owner)
						target = owner.GetAttackerForHelper();

					if (target)
						if (CanAttack(target) && spell.CanAutoCast(target))
						{
							targetSpellStore.Add(Tuple.Create(target, spell));
							spellUsed = true;
						}

					if (spellInfo.HasEffect(SpellEffectName.JumpDest))
					{
						if (!spellUsed)
							spell.Dispose();

						continue; // Pets must only jump to target
					}

					// No enemy, check friendly
					if (!spellUsed)
						foreach (var tar in _allySet)
						{
							var ally = Global.ObjAccessor.GetUnit(Me, tar);

							//only buff targets that are in combat, unless the spell can only be cast while out of combat
							if (!ally)
								continue;

							if (spell.CanAutoCast(ally))
							{
								targetSpellStore.Add(Tuple.Create(ally, spell));
								spellUsed = true;

								break;
							}
						}

					// No valid targets at all
					if (!spellUsed)
						spell.Dispose();
				}
				else if (Me.Victim && CanAttack(Me.Victim) && spellInfo.CanBeUsedInCombat)
				{
					Spell spell = new(Me, spellInfo, TriggerCastFlags.None);

					if (spell.CanAutoCast(Me.Victim))
						targetSpellStore.Add(Tuple.Create(Me.Victim, spell));
					else
						spell.Dispose();
				}
			}

			//found units to cast on to
			if (!targetSpellStore.Empty())
			{
				var index = RandomHelper.IRand(0, targetSpellStore.Count - 1);
				var tss = targetSpellStore[index];

				var (target, spell) = tss;

				targetSpellStore.RemoveAt(index);

				SpellCastTargets targets = new();
				targets.UnitTarget = target;

				spell.Prepare(targets);
			}

			// deleted cached Spell objects
			foreach (var pair in targetSpellStore)
				pair.Item2.Dispose();
		}

		// Update speed as needed to prevent dropping too far behind and despawning
		Me.UpdateSpeed(UnitMoveType.Run);
		Me.UpdateSpeed(UnitMoveType.Walk);
		Me.UpdateSpeed(UnitMoveType.Flight);
	}

	public override void KilledUnit(Unit victim)
	{
		// Called from Unit.Kill() in case where pet or owner kills something
		// if owner killed this victim, pet may still be attacking something else
		if (Me.Victim && Me.Victim != victim)
			return;

		// Clear target just in case. May help problem where health / focus / mana
		// regen gets stuck. Also resets attack command.
		// Can't use StopAttack() because that activates movement handlers and ignores
		// next target selection
		Me.AttackStop();
		Me.InterruptNonMeleeSpells(false);

		// Before returning to owner, see if there are more things to attack
		var nextTarget = SelectNextTarget(false);

		if (nextTarget)
			AttackStart(nextTarget);
		else
			HandleReturnMovement(); // Return
	}

	public override void AttackStart(Unit target)
	{
		// Overrides Unit.AttackStart to prevent pet from switching off its assigned target
		if (target == null || target == Me)
			return;

		if (Me.Victim != null && Me.Victim.IsAlive)
			return;

		_AttackStart(target);
	}

	public void _AttackStart(Unit target)
	{
		// Check all pet states to decide if we can attack this target
		if (!CanAttack(target))
			return;

		// Only chase if not commanded to stay or if stay but commanded to attack
		DoAttack(target, (!Me.GetCharmInfo().HasCommandState(CommandStates.Stay) || Me.GetCharmInfo().IsCommandAttack()));
	}

	public override void OwnerAttackedBy(Unit attacker)
	{
		// Called when owner takes damage. This function helps keep pets from running off
		//  simply due to owner gaining aggro.

		if (attacker == null || !Me.IsAlive)
			return;

		// Passive pets don't do anything
		if (Me.HasReactState(ReactStates.Passive))
			return;

		// Prevent pet from disengaging from current target
		if (Me.Victim && Me.Victim.IsAlive)
			return;

		// Continue to evaluate and attack if necessary
		AttackStart(attacker);
	}

	public override void OwnerAttacked(Unit target)
	{
		// Called when owner attacks something. Allows defensive pets to know
		//  that they need to assist

		// Target might be null if called from spell with invalid cast targets
		if (target == null || !Me.IsAlive)
			return;

		// Passive pets don't do anything
		if (Me.HasReactState(ReactStates.Passive))
			return;

		// Prevent pet from disengaging from current target
		if (Me.Victim && Me.Victim.IsAlive)
			return;

		// Continue to evaluate and attack if necessary
		AttackStart(target);
	}

	public override void MovementInform(MovementGeneratorType type, uint id)
	{
		// Receives notification when pet reaches stay or follow owner
		switch (type)
		{
			case MovementGeneratorType.Point:
			{
				// Pet is returning to where stay was clicked. data should be
				// pet's GUIDLow since we set that as the waypoint ID
				if (id == Me.GUID.Counter && Me.GetCharmInfo().IsReturning())
				{
					ClearCharmInfoFlags();
					Me.GetCharmInfo().SetIsAtStay(true);
					Me.MotionMaster.MoveIdle();
				}

				break;
			}
			case MovementGeneratorType.Follow:
			{
				// If data is owner's GUIDLow then we've reached follow point,
				// otherwise we're probably chasing a creature
				if (Me.CharmerOrOwner && Me.GetCharmInfo() != null && id == Me.CharmerOrOwner.GUID.Counter && Me.GetCharmInfo().IsReturning())
				{
					ClearCharmInfoFlags();
					Me.GetCharmInfo().SetIsFollowing(true);
				}

				break;
			}
			default:
				break;
		}
	}

	public void StartAttackOnOwnersInCombatWith()
	{
		if (!Me.TryGetOwner(out Player owner))
			return;

		var summon = Me.ToTempSummon();

		if (summon != null)
		{
			var attack = owner.SelectedUnit;

			if (attack == null)
				attack = owner.Attackers.FirstOrDefault();

			if (attack != null)
				summon.Attack(attack, true);
		}
	}

	public bool CanAttack(Unit victim)
	{
		// Evaluates wether a pet can attack a specific target based on CommandState, ReactState and other flags
		// IMPORTANT: The order in which things are checked is important, be careful if you add or remove checks

		// Hmmm...
		if (!victim)
			return false;

		if (!victim.IsAlive)
			// if target is invalid, pet should evade automaticly
			// Clear target to prevent getting stuck on dead targets
			//me.AttackStop();
			//me.InterruptNonMeleeSpells(false);
			return false;

		if (Me.GetCharmInfo() == null)
		{
			Log.Logger.Warning($"me.GetCharmInfo() is NULL in PetAI::CanAttack(). Debug info: {GetDebugInfo()}");

			return false;
		}

		// Passive - passive pets can attack if told to
		if (Me.HasReactState(ReactStates.Passive))
			return Me.GetCharmInfo().IsCommandAttack();

		// CC - mobs under crowd control can be attacked if owner commanded
		if (victim.HasBreakableByDamageCrowdControlAura())
			return Me.GetCharmInfo().IsCommandAttack();

		// Returning - pets ignore attacks only if owner clicked follow
		if (Me.GetCharmInfo().IsReturning())
			return !Me.GetCharmInfo().IsCommandFollow();

		// Stay - can attack if target is within range or commanded to
		if (Me.GetCharmInfo().HasCommandState(CommandStates.Stay))
			return (Me.IsWithinMeleeRange(victim) || Me.GetCharmInfo().IsCommandAttack());

		//  Pets attacking something (or chasing) should only switch targets if owner tells them to
		if (Me.Victim && Me.Victim != victim)
		{
			// Check if our owner selected this target and clicked "attack"
			Unit ownerTarget;
			var owner = Me.CharmerOrOwner.AsPlayer;

			if (owner)
				ownerTarget = owner.SelectedUnit;
			else
				ownerTarget = Me.CharmerOrOwner.Victim;

			if (ownerTarget && Me.GetCharmInfo().IsCommandAttack())
				return (victim.GUID == ownerTarget.GUID);
		}

		// Follow
		if (Me.GetCharmInfo().HasCommandState(CommandStates.Follow))
			return !Me.GetCharmInfo().IsReturning();

		// default, though we shouldn't ever get here
		return false;
	}

	public override void ReceiveEmote(Player player, TextEmotes emoteId)
	{
		if (Me.OwnerGUID != player.GUID)
			return;

		switch (emoteId)
		{
			case TextEmotes.Cower:
				if (Me.IsPet && Me.AsPet.IsPetGhoul())
					Me.HandleEmoteCommand(Emote.OneshotOmnicastGhoul);

				break;
			case TextEmotes.Angry:
				if (Me.IsPet && Me.AsPet.IsPetGhoul())
					Me.HandleEmoteCommand(Emote.StateStun);

				break;
			case TextEmotes.Glare:
				if (Me.IsPet && Me.AsPet.IsPetGhoul())
					Me.HandleEmoteCommand(Emote.StateStun);

				break;
			case TextEmotes.Soothe:
				if (Me.IsPet && Me.AsPet.IsPetGhoul())
					Me.HandleEmoteCommand(Emote.OneshotOmnicastGhoul);

				break;
		}
	}

	public override void OnCharmed(bool isNew)
	{
		if (!Me.IsPossessedByPlayer && Me.IsCharmed)
			Me.MotionMaster.MoveFollow(Me.Charmer, SharedConst.PetFollowDist, Me.FollowAngle);

		base.OnCharmed(isNew);
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		AttackStart(attacker);
	}

	public override void JustEnteredCombat(Unit who)
	{
		EngagementStart(who);
	}

	public override void JustExitedCombat()
	{
		EngagementOver();
	}

	// The following aren't used by the PetAI but need to be defined to override
	//  default CreatureAI functions which interfere with the PetAI
	public override void MoveInLineOfSight(Unit who) { }
	public override void MoveInLineOfSight_Safe(Unit who) { }
	public override void JustAppeared() { } // we will control following manually
	public override void EnterEvadeMode(EvadeReason why) { }

	Unit SelectNextTarget(bool allowAutoSelect)
	{
		// Provides next target selection after current target death.
		// This function should only be called internally by the AI
		// Targets are not evaluated here for being valid targets, that is done in _CanAttack()
		// The parameter: allowAutoSelect lets us disable aggressive pet auto targeting for certain situations

		// Passive pets don't do next target selection
		if (Me.HasReactState(ReactStates.Passive))
			return null;

		// Check pet attackers first so we don't drag a bunch of targets to the owner
		var myAttacker = Me.GetAttackerForHelper();

		if (myAttacker)
			if (!myAttacker.HasBreakableByDamageCrowdControlAura())
				return myAttacker;

		// Not sure why we wouldn't have an owner but just in case...
		if (!Me.CharmerOrOwner)
			return null;

		// Check owner attackers
		var ownerAttacker = Me.CharmerOrOwner.GetAttackerForHelper();

		if (ownerAttacker)
			if (!ownerAttacker.HasBreakableByDamageCrowdControlAura())
				return ownerAttacker;

		// Check owner victim
		// 3.0.2 - Pets now start attacking their owners victim in defensive mode as soon as the hunter does
		var ownerVictim = Me.CharmerOrOwner.Victim;

		if (ownerVictim)
			return ownerVictim;

		// Neither pet or owner had a target and aggressive pets can pick any target
		// To prevent aggressive pets from chain selecting targets and running off, we
		//  only select a random target if certain conditions are met.
		if (Me.HasReactState(ReactStates.Aggressive) && allowAutoSelect)
			if (!Me.GetCharmInfo().IsReturning() || Me.GetCharmInfo().IsFollowing() || Me.GetCharmInfo().IsAtStay())
			{
				var nearTarget = Me.SelectNearestHostileUnitInAggroRange(true, true);

				if (nearTarget)
					return nearTarget;
			}

		// Default - no valid targets
		return null;
	}

	void HandleReturnMovement()
	{
		// Handles moving the pet back to stay or owner

		// Prevent activating movement when under control of spells
		// such as "Eyes of the Beast"
		if (Me.IsCharmed)
			return;

		if (Me.GetCharmInfo() == null)
		{
			Log.Logger.Warning($"me.GetCharmInfo() is NULL in PetAI::HandleReturnMovement(). Debug info: {GetDebugInfo()}");

			return;
		}

		if (Me.GetCharmInfo().HasCommandState(CommandStates.Stay))
		{
			if (!Me.GetCharmInfo().IsAtStay() && !Me.GetCharmInfo().IsReturning())
			{
				// Return to previous position where stay was clicked

				Me.GetCharmInfo().GetStayPosition(out var x, out var y, out var z);
				ClearCharmInfoFlags();
				Me.GetCharmInfo().SetIsReturning(true);

				if (Me.HasUnitState(UnitState.Chase))
					Me.MotionMaster.Remove(MovementGeneratorType.Chase);

				Me.MotionMaster.MovePoint((uint)Me.GUID.Counter, x, y, z);
			}
		}
		else // COMMAND_FOLLOW
		{
			if (!Me.GetCharmInfo().IsFollowing() && !Me.GetCharmInfo().IsReturning())
			{
				ClearCharmInfoFlags();
				Me.GetCharmInfo().SetIsReturning(true);

				if (Me.HasUnitState(UnitState.Chase))
					Me.MotionMaster.Remove(MovementGeneratorType.Chase);

				Me.MotionMaster.MoveFollow(Me.CharmerOrOwner, SharedConst.PetFollowDist, Me.FollowAngle);
			}
		}

		Me.RemoveUnitFlag(UnitFlags.PetInCombat); // on player pets, this flag indicates that we're actively going after a target - we're returning, so remove it
	}

	void DoAttack(Unit target, bool chase)
	{
		// Handles attack with or without chase and also resets flags
		// for next update / creature kill

		if (Me.Attack(target, true))
		{
			Me.SetUnitFlag(UnitFlags.PetInCombat); // on player pets, this flag indicates we're actively going after a target - that's what we're doing, so set it

			// Play sound to let the player know the pet is attacking something it picked on its own
			if (Me.HasReactState(ReactStates.Aggressive) && !Me.GetCharmInfo().IsCommandAttack())
				Me.SendPetAIReaction(Me.GUID);

			if (chase)
			{
				var oldCmdAttack = Me.GetCharmInfo().IsCommandAttack(); // This needs to be reset after other flags are cleared
				ClearCharmInfoFlags();
				Me.GetCharmInfo().SetIsCommandAttack(oldCmdAttack); // For passive pets commanded to attack so they will use spells

				if (Me.HasUnitState(UnitState.Follow))
					Me.MotionMaster.Remove(MovementGeneratorType.Follow);

				// Pets with ranged attacks should not care about the chase angle at all.
				var chaseDistance = Me.GetPetChaseDistance();
				var angle = chaseDistance == 0.0f ? MathF.PI : 0.0f;
				var tolerance = chaseDistance == 0.0f ? MathFunctions.PiOver4 : (MathF.PI * 2);
				Me.MotionMaster.MoveChase(target, new ChaseRange(0.0f, chaseDistance), new ChaseAngle(angle, tolerance));
			}
			else
			{
				ClearCharmInfoFlags();
				Me.GetCharmInfo().SetIsAtStay(true);

				if (Me.HasUnitState(UnitState.Follow))
					Me.MotionMaster.Remove(MovementGeneratorType.Follow);

				Me.MotionMaster.MoveIdle();
			}
		}
	}

	bool NeedToStop()
	{
		// This is needed for charmed creatures, as once their target was reset other effects can trigger threat
		if (Me.IsCharmed && Me.Victim == Me.Charmer)
			return true;

		// dont allow pets to follow targets far away from owner
		var owner = Me.CharmerOrOwner;

		if (owner)
			if (owner.Location.GetExactDist(Me.Location) >= (owner.VisibilityRange - 10.0f))
				return true;

		return !Me.IsValidAttackTarget(Me.Victim);
	}

	void StopAttack()
	{
		if (!Me.IsAlive)
		{
			Me.MotionMaster.Clear();
			Me.MotionMaster.MoveIdle();
			Me.CombatStop();

			return;
		}

		Me.AttackStop();
		Me.InterruptNonMeleeSpells(false);
		Me.GetCharmInfo().SetIsCommandAttack(false);
		ClearCharmInfoFlags();
		HandleReturnMovement();
	}

	void UpdateAllies()
	{
		_updateAlliesTimer = 10 * Time.InMilliseconds; // update friendly targets every 10 seconds, lesser checks increase performance

		var owner = Me.CharmerOrOwner;

		if (!owner)
			return;

		PlayerGroup group = null;
		var player = owner.AsPlayer;

		if (player)
			group = player.Group;

		// only pet and owner/not in group.ok
		if (_allySet.Count == 2 && !group)
			return;

		// owner is in group; group members filled in already (no raid . subgroupcount = whole count)
		if (group && !group.IsRaidGroup && _allySet.Count == (group.MembersCount + 2))
			return;

		_allySet.Clear();
		_allySet.Add(Me.GUID);

		if (group) // add group
			for (var refe = group.FirstMember; refe != null; refe = refe.Next())
			{
				var target = refe.Source;

				if (!target || !target.IsInMap(owner) || !group.SameSubGroup(owner.AsPlayer, target))
					continue;

				if (target.GUID == owner.GUID)
					continue;

				_allySet.Add(target.GUID);
			}
		else // remove group
			_allySet.Add(owner.GUID);
	}

	/// <summary>
	///  Quick access to set all flags to FALSE
	/// </summary>
	void ClearCharmInfoFlags()
	{
		var ci = Me.GetCharmInfo();

		if (ci != null)
		{
			ci.SetIsAtStay(false);
			ci.SetIsCommandAttack(false);
			ci.SetIsCommandFollow(false);
			ci.SetIsFollowing(false);
			ci.SetIsReturning(false);
		}
	}
}