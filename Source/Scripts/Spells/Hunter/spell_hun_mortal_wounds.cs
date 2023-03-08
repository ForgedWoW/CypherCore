// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(201075)]
public class spell_hun_mortal_wounds : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if ((eventInfo.HitMask & ProcFlagsHit.None) != 0 && eventInfo.SpellInfo.Id == HunterSpells.LACERATE)
			return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		PreventDefaultAction();

		var player = Caster.AsPlayer;

		if (player != null)
		{
			var chargeCatId = Global.SpellMgr.GetSpellInfo(HunterSpells.MONGOOSE_BITE, Difficulty.None).ChargeCategoryId;

			var mongooseBite = CliDB.SpellCategoryStorage.LookupByKey(chargeCatId);

			if (mongooseBite != null)
				player.GetSpellHistory().RestoreCharge(chargeCatId);
		}
	}
}