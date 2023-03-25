// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Spells;

public class SpellDestination
{
	public WorldLocation Position;
	public ObjectGuid TransportGuid;
	public Position TransportOffset;

	public SpellDestination()
	{
		Position = new WorldLocation();
		TransportGuid = ObjectGuid.Empty;
		TransportOffset = new Position();
	}

	public SpellDestination(float x, float y, float z, float orientation = 0.0f, uint mapId = 0xFFFFFFFF) : this()
	{
		Position.Relocate(x, y, z, orientation);
		TransportGuid = ObjectGuid.Empty;
		Position.MapId = mapId;
	}

	public SpellDestination(Position pos) : this()
	{
		Position.Relocate(pos);
		TransportGuid = ObjectGuid.Empty;
	}

	public SpellDestination(WorldLocation loc) : this()
	{
		Position.WorldRelocate(loc);
		TransportGuid.Clear();
		TransportOffset.Relocate(0, 0, 0, 0);
	}

	public SpellDestination(WorldObject wObj) : this()
	{
		TransportGuid = wObj.GetTransGUID();
		TransportOffset.Relocate(wObj.TransOffsetX, wObj.TransOffsetY, wObj.TransOffsetZ, wObj.TransOffsetO);
		Position.Relocate(wObj.Location);
	}

	public void Relocate(Position pos)
	{
		if (!TransportGuid.IsEmpty)
		{
			Position.GetPositionOffsetTo(pos, out var offset);
			TransportOffset.RelocateOffset(offset);
		}

		Position.Relocate(pos);
	}

	public void RelocateOffset(Position offset)
	{
		if (!TransportGuid.IsEmpty)
			TransportOffset.RelocateOffset(offset);

		Position.RelocateOffset(offset);
	}
}