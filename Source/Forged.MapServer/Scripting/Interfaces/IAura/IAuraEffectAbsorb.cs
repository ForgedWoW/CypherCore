// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraEffectAbsorb : IAuraEffectHandler
{
	double HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, double absorbAmount);
}

public class AuraEffectAbsorbHandler : AuraEffectHandler, IAuraEffectAbsorb
{
	private readonly Func<AuraEffect, DamageInfo, double, double> _fn;

	public AuraEffectAbsorbHandler(Func<AuraEffect, DamageInfo, double, double> fn, int effectIndex, bool overkill = false, AuraScriptHookType hookType = AuraScriptHookType.EffectAbsorb) : base(effectIndex, overkill ? AuraType.SchoolAbsorbOverkill : AuraType.SchoolAbsorb, hookType)
	{
		_fn = fn;

		if (hookType != AuraScriptHookType.EffectAbsorb &&
			hookType != AuraScriptHookType.EffectAfterAbsorb)
			throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectAbsorbHandler)}. Use {AuraScriptHookType.EffectAbsorb} or {AuraScriptHookType.EffectAfterAbsorb}");
	}

	public double HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, double absorbAmount)
	{
		return _fn(aura, damageInfo, absorbAmount);
	}
}