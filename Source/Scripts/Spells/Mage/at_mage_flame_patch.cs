// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script]
public class AtMageFlamePatch : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    public int TimeInterval;

    public void OnCreate()
    {
        TimeInterval = 1000;
    }

    public void OnUpdate(uint diff)
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        TimeInterval += (int)diff;

        if (TimeInterval < 1000)
            return;

        caster.SpellFactory.CastSpell(At.Location, MageSpells.FLAME_PATCH_AOE_DMG, true);

        TimeInterval -= 1000;
    }
}