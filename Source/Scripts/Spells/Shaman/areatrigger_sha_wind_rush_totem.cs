// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Shaman;

[Script] //  12676 - AreaTriggerId
internal class AreatriggerShaWindRushTotem : AreaTriggerScript, IAreaTriggerOnUpdate, IAreaTriggerOnUnitEnter, IAreaTriggerOnCreate
{
    private static readonly int RefreshTime = 4500;

    private int _refreshTimer;

    public void OnCreate()
    {
        _refreshTimer = RefreshTime;
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            if (!caster.IsFriendlyTo(unit))
                return;

            caster.SpellFactory.CastSpell(unit, ShamanSpells.WIND_RUSH, true);
        }
    }

    public void OnUpdate(uint diff)
    {
        _refreshTimer -= (int)diff;

        if (_refreshTimer <= 0)
        {
            var caster = At.GetCaster();

            if (caster != null)
                foreach (var guid in At.InsideUnits)
                {
                    var unit = Global.ObjAccessor.GetUnit(caster, guid);

                    if (unit != null)
                    {
                        if (!caster.IsFriendlyTo(unit))
                            continue;

                        caster.SpellFactory.CastSpell(unit, ShamanSpells.WIND_RUSH, true);
                    }
                }

            _refreshTimer += RefreshTime;
        }
    }
}