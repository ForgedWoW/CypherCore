// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(115057)]
public class SpellMonkFlyingSerpentKick : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                if (player.HasAura(MonkSpells.FLYING_SERPENT_KICK))
                    player.RemoveAura(MonkSpells.FLYING_SERPENT_KICK);

                if (caster.HasAura(MonkSpells.ITEM_PVP_GLOVES_BONUS))
                    caster.RemoveAurasByType(AuraType.ModDecreaseSpeed);

                player.SpellFactory.CastSpell(player, MonkSpells.FLYING_SERPENT_KICK_AOE, true);
            }
        }
    }
}