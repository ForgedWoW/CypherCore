// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.EMERALD_BLOSSOM)]
public class at_evoker_fluttering_seedling : AreaTriggerScript, IAreaTriggerOnRemove
{
    public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

        if (!caster.TryGetAura(EvokerSpells.FLUTTERING_SEEDLINGS, out var fsAura))
            return;

        // get targets
        var targetList = new List<Unit>();
        caster.GetAlliesWithinRange(targetList, (float)fsAura.SpellInfo.GetEffect(1).BasePoints);

        // reduce targetList to the number allowed
        targetList.RandomResize(fsAura.GetEffect(0).Amount);

        // cast on targets
        foreach (var target in targetList)
            caster.CastSpell(target, EvokerSpells.FLUTTERING_SEEDLINGS_HEAL, true);
    }
}