// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116645)]
public class spell_monk_teachings_of_the_monastery_passive : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.SpellInfo.Id != MonkSpells.TIGER_PALM && eventInfo.SpellInfo.Id != MonkSpells.BLACKOUT_KICK && eventInfo.SpellInfo.Id != MonkSpells.BLACKOUT_KICK_TRIGGERED)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		if (eventInfo.SpellInfo.Id == MonkSpells.TIGER_PALM)
		{
			Target.CastSpell(Target, MonkSpells.TEACHINGS_OF_THE_MONASTERY, true);
		}
		else if (RandomHelper.randChance(aurEff.Amount))
		{
			var spellInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.RISING_SUN_KICK, Difficulty.None);

			if (spellInfo != null)
				Target.SpellHistory.RestoreCharge(spellInfo.ChargeCategoryId);
		}
	}
}