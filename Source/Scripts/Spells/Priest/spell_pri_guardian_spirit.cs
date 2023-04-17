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

namespace Scripts.Spells.Priest;

[Script] // 47788 - Guardian Spirit
internal class SpellPriGuardianSpirit : AuraScript, IHasAuraEffects
{
    private uint _healPct;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        _healPct = (uint)GetEffectInfo(1).CalcValue();

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1, false, AuraScriptHookType.EffectAbsorb));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        // Set absorbtion amount to unlimited
        amount.Value = -1;
    }

    private double Absorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        var target = Target;

        if (dmgInfo.Damage < target.Health)
            return absorbAmount;

        var healAmount = (int)target.CountPctFromMaxHealth((int)_healPct);
        // Remove the aura now, we don't want 40% healing bonus
        Remove(AuraRemoveMode.EnemySpell);
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, healAmount);
        target.SpellFactory.CastSpell(target, PriestSpells.GUARDIAN_SPIRIT_HEAL, args);

        return dmgInfo.Damage;
    }
}