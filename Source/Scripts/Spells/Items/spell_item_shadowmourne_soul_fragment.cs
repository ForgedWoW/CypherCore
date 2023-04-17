// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 71905 - Soul Fragment
internal class SpellItemShadowmourneSoulFragment : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnStackChange, 0, AuraType.ModStat, AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStat, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnStackChange(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        switch (StackAmount)
        {
            case 1:
                target.SpellFactory.CastSpell(target, ItemSpellIds.SHADOWMOURNE_VISUAL_LOW, true);

                break;
            case 6:
                target.RemoveAura(ItemSpellIds.SHADOWMOURNE_VISUAL_LOW);
                target.SpellFactory.CastSpell(target, ItemSpellIds.SHADOWMOURNE_VISUAL_HIGH, true);

                break;
            case 10:
                target.RemoveAura(ItemSpellIds.SHADOWMOURNE_VISUAL_HIGH);
                target.SpellFactory.CastSpell(target, ItemSpellIds.SHADOWMOURNE_CHAOS_BANE_BUFF, true);

                break;
        }
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.RemoveAura(ItemSpellIds.SHADOWMOURNE_VISUAL_LOW);
        target.RemoveAura(ItemSpellIds.SHADOWMOURNE_VISUAL_HIGH);
    }
}