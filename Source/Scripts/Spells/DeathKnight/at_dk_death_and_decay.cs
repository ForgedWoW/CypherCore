﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DeathKnight;

[Script]
public class AtDkDeathAndDecay : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster != null)
            if (unit.GUID == caster.GUID)
                if (!caster.HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE))
                    caster.SpellFactory.CastSpell(unit, DeathKnightSpells.DEATH_AND_DECAY_CLEAVE, true);
    }

    public void OnUnitExit(Unit unit)
    {
        if (At.GetCaster().HasAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE))
            unit.RemoveAura(DeathKnightSpells.DEATH_AND_DECAY_CLEAVE, Game.Spells.AuraRemoveMode.Cancel);
    }
}