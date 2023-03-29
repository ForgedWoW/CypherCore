﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 204288 - Earth Shield
[SpellScript(204288)]
internal class spell_sha_earth_shield : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.DamageInfo == null ||
            !HasEffect(1) ||
            eventInfo.DamageInfo.Damage < Target.CountPctFromMaxHealth(GetEffect(1).Amount))
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        Target.CastSpell(Target, ShamanSpells.EarthShieldHeal, new CastSpellExtraArgs(aurEff).SetOriginalCaster(CasterGUID));
    }
}