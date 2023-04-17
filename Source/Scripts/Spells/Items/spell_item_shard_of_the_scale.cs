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

[Script("spell_item_purified_shard_of_the_scale", ItemSpellIds.PURIFIED_CAUTERIZING_HEAL, ItemSpellIds.PURIFIED_SEARING_FLAMES)]
[Script("spell_item_shiny_shard_of_the_scale", ItemSpellIds.SHINY_CAUTERIZING_HEAL, ItemSpellIds.SHINY_SEARING_FLAMES)]
internal class SpellItemShardOfTheScale : AuraScript, IHasAuraEffects
{
    private readonly uint _damageProcSpellId;

    private readonly uint _healProcSpellId;

    public SpellItemShardOfTheScale(uint healProcSpellId, uint damageProcSpellId)
    {
        _healProcSpellId = healProcSpellId;
        _damageProcSpellId = damageProcSpellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        if (eventInfo.TypeMask.HasFlag(ProcFlags.DealHelpfulSpell))
            caster.SpellFactory.CastSpell(target, _healProcSpellId, new CastSpellExtraArgs(aurEff));

        if (eventInfo.TypeMask.HasFlag(ProcFlags.DealHarmfulSpell))
            caster.SpellFactory.CastSpell(target, _damageProcSpellId, new CastSpellExtraArgs(aurEff));
    }
}