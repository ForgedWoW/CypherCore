﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2)]
public class aura_evoker_crippling_force : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, Framework.Constants.AuraType.PeriodicDamage));
    }

    private void OnTick(AuraEffect obj)
    {
        Aura.OwnerAsUnit.AddAura(EvokerSpells.CRIPPLING_FORCE_AURA);
    }
}