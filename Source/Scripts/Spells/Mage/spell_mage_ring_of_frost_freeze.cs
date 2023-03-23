// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 82691 - Ring of Frost (freeze efect)
internal class spell_mage_ring_of_frost_freeze : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		var dest = ExplTargetDest;
		var outRadius = Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, CastDifficulty).GetEffect(0).CalcRadius();
		var inRadius = 6.5f;

		targets.RemoveAll(target =>
		{
			var unit = target.AsUnit;

			if (!unit)
				return true;

			return unit.HasAura(MageSpells.RingOfFrostDummy) || unit.HasAura(MageSpells.RingOfFrostFreeze) || unit.Location.GetExactDist(dest) > outRadius || unit.Location.GetExactDist(dest) < inRadius;
		});
	}
}