// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura;

public interface IAuraEffectAbsorbHeal : IAuraEffectHandler
{
	double HandleAbsorb(AuraEffect aura, HealInfo healInfo, double absorbAmount);
}

public class AuraEffectAbsorbHealHandler : AuraEffectHandler, IAuraEffectAbsorbHeal
{
	private readonly Func<AuraEffect, HealInfo, double, double> _fn;

	public AuraEffectAbsorbHealHandler(Func<AuraEffect, HealInfo, double, double> fn, int effectIndex, AuraType auraType, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
	{
		_fn = fn;

		if (hookType != AuraScriptHookType.EffectAbsorbHeal &&
			hookType != AuraScriptHookType.EffectAfterAbsorbHeal &&
			hookType != AuraScriptHookType.EffectManaShield &&
			hookType != AuraScriptHookType.EffectAfterManaShield)
			throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectAbsorbHealHandler)}. Use {AuraScriptHookType.EffectAbsorbHeal}, {AuraScriptHookType.EffectAfterManaShield}, {AuraScriptHookType.EffectManaShield} or {AuraScriptHookType.EffectAfterAbsorbHeal}");
	}

	public double HandleAbsorb(AuraEffect aura, HealInfo healInfo, double absorbAmount)
	{
		return _fn(aura, healInfo, absorbAmount);
	}
}