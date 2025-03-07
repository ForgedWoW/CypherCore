﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 59640 Underbelly Elixir
internal class spell_item_underbelly_elixir : SpellScript, IHasSpellEffects
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
		var spellId = ItemSpellIds.UnderbellyElixirTriggered3;

		switch (RandomHelper.URand(1, 3))
		{
			case 1:
				spellId = ItemSpellIds.UnderbellyElixirTriggered1;

				break;
			case 2:
				spellId = ItemSpellIds.UnderbellyElixirTriggered2;

				break;
		}

		caster.CastSpell(caster, spellId, true);
	}
}