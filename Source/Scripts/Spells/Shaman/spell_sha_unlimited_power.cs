// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 260895 - Unlimited Power
[SpellScript(260895)]
internal class spell_sha_unlimited_power : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var caster = procInfo.Actor;
		var aura = caster.GetAura(ShamanSpells.UnlimitedPowerBuff);

		if (aura != null)
			aura.SetStackAmount((byte)(aura.StackAmount + 1));
		else
			caster.CastSpell(caster, ShamanSpells.UnlimitedPowerBuff, procInfo.ProcSpell);
	}
}