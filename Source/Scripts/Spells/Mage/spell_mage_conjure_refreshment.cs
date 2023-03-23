// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 190336 - Conjure Refreshment
internal class spell_mage_conjure_refreshment : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster.AsPlayer;

		if (caster)
		{
			var group = caster.Group;

			if (group)
				caster.CastSpell(caster, MageSpells.ConjureRefreshmentTable, true);
			else
				caster.CastSpell(caster, MageSpells.ConjureRefreshment, true);
		}
	}
}