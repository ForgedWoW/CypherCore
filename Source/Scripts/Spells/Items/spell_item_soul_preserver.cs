// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_soul_preserver : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var caster = eventInfo.Actor;

        switch (caster.Class)
        {
            case PlayerClass.Druid:
                caster.CastSpell(caster, ItemSpellIds.SoulPreserverDruid, new CastSpellExtraArgs(aurEff));

                break;
            case PlayerClass.Paladin:
                caster.CastSpell(caster, ItemSpellIds.SoulPreserverPaladin, new CastSpellExtraArgs(aurEff));

                break;
            case PlayerClass.Priest:
                caster.CastSpell(caster, ItemSpellIds.SoulPreserverPriest, new CastSpellExtraArgs(aurEff));

                break;
            case PlayerClass.Shaman:
                caster.CastSpell(caster, ItemSpellIds.SoulPreserverShaman, new CastSpellExtraArgs(aurEff));

                break;
            default:
                break;
        }
    }
}