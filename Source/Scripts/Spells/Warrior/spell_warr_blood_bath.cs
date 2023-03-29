﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior;

// BloodBath - 12292
[SpellScript(12292)]
public class spell_warr_blood_bath : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleOnProc, 1, AuraType.None, AuraScriptHookType.EffectProc));
    }

    private void HandleOnProc(AuraEffect aurEff, ProcEventInfo p_ProcInfo)
    {
        PreventDefaultAction();

        if (p_ProcInfo?.DamageInfo?.SpellInfo == null)
            return;

        if (p_ProcInfo.DamageInfo.SpellInfo.Id == eSpells.BLOOD_BATH_DAMAGE)
            return;

        var l_Target = p_ProcInfo.ActionTarget;
        var l_Caster = Caster;

        if (l_Target == null || l_Caster == null || l_Target == l_Caster)
            return;

        var l_SpellInfo = Global.SpellMgr.GetSpellInfo(eSpells.BLOOD_BATH, Difficulty.None);
        var l_SpellInfoDamage = Global.SpellMgr.GetSpellInfo(eSpells.BLOOD_BATH_DAMAGE, Difficulty.None);

        if (l_SpellInfo == null || l_SpellInfoDamage == null)
            return;

        var l_Damage = MathFunctions.CalculatePct(p_ProcInfo.DamageInfo.Damage, aurEff.BaseAmount);

        double l_PreviousTotalDamage = 0;

        var l_PreviousBloodBath = l_Target.GetAuraEffect(eSpells.BLOOD_BATH_DAMAGE, 0, l_Caster.GUID);

        if (l_PreviousBloodBath != null)
        {
            var l_PeriodicDamage = l_PreviousBloodBath.Amount;
            var l_Duration = l_Target.GetAura(eSpells.BLOOD_BATH_DAMAGE, l_Caster.GUID).Duration;
            var l_Amplitude = l_PreviousBloodBath.GetSpellEffectInfo().Amplitude;

            if (l_Amplitude != 0)
                l_PreviousTotalDamage = l_PeriodicDamage * ((l_Duration / l_Amplitude) + 1);

            l_PreviousTotalDamage /= (l_SpellInfoDamage.MaxDuration / l_SpellInfoDamage.GetEffect(0).Amplitude);
        }

        if (l_SpellInfoDamage.GetEffect(0).Amplitude != 0)
            l_Damage /= (l_SpellInfoDamage.MaxDuration / l_SpellInfoDamage.GetEffect(0).Amplitude);

        l_Damage += l_PreviousTotalDamage;

        if (l_Target.HasAura(eSpells.BLOOD_BATH_DAMAGE, l_Caster.GUID))
        {
            var l_ActualBloodBath = l_Target.GetAura(eSpells.BLOOD_BATH_DAMAGE, l_Caster.GUID);

            if (l_ActualBloodBath != null)
                l_ActualBloodBath.SetDuration(l_ActualBloodBath.MaxDuration);
        }
        else
        {
            l_Caster.CastSpell(l_Target, eSpells.BLOOD_BATH_DAMAGE, true);
        }

        var l_NewBloodBath = l_Target.GetAuraEffect(eSpells.BLOOD_BATH_DAMAGE, 0, l_Caster.GUID);

        if (l_NewBloodBath != null)
            l_NewBloodBath.SetAmount((int)Math.Floor(l_Damage));
    }

    private struct eSpells
    {
        public const uint BLOOD_BATH = 12292;
        public const uint BLOOD_BATH_DAMAGE = 113344;
    }
}