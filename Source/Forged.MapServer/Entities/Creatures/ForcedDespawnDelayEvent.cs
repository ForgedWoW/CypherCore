// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Dynamic;

namespace Forged.MapServer.Entities.Creatures;

public class ForcedDespawnDelayEvent : BasicEvent
{
    private readonly Creature _owner;
    private readonly TimeSpan _respawnTimer;

	public ForcedDespawnDelayEvent(Creature owner, TimeSpan respawnTimer = default)
	{
		_owner = owner;
		_respawnTimer = respawnTimer;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		_owner.DespawnOrUnsummon(TimeSpan.Zero, _respawnTimer); // since we are here, we are not TempSummon as object type cannot change during runtime

		return true;
	}
}