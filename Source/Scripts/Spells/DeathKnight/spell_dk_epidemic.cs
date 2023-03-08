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
	private readonly List<Unit> savedTargets = new();
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		savedTargets.Clear();
		Caster.GetEnemiesWithinRangeWithOwnedAura(savedTargets, SpellInfo.GetMaxRange(), DeathKnightSpells.VIRULENT_PLAGUE);

		if (!savedTargets.Empty())
			return SpellCastResult.SpellCastOk;

		return SpellCastResult.NoValidTargets;
	}

	public void OnHit()
	{
		PreventHitAura();
		var caster = Caster;

		if (!savedTargets.Empty())
			foreach (var tar in savedTargets)
			{
				var aura = tar.GetAura(DeathKnightSpells.VIRULENT_PLAGUE, caster.GUID);

				if (aura != null)
					Caster.CastSpell(tar, DeathKnightSpells.EPIDEMIC_DAMAGE, true);
			}
	}
}