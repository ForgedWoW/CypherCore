// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Evoker;

//AT ID : 23318
//Spell ID : 355913
[AreaTriggerScript(EvokerAreaTriggers.EMERALD_BLOSSOM)]
public class at_evoker_emerald_blossom : AreaTriggerAI
{
	public int timeInterval;

	public at_evoker_emerald_blossom(AreaTrigger areatrigger) : base(areatrigger) { }

    public override void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

        caster.CastSpell(caster, EvokerSpells.EMERALD_BLOSSOM_HEAL);
	}
}