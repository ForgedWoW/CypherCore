// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman;

//60561
[CreatureScript(60561)]
public class npc_earth_grab_totem : ScriptedAI
{
	public List<ObjectGuid> alreadyRooted = new();

	public npc_earth_grab_totem(Creature creature) : base(creature) { }

	public override void Reset()
	{
		var time = TimeSpan.FromSeconds(2);

		Me.Events.AddRepeatEventAtOffset(() =>
										{
											var unitList = new List<Unit>();
											Me.GetAttackableUnitListInRange(unitList, 10.0f);

											foreach (var target in unitList)
											{
												if (target.HasAura(TotemSpells.TOTEM_EARTH_GRAB_ROOT_EFFECT))
													continue;

												if (!alreadyRooted.Contains(target.GUID))
												{
													alreadyRooted.Add(target.GUID);
													Me.CastSpell(target, TotemSpells.TOTEM_EARTH_GRAB_ROOT_EFFECT, true);
												}
												else
												{
													Me.CastSpell(target, TotemSpells.TOTEM_EARTH_GRAB_SLOW_EFFECT, true);
												}
											}

											return time;
										},
										time);
	}
}