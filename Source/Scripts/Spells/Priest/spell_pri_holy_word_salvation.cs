// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(265202)]
public class SpellPriHolyWordSalvation : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var eff = GetEffectInfo(1);
        var friendlyList = caster.GetPlayerListInGrid(40);

        foreach (var friendPlayers in friendlyList)
            if (friendPlayers.IsFriendlyTo(caster))
            {
                caster.SpellFactory.CastSpell(friendPlayers, PriestSpells.RENEW, true);

                var prayer = friendPlayers.GetAura(PriestSpells.PRAYER_OF_MENDING_AURA);

                if (prayer != null)
                    prayer.ModStackAmount(eff.BasePoints);
            }
    }
}