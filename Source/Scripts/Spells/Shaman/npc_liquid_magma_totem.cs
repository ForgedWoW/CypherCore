﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman;

//NPC ID : 97369
[CreatureScript(97369)]
public class npc_liquid_magma_totem : ScriptedAI
{
	public npc_liquid_magma_totem(Creature creature) : base(creature) { }

	public override void Reset()
	{
		var time = TimeSpan.FromSeconds(15);

		Me.Events.AddRepeatEventAtOffset(() =>
										{
											Me.CastSpell(Me, TotemSpells.TOTEM_LIQUID_MAGMA_EFFECT, true);

											return time;
										},
										time);
	}
}