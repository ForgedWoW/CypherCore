// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(115181)]
public class SpellMonkBreathOfFire : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                var target = HitUnit;

                if (target != null)
                {
                    // if Dizzying Haze is on the target, they will burn for an additionnal damage over 8s
                    if (target.HasAura(MonkSpells.DIZZYING_HAZE))
                        player.SpellFactory.CastSpell(target, MonkSpells.BREATH_OF_FIRE_DOT, true);

                    if (target.HasAura(MonkSpells.KEG_SMASH_AURA))
                        player.SpellFactory.CastSpell(target, MonkSpells.BREATH_OF_FIRE_DOT, true);
                }
            }
        }
    }
}