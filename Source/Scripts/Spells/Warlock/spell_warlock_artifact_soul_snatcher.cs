// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 196236 - Soulsnatcher
[SpellScript(196236)]
internal class SpellWarlockArtifactSoulSnatcher : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo unnamedParameter)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (caster == null)
            return;

        if (RandomHelper.randChance(aurEff.Amount))
            caster.SpellFactory.CastSpell(caster, WarlockSpells.SOULSNATCHER_PROC, true);
    }
}