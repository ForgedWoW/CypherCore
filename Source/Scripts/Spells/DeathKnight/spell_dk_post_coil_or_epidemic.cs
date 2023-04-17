// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47632)]
[SpellScript(212739)]
internal class SpellDkPostCoilOrEpidemic : SpellScript, ISpellAfterHit
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                var deathRotApply = 1;
                var suddenDoom = caster.GetAura(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM_AURA);

                if (suddenDoom != null)
                {
                    deathRotApply += 1;
                    suddenDoom.ModStackAmount(-1);
                }

                if (caster.HasAura(DeathKnightSpells.DEATH_ROT))
                    caster.SpellFactory.CastSpell(target, DeathKnightSpells.DEATH_ROT_AURA, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, deathRotApply));
            }
        }
    }
}