// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CYCLE_OF_LIFE_AURA)]
public class aura_evoker_cycle_of_life : AuraScript, IAuraOnProc, IAuraOnApply
{
	double _multiplier = 0;

	public void AuraApplied()
	{
		_multiplier = SpellManager.Instance.GetSpellInfo(EvokerSpells.CYCLE_OF_LIFE).GetEffect(0).BasePoints * 0.01;
	}

	public void OnProc(ProcEventInfo info)
	{
		if (info.HealInfo == null)
			return;

		var eff = Aura.AuraEffects[0];

		eff.ChangeAmount(eff.Amount + info.HealInfo.Heal * _multiplier);
	}
}