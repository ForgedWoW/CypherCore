﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 60321 - Scroll of Recall III
internal class spell_item_scroll_of_recall : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TeleportUnits, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Unit caster       = GetCaster();
		byte maxSafeLevel = 0;

		switch (GetSpellInfo().Id)
		{
			case ItemSpellIds.ScrollOfRecallI: // Scroll of Recall
				maxSafeLevel = 40;

				break;
			case ItemSpellIds.ScrollOfRecallII: // Scroll of Recall II
				maxSafeLevel = 70;

				break;
			case ItemSpellIds.ScrollOfRecallIII: // Scroll of Recal III
				maxSafeLevel = 80;

				break;
			default:
				break;
		}

		if (caster.GetLevel() > maxSafeLevel)
		{
			caster.CastSpell(caster, ItemSpellIds.Lost, true);

			// ALLIANCE from 60323 to 60330 - HORDE from 60328 to 60335
			uint spellId = ItemSpellIds.ScrollOfRecallFailAlliance1;

			if (GetCaster().ToPlayer().GetTeam() == Team.Horde)
				spellId = ItemSpellIds.ScrollOfRecallFailHorde1;

			GetCaster().CastSpell(GetCaster(), spellId + RandomHelper.URand(0, 7), true);

			PreventHitDefaultEffect(effIndex);
		}
	}
}