// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 53480 - Roar of Sacrifice
internal class spell_hun_roar_of_sacrifice : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 1, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo == null ||
            !Convert.ToBoolean((int)damageInfo.SchoolMask & aurEff.MiscValue))
            return false;

        if (!Caster)
            return false;

        return true;
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, aurEff.Amount));
        eventInfo.Actor.CastSpell(Caster, HunterSpells.RoarOfSacrificeTriggered, args);
    }
}