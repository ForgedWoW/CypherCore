// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warrior;

[CreatureScript(119052)] //119052
public class npc_warr_banner : ScriptedAI
{
	private uint _timer;

	public npc_warr_banner(Creature creature) : base(creature)
	{
		Initialize();
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		base.IsSummonedBy(summoner);
		Me.ReactState = Framework.Constants.ReactStates.Passive;
	}

	public override void UpdateAI(uint diff)
	{
		if (_timer <= diff)
		{
			var owner = Me.OwnerUnit;

			if (owner != null)
			{
				Me.SetLevel(owner.Level);
				var allies = new List<Unit>();

				Me.GetFriendlyUnitListInRange(allies, 30.0f, true);

				foreach (var targets in allies)
					if (targets.IsFriendlyTo(owner) && targets.IsPlayer)
					{
						if (!targets.HasAura(WarriorSpells.WAR_BANNER_BUFF))
							targets.AddAura(WarriorSpells.WAR_BANNER_BUFF, targets);

						targets.Events.AddEventAtOffset(() =>
														{
															if (!targets)
																return;

															targets.RemoveAura(WarriorSpells.WAR_BANNER_BUFF);
														},
														TimeSpan.FromSeconds(15));

						_timer = 1000;
					}
			}
			else
			{
				_timer -= diff;
			}
		}
	}

	private void Initialize()
	{
		_timer = 0;
	}
}