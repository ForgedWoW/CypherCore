// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.EMERALD_BLOSSOM)]
public class spell_evoker_cycle_of_life : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.CYCLE_OF_LIFE))
		{
			player.AddAura(EvokerSpells.CYCLE_OF_LIFE_USE_COUNT);

			if (!player.TryGetAura(EvokerSpells.CYCLE_OF_LIFE_USE_COUNT, out var colAura))
				return;

			if (colAura.StackAmount < SpellManager.Instance.GetSpellInfo(EvokerSpells.CYCLE_OF_LIFE).GetEffect(1).BasePoints)
				return;

			player.CastSpell(TargetPosition, EvokerSpells.CYCLE_OF_LIFE_SUMMON, true);
            var aura = player.AddAura(EvokerSpells.CYCLE_OF_LIFE_AURA);
			aura.ForEachAuraScript<IAuraScriptValues>(a => a.ScriptValues["pos"] = TargetPosition);
        }
	}
}