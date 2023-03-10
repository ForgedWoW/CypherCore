// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.DISINTEGRATE)]
public class aura_evoker_crippling_force : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

    private void OnTick(AuraEffect obj)
    {
		Aura.OwnerAsUnit.AddAura(EvokerSpells.CRIPPLING_FORCE_AURA);
    }

    public void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, Framework.Constants.AuraType.PeriodicDamage));
	}
}