// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunTarTrapNotActivatedAI : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUnitEnter
{
    public enum UsedSpells
    {
        ActivateTarTrap = 187700
    }

    public int TimeInterval;

    public void OnCreate()
    {
        TimeInterval = 200;
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.AsPlayer)
            return;

        foreach (var itr in At.InsideUnits)
        {
            var target = ObjectAccessor.Instance.GetUnit(caster, itr);

            if (!caster.IsFriendlyTo(target))
            {
                var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

                if (tempSumm != null)
                {
                    tempSumm.Faction = caster.Faction;
                    tempSumm.SetSummonerGUID(caster.GUID);
                    PhasingHandler.InheritPhaseShift(tempSumm, caster);
                    caster.SpellFactory.CastSpell(tempSumm, UsedSpells.ActivateTarTrap, true);
                    At.Remove();
                }
            }
        }
    }

    public void OnUnitEnter(Unit unit)
    {
        var caster = At.GetCaster();

        if (caster == null || unit == null)
            return;

        if (!caster.AsPlayer)
            return;

        if (!caster.IsFriendlyTo(unit))
        {
            var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(1));

            if (tempSumm != null)
            {
                tempSumm.Faction = caster.Faction;
                tempSumm.SetSummonerGUID(caster.GUID);
                PhasingHandler.InheritPhaseShift(tempSumm, caster);
                caster.SpellFactory.CastSpell(tempSumm, UsedSpells.ActivateTarTrap, true);
                At.Remove();
            }
        }
    }
}