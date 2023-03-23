// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 52759 - Ancestral Awakening
/// Updated 4.3.4
[SpellScript(52759)]
public class spell_sha_ancestral_awakening_proc : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaRaid));
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		if (targets.Count < 2)
			return;

		targets.Sort(new HealthPctOrderPred());

		var target = targets.First();
		targets.Clear();
		targets.Add(target);
	}

	private void HandleDummy(int effIndex)
	{
		Caster.CastSpell(HitUnit, ShamanSpells.ANCESTRAL_AWAKENING_PROC, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)EffectValue));
	}
}