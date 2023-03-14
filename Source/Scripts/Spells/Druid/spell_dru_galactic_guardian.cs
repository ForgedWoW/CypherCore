// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script] // 203964 - Galactic Guardian
internal class spell_dru_galactic_guardian : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.DamageInfo;

		if (damageInfo != null)
		{
			var target = Target;

			// free automatic moonfire on Target
			target.CastSpell(damageInfo.Victim, DruidSpellIds.MoonfireDamage, true);

			// Cast aura
			target.CastSpell(damageInfo.Victim, DruidSpellIds.GalacticGuardianAura, true);
		}
	}
}