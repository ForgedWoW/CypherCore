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
		[CreatureScript(17252)]
		public class npc_warl_felguard : SmartAI
		{
			public npc_warl_felguard(Creature creature) : base(creature)
			{
				if (!me.TryGetOwner(out Player owner))
					return;

				creature.SetLevel(owner.Level);
				creature.UpdateLevelDependantStats();
				creature.ReactState = ReactStates.Assist;
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
				var owner = me.GetOwner();

				if (owner == null)
					return;

				me.SetMaxHealth(owner.GetMaxHealth());
				me.SetHealth(me.GetMaxHealth());
				me.SetControlled(true, UnitState.Root);
			}
		}
	}
}