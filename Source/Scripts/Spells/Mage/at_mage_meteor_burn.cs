// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script]
public class AtMageMeteorBurn : AreaTriggerScript, IAreaTriggerOnUnitEnter, IAreaTriggerOnUnitExit
{
    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        if (caster.IsValidAttackTarget(unit))
            caster.SpellFactory.CastSpell(unit, MageSpells.METEOR_BURN, true);
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