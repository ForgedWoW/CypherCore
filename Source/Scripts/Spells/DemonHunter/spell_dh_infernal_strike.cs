﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(189110)]
public class spell_dh_infernal_strike : SpellScript, ISpellOnCast, ISpellOnHit
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
            caster.Events.AddEventAtOffset(new event_dh_infernal_strike(caster), TimeSpan.FromMilliseconds(750));
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
            caster.CastSpell(new Position(dest.X, dest.Y, dest.Z), DemonHunterSpells.INFERNAL_STRIKE_JUMP, true);
            caster.CastSpell(caster, DemonHunterSpells.INFERNAL_STRIKE_VISUAL, true);
        }
    }
}