// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ESSENCE_BURST)]
public class aura_evoker_essence_burst : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo info)
	{
		if (TryGetCasterAsPlayer(out var player) 
			&& (info.ProcSpell.SpellInfo.Id == EvokerSpells.RED_LIVING_FLAME_DAMAGE
				|| info.ProcSpell.SpellInfo.Id == EvokerSpells.RED_LIVING_FLAME_HEAL)
			&& RandomHelper.randChance(Aura.GetEffect(0).Amount))
			player.AddAura(EvokerSpells.ESSENCE_BURST_AURA);
	}
}