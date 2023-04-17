// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 260878 - Spirit Wolf
[SpellScript(260878)]
internal class SpellShaSpiritWolf : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        if (target.HasAura(ShamanSpells.SPIRIT_WOLF_TALENT) &&
            target.HasAura(ShamanSpells.GhostWolf))
            target.SpellFactory.CastSpell(target, ShamanSpells.SPIRIT_WOLF_PERIODIC, new CastSpellExtraArgs(aurEff));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(ShamanSpells.SPIRIT_WOLF_PERIODIC);
        Target.RemoveAura(ShamanSpells.SPIRIT_WOLF_AURA);
    }
}