// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 147066 - (Serverside/Non-DB2) Generic - Mount Check Aura
internal class SpellGenMountCheckAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        var target = Target;
        uint mountDisplayId = 0;

        var tempSummon = target.ToTempSummon();

        if (tempSummon == null)
            return;

        var summoner = tempSummon.Summoner?.AsPlayer;

        if (summoner == null)
            return;

        if (summoner.IsMounted &&
            (!summoner.IsInCombat || summoner.IsFlying))
        {
            var summonedData = Global.ObjectMgr.GetCreatureSummonedData(tempSummon.Entry);

            if (summonedData != null)
            {
                if (summoner.IsFlying &&
                    summonedData.FlyingMountDisplayId.HasValue)
                    mountDisplayId = summonedData.FlyingMountDisplayId.Value;
                else if (summonedData.GroundMountDisplayId.HasValue)
                    mountDisplayId = summonedData.GroundMountDisplayId.Value;
            }
        }

        if (mountDisplayId != target.MountDisplayId)
            target.MountDisplayId = mountDisplayId;
    }
}