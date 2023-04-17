// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206803)]
public class SpellDemonHunterRainFromAbove : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (caster == null || !caster.AsPlayer)
            return;

        caster.Events.AddEventAtOffset(() => { caster.SpellFactory.CastSpell(caster, DemonHunterSpells.RAIN_FROM_ABOVE_SLOWFALL); }, TimeSpan.FromMilliseconds(1750));
    }
}