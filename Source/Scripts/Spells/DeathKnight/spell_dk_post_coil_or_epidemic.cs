// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47632)]
[SpellScript(212739)]
internal class spell_dk_post_coil_or_epidemic : SpellScript, ISpellAfterHit
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(DeathKnightSpells.DEATH_COIL_DAMAGE, DeathKnightSpells.EPIDEMIC_DAMAGE);
	}

	public void AfterHit()
	{
		var caster = GetCaster();
		if (caster != null) {
			var target = GetHitUnit();
			if (target != null) {
				var deathRotApply = 1;
                var suddenDoom = caster.GetAura(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM_AURA);
				if (suddenDoom != null)
				{
					deathRotApply += 1;
					suddenDoom.ModStackAmount(-1);
                }

                if (caster.HasAura(DeathKnightSpells.DEATH_ROT))
                    caster.CastSpell(target, DeathKnightSpells.DEATH_ROT_AURA, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, deathRotApply));
            }
		}
	}
}