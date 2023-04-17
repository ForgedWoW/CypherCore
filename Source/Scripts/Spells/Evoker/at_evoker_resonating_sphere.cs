// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BRONZE_TEMPORAL_ANOMALY)]
public class AtEvokerResonatingSphere : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    readonly List<Unit> _hit = new();
    double _amount = 0;
    int _targets = 0;

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
            var args = new CastSpellExtraArgs(true);
            args.TriggeringAura = aura.GetEffect(0);
            caster.SpellFactory.CastSpell(unit, EvokerSpells.ECHO, args);
        }
    }
}