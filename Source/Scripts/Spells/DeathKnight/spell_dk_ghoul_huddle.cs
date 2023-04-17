// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47484)]
public class SpellDkGhoulHuddle : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        Unit owner = caster.OwnerUnit.AsPlayer;

        if (owner != null)
            caster.SpellFactory.CastSpell(caster.HasAura(DeathKnightSpells.DARK_TRANSFORMATION) ? DeathKnightSpells.DT_GHOUL_HUDDLE : DeathKnightSpells.GHOUL_HUDDLE, true);
    }
}