// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 121536 - Angelic Feather talent
internal class spell_pri_angelic_feather_trigger : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleEffectDummy(int effIndex)
	{
		var destPos = HitDest;
		var radius = EffectInfo.CalcRadius();

		// Caster is prioritary
		if (Caster.IsWithinDist2d(destPos, radius))
		{
			Caster.CastSpell(Caster, PriestSpells.ANGELIC_FEATHER_AURA, true);
		}
		else
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.CastDifficulty = CastDifficulty;
			Caster.CastSpell(destPos, PriestSpells.ANGELIC_FEATHER_AREATRIGGER, args);
		}
	}
}