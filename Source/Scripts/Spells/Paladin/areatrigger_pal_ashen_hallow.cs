// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;

namespace Scripts.Spells.Paladin;

// 19042 - Ashen Hallow
[Script]
internal class AreatriggerPalAshenHallow : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    private TimeSpan _period;
    private TimeSpan _refreshTimer;

    public void OnCreate()
    {
        RefreshPeriod();
        _refreshTimer = _period;
    }

    public void OnUnitEnter(Unit unit)
    {
        if (unit.GUID == At.CasterGuid)
            unit.SpellFactory.CastSpell(unit, PaladinSpells.ASHEN_HALLOW_ALLOW_HAMMER, true);
    }

    public void OnUnitExit(Unit unit)
    {
        if (unit.GUID == At.CasterGuid)
            unit.RemoveAura(PaladinSpells.ASHEN_HALLOW_ALLOW_HAMMER);
    }

    public void OnUpdate(uint diff)
    {
        _refreshTimer -= TimeSpan.FromMilliseconds(diff);

        while (_refreshTimer <= TimeSpan.Zero)
        {
            var caster = At.GetCaster();

            if (caster != null)
            {
                caster.SpellFactory.CastSpell(At.Location, PaladinSpells.ASHEN_HALLOW_HEAL, new CastSpellExtraArgs());
                caster.SpellFactory.CastSpell(At.Location, PaladinSpells.ASHEN_HALLOW_DAMAGE, new CastSpellExtraArgs());
            }

            RefreshPeriod();

            _refreshTimer += _period;
        }
    }

    private void RefreshPeriod()
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            var ashen = caster.GetAuraEffect(PaladinSpells.ASHEN_HALLOW, 1);

            if (ashen != null)
                _period = TimeSpan.FromMilliseconds(ashen.Period);
        }
    }
}