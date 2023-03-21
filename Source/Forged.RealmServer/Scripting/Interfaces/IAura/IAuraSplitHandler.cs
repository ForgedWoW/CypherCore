// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Scripting.Interfaces.IAura;

public interface IAuraSplitHandler : IAuraEffectHandler
{
	double Split(AuraEffect aura, DamageInfo damageInfo, double splitAmount);
}

public class AuraEffectSplitHandler : AuraEffectHandler, IAuraSplitHandler
{
	private readonly Func<AuraEffect, DamageInfo, double, double> _fn;

	public AuraEffectSplitHandler(Func<AuraEffect, DamageInfo, double, double> fn, int effectIndex) : base(effectIndex, AuraType.SplitDamagePct, AuraScriptHookType.EffectSplit)
	{
		_fn = fn;
	}

	public double Split(AuraEffect aura, DamageInfo damageInfo, double splitAmount)
	{
		return _fn(aura, damageInfo, splitAmount);
	}
}