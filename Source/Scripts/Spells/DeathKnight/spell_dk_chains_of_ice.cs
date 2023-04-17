// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(45524)]
public class SpellDkChainsOfIce : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (target != null)
        {
            if (caster.HasAura(152281))
                caster.SpellFactory.CastSpell(target, 155159, true);
            else
                caster.SpellFactory.CastSpell(target, DeathKnightSpells.FROST_FEVER, true);
        }
    }
}