// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(197862)]
public class spell_pri_archangel : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitCasterAreaParty));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(false, PriestSpells.ATONEMENT_AURA, Caster.GUID));
	}

	private void HandleScriptEffect(int effIndex)
	{
		var aura = HitUnit.GetAura(PriestSpells.ATONEMENT_AURA, Caster.GUID);

		if (aura != null)
			aura.RefreshDuration();
	}
}