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

[Script("spell_item_tiny_abomination_in_a_jar", 8)]
[Script("spell_item_tiny_abomination_in_a_jar_hero", 7)]
internal class SpellItemAngerCapacitor : AuraScript, IHasAuraEffects
{
    private readonly int _stackAmount;

    public SpellItemAngerCapacitor(int stackAmount)
    {
        _stackAmount = stackAmount;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        caster.SpellFactory.CastSpell((Unit)null, ItemSpellIds.MOTE_OF_ANGER, true);
        var motes = caster.GetAura(ItemSpellIds.MOTE_OF_ANGER);

        if (motes == null ||
            motes.StackAmount < _stackAmount)
            return;

        caster.RemoveAura(ItemSpellIds.MOTE_OF_ANGER);
        var spellId = ItemSpellIds.MANIFEST_ANGER_MAIN_HAND;
        var player = caster.AsPlayer;

        if (player)
            if (player.GetWeaponForAttack(WeaponAttackType.OffAttack, true) &&
                RandomHelper.URand(0, 1) != 0)
                spellId = ItemSpellIds.MANIFEST_ANGER_OFF_HAND;

        caster.SpellFactory.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(ItemSpellIds.MOTE_OF_ANGER);
    }
}