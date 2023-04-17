// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Monk;

[SpellScript(125883)]
public class SpellMonkZenFlightCheckAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        return Caster && Caster.TypeId == TypeId.Player;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        if (!Caster)
            return;

        var caster = Caster.AsPlayer;

        if (caster != null)
            if (caster.GetSkillValue(SkillType.Riding) >= 375)
                amount.Value = 310;
    }
}