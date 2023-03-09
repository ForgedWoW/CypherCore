// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
	namespace Warlock
	{
		[CreatureScript(185584)]
		public class npc_warlock_blasphemy : SmartAI
		{
			private static readonly TimeSpan _tickTime = TimeSpan.FromMilliseconds(500);
			private readonly Player _owner;

			public npc_warlock_blasphemy(Creature creature) : base(creature)
			{
				if (!Me.TryGetOwner(out Player owner))
					return;

				_owner = owner;
				creature.SetLevel(owner.Level);
				creature.UpdateLevelDependantStats();
				creature.ReactState = ReactStates.Assist;
				creature.SetCreatorGUID(owner.GUID);

				var summon = creature.ToTempSummon();

				if (summon != null)
				{
					StartAttackOnOwnersInCombatWith();

					if (owner.TryGetAura(WarlockSpells.AVATAR_OF_DESTRUCTION, out var avatar))
						summon.UnSummon(TimeSpan.FromSeconds(avatar.GetEffect(0).Amount));
				}

				creature.Events.AddRepeatEventAtOffset(() =>
														{
															_owner.ModifyPower(PowerType.SoulShards, 1);

															return _tickTime;
														},
														_tickTime);
			}


			public override void UpdateAI(uint UnnamedParameter)
			{
				if (!Me.HasAura(WarlockSpells.IMMOLATION))
					DoCast(WarlockSpells.IMMOLATION);


				//DoMeleeAttackIfReady();
				base.UpdateAI(UnnamedParameter);
			}
		}
	}
}