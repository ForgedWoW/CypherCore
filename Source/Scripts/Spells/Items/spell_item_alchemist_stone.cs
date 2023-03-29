﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 17619 - Alchemist Stone
internal class spell_item_alchemist_stone : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.DamageInfo.SpellInfo.SpellFamilyName == SpellFamilyNames.Potion;
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId = 0;
        var amount = (int)(eventInfo.DamageInfo.Damage * 0.4f);

        if (eventInfo.DamageInfo.SpellInfo.HasEffect(SpellEffectName.Heal))
            spellId = ItemSpellIds.AlchemistStoneExtraHeal;
        else if (eventInfo.DamageInfo.SpellInfo.HasEffect(SpellEffectName.Energize))
            spellId = ItemSpellIds.AlchemistStoneExtraMana;

        if (spellId == 0)
            return;

        var caster = eventInfo.ActionTarget;
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.CastSpell((Unit)null, spellId, args);
    }
}