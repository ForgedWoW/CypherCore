// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(new uint[]
{
    686, 6353, 103964, 205145
})]
public class SpellWarlDemonicCall : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (HitUnit)
                if (player.HasAura(WarlockSpells.DEMONIC_CALL) && !player.HasAura(WarlockSpells.DISRUPTED_NETHER))
                {
                    player.SpellFactory.CastSpell(player, WarlockSpells.HAND_OF_GULDAN_SUMMON, true);
                    player.RemoveAura(WarlockSpells.DEMONIC_CALL);
                }
    }
}