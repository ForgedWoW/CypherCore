// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Groups;

public class RaidMarker
{
    public WorldLocation Location { get; set; }
    public ObjectGuid TransportGUID { get; set; }

    public RaidMarker(uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
    {
        Location = new WorldLocation(mapId, positionX, positionY, positionZ);
        TransportGUID = transportGuid;
    }
}