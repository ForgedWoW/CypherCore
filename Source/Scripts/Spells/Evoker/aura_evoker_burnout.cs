// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BURNOUT)]
public class aura_evoker_burnout : AuraScript, IAuraCheckProc, IAuraOnProc
{
	public bool CheckProc(ProcEventInfo info)
	{
		return info.ProcSpell.SpellInfo.Id == EvokerSpells.FIRE_BREATH_CHARGED;
	}

	public void OnProc(ProcEventInfo info)
	{
		Caster.AddAura(EvokerSpells.BURNOUT_AURA);
	}
}