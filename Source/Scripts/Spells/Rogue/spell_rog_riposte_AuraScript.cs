// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(199754)]
public class SpellRogRiposteAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
    }


    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        var caster = Caster;

        if (caster == null)
            return;

        var target = procInfo.ActionTarget;

        if (target == null)
            return;

        caster.SpellFactory.CastSpell(target, RogueSpells.RIPOSTE_DAMAGE, true);
    }
}