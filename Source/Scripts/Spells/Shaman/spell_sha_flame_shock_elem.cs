// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//188389
[SpellScript(188389)]
public class SpellShaFlameShockElem : AuraScript, IHasAuraEffects
{
    private int _mExtraSpellCost;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        var caster = Caster;

        if (caster == null)
            return false;

        _mExtraSpellCost = Math.Min(caster.GetPower(PowerType.Maelstrom), 20);

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDamage));
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var mNewDuration = Duration + (Duration * (_mExtraSpellCost / 20));
        SetDuration(mNewDuration);

        var caster = Caster;

        if (caster != null)
        {
            var mNewMael = caster.GetPower(PowerType.Maelstrom) - _mExtraSpellCost;

            if (mNewMael < 0)
                mNewMael = 0;

            var mael = caster.GetPower(PowerType.Maelstrom);

            if (mael > 0)
                caster.SetPower(PowerType.Maelstrom, mNewMael);
        }
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.HasAura(ShamanSpells.LAVA_SURGE) && RandomHelper.randChance(15))
        {
            caster.SpellFactory.CastSpell(ShamanSpells.LAVA_SURGE_CAST_TIME);
            caster.SpellHistory.ResetCooldown(ShamanSpells.LAVA_BURST, true);
        }
    }
}