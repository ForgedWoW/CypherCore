// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(2823)]
public class SpellRogDeadlyPoisonInstantDamageSpellScript : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = ExplTargetUnit;

            if (target != null)
                if (target.HasAura(RogueSpells.DEADLY_POISON_DOT, player.GUID))
                    player.SpellFactory.CastSpell(target, RogueSpells.DEADLY_POISON_INSTANT_DAMAGE, true);
        }
    }
}