// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//202192 - Resonance totem
[SpellScript(202192)]
public class SpellShaResonanceEffect : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicEnergize));
    }

    private void HandlePeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.OwnerUnit)
            caster.OwnerUnit.ModifyPower(PowerType.Maelstrom, +1);
    }
}