// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Evoker;

[AreaTriggerScript(EvokerAreaTriggers.BLACK_LANDSLIDE_ROOT)]
public class at_evoker_landslide_root : AreaTriggerScript, IAreaTriggerOnCreate
{
    public void OnCreate()
    {
        At.SetDuration(500);
    }
}