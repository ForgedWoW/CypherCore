// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(5384)]
public class SpellHunFeignDeath : AuraScript, IHasAuraEffects
{
    private long _health;
    private int _focus;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.FeignDeath, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.FeignDeath, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleEffectApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        _health = Target.Health;
        _focus = Target.GetPower(PowerType.Focus);
    }

    private void HandleEffectRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        if (_health != 0 && _focus != 0)
        {
            Target.SetHealth(_health);
            Target.SetPower(PowerType.Focus, _focus);
        }
    }
}