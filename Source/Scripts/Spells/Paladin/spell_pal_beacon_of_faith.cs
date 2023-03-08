// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Beacon of Faith - 156910
[SpellScript(156910)]
public class spell_pal_beacon_of_faith : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var target = ExplTargetUnit;

		if (target == null)
			return SpellCastResult.DontReport;

		if (target.HasAura(PaladinSpells.BeaconOfLight))
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}