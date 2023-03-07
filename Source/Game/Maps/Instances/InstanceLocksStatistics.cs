// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Maps;

public struct InstanceLocksStatistics
{
	public int InstanceCount; // Number of existing ID-based locks
	public int PlayerCount;   // Number of players that have any lock
}