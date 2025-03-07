﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 40802 Mingo's Fortune Generator
internal class spell_item_mingos_fortune_generator : SpellScript, IHasSpellEffects
{
	private readonly uint[] CreateFortuneSpells =
	{
		ItemSpellIds.CreateFortune1, ItemSpellIds.CreateFortune2, ItemSpellIds.CreateFortune3, ItemSpellIds.CreateFortune4, ItemSpellIds.CreateFortune5, ItemSpellIds.CreateFortune6, ItemSpellIds.CreateFortune7, ItemSpellIds.CreateFortune8, ItemSpellIds.CreateFortune9, ItemSpellIds.CreateFortune10, ItemSpellIds.CreateFortune11, ItemSpellIds.CreateFortune12, ItemSpellIds.CreateFortune13, ItemSpellIds.CreateFortune14, ItemSpellIds.CreateFortune15, ItemSpellIds.CreateFortune16, ItemSpellIds.CreateFortune17, ItemSpellIds.CreateFortune18, ItemSpellIds.CreateFortune19, ItemSpellIds.CreateFortune20
	};

	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		Caster.CastSpell(Caster, CreateFortuneSpells.SelectRandom(), true);
	}
}