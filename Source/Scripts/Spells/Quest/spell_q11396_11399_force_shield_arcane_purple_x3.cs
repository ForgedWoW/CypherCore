// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 43874 - Scourge Mur'gul Camp: Force Shield Arcane Purple x3
internal class SpellQ1139611399ForceShieldArcanePurpleX3 : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SetImmuneToPC(true);
        target.AddUnitState(UnitState.Root);
    }

    private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.SetImmuneToPC(false);
    }
}