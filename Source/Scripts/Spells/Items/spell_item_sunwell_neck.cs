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

[Script("spell_item_sunwell_exalted_caster_neck", ItemSpellIds.LIGHTS_WRATH, ItemSpellIds.ARCANE_BOLT)]
[Script("spell_item_sunwell_exalted_melee_neck", ItemSpellIds.LIGHTS_STRENGTH, ItemSpellIds.ARCANE_STRIKE)]
[Script("spell_item_sunwell_exalted_tank_neck", ItemSpellIds.LIGHTS_WARD, ItemSpellIds.ARCANE_INSIGHT)]
[Script("spell_item_sunwell_exalted_healer_neck", ItemSpellIds.LIGHTS_SALVATION, ItemSpellIds.ARCANE_SURGE)]
internal class SpellItemSunwellNeck : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    private readonly uint _aldorSpellId;
    private readonly uint _scryersSpellId;

    public SpellItemSunwellNeck(uint aldorSpellId, uint scryersSpellId)
    {
        _aldorSpellId = aldorSpellId;
        _scryersSpellId = scryersSpellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.Actor.TypeId != TypeId.Player)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var player = eventInfo.Actor.AsPlayer;
        var target = eventInfo.ProcTarget;

        // Aggression checks are in the spell system... just cast and forget
        if (player.GetReputationRank(FactionIds.ALDOR) == ReputationRank.Exalted)
            player.SpellFactory.CastSpell(target, _aldorSpellId, new CastSpellExtraArgs(aurEff));

        if (player.GetReputationRank(FactionIds.SCRYERS) == ReputationRank.Exalted)
            player.SpellFactory.CastSpell(target, _scryersSpellId, new CastSpellExtraArgs(aurEff));
    }
}