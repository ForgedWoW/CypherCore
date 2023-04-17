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
internal class SpellItemSoulPreserver : AuraScript, IHasAuraEffects
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
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.SOUL_PRESERVER_DRUID, new CastSpellExtraArgs(aurEff));

                break;
            case PlayerClass.Paladin:
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.SOUL_PRESERVER_PALADIN, new CastSpellExtraArgs(aurEff));

                break;
            case PlayerClass.Priest:
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.SOUL_PRESERVER_PRIEST, new CastSpellExtraArgs(aurEff));

                break;
            case PlayerClass.Shaman:
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.SOUL_PRESERVER_SHAMAN, new CastSpellExtraArgs(aurEff));

                break;
        }
    }
}