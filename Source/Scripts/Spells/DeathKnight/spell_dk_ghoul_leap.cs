// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47482)]
public class SpellDkGhoulLeap : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || target == null)
            return;

        Unit owner = caster.OwnerUnit.AsPlayer;

        if (owner != null)
        {
            if (caster.HasAura(DeathKnightSpells.DARK_TRANSFORMATION))
                caster.SpellFactory.CastSpell(target, DeathKnightSpells.DT_GHOUL_LEAP, true);
            else
                caster.SpellFactory.CastSpell(target, DeathKnightSpells.GHOUL_LEAP, true);
        }
    }
}