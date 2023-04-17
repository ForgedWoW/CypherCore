// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DeathKnight;

[Script]
public class AtDkUnholyAura : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster != null)
            if (!unit.IsFriendlyTo(caster))
                caster.SpellFactory.CastSpell(unit, DeathKnightSpells.UNHOLY_AURA, true);
    }

    public void OnUnitExit(Unit unit)
    {
        unit.RemoveAura(DeathKnightSpells.UNHOLY_AURA);
    }
}