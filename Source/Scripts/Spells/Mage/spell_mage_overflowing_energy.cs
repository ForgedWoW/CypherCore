// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(390218)]
public class spell_mage_overflowing_energy : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == 390218)
            return false;

        if ((eventInfo.HitMask & ProcFlagsHit.Critical) != 0)
            return false;

        if (eventInfo.DamageInfo != null)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ModCritChanceForCaster, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var amount = aurEff.Amount;

        if (eventInfo.DamageInfo.SpellInfo.Id == 390218)
            amount = 0;

        var target = Target;

        Target.CastSpell(target, 390218, new CastSpellExtraArgs(SpellValueMod.AuraStack, 5));
    }
}