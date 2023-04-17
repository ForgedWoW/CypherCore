// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Druid;

[Script]
public class AtDruUrsolVortex : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    private bool _hasPull = false;

    public void OnUnitEnter(Unit target)
    {
        var caster = At.GetCaster();

        if (caster != null && caster.IsInCombatWith(target))
            caster.SpellFactory.CastSpell(target, DruidSpells.UrsolVortexDebuff, true);
    }

    public void OnUnitExit(Unit target)
    {
        target.RemoveAura(DruidSpells.UrsolVortexDebuff);

        if (!_hasPull && target.IsValidAttackTarget(At.GetCaster()))
        {
            _hasPull = true;
            target.SpellFactory.CastSpell(At.Location, DruidSpells.UrsolVortexPull, true);
        }
    }
}