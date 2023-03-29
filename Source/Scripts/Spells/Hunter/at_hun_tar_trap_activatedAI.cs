// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Hunter;

[Script]
public class at_hun_tar_trap_activatedAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnRemove
{
    public enum UsedSpells
    {
        TAR_TRAP_SLOW = 135299
    }

    public int timeInterval;

    public void OnCreate()
    {
        timeInterval = 200;
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.AsPlayer)
            return;

        foreach (var itr in At.InsideUnits)
        {
            var target = ObjectAccessor.Instance.GetUnit(caster, itr);

            if (!caster.IsFriendlyTo(target))
                caster.CastSpell(target, UsedSpells.TAR_TRAP_SLOW, true);
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

            if (target.HasAura(UsedSpells.TAR_TRAP_SLOW) && target.GetAura(UsedSpells.TAR_TRAP_SLOW).Caster == caster)
                target.RemoveAura(UsedSpells.TAR_TRAP_SLOW);
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
            caster.CastSpell(unit, UsedSpells.TAR_TRAP_SLOW, true);
    }

    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (unit.HasAura(UsedSpells.TAR_TRAP_SLOW) && unit.GetAura(UsedSpells.TAR_TRAP_SLOW).Caster == caster)
            unit.RemoveAura(UsedSpells.TAR_TRAP_SLOW);
    }
}