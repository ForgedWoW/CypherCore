// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

//197995
[SpellScript(197995)]
public class SpellShaWellspring : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        caster.SpellFactory.CastSpell(target, ShamanSpells.WELLSPRING_MISSILE, true);
    }
}