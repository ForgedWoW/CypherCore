// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(204074)] // 204074 - Righteous Protector
internal class SpellPalRighteousProtector : AuraScript, IHasAuraEffects
{
    private SpellPowerCost _baseHolyPowerCost;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.SpellInfo;

        if (procSpell != null)
            _baseHolyPowerCost = procSpell.CalcPowerCost(PowerType.HolyPower, false, eventInfo.Actor, eventInfo.SchoolMask);
        else
            _baseHolyPowerCost = null;

        return _baseHolyPowerCost != null;
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var value = aurEff.Amount * 100 * _baseHolyPowerCost.Amount;

        Target.SpellHistory.ModifyCooldown(PaladinSpells.AVENGING_WRATH, TimeSpan.FromMilliseconds(-value));
        Target.SpellHistory.ModifyCooldown(PaladinSpells.GUARDIAN_OF_ACIENT_KINGS, TimeSpan.FromMilliseconds(-value));
    }
}