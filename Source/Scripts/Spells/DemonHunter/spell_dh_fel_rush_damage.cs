// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(223107)]
public class spell_dh_fel_rush_damage : SpellScript, IHasSpellEffects, ISpellOnHit, ISpellOnCast
{
	private bool _targetHit;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void OnCast()
	{
		var caster = Caster;

		if (caster != null)
			if (caster.HasAura(DemonHunterSpells.FEL_MASTERY) && _targetHit)
				caster.CastSpell(caster, DemonHunterSpells.FEL_MASTERY_FURY, true);
	}

	public void OnHit()
	{
		if (Caster && HitUnit)
		{
			var attackPower = Caster.UnitData.AttackPower / 100 * 25.3f;
			HitDamage = attackPower;
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitRectCasterEnemy));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitRectCasterEnemy));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		targets.Remove(Caster);
	}

	private void CountTargets(List<WorldObject> targets)
	{
		var caster = Caster;

		if (caster == null)
			return;

		targets.Clear();
		var units = new List<Unit>();
		caster.GetAttackableUnitListInRange(units, 25.0f);


		units.RemoveIf((Unit unit) => { return !caster.Location.HasInLine(unit.Location, 6.0f, caster.ObjectScale); });

		foreach (var unit in units)
			targets.Add(unit);

		_targetHit = targets.Count > 0;
	}
}