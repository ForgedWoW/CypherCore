// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Forged.RealmServer.Groups;

public class RaidMarker
{
	public WorldLocation Location;
	public ObjectGuid TransportGUID;

	public RaidMarker(uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
	{
		Location = new WorldLocation(mapId, positionX, positionY, positionZ);
		TransportGUID = transportGuid;
	}
}