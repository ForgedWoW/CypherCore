// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(189110)]
public class SpellDhInfernalStrike : SpellScript, ISpellOnCast, ISpellOnHit
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
            caster.Events.AddEventAtOffset(new EventDhInfernalStrike(caster), TimeSpan.FromMilliseconds(750));
    }

    public void OnHit()
    {
        var caster = Caster;
        var dest = HitDest;
        var target = HitUnit;

        if (caster == null || dest == null || target == null)
            return;

        if (target.IsHostileTo(caster))
        {
            caster.SpellFactory.CastSpell(new Position(dest.X, dest.Y, dest.Z), DemonHunterSpells.INFERNAL_STRIKE_JUMP, true);
            caster.SpellFactory.CastSpell(caster, DemonHunterSpells.INFERNAL_STRIKE_VISUAL, true);
        }
    }
}