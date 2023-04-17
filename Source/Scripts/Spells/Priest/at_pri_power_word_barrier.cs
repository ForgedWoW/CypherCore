// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Priest;

[Script]
public class AtPriPowerWordBarrier : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (caster.IsFriendlyTo(unit))
            caster.SpellFactory.CastSpell(unit, PriestSpells.POWER_WORD_BARRIER_BUFF, true);
    }

    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (unit.HasAura(PriestSpells.POWER_WORD_BARRIER_BUFF, caster.GUID))
            unit.RemoveAurasDueToSpell(PriestSpells.POWER_WORD_BARRIER_BUFF, caster.GUID);
    }
}