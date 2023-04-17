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

[Script] // 27522, 40336 - Mana Drain
internal class SpellItemManaDrain : AuraScript, IHasAuraEffects
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
        var target = eventInfo.ActionTarget;

        if (caster.IsAlive)
            caster.SpellFactory.CastSpell(caster, ItemSpellIds.MANA_DRAIN_ENERGIZE, new CastSpellExtraArgs(aurEff));

        if (target && target.IsAlive)
            caster.SpellFactory.CastSpell(target, ItemSpellIds.MANA_DRAIN_LEECH, new CastSpellExtraArgs(aurEff));
    }
}