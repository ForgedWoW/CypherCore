﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 47496 - Explode, Ghoul spell for Corpse Explosion
internal class spell_dk_ghoul_explode : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.CorpseExplosionTriggered) && spellInfo.GetEffects().Count > 2;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(Suicide, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDamage(uint effIndex)
	{
		SetHitDamage((int)GetCaster().CountPctFromMaxHealth(GetEffectInfo(2).CalcValue(GetCaster())));
	}

	private void Suicide(uint effIndex)
	{
		Unit unitTarget = GetHitUnit();

		if (unitTarget)
			// Corpse Explosion (Suicide)
			unitTarget.CastSpell(unitTarget, DeathKnightSpells.CorpseExplosionTriggered, true);
	}
}