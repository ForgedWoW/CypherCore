﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// -51556 - Ancestral Awakening
[SpellScript(51556)]
public class spell_sha_ancestral_awakening : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var heal = MathFunctions.CalculatePct(eventInfo.HealInfo.Heal, aurEff.Amount);
        Target.CastSpell(Target, ShamanSpells.ANCESTRAL_AWAKENING, new CastSpellExtraArgs().AddSpellMod(SpellValueMod.BasePoint0, (int)heal));
    }
}