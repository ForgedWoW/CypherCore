// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunBindingShotAI : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public enum UsedSpells
    {
        BindingShotAura = 117405,
        BindingShotStun = 117526,
        BindingShotImmune = 117553,
        BindingShotVisual1 = 118306,
        HunderBindingShotVisual2 = 117614
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (unit == null)
            return;

        if (!caster.IsFriendlyTo(unit))
            unit.SpellFactory.CastSpell(unit, UsedSpells.BindingShotAura, true);
    }

    public void OnUnitExit(Unit unit)
    {
        if (unit == null || !At.GetCaster())
            return;

        var pos = At.Location;

        // Need to check range also, since when the trigger is removed, this get called as well.
        if (unit.HasAura(UsedSpells.BindingShotAura) && unit.Location.GetExactDist(pos) >= 5.0f)
        {
            unit.RemoveAura(UsedSpells.BindingShotAura);
            At.GetCaster().SpellFactory.CastSpell(unit, UsedSpells.BindingShotStun, true);
        }
    }
}