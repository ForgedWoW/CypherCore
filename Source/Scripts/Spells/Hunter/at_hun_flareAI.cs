// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script]
public class AtHunFlareAI : AreaTriggerScript, IAreaTriggerOnCreate
{
    public void OnCreate()
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(200));

        if (tempSumm == null)
        {
            tempSumm.Faction = caster.Faction;
            tempSumm.SetSummonerGUID(caster.GUID);
            PhasingHandler.InheritPhaseShift(tempSumm, caster);
            caster.SpellFactory.CastSpell(tempSumm, HunterSpells.FLARE_EFFECT, true);
        }
    }
}