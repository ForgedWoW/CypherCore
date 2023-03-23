// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.AZURE_STRIKE)]
public class spell_evoker_azure_strike : SpellScript, ISpellOnHit, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void OnHit()
	{
		if (TryGetCaster(out Unit caster) && TryGetExplTargetUnit(out var target))
		{
			var damage = HitDamage;
			var bp0 = (damage + (damage * 0.5f)); // Damage + 50% of damage
			caster.CastSpell(target, EvokerSpells.AZURE_STRIKE, bp0, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
	}

	void FilterTargets(List<WorldObject> targets)
	{
		targets.Remove(ExplTargetUnit);
		targets.RandomResize((uint)GetEffectInfo(0).CalcValue(Caster) - 1);
		targets.Add(ExplTargetUnit);
	}
}