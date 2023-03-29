// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_meteor_burn : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        if (caster.IsValidAttackTarget(unit))
            caster.CastSpell(unit, MageSpells.METEOR_BURN, true);
    }

    public void OnUnitExit(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        var meteor = unit.GetAura(MageSpells.METEOR_BURN, caster.GUID);

        if (meteor != null)
            meteor.SetDuration(0);
    }
}