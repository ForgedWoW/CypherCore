// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 13120 Net-o-Matic
internal class spell_item_net_o_matic : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var target = HitUnit;

		if (target)
		{
			var spellId = ItemSpellIds.NetOMaticTriggered3;
			var roll = RandomHelper.URand(0, 99);

			if (roll < 2) // 2% for 30 sec self root (off-like chance unknown)
				spellId = ItemSpellIds.NetOMaticTriggered1;
			else if (roll < 4) // 2% for 20 sec root, charge to Target (off-like chance unknown)
				spellId = ItemSpellIds.NetOMaticTriggered2;

			Caster.CastSpell(target, spellId, true);
		}
	}
}