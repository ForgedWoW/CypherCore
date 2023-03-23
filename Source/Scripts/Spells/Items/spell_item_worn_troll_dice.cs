﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 47776 - Roll 'dem Bones
internal class spell_item_worn_troll_dice : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.TypeId == TypeId.Player;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		Caster.TextEmote(TextIds.WornTrollDice, HitUnit);

		uint minimum = 1;
		uint maximum = 6;

		// roll twice
		Caster.
			// roll twice
			AsPlayer.DoRandomRoll(minimum, maximum);

		Caster.AsPlayer.DoRandomRoll(minimum, maximum);
	}
}