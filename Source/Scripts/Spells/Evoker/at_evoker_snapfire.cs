// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.RED_FIRE_STORM)]
public class AtEvokerSnapfire : AreaTriggerScript, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        if (At.GetCaster().TryGetAura(EvokerSpells.SNAPFIRE_AURA, out var aura))
            aura.Remove();
    }
}