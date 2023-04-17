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

[Script]
internal class SpellItemDeathChoice : AuraScript, IHasAuraEffects
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

        switch (aurEff.Id)
        {
            case ItemSpellIds.DEATH_CHOICE_NORMAL_AURA:
            {
                if (str > agi)
                    caster.SpellFactory.CastSpell(caster, ItemSpellIds.DEATH_CHOICE_NORMAL_STRENGTH, new CastSpellExtraArgs(aurEff));
                else
                    caster.SpellFactory.CastSpell(caster, ItemSpellIds.DEATH_CHOICE_NORMAL_AGILITY, new CastSpellExtraArgs(aurEff));

                break;
            }
            case ItemSpellIds.DEATH_CHOICE_HEROIC_AURA:
            {
                if (str > agi)
                    caster.SpellFactory.CastSpell(caster, ItemSpellIds.DEATH_CHOICE_HEROIC_STRENGTH, new CastSpellExtraArgs(aurEff));
                else
                    caster.SpellFactory.CastSpell(caster, ItemSpellIds.DEATH_CHOICE_HEROIC_AGILITY, new CastSpellExtraArgs(aurEff));

                break;
            }
        }
    }
}