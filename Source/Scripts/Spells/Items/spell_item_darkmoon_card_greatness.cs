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

[Script] // 57345 - Darkmoon Card: Greatness
internal class SpellItemDarkmoonCardGreatness : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var caster = eventInfo.Actor;
        var str = caster.GetStat(Stats.Strength);
        var agi = caster.GetStat(Stats.Agility);
        var intl = caster.GetStat(Stats.Intellect);
        var vers = 0.0f; // caster.GetStat(STAT_VERSATILITY);
        var stat = 0.0f;

        var spellTrigger = ItemSpellIds.DARKMOON_CARD_STRENGHT;

        if (str > stat)
        {
            spellTrigger = ItemSpellIds.DARKMOON_CARD_STRENGHT;
            stat = str;
        }

        if (agi > stat)
        {
            spellTrigger = ItemSpellIds.DARKMOON_CARD_AGILITY;
            stat = agi;
        }

        if (intl > stat)
        {
            spellTrigger = ItemSpellIds.DARKMOON_CARD_INTELLECT;
            stat = intl;
        }

        if (vers > stat)
        {
            spellTrigger = ItemSpellIds.DARKMOON_CARD_VERSATILITY;
            stat = vers;
        }

        caster.SpellFactory.CastSpell(caster, spellTrigger, new CastSpellExtraArgs(aurEff));
    }
}