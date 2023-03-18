// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BRONZE_TEMPORAL_ANOMALY)]
public class at_evoker_resonating_sphere : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    double _amount = 0;
    int _targets = 0;
    List<Unit> _hit = new();

    public void OnCreate()
    {
        if (!At.GetCaster().TryGetAura(EvokerSpells.RESONATING_SPHERE, out var aura))
            return;

        _amount = aura.GetEffect(0).Amount;
        _targets = (int)aura.GetEffect(1).Amount;
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster().AsPlayer;

        if (!At.GetCaster().TryGetAura(EvokerSpells.RESONATING_SPHERE, out var aura))
            return;

        if (caster.IsFriendlyTo(unit) && _hit.Count < _targets && !_hit.Contains(unit))
        {
            _hit.Add(unit);
            CastSpellExtraArgs args = new CastSpellExtraArgs(true);
            args.TriggeringAura = aura.GetEffect(0);
            caster.CastSpell(unit, EvokerSpells.ECHO, args);
        }
    }
}