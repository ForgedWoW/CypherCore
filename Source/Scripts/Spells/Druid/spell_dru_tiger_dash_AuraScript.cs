// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 252216 - Tiger Dash (Aura)
internal class SpellDruTigerDashAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
    }

    private void HandlePeriodic(AuraEffect aurEff)
    {
        var effRunSpeed = GetEffect(0);

        if (effRunSpeed != null)
        {
            var reduction = aurEff.Amount;
            effRunSpeed.ChangeAmount(effRunSpeed.Amount - reduction);
        }
    }
}