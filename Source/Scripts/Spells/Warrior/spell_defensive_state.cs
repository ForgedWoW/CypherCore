// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

//197690
[SpellScript(197690)]
public class SpellDefensiveState : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModDamagePercentTaken, AuraEffectHandleModes.Real));
    }

    private void OnApply(AuraEffect aura, AuraEffectHandleModes auraMode)
    {
        var caster = Caster;

        if (caster != null)
        {
            var defensiveState = caster?.GetAura(197690)?.GetEffect(0);

            //if (defensiveState != null)
            //	defensiveState.Amount;
        }
    }
}