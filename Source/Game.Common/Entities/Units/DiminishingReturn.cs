// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Common.Entities.Units;

public struct DiminishingReturn
{
	public DiminishingReturn(uint hitTime, DiminishingLevels hitCount)
	{
		Stack = 0;
		HitTime = hitTime;
		HitCount = hitCount;
	}

	public void Clear()
	{
		Stack = 0;
		HitTime = 0;
		HitCount = DiminishingLevels.Level1;
	}

	public uint Stack;
	public uint HitTime;
	public DiminishingLevels HitCount;
}
