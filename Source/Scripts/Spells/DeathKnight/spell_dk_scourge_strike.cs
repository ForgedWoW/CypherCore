﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(55090)]
public class spell_dk_scourge_strike : SpellScript, IHasSpellEffects
{
	private List<WorldObject> saveTargets = new();
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(GetTargetUnit, 1, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(TriggerFollowup, 2, SpellEffectName.TriggerSpell, SpellScriptHookType.LaunchTarget));
	}

	private void HandleOnHit(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster = Caster;

		foreach (var target in saveTargets)
			if (target != null)
			{
				target.TryGetAsUnit(out var tar);

				if (tar != null)
				{
					var festeringWoundAura = tar.GetAura(DeathKnightSpells.FESTERING_WOUND, Caster.GUID);

					if (festeringWoundAura != null)
					{
						caster.CastSpell(tar, DeathKnightSpells.FESTERING_WOUND_DAMAGE, true);
						festeringWoundAura.ModStackAmount(-1);

						if (caster.HasAura(DeathKnightSpells.BURSTING_SORES))
							caster.CastSpell(tar, DeathKnightSpells.BURSTING_SORES_DAMAGE, true);
					}

					caster.CastSpell(tar, DeathKnightSpells.SCOURGE_STRIKE_TRIGGERED, true);
				}
			}
	}

	private void GetTargetUnit(List<WorldObject> targets)
	{
		saveTargets.Clear();

		if (!Caster.HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE))
			targets.RemoveIf((WorldObject target) => { return ExplTargetUnit != target; });

		saveTargets = targets;
	}

	private void TriggerFollowup(int effIndex)
	{
		PreventHitEffect(effIndex);
	}
}