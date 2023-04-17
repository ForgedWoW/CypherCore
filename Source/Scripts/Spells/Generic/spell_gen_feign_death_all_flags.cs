// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenFeignDeathAllFlags : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SetUnitFlag3(UnitFlags3.FakeDead);
        target.SetUnitFlag2(UnitFlags2.FeignDeath);
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);

        var creature = target.AsCreature;

        creature.ReactState = ReactStates.Passive;
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.RemoveUnitFlag3(UnitFlags3.FakeDead);
        target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);

        var creature = target.AsCreature;

        creature?.InitializeReactState();
    }
}