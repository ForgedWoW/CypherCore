// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(186387)]
public class SpellBurstingShot : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;

        if (caster != null)
            caster.SpellFactory.CastSpell(HitUnit, HunterSpells.AURA_SHOOTING, true);
    }
}