// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.DIMENSIONAL_RIFT)]
public class spell_warl_dimensional_rift : SpellScript, IHasSpellEffects
{
	private readonly List<uint> _spells = new()
	{
		WarlockSpells.SHADOWY_TEAR,
		WarlockSpells.UNSTABLE_TEAR,
		WarlockSpells.CHAOS_TEAR
	};

	public List<ISpellEffect> SpellEffects { get; } = new();

	public void HandleScriptEffect(int effectIndex)
	{
		Caster.CastSpell(_spells.SelectRandom(), true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, Framework.Constants.SpellEffectName.ScriptEffect, Framework.Constants.SpellScriptHookType.EffectHitTarget));
	}
}