// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class at_monk_windwalking : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.AsPlayer)
            return;

        foreach (var guid in At.InsideUnits)
        {
            var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

            if (unit != null)
            {
                if (unit.HasAura(MonkSpells.WINDWALKING) && unit != caster) // Don't remove from other WW monks.
                    continue;

                var aur = unit.GetAura(MonkSpells.WINDWALKER_AURA, caster.GUID);

                if (aur != null)
                {
                    aur.SetMaxDuration(10 * Time.IN_MILLISECONDS);
                    aur.SetDuration(10 * Time.IN_MILLISECONDS);
                }
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

        var aur = unit.GetAura(MonkSpells.WINDWALKER_AURA);

        if (aur != null)
            aur.SetDuration(-1);
        else if (caster.IsFriendlyTo(unit))
            caster.CastSpell(unit, MonkSpells.WINDWALKER_AURA, true);
    }

    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (unit.HasAura(MonkSpells.WINDWALKING) && unit != caster) // Don't remove from other WW monks.
            return;

        var aur = unit.GetAura(MonkSpells.WINDWALKER_AURA, caster.GUID);

        if (aur != null)
        {
            aur.SetMaxDuration(10 * Time.IN_MILLISECONDS);
            aur.SetDuration(10 * Time.IN_MILLISECONDS);
        }
    }
}