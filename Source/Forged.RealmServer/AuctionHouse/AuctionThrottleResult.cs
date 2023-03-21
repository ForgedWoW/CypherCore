// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.RealmServer;

public class AuctionThrottleResult
{
	public TimeSpan DelayUntilNext;
	public bool Throttled;

	public AuctionThrottleResult(TimeSpan delayUntilNext, bool throttled)
	{
		DelayUntilNext = delayUntilNext;
		Throttled = throttled;
	}
}