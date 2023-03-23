// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// 152262 - Seraphim
[SpellScript(152262)]
public class spell_pal_seraphim : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public SpellCastResult CheckCast()
	{
		var ChargeCategoryId = Global.SpellMgr.GetSpellInfo(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS, Difficulty.None).ChargeCategoryId;

		if (!Caster.SpellHistory.HasCharge(ChargeCategoryId))
			return SpellCastResult.NoPower;

		return SpellCastResult.Success;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var ChargeCategoryId = Global.SpellMgr.GetSpellInfo(PaladinSpells.SHIELD_OF_THE_RIGHTEOUS, Difficulty.None).ChargeCategoryId;
		var spellHistory = Caster.SpellHistory;

		spellHistory.ConsumeCharge(ChargeCategoryId);
		spellHistory.ForceSendSpellCharge(CliDB.SpellCategoryStorage.LookupByKey(ChargeCategoryId));
	}
}