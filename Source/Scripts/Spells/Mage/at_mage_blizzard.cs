// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script]
public class AtMageBlizzard : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    public int TimeInterval;

    public void OnCreate()
    {
        TimeInterval = 1000;
        At.SetDuration(8000);
    }

    public void OnUpdate(uint diff)
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.IsPlayer)
            return;

        TimeInterval += (int)diff;

        if (TimeInterval < 1000)
            return;

        var tempSumm = caster.SummonCreature(12999, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(8100));

        {
            tempSumm.Faction = caster.Faction;
            tempSumm.SetSummonerGUID(caster.GUID);
            PhasingHandler.InheritPhaseShift(tempSumm, caster);
            caster.SpellFactory.CastSpell(tempSumm, UsingSpells.BLIZZARD_DAMAGE, true);
        }

        TimeInterval -= 1000;
    }

    public struct UsingSpells
    {
        public const uint BLIZZARD_DAMAGE = 190357;
    }
}