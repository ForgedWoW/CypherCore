// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Shaman;

// Spell 196935 - Voodoo Totem
// AT - 11577
[Script]
public class AtShaVoodooTotem : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.IsValidAttackTarget(unit))
        {
            caster.SpellFactory.CastSpell(unit, TotemSpells.TOTEM_VOODOO_EFFECT, true);
            caster.SpellFactory.CastSpell(unit, TotemSpells.TOTEM_VOODOO_COOLDOWN, true);
        }
    }

    public void OnUnitExit(Unit unit)
    {
        unit.RemoveAurasDueToSpell(TotemSpells.TOTEM_VOODOO_EFFECT, At.CasterGuid);
    }
}