// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class AtMageCinderstorm : AreaTriggerScript, IAreaTriggerOnUnitEnter
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster != null)
            if (caster.IsValidAttackTarget(unit))
                caster.SpellFactory.CastSpell(unit, MageSpells.CINDERSTORM_DMG, true);
    }
}