﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 196236 - Soulsnatcher
[SpellScript(196236)]
internal class spell_warlock_artifact_soul_snatcher : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (caster == null)
            return;

        if (RandomHelper.randChance(aurEff.Amount))
            caster.CastSpell(caster, WarlockSpells.SOULSNATCHER_PROC, true);
    }
}