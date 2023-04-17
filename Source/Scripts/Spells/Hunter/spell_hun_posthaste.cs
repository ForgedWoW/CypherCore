// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script] // 781 - Disengage
internal class SpellHunPosthaste : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.HasAura(HunterSpells.POSTHASTE_TALENT))
        {
            Caster.RemoveMovementImpairingAuras(true);
            Caster.SpellFactory.CastSpell(Caster, HunterSpells.POSTHASTE_INCREASE_SPEED, Spell);
        }
    }
}