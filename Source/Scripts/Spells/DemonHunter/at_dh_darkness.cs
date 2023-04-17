// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class AtDhDarkness : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnCreate
{
    private bool _entered;

    public void OnCreate()
    {
        At.SetDuration(8000);
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.IsFriendlyTo(unit) && !unit.HasAura(DemonHunterSpells.DARKNESS_ABSORB))
        {
            _entered = true;

            if (_entered)
            {
                caster.SpellFactory.CastSpell(unit, DemonHunterSpells.DARKNESS_ABSORB, true);
                _entered = false;
            }
        }
    }

    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (unit.HasAura(DemonHunterSpells.DARKNESS_ABSORB))
            unit.RemoveAurasDueToSpell(DemonHunterSpells.DARKNESS_ABSORB, caster.GUID);
    }
}