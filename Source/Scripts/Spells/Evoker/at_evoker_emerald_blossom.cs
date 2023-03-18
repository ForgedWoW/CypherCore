// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

//AT ID : 23318
//Spell ID : 355913
[AreaTriggerScript(EvokerAreaTriggers.GREEN_EMERALD_BLOSSOM)]
public class at_evoker_emerald_blossom : AreaTriggerScript, IAreaTriggerOnRemove
{
	public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(At.Location, EvokerSpells.GREEN_EMERALD_BLOSSOM_HEAL, true);
	}
}