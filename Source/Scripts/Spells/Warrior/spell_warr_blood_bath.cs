// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// BloodBath - 12292
[SpellScript(12292)]
public class SpellWarrBloodBath : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleOnProc, 1, AuraType.None, AuraScriptHookType.EffectProc));
    }

    private void HandleOnProc(AuraEffect aurEff, ProcEventInfo pProcInfo)
    {
        PreventDefaultAction();

        if (pProcInfo?.DamageInfo?.SpellInfo == null)
            return;

        if (pProcInfo.DamageInfo.SpellInfo.Id == ESpells.BLOOD_BATH_DAMAGE)
            return;

        var lTarget = pProcInfo.ActionTarget;
        var lCaster = Caster;

        if (lTarget == null || lCaster == null || lTarget == lCaster)
            return;

        var lSpellInfo = Global.SpellMgr.GetSpellInfo(ESpells.BLOOD_BATH, Difficulty.None);
        var lSpellInfoDamage = Global.SpellMgr.GetSpellInfo(ESpells.BLOOD_BATH_DAMAGE, Difficulty.None);

        if (lSpellInfo == null || lSpellInfoDamage == null)
            return;

        var lDamage = MathFunctions.CalculatePct(pProcInfo.DamageInfo.Damage, aurEff.BaseAmount);

        double lPreviousTotalDamage = 0;

        var lPreviousBloodBath = lTarget.GetAuraEffect(ESpells.BLOOD_BATH_DAMAGE, 0, lCaster.GUID);

        if (lPreviousBloodBath != null)
        {
            var lPeriodicDamage = lPreviousBloodBath.Amount;
            var lDuration = lTarget.GetAura(ESpells.BLOOD_BATH_DAMAGE, lCaster.GUID).Duration;
            var lAmplitude = lPreviousBloodBath.GetSpellEffectInfo().Amplitude;

            if (lAmplitude != 0)
                lPreviousTotalDamage = lPeriodicDamage * ((lDuration / lAmplitude) + 1);

            lPreviousTotalDamage /= (lSpellInfoDamage.MaxDuration / lSpellInfoDamage.GetEffect(0).Amplitude);
        }

        if (lSpellInfoDamage.GetEffect(0).Amplitude != 0)
            lDamage /= (lSpellInfoDamage.MaxDuration / lSpellInfoDamage.GetEffect(0).Amplitude);

        lDamage += lPreviousTotalDamage;

        if (lTarget.HasAura(ESpells.BLOOD_BATH_DAMAGE, lCaster.GUID))
        {
            var lActualBloodBath = lTarget.GetAura(ESpells.BLOOD_BATH_DAMAGE, lCaster.GUID);

            if (lActualBloodBath != null)
                lActualBloodBath.SetDuration(lActualBloodBath.MaxDuration);
        }
        else
        {
            lCaster.SpellFactory.CastSpell(lTarget, ESpells.BLOOD_BATH_DAMAGE, true);
        }

        var lNewBloodBath = lTarget.GetAuraEffect(ESpells.BLOOD_BATH_DAMAGE, 0, lCaster.GUID);

        if (lNewBloodBath != null)
            lNewBloodBath.SetAmount((int)Math.Floor(lDamage));
    }

    private struct ESpells
    {
        public const uint BLOOD_BATH = 12292;
        public const uint BLOOD_BATH_DAMAGE = 113344;
    }
}