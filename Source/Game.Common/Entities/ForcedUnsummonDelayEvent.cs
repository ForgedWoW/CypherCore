// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Entities;
using Game.Common.Entities;

namespace Game.Common.Entities;

public class ForcedUnsummonDelayEvent : BasicEvent
{
	readonly TempSummon _owner;

	public ForcedUnsummonDelayEvent(TempSummon owner)
	{
		_owner = owner;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		_owner.UnSummon();

		return true;
	}
}
