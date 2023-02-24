// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207317)]
public class spell_dk_epidemic : SpellScript, IHasSpellEffects, ISpellCheckCast, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects { get; } = new();
	private List<Unit> savedTargets = new();

	public void OnHit()
	{
        PreventHitAura();
        var caster = GetCaster();
		if (!savedTargets.Empty()) {
			foreach (var tar in savedTargets) {
				var aura = tar.GetAura(DeathKnightSpells.VIRULENT_PLAGUE, caster.GetGUID());
				if (aura != null)
					GetCaster().CastSpell(tar, DeathKnightSpells.EPIDEMIC_DAMAGE, true);
			}
		}
	}

	public SpellCastResult CheckCast()
	{
		savedTargets.Clear();
		GetCaster().GetEnemiesWithinRangeWithOwnedAura(savedTargets,GetSpellInfo().GetMaxRange(), DeathKnightSpells.VIRULENT_PLAGUE);
		if (!savedTargets.Empty())
			return SpellCastResult.SpellCastOk;
        return SpellCastResult.NoValidTargets;
    }

}