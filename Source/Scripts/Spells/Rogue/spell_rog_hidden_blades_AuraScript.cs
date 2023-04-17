// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(270061)]
public class SpellRogHiddenBladesAuraScript : AuraScript, IHasAuraEffects
{
    private byte _stacks;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster != null)
        {
            if (_stacks != 20)
            {
                caster.AddAura(RogueSpells.HIDDEN_BLADES_BUFF, caster);
                _stacks++;
            }

            if (_stacks >= 20)
                return;
        }
    }
}