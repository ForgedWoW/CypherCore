// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(115939)]
public class SpellHunBeastCleave : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(HunterSpells.BEAST_CLEAVE_AURA))
            {
                var pet = player.CurrentPet;

                if (pet != null)
                    player.SpellFactory.CastSpell(pet, HunterSpells.BEAST_CLEAVE_PROC, true);
            }
    }
}