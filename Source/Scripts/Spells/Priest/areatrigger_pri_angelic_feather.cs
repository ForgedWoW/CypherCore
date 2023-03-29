// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Priest;

[Script] // Angelic Feather areatrigger - created by ANGELIC_FEATHER_AREATRIGGER
internal class areatrigger_pri_angelic_feather : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    // Called when the AreaTrigger has just been initialized, just before added to map
    public void OnCreate()
    {
        var caster = At.GetCaster();

        if (caster)
        {
            var areaTriggers = caster.GetAreaTriggers(PriestSpells.ANGELIC_FEATHER_AREATRIGGER);

            if (areaTriggers.Count >= 3)
                areaTriggers.First().SetDuration(0);
        }
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster)
            if (caster.IsFriendlyTo(unit))
            {
                // If Target already has aura, increase duration to max 130% of initial duration
                caster.CastSpell(unit, PriestSpells.ANGELIC_FEATHER_AURA, true);
                At.SetDuration(0);
            }
    }
}