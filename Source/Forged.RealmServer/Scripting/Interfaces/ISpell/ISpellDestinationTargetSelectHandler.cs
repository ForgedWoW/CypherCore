// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Scripting.Interfaces.ISpell;

public interface ISpellDestinationTargetSelectHandler : ITargetHookHandler
{
	void SetDest(SpellDestination dest);
}

public class DestinationTargetSelectHandler : TargetHookHandler, ISpellDestinationTargetSelectHandler
{
	private readonly Action<SpellDestination> _func;


	public DestinationTargetSelectHandler(Action<SpellDestination> func, int effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.DestinationTargetSelect) : base(effectIndex, targetType, false, hookType, true)
	{
		_func = func;
	}

	public void SetDest(SpellDestination dest)
	{
		_func(dest);
	}
}