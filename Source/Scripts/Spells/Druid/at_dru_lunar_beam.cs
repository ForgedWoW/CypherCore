// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Druid;

[Script]
public class AtDruLunarBeam : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnPeriodicProc
{
    public void OnCreate()
    {
        At.SetPeriodicProcTimer(1000);
    }

    public void OnPeriodicProc()
    {
        if (At.GetCaster())
            At.GetCaster().SpellFactory.CastSpell(At.Location, DruidSpells.LunarBeamDamageHeal, true);
    }
}