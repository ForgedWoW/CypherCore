// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 171017 - Meteor Strike
[SpellScript(171017)]
public class SpellWarlockInfernalMeteorStrike : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var player = caster.CharmerOrOwnerPlayerOrPlayerItself;

        if (player != null)
            if (player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES) && !player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES_CD))
            {
                for (uint i = 0; i < 3; ++i)
                    player.SpellFactory.CastSpell(caster, WarlockSpells.LORD_OF_THE_FLAMES_SUMMON, true);

                player.SpellFactory.CastSpell(player, WarlockSpells.LORD_OF_THE_FLAMES_CD, true);
            }
    }
}