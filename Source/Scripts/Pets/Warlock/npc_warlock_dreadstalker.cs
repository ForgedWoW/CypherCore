// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
	namespace Warlock
	{
		// Dreadstalker - 98035
		[CreatureScript(98035)]
		public class npc_warlock_dreadstalker : PetAI
		{
			public bool firstTick = true;

			public npc_warlock_dreadstalker(Creature creature) : base(creature)
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
					StartAttackOnOwnersInCombatWith();
				}
			}

			public override void UpdateAI(uint UnnamedParameter)
			{
				if (firstTick)
				{
					var owner = me.GetOwner();

					if (!me.GetOwner() ||
						!me.GetOwner().ToPlayer())
						return;

					var target = owner.ToPlayer().GetSelectedUnit();

					if (target)
						me.CastSpell(target, WarlockSpells.DREADSTALKER_CHARGE, true);

					firstTick = false;
				}

				base.UpdateAI(UnnamedParameter);
			}
		}
	}
}