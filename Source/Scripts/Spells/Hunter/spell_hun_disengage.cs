// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(781)]
public class SpellHunDisengage : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var spec = player.GetPrimarySpecialization();

            if (player.HasSpell(HunterSpells.POSTHAST))
                if (spec == TalentSpecialization.HunterMarksman || spec == TalentSpecialization.HunterBeastMastery)
                {
                    player.RemoveMovementImpairingAuras(false);
                    player.SpellFactory.CastSpell(player, HunterSpells.POSTHAST_SPEED, true);
                }
        }
    }
}