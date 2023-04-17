// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(14062)]
public class SpellRogNightstalkerAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster)
        {
            if (caster.HasAura(RogueSpells.NIGHTSTALKER_DAMAGE_DONE))
                caster.RemoveAura(RogueSpells.NIGHTSTALKER_DAMAGE_DONE);

            if (caster.HasAura(RogueSpells.SHADOW_FOCUS_EFFECT))
                caster.RemoveAura(RogueSpells.SHADOW_FOCUS_EFFECT);
        }
    }
}