// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//AT id : 3691
//Spell ID : 61882
[Script]
public class AtShaEarthquakeTotem : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    public int TimeInterval;

    public void OnCreate()
    {
        TimeInterval = 200;
    }

    public void OnUpdate(uint pTime)
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (!caster.AsPlayer)
            return;

        // Check if we can handle actions
        TimeInterval += (int)pTime;

        if (TimeInterval < 1000)
            return;

        var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(200));

        if (tempSumm != null)
        {
            tempSumm.Faction = caster.Faction;
            tempSumm.SetSummonerGUID(caster.GUID);
            PhasingHandler.InheritPhaseShift(tempSumm, caster);

            tempSumm.SpellFactory.CastSpell(caster,
                               UsedSpells.EARTHQUAKE_DAMAGE,
                               new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                   .AddSpellMod(SpellValueMod.BasePoint0, (int)(caster.GetTotalSpellPowerValue(SpellSchoolMask.Normal, false) * 0.3)));
        }

        TimeInterval -= 1000;
    }

    public struct UsedSpells
    {
        public const uint EARTHQUAKE_DAMAGE = 77478;
        public const uint EARTHQUAKE_STUN = 77505;
    }
}