// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenReplenishmentAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        return OwnerAsUnit.GetPower(PowerType.Mana) != 0;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicEnergize));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        switch (SpellInfo.Id)
        {
            case GenericSpellIds.REPLENISHMENT:
                amount.Value = (OwnerAsUnit.GetMaxPower(PowerType.Mana) * 0.002f);

                break;
            case GenericSpellIds.INFINITE_REPLENISHMENT:
                amount.Value = (OwnerAsUnit.GetMaxPower(PowerType.Mana) * 0.0025f);

                break;
        }
    }
}