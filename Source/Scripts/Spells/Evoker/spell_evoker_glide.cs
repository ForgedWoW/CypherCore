// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GLIDE)] // 358733 - Glide (Racial)
internal class spell_evoker_glide : SpellScript, ISpellCheckCast, ISpellOnCast
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(EvokerSpells.GLIDE_KNOCKBACK, EvokerSpells.HOVER, EvokerSpells.SOAR_RACIAL);
	}

	public SpellCastResult CheckCast()
	{
		var caster = Caster;

		if (!caster.IsFalling())
			return SpellCastResult.NotOnGround;

		if (caster.HasAura(EvokerSpells.VISAGE_AURA))
			return SpellCastResult.DontReport; // SpellCastResult.NotShapeshift;

		return SpellCastResult.SpellCastOk;
	}

	public void OnCast()
	{
		var caster = Caster.ToPlayer();

		if (caster == null)
			return;

		caster.CastSpell(caster, EvokerSpells.GLIDE_KNOCKBACK, true);

		caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(EvokerSpells.HOVER, CastDifficulty), 0, null, false, TimeSpan.FromMilliseconds(250));
		caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(EvokerSpells.SOAR_RACIAL, CastDifficulty), 0, null, false, TimeSpan.FromMilliseconds(250));
	}
}