// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Monk;

[Script]
public class AtMonkGiftOfTheOxSphere : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate, IAreaTriggerOnUnitEnter, IAreaTriggerOnRemove
{
    public enum SpellsUsed
    {
        GiftOfTheOxHeal = 178173,
        HealingSphereCooldown = 224863
    }

    public uint PickupDelay;

    public void OnCreate()
    {
        PickupDelay = 1000;
    }

    public void OnRemove()
    {
        //Todo : Remove cooldown
        var caster = At.GetCaster();

        if (caster != null)
            if (caster.HasAura(SpellsUsed.HealingSphereCooldown))
                caster.RemoveAura(SpellsUsed.HealingSphereCooldown);
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster != null)
            if (unit == caster && PickupDelay == 0)
            {
                caster.SpellFactory.CastSpell(caster, SpellsUsed.GiftOfTheOxHeal, true);
                At.Remove();
            }
    }

    public void OnUpdate(uint diff)
    {
        if (PickupDelay >= diff)
            PickupDelay -= diff;
        else
            PickupDelay = 0;
    }
}