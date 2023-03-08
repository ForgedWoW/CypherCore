// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.MAYHEM)]
public class aura_warl_mayhem : AuraScript, IAuraCheckProc, IAuraOnProc
{
	public bool CheckProc(ProcEventInfo info)
	{
		if (info.ProcTarget != null)
			return RandomHelper.randChance(GetEffectInfo(0).BasePoints);

		return false;
	}

	public void OnProc(ProcEventInfo info)
	{
		Caster.CastSpell(info.ProcTarget, WarlockSpells.HAVOC, new CastSpellExtraArgs(SpellValueMod.Duration, GetEffectInfo(2).BasePoints * Time.InMilliseconds).SetIsTriggered(true));
	}
}