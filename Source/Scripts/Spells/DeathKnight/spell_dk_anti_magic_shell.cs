// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DeathKnight;

[Script] // 48707 - Anti-Magic Shell
internal class SpellDkAntiMagicShell : AuraScript, IHasAuraEffects
{
    private double _absorbedAmount;
    private double _absorbPct;
    private long _maxHealth;

    public SpellDkAntiMagicShell()
    {
        _absorbPct = 0;
        _maxHealth = 0;
        _absorbedAmount = 0;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        _absorbPct = GetEffectInfo(1).CalcValue(Caster);
        _maxHealth = Caster.MaxHealth;
        _absorbedAmount = 0;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Trigger, 0, false, AuraScriptHookType.EffectAfterAbsorb));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = MathFunctions.CalculatePct(_maxHealth, _absorbPct);
    }

    private double Trigger(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        _absorbedAmount += absorbAmount;

        if (!Target.HasAura(DeathKnightSpells.VOLATILE_SHIELDING))
        {
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(absorbAmount, 2 * absorbAmount * 100 / _maxHealth));
            Target.SpellFactory.CastSpell(Target, DeathKnightSpells.RunicPowerEnergize, args);
        }

        return absorbAmount;
    }

    private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var volatileShielding = Target.GetAuraEffect(DeathKnightSpells.VOLATILE_SHIELDING, 1);

        if (volatileShielding != null)
        {
            CastSpellExtraArgs args = new(volatileShielding);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(_absorbedAmount, volatileShielding.Amount));
            Target.SpellFactory.CastSpell((Unit)null, DeathKnightSpells.VOLATILE_SHIELDING_DAMAGE, args);
        }
    }
}