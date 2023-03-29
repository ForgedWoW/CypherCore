﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206649)]
public class spell_dh_eye_of_leotheras : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;
        var target = Aura.Owner;

        if (caster == null || target == null || eventInfo.SpellInfo != null || !caster.AsPlayer)
            return false;

        var unitTarget = target.AsUnit;

        if (unitTarget == null || eventInfo.SpellInfo.IsPositive)
            return false;

        var aurEff = Aura.GetEffect(0);

        if (aurEff != null)
        {
            var bp = aurEff.Amount;
            Aura.RefreshDuration();


            caster.Events.AddEventAtOffset(() => { caster.CastSpell(unitTarget, DemonHunterSpells.EYE_OF_LEOTHERAS_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp)); }, TimeSpan.FromMilliseconds(100));

            return true;
        }

        return false;
    }
}