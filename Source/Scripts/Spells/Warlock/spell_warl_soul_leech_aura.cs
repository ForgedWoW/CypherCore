// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock;

// Soul Leech aura - 228974
[SpellScript(WarlockSpells.SOUL_LEECH)]
public class spell_warl_soul_leech_aura : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (!TryGetCaster(out var caster))
			return false;

		var basePoints = SpellInfo.GetEffect(0).BasePoints;
		var absorb = ((eventInfo.DamageInfo != null ? eventInfo.DamageInfo.GetDamage() : 0) * basePoints) / 100.0f;

		// Add remaining amount if already applied
		if (caster.TryGetAura(WarlockSpells.SOUL_LEECH_ABSORB, out var aur) && aur.TryGetEffect(0, out var auraEffect))
			absorb += auraEffect.Amount;

		// Cannot go over 5% (or 10% with Demonskin) max health
		var basePointNormal = SpellInfo.GetEffect(1).BasePoints;

		if (caster.TryGetAura(WarlockSpells.DEMON_SKIN, out var ds))
			basePointNormal = ds.GetEffect(1).Amount;

		var threshold = (caster.GetMaxHealth() * basePointNormal) / 100.0f;
		absorb = Math.Min(absorb, threshold);

		caster.CastSpell(caster, WarlockSpells.SOUL_LEECH_ABSORB, absorb, true);

		return true;
	}
}