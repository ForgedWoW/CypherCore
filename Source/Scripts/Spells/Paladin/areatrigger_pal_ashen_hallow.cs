﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 19042 - Ashen Hallow
[Script]
internal class areatrigger_pal_ashen_hallow : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
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
            unit.CastSpell(unit, PaladinSpells.AshenHallowAllowHammer, true);
    }

    public void OnUnitExit(Unit unit)
    {
        if (unit.GUID == At.CasterGuid)
            unit.RemoveAura(PaladinSpells.AshenHallowAllowHammer);
    }

    public void OnUpdate(uint diff)
    {
        _refreshTimer -= TimeSpan.FromMilliseconds(diff);

        while (_refreshTimer <= TimeSpan.Zero)
        {
            var caster = At.GetCaster();

            if (caster != null)
            {
                caster.CastSpell(At.Location, PaladinSpells.AshenHallowHeal, new CastSpellExtraArgs());
                caster.CastSpell(At.Location, PaladinSpells.AshenHallowDamage, new CastSpellExtraArgs());
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
            var ashen = caster.GetAuraEffect(PaladinSpells.AshenHallow, 1);

            if (ashen != null)
                _period = TimeSpan.FromMilliseconds(ashen.Period);
        }
    }
}