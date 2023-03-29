﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 51562 - Tidal Waves
[SpellScript(51562)]
internal class spell_sha_tidal_waves : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, -aurEff.Amount);
        args.AddSpellMod(SpellValueMod.BasePoint1, aurEff.Amount);

        Target.CastSpell(Target, ShamanSpells.TidalWaves, args);
    }
}