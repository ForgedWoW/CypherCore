// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
	namespace Warlock
	{
		// 107024 - Fel Lord
		[CreatureScript(107024)]
		public class npc_warl_fel_lord : SmartAI
		{
			public npc_warl_fel_lord(Creature creature) : base(creature)
			{
				if (!me.TryGetOwner(out Player owner))
					return;

				creature.SetLevel(owner.Level);
				creature.UpdateLevelDependantStats();
				creature.ReactState = ReactStates.Aggressive;
				creature.SetCreatorGUID(owner.GUID);

				var summon = creature.ToTempSummon();

				if (summon != null)
				{
					summon.SetCanFollowOwner(true);
					summon.MotionMaster.Clear();
					summon.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, summon.FollowAngle);
					StartAttackOnOwnersInCombatWith();
				}
			}

			public override void Reset()
			{
				var owner = me.OwnerUnit;

				if (owner == null)
					return;

				me.SetMaxHealth(owner.MaxHealth);
				me.SetHealth(me.MaxHealth);
				me.SetControlled(true, UnitState.Root);
			}

			//public override void UpdateAI(uint UnnamedParameter)
			//{
			//    if (me.HasUnitState(UnitState.Casting))
			//        return;

			//    me.CastSpell(me, WarlockSpells.FEL_LORD_CLEAVE, false);
			//}
		}
	}
}