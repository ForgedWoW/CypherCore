// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// Frostbrand - 196834
[SpellScript(196834)]
public class BfaSpellFrostbrandSpellScript : SpellScript, ISpellOnHit
{
    public override bool Load()
    {
        return Caster.IsPlayer;
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        caster.SpellFactory.CastSpell(target, ShamanSpells.FROSTBRAND_SLOW, true);
    }
}