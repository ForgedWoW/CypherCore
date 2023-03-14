// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 32182 - Heroism
[SpellScript(32182)]
internal class spell_sha_heroism : SpellScript, ISpellAfterHit, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public void AfterHit()
	{
		var target = HitUnit;

		if (target)
			target.CastSpell(target, ShamanSpells.Exhaustion, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 0, Targets.UnitCasterAreaRaid));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(RemoveInvalidTargets, 1, Targets.UnitCasterAreaRaid));
	}

	private void RemoveInvalidTargets(List<WorldObject> targets)
	{
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, ShamanSpells.Exhaustion));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, ShamanSpells.HunterInsanity));
		targets.RemoveAll(new UnitAuraCheck<WorldObject>(true, ShamanSpells.MageTemporalDisplacement));
	}
}