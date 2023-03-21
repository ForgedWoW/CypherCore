// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Scripting.Interfaces.IAura;

public interface IAuraApplyHandler : IAuraEffectHandler
{
	AuraEffectHandleModes Modes { get; }
	void Apply(AuraEffect aura, AuraEffectHandleModes auraMode);
}

public class AuraEffectApplyHandler : AuraEffectHandler, IAuraApplyHandler
{
	private readonly Action<AuraEffect, AuraEffectHandleModes> _fn;

	public AuraEffectHandleModes Modes { get; }

	public AuraEffectApplyHandler(Action<AuraEffect, AuraEffectHandleModes> fn, int effectIndex, AuraType auraType, AuraEffectHandleModes mode, AuraScriptHookType hookType = AuraScriptHookType.EffectApply) : base(effectIndex, auraType, hookType)
	{
		_fn = fn;
		Modes = mode;

		if (hookType != AuraScriptHookType.EffectApply &&
			hookType != AuraScriptHookType.EffectRemove &&
			hookType != AuraScriptHookType.EffectAfterApply &&
			hookType != AuraScriptHookType.EffectAfterRemove)
			throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectApplyHandler)}. Use {AuraScriptHookType.EffectApply}, {AuraScriptHookType.EffectRemove}, {AuraScriptHookType.EffectAfterApply}, or {AuraScriptHookType.EffectAfterRemove}");
	}

	public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
	{
		if (Convert.ToBoolean(Modes & auraMode))
			_fn(aura, auraMode);
	}
}