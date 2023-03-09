// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Pets
{
	namespace DeathKnight
	{
		internal struct SpellIds
		{
			public const uint SummonGargoyle1 = 49206;
			public const uint SummonGargoyle2 = 50514;
			public const uint DismissGargoyle = 50515;
			public const uint Sanctuary = 54661;
		}

		[Script]
		internal class npc_pet_dk_ebon_gargoyle : CasterAI
		{
			public npc_pet_dk_ebon_gargoyle(Creature creature) : base(creature) { }

			public override void InitializeAI()
			{
				base.InitializeAI();
				var ownerGuid = Me.OwnerGUID;

				if (ownerGuid.IsEmpty)
					return;

				// Find victim of Summon Gargoyle spell
				List<Unit> targets = new();
				var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(Me, Me, 30.0f, target => target.HasAura(SpellIds.SummonGargoyle1, ownerGuid));
				var searcher = new UnitListSearcher(Me, targets, u_check, GridType.All);
				Cell.VisitGrid(Me, searcher, 30.0f);

				foreach (var target in targets)
				{
					Me.Attack(target, false);

					break;
				}
			}

			public override void JustDied(Unit killer)
			{
				// Stop Feeding Gargoyle when it dies
				var owner = Me.OwnerUnit;

				if (owner)
					owner.RemoveAura(SpellIds.SummonGargoyle2);
			}

			// Fly away when dismissed
			public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
			{
				if (spellInfo.Id != SpellIds.DismissGargoyle ||
					!Me.IsAlive)
					return;

				var owner = Me.OwnerUnit;

				if (!owner ||
					owner != caster)
					return;

				// Stop Fighting
				Me.SetUnitFlag(UnitFlags.NonAttackable);

				// Sanctuary
				Me.CastSpell(Me, SpellIds.Sanctuary, true);
				Me.ReactState = ReactStates.Passive;

				//! HACK: Creature's can't have MOVEMENTFLAG_FLYING
				// Fly Away
				Me.SetCanFly(true);
				Me.SetSpeedRate(UnitMoveType.Flight, 0.75f);
				Me.SetSpeedRate(UnitMoveType.Run, 0.75f);
				var x = Me.Location.X + 20 * (float)Math.Cos(Me.Location.Orientation);
				var y = Me.Location.Y + 20 * (float)Math.Sin(Me.Location.Orientation);
				var z = Me.Location.Z + 40;
				Me.MotionMaster.Clear();
				Me.MotionMaster.MovePoint(0, x, y, z);

				// Despawn as soon as possible
				Me.DespawnOrUnsummon(TimeSpan.FromSeconds(4));
			}
		}

		[Script]
		internal class npc_pet_dk_guardian : AggressorAI
		{
			public npc_pet_dk_guardian(Creature creature) : base(creature) { }

			public override bool CanAIAttack(Unit target)
			{
				if (!target)
					return false;

				var owner = Me.OwnerUnit;

				if (owner && !target.IsInCombatWith(owner))
					return false;

				return base.CanAIAttack(target);
			}
		}
	}
}