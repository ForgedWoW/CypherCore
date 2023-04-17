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

[Script("spell_item_lightning_capacitor", ItemSpellIds.LIGHTNING_CAPACITOR_STACK, ItemSpellIds.LIGHTNING_CAPACITOR_TRIGGER)]
[Script("spell_item_thunder_capacitor", ItemSpellIds.THUNDER_CAPACITOR_STACK, ItemSpellIds.THUNDER_CAPACITOR_TRIGGER)]
[Script("spell_item_toc25_normal_caster_trinket", ItemSpellIds.TOC25_CASTER_TRINKET_NORMAL_STACK, ItemSpellIds.TOC25_CASTER_TRINKET_NORMAL_TRIGGER)]
[Script("spell_item_toc25_heroic_caster_trinket", ItemSpellIds.TOC25_CASTER_TRINKET_HEROIC_STACK, ItemSpellIds.TOC25_CASTER_TRINKET_HEROIC_TRIGGER)]
internal class SpellItemTrinketStack : AuraScript, IHasAuraEffects
{
    private readonly uint _stackSpell;
    private readonly uint _triggerSpell;

    public SpellItemTrinketStack(uint stackSpell, uint triggerSpell)
    {
        _stackSpell = stackSpell;
        _triggerSpell = triggerSpell;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var caster = eventInfo.Actor;

        caster.SpellFactory.CastSpell(caster, _stackSpell, new CastSpellExtraArgs(aurEff)); // cast the stack

        var dummy = caster.GetAura(_stackSpell); // retrieve aura

        //dont do anything if it's not the right amount of stacks;
        if (dummy == null ||
            dummy.StackAmount < aurEff.Amount)
            return;

        // if right amount, Remove the aura and cast real trigger
        caster.RemoveAura(_stackSpell);
        var target = eventInfo.ActionTarget;

        if (target)
            caster.SpellFactory.CastSpell(target, _triggerSpell, new CastSpellExtraArgs(aurEff));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(_stackSpell);
    }
}