﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[Script] // 131347 - Glide
internal class spell_dh_glide : SpellScript, ISpellCheckCast, ISpellBeforeCast
{
	public void BeforeCast()
	{
		var caster = Caster.AsPlayer;

		if (!caster)
			return;

		caster.CastSpell(caster, DemonHunterSpells.GlideKnockback, true);
		caster.CastSpell(caster, DemonHunterSpells.GlideDuration, true);

		caster.SpellHistory.StartCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.VengefulRetreatTrigger, CastDifficulty), 0, null, false, TimeSpan.FromMilliseconds(250));
		caster.SpellHistory.StartCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FelRush, CastDifficulty), 0, null, false, TimeSpan.FromMilliseconds(250));
	}


	public SpellCastResult CheckCast()
	{
		var caster = Caster;

		if (caster.IsMounted ||
			caster.VehicleBase)
			return SpellCastResult.DontReport;

		if (!caster.IsFalling)
			return SpellCastResult.NotOnGround;

		return SpellCastResult.SpellCastOk;
	}
}