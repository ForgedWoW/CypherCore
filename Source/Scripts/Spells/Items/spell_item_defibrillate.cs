﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_goblin_jumper_cables", 33u, ItemSpellIds.GoblinJumperCablesFail)]
[Script("spell_item_goblin_jumper_cables_xl", 50u, ItemSpellIds.GoblinJumperCablesXlFail)]
[Script("spell_item_gnomish_army_knife", 67u, 0u)]
internal class spell_item_defibrillate : SpellScript, IHasSpellEffects
{
	private readonly uint _chance;
	private readonly uint _failSpell;

	public List<ISpellEffect> SpellEffects { get; } = new();

	public spell_item_defibrillate(uint chance, uint failSpell)
	{
		_chance = chance;
		_failSpell = failSpell;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Resurrect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		if (RandomHelper.randChance(_chance))
		{
			PreventHitDefaultEffect(effIndex);

			if (_failSpell != 0)
				Caster.CastSpell(Caster, _failSpell, new CastSpellExtraArgs(CastItem));
		}
	}
}