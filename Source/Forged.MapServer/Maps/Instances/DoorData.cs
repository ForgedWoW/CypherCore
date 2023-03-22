// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Maps;

public class DoorData
{
	public uint Entry { get; set; }
	public uint bossId { get; set; }
	public DoorType Type { get; set; }

	public DoorData(uint entry, uint bossid, DoorType doorType)
	{
		Entry = entry;
		bossId = bossid;
		Type = doorType;
	}
}