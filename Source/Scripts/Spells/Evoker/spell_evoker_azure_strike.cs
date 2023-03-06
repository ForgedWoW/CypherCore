// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.AZURE_STRIKE)]
public class spell_evoker_azure_strike : SpellScript, ISpellOnHit, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Validate(SpellInfo UnnamedParameter)
    {
        if (Global.SpellMgr.GetSpellInfo(EvokerSpells.AZURE_STRIKE, Difficulty.None) != null)
            return false;

        return true;
    }

    void FilterTargets(List<WorldObject> targets)
    {
        targets.Remove(GetExplTargetUnit());
        targets.RandomResize((uint)GetEffectInfo(0).CalcValue(GetCaster()) - 1);
        targets.Add(GetExplTargetUnit());
    }

	public void OnHit()
	{
		if (TryGetCaster(out Unit caster) && TryGetExplTargetUnit(out Unit target))
		{
			var damage = GetHitDamage();
			var bp0    = (damage + (damage * 0.5f)); // Damage + 50% of damage
			caster.CastSpell(target, EvokerSpells.AZURE_STRIKE, bp0);
		}
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
    }
}