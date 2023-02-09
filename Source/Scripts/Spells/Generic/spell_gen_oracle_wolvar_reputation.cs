﻿using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_oracle_wolvar_reputation : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1;
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(uint effIndex)
	{
		Player player    = GetCaster().ToPlayer();
		uint   factionId = (uint)GetEffectInfo().CalcValue();
		int    repChange = GetEffectInfo(1).CalcValue();

		FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(factionId);

		if (factionEntry == null)
			return;

		// Set rep to baserep + basepoints (expecting spillover for oposite faction . become hated)
		// Not when player already has equal or higher rep with this faction
		if (player.GetReputationMgr().GetBaseReputation(factionEntry) < repChange)
			player.GetReputationMgr().SetReputation(factionEntry, repChange);

		// EFFECT_INDEX_2 most likely update at war State, we already handle this in SetReputation
	}
}