// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Druid;

[Script]
public class AtDruStarfall : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnPeriodicProc
{
    public int TimeInterval;

    public void OnCreate()
    {
        // How often should the action be executed
        At.SetPeriodicProcTimer(850);
    }

    public void OnPeriodicProc()
    {
        var caster = At.GetCaster();

        if (caster != null)
            foreach (var objguid in At.InsideUnits)
            {
                var unit = ObjectAccessor.Instance.GetUnit(caster, objguid);

                if (unit != null)
                    if (caster.IsValidAttackTarget(unit))
                        if (unit.IsInCombat)
                        {
                            caster.SpellFactory.CastSpell(unit, StarfallSpells.STARFALL_DAMAGE, true);
                            caster.SpellFactory.CastSpell(unit, StarfallSpells.STELLAR_EMPOWERMENT, true);
                        }
            }
    }
}