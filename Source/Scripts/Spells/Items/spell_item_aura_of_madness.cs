// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 39446 - Aura of Madness
internal class SpellItemAuraOfMadness : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        uint[][] triggeredSpells =
        {
            //CLASS_NONE
            Array.Empty<uint>(),
            //CLASS_WARRIOR
            new uint[]
            {
                ItemSpellIds.SOCIOPATH, ItemSpellIds.DELUSIONAL, ItemSpellIds.KLEPTOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.MARTYR_COMPLEX
            },
            //CLASS_PALADIN
            new uint[]
            {
                ItemSpellIds.SOCIOPATH, ItemSpellIds.DELUSIONAL, ItemSpellIds.KLEPTOMANIA, ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            },
            //CLASS_HUNTER
            new uint[]
            {
                ItemSpellIds.DELUSIONAL, ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            },
            //CLASS_ROGUE
            new uint[]
            {
                ItemSpellIds.SOCIOPATH, ItemSpellIds.DELUSIONAL, ItemSpellIds.KLEPTOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.MARTYR_COMPLEX
            },
            //CLASS_PRIEST
            new uint[]
            {
                ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            },
            //CLASS_DEATH_KNIGHT
            new uint[]
            {
                ItemSpellIds.SOCIOPATH, ItemSpellIds.DELUSIONAL, ItemSpellIds.KLEPTOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.MARTYR_COMPLEX
            },
            //CLASS_SHAMAN
            new uint[]
            {
                ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            },
            //CLASS_MAGE
            new uint[]
            {
                ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            },
            //CLASS_WARLOCK
            new uint[]
            {
                ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            },
            //CLASS_UNK
            Array.Empty<uint>(),
            //CLASS_DRUID
            new uint[]
            {
                ItemSpellIds.SOCIOPATH, ItemSpellIds.DELUSIONAL, ItemSpellIds.KLEPTOMANIA, ItemSpellIds.MEGALOMANIA, ItemSpellIds.PARANOIA, ItemSpellIds.MANIC, ItemSpellIds.NARCISSISM, ItemSpellIds.MARTYR_COMPLEX, ItemSpellIds.DEMENTIA
            }
        };

        PreventDefaultAction();
        var caster = eventInfo.Actor;
        var spellId = triggeredSpells[(int)caster.Class].SelectRandom();
        caster.SpellFactory.CastSpell(caster, spellId, new CastSpellExtraArgs(aurEff));

        if (RandomHelper.randChance(10))
            caster.Say(TextIds.SAY_MADNESS);
    }
}