// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(232698)]
public class SpellPriShadowform : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.RealOrReapplyMask));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleEffectApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        Target.SpellFactory.CastSpell(Target, Target.HasAura(PriestSpells.GLYPH_OF_SHADOW) ? PriestSpells.SHADOWFORM_VISUAL_WITH_GLYPH : PriestSpells.SHADOWFORM_VISUAL_WITHOUT_GLYPH, true);
    }

    private void HandleEffectRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        Target.RemoveAura(Target.HasAura(PriestSpells.GLYPH_OF_SHADOW) ? PriestSpells.SHADOWFORM_VISUAL_WITH_GLYPH : PriestSpells.SHADOWFORM_VISUAL_WITHOUT_GLYPH);
    }
}