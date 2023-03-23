// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[Script] // 49576 - Death Grip Initial
internal class spell_dk_death_grip_initial : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public SpellCastResult CheckCast()
	{
		var caster = Caster;

		// Death Grip should not be castable while jumping/falling
		if (caster.HasUnitState(UnitState.Jumping) ||
			caster.HasUnitMovementFlag(MovementFlag.Falling))
			return SpellCastResult.Moving;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		Caster.CastSpell(HitUnit, DeathKnightSpells.DeathGripDummy, true);
		HitUnit.CastSpell(Caster, DeathKnightSpells.DeathGripJump, true);

		if (Caster.HasAura(DeathKnightSpells.Blood))
			Caster.CastSpell(HitUnit, DeathKnightSpells.DeathGripTaunt, true);
	}
}