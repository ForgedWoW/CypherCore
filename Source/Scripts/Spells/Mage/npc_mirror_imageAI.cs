﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[CreatureScript(31216)]
public class npc_mirror_imageAI : CasterAI
{
	public npc_mirror_imageAI(Creature creature) : base(creature) { }

	public override void IsSummonedBy(WorldObject owner)
	{
		if (owner == null || !owner.IsPlayer)
			return;

		if (!Me.HasUnitState(UnitState.Follow))
		{
			Me.MotionMaster.Clear();
			Me.MotionMaster.MoveFollow(owner.AsUnit, SharedConst.PetFollowDist, Me.FollowAngle, MovementSlot.Active);
		}

		// me->SetMaxPower(me->GetPowerType(), owner->GetMaxPower(me->GetPowerType()));
		Me.SetFullPower(Me.DisplayPowerType);
		Me.SetMaxHealth(owner.AsUnit.MaxHealth);
		Me.SetHealth(owner.AsUnit.Health);
		Me.ReactState = ReactStates.Defensive;

		Me.CastSpell(owner, eSpells.INHERIT_MASTER_THREAT, true);

		// here mirror image casts on summoner spell (not present in client dbc) 49866
		// here should be auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcasted by mirror images (stats related?)

		for (uint attackType = 0; attackType < (int)WeaponAttackType.Max; ++attackType)
		{
			var attackTypeEnum = (WeaponAttackType)attackType;
			Me.SetBaseWeaponDamage(attackTypeEnum, WeaponDamageRange.MaxDamage, owner.AsUnit.GetWeaponDamageRange(attackTypeEnum, WeaponDamageRange.MaxDamage));
			Me.SetBaseWeaponDamage(attackTypeEnum, WeaponDamageRange.MinDamage, owner.AsUnit.GetWeaponDamageRange(attackTypeEnum, WeaponDamageRange.MinDamage));
		}

		Me.UpdateAttackPowerAndDamage();
	}

	public override void JustEngagedWith(Unit who)
	{
		var owner = Me.OwnerUnit;

		if (owner == null)
			return;

		var ownerPlayer = owner.AsPlayer;

		if (ownerPlayer == null)
			return;

		var spellId = eSpells.FROSTBOLT;

		switch (ownerPlayer.GetPrimarySpecialization())
		{
			case TalentSpecialization.MageArcane:
				spellId = eSpells.ARCANE_BLAST;

				break;
			case TalentSpecialization.MageFire:
				spellId = eSpells.FIREBALL;

				break;
			default:
				break;
		}

		Events.ScheduleEvent(spellId, TimeSpan.Zero); ///< Schedule cast

		Me. ///< Schedule cast
			MotionMaster.Clear();
	}

	public override void EnterEvadeMode(EvadeReason UnnamedParameter)
	{
		if (Me.IsInEvadeMode || !Me.IsAlive)
			return;

		var owner = Me.OwnerUnit;

		Me.CombatStop(true);

		if (owner != null && !Me.HasUnitState(UnitState.Follow))
		{
			Me.MotionMaster.Clear();
			Me.MotionMaster.MoveFollow(owner.AsUnit, SharedConst.PetFollowDist, Me.FollowAngle, MovementSlot.Active);
		}
	}

	public override void Reset()
	{
		var owner = Me.OwnerUnit;

		if (owner != null)
		{
			owner.CastSpell(Me, eSpells.INITIALIZE_IMAGES, true);
			owner.CastSpell(Me, eSpells.CLONE_CASTER, true);
		}
	}


	public override bool CanAIAttack(Unit target)
	{
		/// Am I supposed to attack this target? (ie. do not attack polymorphed target)
		return target != null && !target.HasBreakableByDamageCrowdControlAura();
	}

	public override void UpdateAI(uint diff)
	{
		Events.Update(diff);

		var l_Victim = Me.Victim;

		if (l_Victim != null)
		{
			if (CanAIAttack(l_Victim))
			{
				/// If not already casting, cast! ("I'm a cast machine")
				if (!Me.HasUnitState(UnitState.Casting))
				{
					var spellId = Events.ExecuteEvent();

					if (Events.ExecuteEvent() != 0)
					{
						DoCast(spellId);
						var castTime = Me.GetCurrentSpellCastTime(spellId);
						Events.ScheduleEvent(spellId, TimeSpan.FromSeconds(5), Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None).ProcCooldown);
					}
				}
			}
			else
			{
				/// My victim has changed state, I shouldn't attack it anymore
				if (Me.HasUnitState(UnitState.Casting))
					Me.CastStop();

				Me.AI.EnterEvadeMode();
			}
		}
		else
		{
			/// Let's choose a new target
			var target = Me.SelectVictim();

			if (target == null)
			{
				/// No target? Let's see if our owner has a better target for us
				var owner = Me.OwnerUnit;

				if (owner != null)
				{
					var ownerVictim = owner.Victim;

					if (ownerVictim != null && Me.CanCreatureAttack(ownerVictim))
						target = ownerVictim;
				}
			}

			if (target != null)
				Me.AI.AttackStart(target);
		}
	}

	public struct eSpells
	{
		public const uint FROSTBOLT = 59638;
		public const uint FIREBALL = 133;
		public const uint ARCANE_BLAST = 30451;
		public const uint GLYPH = 63093;
		public const uint INITIALIZE_IMAGES = 102284;
		public const uint CLONE_CASTER = 60352;
		public const uint INHERIT_MASTER_THREAT = 58838;
	}
}