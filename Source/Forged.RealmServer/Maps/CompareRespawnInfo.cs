// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Maps;

struct CompareRespawnInfo : IComparer<RespawnInfo>
{
	public int Compare(RespawnInfo a, RespawnInfo b)
	{
		if (a == b)
			return 0;

		if (a.RespawnTime != b.RespawnTime)
			return a.RespawnTime.CompareTo(b.RespawnTime);

		if (a.SpawnId != b.SpawnId)
			return a.SpawnId.CompareTo(b.SpawnId);

		return a.ObjectType.CompareTo(b.ObjectType);
	}
}