// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS_OVERRIDE_SPELL)]
public class spell_evoker_stasis_override_cast : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
        if (!Caster.TryGetAura(EvokerSpells.STASIS, out var aura))
            return;

        List<HealInfo> heals = new List<HealInfo>();

        aura.ForEachAuraScript<IAuraScriptValues>(a =>
        {
            if (a.ScriptValues.TryGetValue("heals", out var healsObj))
                heals = (List<HealInfo>)healsObj;
        });

        if (heals.Count == 0)
            return;

        foreach (var heal in heals)
            Unit.DealHeal(heal);

        Caster.RemoveAura(EvokerSpells.STASIS_OVERRIDE_AURA);
    }
}