// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Paladin;

// Light's Hammer
// NPC Id - 59738
[CreatureScript(59738)]
public class npc_pal_lights_hammer : ScriptedAI
{
	public npc_pal_lights_hammer(Creature creature) : base(creature) { }

	public override void Reset()
	{
		me.CastSpell(me, PaladinSpells.LightHammerCosmetic, true);
		me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.RemoveClientControl);
	}
}