// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BLACK_LANDSLIDE_ROOT)]
public class AtEvokerLandslideRoot : AreaTriggerScript, IAreaTriggerOnCreate
{
    public void OnCreate()
    {
        At.SetDuration(500);
    }
}