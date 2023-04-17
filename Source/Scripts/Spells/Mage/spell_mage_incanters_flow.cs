// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 1463 - Incanter's Flow
internal class SpellMageIncantersFlow : AuraScript, IHasAuraEffects
{
    private sbyte _modifier = 1;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicDummy));
    }

    private void HandlePeriodicTick(AuraEffect aurEff)
    {
        // Incanter's flow should not cycle out of combat
        if (!Target.IsInCombat)
            return;

        var aura = Target.GetAura(MageSpells.INCANTERS_FLOW);

        if (aura != null)
        {
            uint stacks = aura.StackAmount;

            // Force always to values between 1 and 5
            if ((_modifier == -1 && stacks == 1) ||
                (_modifier == 1 && stacks == 5))
            {
                _modifier *= -1;

                return;
            }

            aura.ModStackAmount(_modifier);
        }
        else
            Target.SpellFactory.CastSpell(Target, MageSpells.INCANTERS_FLOW, true);
    }
}