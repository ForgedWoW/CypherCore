// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunTarTrapActivatedAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnRemove
{
    public enum UsedSpells
    {
        TarTrapSlow = 135299
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
                caster.SpellFactory.CastSpell(target, UsedSpells.TarTrapSlow, true);
        }
    }

    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.AsPlayer)
            return;

        foreach (var itr in At.InsideUnits)
        {
            var target = ObjectAccessor.Instance.GetUnit(caster, itr);

            if (target.HasAura(UsedSpells.TarTrapSlow) && target.GetAura(UsedSpells.TarTrapSlow).Caster == caster)
                target.RemoveAura(UsedSpells.TarTrapSlow);
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
            caster.SpellFactory.CastSpell(unit, UsedSpells.TarTrapSlow, true);
    }

    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (unit.HasAura(UsedSpells.TarTrapSlow) && unit.GetAura(UsedSpells.TarTrapSlow).Caster == caster)
            unit.RemoveAura(UsedSpells.TarTrapSlow);
    }
}