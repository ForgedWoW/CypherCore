// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.Spells.Paladin;

[SpellScript(54149)] // 54149 - Infusion of Light
internal class SpellPalInfusionOfLight : AuraScript, IHasAuraEffects
{
    private static readonly FlagArray128 HolyLightSpellClassMask = new(0, 0, 0x400);
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckFlashOfLightProc, 0, AuraType.AddPctModifier));
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckFlashOfLightProc, 2, AuraType.AddFlatModifier));

        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckHolyLightProc, 1, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckFlashOfLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.ProcSpell && eventInfo.ProcSpell.AppliedMods.Contains(Aura);
    }

    private bool CheckHolyLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo != null && eventInfo.SpellInfo.IsAffected(SpellFamilyNames.Paladin, HolyLightSpellClassMask);
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        eventInfo.Actor
                 .SpellFactory.CastSpell(eventInfo.Actor,
                                         PaladinSpells.INFUSION_OF_LIGHT_ENERGIZE,
                                         new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(eventInfo.ProcSpell));
    }
}