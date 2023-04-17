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

namespace Scripts.Spells.Monk;

[SpellScript(122470)]
public class SpellMonkTouchOfKarma : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 1));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster != null)
        {
            var effInfo = Aura.SpellInfo.GetEffect(2).CalcValue();

            if (Aura.SpellInfo.GetEffect(2).CalcValue() != 0)
            {
                amount.Value = caster.CountPctFromMaxHealth(effInfo);

                aurEff.SetAmount(amount);
            }
        }
    }

    private double OnAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return unnamedParameter;

        foreach (var aurApp in caster.GetAppliedAurasQuery().HasSpellId(MonkSpells.TOUCH_OF_KARMA).GetResults())
            if (aurApp.Target != caster)
            {
                var periodicDamage = dmgInfo.Damage / Global.SpellMgr.GetSpellInfo(MonkSpells.TOUCH_OF_KARMA_DAMAGE, Difficulty.None).MaxTicks;
                //  periodicDamage += int32(aurApp->GetTarget()->GetRemainingPeriodicAmount(GetCasterGUID(), TOUCH_OF_KARMA_DAMAGE, AuraType.PeriodicDamage));
                caster.SpellFactory.CastSpell(aurApp.Target, MonkSpells.TOUCH_OF_KARMA_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, periodicDamage).SetTriggeringAura(aurEff));

                if (caster.HasAura(MonkSpells.GOOD_KARMA_TALENT))
                    caster.SpellFactory.CastSpell(caster, MonkSpells.GOOD_KARMA_TALENT_HEAL, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, periodicDamage).SetTriggeringAura(aurEff));
            }

        return unnamedParameter;
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        Caster.SpellFactory.CastSpell(Caster, MonkSpells.TOUCH_OF_KARMA_BUFF, true);
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.RemoveAura(MonkSpells.TOUCH_OF_KARMA_BUFF);

        foreach (var aurApp in caster.GetAppliedAurasQuery().HasSpellId(MonkSpells.TOUCH_OF_KARMA).GetResults())
        {
            var targetAura = aurApp.Base;

            if (targetAura != null)
                targetAura.Remove();
        }
    }
}