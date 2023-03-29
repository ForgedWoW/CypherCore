// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_lightning_capacitor", ItemSpellIds.LightningCapacitorStack, ItemSpellIds.LightningCapacitorTrigger)]
[Script("spell_item_thunder_capacitor", ItemSpellIds.ThunderCapacitorStack, ItemSpellIds.ThunderCapacitorTrigger)]
[Script("spell_item_toc25_normal_caster_trinket", ItemSpellIds.Toc25CasterTrinketNormalStack, ItemSpellIds.Toc25CasterTrinketNormalTrigger)]
[Script("spell_item_toc25_heroic_caster_trinket", ItemSpellIds.Toc25CasterTrinketHeroicStack, ItemSpellIds.Toc25CasterTrinketHeroicTrigger)]
internal class spell_item_trinket_stack : AuraScript, IHasAuraEffects
{
    private readonly uint _stackSpell;
    private readonly uint _triggerSpell;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public spell_item_trinket_stack(uint stackSpell, uint triggerSpell)
    {
        _stackSpell = stackSpell;
        _triggerSpell = triggerSpell;
    }


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var caster = eventInfo.Actor;

        caster.CastSpell(caster, _stackSpell, new CastSpellExtraArgs(aurEff)); // cast the stack

        var dummy = caster.GetAura(_stackSpell); // retrieve aura

        //dont do anything if it's not the right amount of stacks;
        if (dummy == null ||
            dummy.StackAmount < aurEff.Amount)
            return;

        // if right amount, Remove the aura and cast real trigger
        caster.RemoveAura(_stackSpell);
        var target = eventInfo.ActionTarget;

        if (target)
            caster.CastSpell(target, _triggerSpell, new CastSpellExtraArgs(aurEff));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(_stackSpell);
    }
}