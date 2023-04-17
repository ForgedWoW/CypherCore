// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102558)]
public class SpellDruIncarnationGuardianOfUrsoc : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (!player.HasAura(ShapeshiftFormSpells.BearForm))
                player.SpellFactory.CastSpell(player, ShapeshiftFormSpells.BearForm, true);
    }
}