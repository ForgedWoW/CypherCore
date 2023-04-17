// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class AtDemonHunterDemonicTrample : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.IsValidAttackTarget(unit))
        {
            caster.SpellFactory.CastSpell(unit, DemonHunterSpells.DEMONIC_TRAMPLE_STUN, true);
            caster.SpellFactory.CastSpell(unit, DemonHunterSpells.DEMONIC_TRAMPLE_DAMAGE, true);
        }
    }
}