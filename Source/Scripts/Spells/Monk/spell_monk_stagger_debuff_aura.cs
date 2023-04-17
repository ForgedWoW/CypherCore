// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script] // 124273, 124274, 124275 - Light/Moderate/Heavy Stagger - STAGGER_LIGHT / STAGGER_MODERATE / STAGGER_HEAVY
internal class SpellMonkStaggerDebuffAura : AuraScript, IHasAuraEffects
{
    private double _period;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        _period = (double)Global.SpellMgr.GetSpellInfo(MonkSpells.STAGGER_DAMAGE_AURA, CastDifficulty).GetEffect(0).ApplyAuraPeriod;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnReapply, 1, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnReapply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Calculate Damage per tick
        var total = aurEff.Amount;
        var perTick = total * _period / (double)Duration; // should be same as GetMaxDuration() TODO: verify

        // Set amount on effect for tooltip
        var effInfo = Aura.GetEffect(0);

        effInfo?.ChangeAmount((int)perTick);

        // Set amount on Damage aura (or cast it if needed)
        CastOrChangeTickDamage(perTick);
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (mode != AuraEffectHandleModes.Real)
            return;

        // Remove Damage aura
        Target.RemoveAura(MonkSpells.STAGGER_DAMAGE_AURA);
    }

    private void CastOrChangeTickDamage(double tickDamage)
    {
        var unit = Target;
        var auraDamage = unit.GetAura(MonkSpells.STAGGER_DAMAGE_AURA);

        if (auraDamage == null)
        {
            unit.SpellFactory.CastSpell(unit, MonkSpells.STAGGER_DAMAGE_AURA, true);
            auraDamage = unit.GetAura(MonkSpells.STAGGER_DAMAGE_AURA);
        }

        if (auraDamage != null)
        {
            var eff = auraDamage.GetEffect(0);

            eff?.ChangeAmount((int)tickDamage);
        }
    }
}