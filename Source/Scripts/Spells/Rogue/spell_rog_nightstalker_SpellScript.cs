// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(14062)]
public class SpellRogNightstalkerSpellScript : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            if (player.HasAura(RogueSpells.NIGHTSTALKER_AURA))
                player.SpellFactory.CastSpell(player, RogueSpells.NIGHTSTALKER_DAMAGE_DONE, true);

            if (player.HasAura(RogueSpells.SHADOW_FOCUS))
                player.SpellFactory.CastSpell(player, RogueSpells.SHADOW_FOCUS_EFFECT, true);
        }
    }
}