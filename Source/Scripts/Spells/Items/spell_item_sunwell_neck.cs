﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_sunwell_exalted_caster_neck", ItemSpellIds.LightsWrath, ItemSpellIds.ArcaneBolt)]
[Script("spell_item_sunwell_exalted_melee_neck", ItemSpellIds.LightsStrength, ItemSpellIds.ArcaneStrike)]
[Script("spell_item_sunwell_exalted_tank_neck", ItemSpellIds.LightsWard, ItemSpellIds.ArcaneInsight)]
[Script("spell_item_sunwell_exalted_healer_neck", ItemSpellIds.LightsSalvation, ItemSpellIds.ArcaneSurge)]
internal class spell_item_sunwell_neck : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    private readonly uint _aldorSpellId;
    private readonly uint _scryersSpellId;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public spell_item_sunwell_neck(uint aldorSpellId, uint scryersSpellId)
    {
        _aldorSpellId = aldorSpellId;
        _scryersSpellId = scryersSpellId;
    }


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
        if (player.GetReputationRank(FactionIds.Aldor) == ReputationRank.Exalted)
            player.CastSpell(target, _aldorSpellId, new CastSpellExtraArgs(aurEff));

        if (player.GetReputationRank(FactionIds.Scryers) == ReputationRank.Exalted)
            player.CastSpell(target, _scryersSpellId, new CastSpellExtraArgs(aurEff));
    }
}