﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Maps;

public class RespawnInfo
{
	public SpawnObjectType ObjectType { get; set; }
	public ulong SpawnId { get; set; }
	public uint Entry { get; set; }
	public long RespawnTime { get; set; }
	public uint GridId { get; set; }

	public RespawnInfo() { }

	public RespawnInfo(RespawnInfo info)
	{
		ObjectType = info.ObjectType;
		SpawnId = info.SpawnId;
		Entry = info.Entry;
		RespawnTime = info.RespawnTime;
		GridId = info.GridId;
	}
}