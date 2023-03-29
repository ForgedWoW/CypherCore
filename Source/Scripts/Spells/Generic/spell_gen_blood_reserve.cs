// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // Blood Reserve - 64568
internal class spell_gen_blood_reserve : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = eventInfo.ActionTarget;

        if (caster != null)
            if (caster.HealthBelowPct(35))
                return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var caster = eventInfo.ActionTarget;
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.Amount);
        caster.CastSpell(caster, GenericSpellIds.BloodReserveHeal, args);
        caster.RemoveAura(GenericSpellIds.BloodReserveAura);
    }
}