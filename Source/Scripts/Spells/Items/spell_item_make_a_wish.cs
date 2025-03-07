﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 33060 Make a Wish
internal class spell_item_make_a_wish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.TypeId == TypeId.Player;
	}


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var spellId = ItemSpellIds.MrPinchysGift;

		switch (RandomHelper.URand(1, 5))
		{
			case 1:
				spellId = ItemSpellIds.MrPinchysBlessing;

				break;
			case 2:
				spellId = ItemSpellIds.SummonMightyMrPinchy;

				break;
			case 3:
				spellId = ItemSpellIds.SummonFuriousMrPinchy;

				break;
			case 4:
				spellId = ItemSpellIds.TinyMagicalCrawdad;

				break;
		}

		caster.CastSpell(caster, spellId, true);
	}
}