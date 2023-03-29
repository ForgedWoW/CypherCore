﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 1463 - Incanter's Flow
internal class spell_mage_incanters_flow : AuraScript, IHasAuraEffects
{
    private sbyte modifier = 1;
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

        var aura = Target.GetAura(MageSpells.IncantersFlow);

        if (aura != null)
        {
            uint stacks = aura.StackAmount;

            // Force always to values between 1 and 5
            if ((modifier == -1 && stacks == 1) ||
                (modifier == 1 && stacks == 5))
            {
                modifier *= -1;

                return;
            }

            aura.ModStackAmount(modifier);
        }
        else
        {
            Target.CastSpell(Target, MageSpells.IncantersFlow, true);
        }
    }
}