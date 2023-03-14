// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_pygmy_oil : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var aura = caster.GetAura(ItemSpellIds.PygmyOilPygmyAura);

		if (aura != null)
		{
			aura.RefreshDuration();
		}
		else
		{
			aura = caster.GetAura(ItemSpellIds.PygmyOilSmallerAura);

			if (aura == null ||
				aura.StackAmount < 5 ||
				!RandomHelper.randChance(50))
			{
				caster.CastSpell(caster, ItemSpellIds.PygmyOilSmallerAura, true);
			}
			else
			{
				aura.Remove();
				caster.CastSpell(caster, ItemSpellIds.PygmyOilPygmyAura, true);
			}
		}
	}
}