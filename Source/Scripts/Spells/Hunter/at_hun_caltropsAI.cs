// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunCaltropsAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    public enum UsedSpells
    {
        CaltropsAura = 194279
    }

    public int TimeInterval;

    public void OnCreate()
    {
        // How often should the action be executed
        TimeInterval = 1000;
    }

    public void OnUpdate(uint pTime)
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        // Check if we can handle actions
        TimeInterval += (int)pTime;

        if (TimeInterval < 1000)
            return;

        foreach (var guid in At.InsideUnits)
        {
            var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

            if (unit != null)
                if (!caster.IsFriendlyTo(unit))
                    caster.SpellFactory.CastSpell(unit, UsedSpells.CaltropsAura, true);
        }

        TimeInterval -= 1000;
    }
}