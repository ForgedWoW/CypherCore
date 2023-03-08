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
				var ownerGuid = me.OwnerGUID;

				if (ownerGuid.IsEmpty)
					return;

				// Find victim of Summon Gargoyle spell
				List<Unit> targets = new();
				var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(me, me, 30.0f, target => target.HasAura(SpellIds.SummonGargoyle1, ownerGuid));
				var searcher = new UnitListSearcher(me, targets, u_check, GridType.All);
				Cell.VisitGrid(me, searcher, 30.0f);

				foreach (var target in targets)
				{
					me.Attack(target, false);

					break;
				}
			}

			public override void JustDied(Unit killer)
			{
				// Stop Feeding Gargoyle when it dies
				var owner = me.OwnerUnit;

				if (owner)
					owner.RemoveAura(SpellIds.SummonGargoyle2);
			}

			// Fly away when dismissed
			public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
			{
				if (spellInfo.Id != SpellIds.DismissGargoyle ||
					!me.IsAlive)
					return;

				var owner = me.OwnerUnit;

				if (!owner ||
					owner != caster)
					return;

				// Stop Fighting
				me.SetUnitFlag(UnitFlags.NonAttackable);

				// Sanctuary
				me.CastSpell(me, SpellIds.Sanctuary, true);
				me.ReactState = ReactStates.Passive;

				//! HACK: Creature's can't have MOVEMENTFLAG_FLYING
				// Fly Away
				me.SetCanFly(true);
				me.SetSpeedRate(UnitMoveType.Flight, 0.75f);
				me.SetSpeedRate(UnitMoveType.Run, 0.75f);
				var x = me.Location.X + 20 * (float)Math.Cos(me.Location.Orientation);
				var y = me.Location.Y + 20 * (float)Math.Sin(me.Location.Orientation);
				var z = me.Location.Z + 40;
				me.MotionMaster.Clear();
				me.MotionMaster.MovePoint(0, x, y, z);

				// Despawn as soon as possible
				me.DespawnOrUnsummon(TimeSpan.FromSeconds(4));
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

				var owner = me.OwnerUnit;

				if (owner && !target.IsInCombatWith(owner))
					return false;

				return base.CanAIAttack(target);
			}
		}
	}
}