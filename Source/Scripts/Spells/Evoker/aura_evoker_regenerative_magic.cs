// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_SOURCE_OF_MAGIC)]
public class aura_evoker_regenerative_magic : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo info)
	{
		var owner = Aura.OwnerAsUnit;

        if (!owner.TryGetAura(EvokerSpells.REGENERATIVE_MAGIC, out var rmAura) 
			|| !owner.HealthBelowPct(rmAura.GetEffect(1).Amount))
			return;

		owner.CastSpell(owner, EvokerSpells.REGENERATIVE_MAGIC_HEAL, info.HealInfo.Heal * (rmAura.GetEffect(0).Amount * 0.01));
	}
}