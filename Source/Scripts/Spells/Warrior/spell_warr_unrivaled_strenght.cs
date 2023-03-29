﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior;

//200860 Unrivaled Strenght
[SpellScript(200860)]
public class spell_warr_unrivaled_strenght : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
    {
        Caster.CastSpell(Caster, 200977, true);

        if (Caster.HasAura(200977))
            Caster.GetAura(200977).GetEffect(0).SetAmount(aurEff.BaseAmount);
    }
}