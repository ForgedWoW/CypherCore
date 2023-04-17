// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Mage;

[SpellScript(190319)]
public class SpellMageCombustion : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.ModRating));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.ModRating, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void CalcAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (!caster.IsPlayer)
            return;

        var crit = caster.AsPlayer.GetRatingBonusValue(CombatRating.CritSpell);
        amount.Value += crit;
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        Caster.RemoveAura(MageSpells.INFERNO);
    }
}