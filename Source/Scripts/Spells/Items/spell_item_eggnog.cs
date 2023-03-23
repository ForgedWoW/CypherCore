﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 21149 - Egg Nog
internal class spell_item_eggnog : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.Inebriate, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		if (RandomHelper.randChance(40))
			Caster.CastSpell(HitUnit, RandomHelper.randChance(50) ? ItemSpellIds.EggNogReindeer : ItemSpellIds.EggNogSnowman, CastItem);
	}
}