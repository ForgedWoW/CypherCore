// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[Script] // 131347 - Glide
internal class SpellDhGlideAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.FeatherFall, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(DemonHunterSpells.GlideDuration);
    }
}