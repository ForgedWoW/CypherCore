// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(197908)]
public class SpellMonkManaTeaAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.ModPowerCostSchoolPct));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        if (Caster)
        {
            // remove one charge per tick instead of remove aura on cast
            // "Cancelling the channel will not waste stacks"
            var manaTea = Caster.GetAura(MonkSpells.MANA_TEA_STACKS);

            if (manaTea != null)
            {
                if (manaTea.StackAmount > 1)
                    manaTea.ModStackAmount(-1);
                else
                    Caster.RemoveAura(MonkSpells.MANA_TEA_STACKS);
            }
        }
    }
}