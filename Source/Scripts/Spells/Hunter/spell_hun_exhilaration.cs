// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script] // 109304 - Exhilaration
internal class SpellHunExhilaration : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (Caster.HasAura(HunterSpells.EXHILARATION_R2) && !Caster.HasAura(HunterSpells.LONEWOLF))
            Caster.SpellFactory.CastSpell(null, HunterSpells.ExhilarationPet, true);
    }
}