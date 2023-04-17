// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 24858 - Moonkin Form
internal class SpellDruGlyphOfStars : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        if (target.HasAura(DruidSpellIds.GlyphOfStars))
            target.SpellFactory.CastSpell(target, DruidSpellIds.GlyphOfStarsVisual, true);
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(DruidSpellIds.GlyphOfStarsVisual);
    }
}