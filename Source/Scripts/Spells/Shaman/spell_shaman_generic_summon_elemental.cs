// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// Summon Fire, Earth & Storm Elemental  - Called By 198067 Fire Elemental, 198103 Earth Elemental, 192249 Storm Elemental
[SpellScript(new uint[]
{
	198067, 198103, 192249
})]
public class spell_shaman_generic_summon_elemental : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleSummon(int effIndex)
	{
		uint triggerSpell;

		switch (SpellInfo.Id)
		{
			case Spells.SummonFireElemental:
				triggerSpell = (Caster.HasAura(Spells.PrimalElementalist)) ? Spells.SummonPrimalElementalistFireElemental : Spells.SummonFireElementalTriggered;

				break;
			case Spells.SummonEarthElemental:
				triggerSpell = (Caster.HasAura(Spells.PrimalElementalist)) ? Spells.SummonPrimalElementalistEarthElemental : Spells.SummonEarthElementalTriggered;

				break;
			case Spells.SummonStormElemental:
				triggerSpell = (Caster.HasAura(Spells.PrimalElementalist)) ? Spells.SummonPrimalElementalistStormElemental : Spells.SummonStormElementalTriggered;

				break;
			default:
				triggerSpell = 0;

				break;
		}

		if (triggerSpell != 0)
			Caster.CastSpell(Caster, triggerSpell, true);
	}

	private struct Spells
	{
		public const uint PrimalElementalist = 117013;
		public const uint SummonFireElemental = 198067;
		public const uint SummonFireElementalTriggered = 188592;
		public const uint SummonPrimalElementalistFireElemental = 118291;
		public const uint SummonEarthElemental = 198103;
		public const uint SummonEarthElementalTriggered = 188616;
		public const uint SummonPrimalElementalistEarthElemental = 118323;
		public const uint SummonStormElemental = 192249;
		public const uint SummonStormElementalTriggered = 157299;
		public const uint SummonPrimalElementalistStormElemental = 157319;
	}
}