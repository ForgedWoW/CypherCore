// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.RealmServer;

public class PoolObject
{
	public ulong Guid;
	public float Chance;

	public PoolObject(ulong guid, float chance)
	{
		Guid = guid;
		Chance = Math.Abs(chance);
	}
}