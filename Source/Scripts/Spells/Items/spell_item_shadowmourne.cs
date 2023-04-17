// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 71903 - Item - Shadowmourne Legendary
internal class SpellItemShadowmourne : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (Target.HasAura(ItemSpellIds.SHADOWMOURNE_CHAOS_BANE_BUFF)) // cant collect shards while under effect of Chaos Bane buff
            return false;

        return eventInfo.ProcTarget && eventInfo.ProcTarget.IsAlive;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Target.SpellFactory.CastSpell(Target, ItemSpellIds.SHADOWMOURNE_SOUL_FRAGMENT, new CastSpellExtraArgs(aurEff));

        // this can't be handled in AuraScript of SoulFragments because we need to know victim
        var soulFragments = Target.GetAura(ItemSpellIds.SHADOWMOURNE_SOUL_FRAGMENT);

        if (soulFragments != null)
            if (soulFragments.StackAmount >= 10)
            {
                Target.SpellFactory.CastSpell(eventInfo.ProcTarget, ItemSpellIds.SHADOWMOURNE_CHAOS_BANE_DAMAGE, new CastSpellExtraArgs(aurEff));
                soulFragments.Remove();
            }
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(ItemSpellIds.SHADOWMOURNE_SOUL_FRAGMENT);
    }
}