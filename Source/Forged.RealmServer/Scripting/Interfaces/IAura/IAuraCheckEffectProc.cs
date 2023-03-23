// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Spells;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.Scripting.Interfaces.IAura;

public interface IAuraCheckEffectProc : IAuraEffectHandler
{
	bool CheckProc(AuraEffect aura, ProcEventInfo info);
}

public class AuraCheckEffectProcHandler : AuraEffectHandler, IAuraCheckEffectProc
{
	private readonly Func<AuraEffect, ProcEventInfo, bool> _fn;

	public AuraCheckEffectProcHandler(Func<AuraEffect, ProcEventInfo, bool> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.CheckEffectProc)
	{
		_fn = fn;
	}

	public bool CheckProc(AuraEffect aura, ProcEventInfo info)
	{
		return _fn(aura, info);
	}
}