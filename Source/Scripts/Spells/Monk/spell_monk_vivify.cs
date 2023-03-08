// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(116670)]
public class spell_monk_vivify : SpellScript, IHasSpellEffects, ISpellAfterCast, ISpellBeforeCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void AfterCast()
	{
		var caster = Caster.ToPlayer();

		if (caster == null)
			return;

		if (caster.HasAura(MonkSpells.LIFECYCLES))
			caster.CastSpell(caster, MonkSpells.LIFECYCLES_ENVELOPING_MIST, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterRenewingMist, 1, Targets.UnitDestAreaAlly));
	}

	public void BeforeCast()
	{
		if (Caster.GetCurrentSpell(CurrentSpellTypes.Channeled) && Caster.GetCurrentSpell(CurrentSpellTypes.Channeled).SpellInfo.Id == MonkSpells.SOOTHING_MIST)
		{
			Spell.CastFlagsEx = SpellCastFlagsEx.None;
			var targets = Caster.GetCurrentSpell(CurrentSpellTypes.Channeled).Targets;
			Spell.InitExplicitTargets(targets);
		}
	}

	private void FilterRenewingMist(List<WorldObject> targets)
	{
		targets.RemoveIf(new UnitAuraCheck<WorldObject>(false, MonkSpells.RENEWING_MIST_HOT, Caster.GUID));
	}
}