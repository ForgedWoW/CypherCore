// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194844)]
public class SpellDkBonestorm : AuraScript, IHasAuraEffects
{
    private int _mExtraSpellCost;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        var caster = Caster;

        if (caster == null)
            return false;

        var availablePower = Math.Min(caster.GetPower(PowerType.RunicPower), 90);

        //Round down to nearest multiple of 10
        _mExtraSpellCost = availablePower - (availablePower % 10);

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicTriggerSpell));
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var mNewDuration = Duration + (_mExtraSpellCost / 10);
        SetDuration(mNewDuration);

        var caster = Caster;

        if (caster != null)
        {
            var mNewPower = caster.GetPower(PowerType.RunicPower) - _mExtraSpellCost;

            if (mNewPower < 0)
                mNewPower = 0;

            caster.SetPower(PowerType.RunicPower, mNewPower);
        }
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.SpellFactory.CastSpell(caster, DeathKnightSpells.BONESTORM_HEAL, true);
    }
}