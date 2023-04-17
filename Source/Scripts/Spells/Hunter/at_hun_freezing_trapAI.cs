// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunFreezingTrapAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    public enum UsedSpells
    {
        FreezingTrapStun = 3355
    }

    public int TimeInterval;

    public void OnCreate()
    {
        TimeInterval = 200;
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.AsPlayer)
            return;

        foreach (var itr in At.InsideUnits)
        {
            var target = ObjectAccessor.Instance.GetUnit(caster, itr);

            if (!caster.IsFriendlyTo(target))
            {
                caster.SpellFactory.CastSpell(target, UsedSpells.FreezingTrapStun, true);
                At.Remove();

                return;
            }
        }
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (!caster.IsFriendlyTo(unit))
        {
            caster.SpellFactory.CastSpell(unit, UsedSpells.FreezingTrapStun, true);
            At.Remove();

            return;
        }
    }
}