// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 66020 Chains of Ice
internal class SpellGenChainsOfIce : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectUpdatePeriodicHandler(UpdatePeriodic, 1, AuraType.PeriodicDummy));
    }

    private void UpdatePeriodic(AuraEffect aurEff)
    {
        // Get 0 effect aura
        var slow = Aura.GetEffect(0);

        if (slow == null)
            return;

        var newAmount = Math.Min(slow.Amount + aurEff.Amount, 0);
        slow.ChangeAmount(newAmount);
    }
}