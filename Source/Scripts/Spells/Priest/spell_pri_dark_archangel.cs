// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(197871)]
public class spell_pri_dark_archangel : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.DARK_ARCHANGEL_BUFF);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.Remove(Caster);
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(false, PriestSpells.ATONEMENT_AURA, Caster.GetGUID()));
	}

	private void HandleScriptEffect(int effIndex)
	{
		Caster.CastSpell(HitUnit, PriestSpells.DARK_ARCHANGEL_BUFF, true);
	}
}