// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 108415 - Soul Link 8.xx
[SpellScript(WarlockSpells.SOUL_LINK)]
public class SpellWarlSoulLink : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
                if (!target.HasAura(WarlockSpells.SOUL_LINK_BUFF))
                    caster.SpellFactory.CastSpell(caster, WarlockSpells.SOUL_LINK_BUFF, true);
        }
    }
}