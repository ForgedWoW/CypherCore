// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_demon_hunter_demonic_trample : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.IsValidAttackTarget(unit))
        {
            caster.CastSpell(unit, DemonHunterSpells.DEMONIC_TRAMPLE_STUN, true);
            caster.CastSpell(unit, DemonHunterSpells.DEMONIC_TRAMPLE_DAMAGE, true);
        }
    }
}