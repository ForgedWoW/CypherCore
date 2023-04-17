// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Spells;

public class SpellDestination
{
    public SpellDestination()
    {
        Position = new WorldLocation();
        TransportGuid = ObjectGuid.Empty;
        TransportOffset = new Position();
    }

    public SpellDestination(float x, float y, float z, float orientation = 0.0f, uint mapId = 0xFFFFFFFF) : this()
    {
        Position = new WorldLocation(mapId, x, y, z, orientation);
        TransportGuid = ObjectGuid.Empty;
    }

    public SpellDestination(Position pos) : this()
    {
        Position = new WorldLocation(pos);
        TransportGuid = ObjectGuid.Empty;
    }

    public SpellDestination(WorldLocation loc) : this()
    {
        Position = new WorldLocation(loc);
        TransportGuid.Clear();
        TransportOffset = new Position();
    }

    public SpellDestination(WorldObject wObj) : this()
    {
        TransportGuid = wObj.GetTransGUID();
        TransportOffset = new Position(wObj.MovementInfo.Transport.Pos.X, wObj.MovementInfo.Transport.Pos.Y, wObj.MovementInfo.Transport.Pos.Z, wObj.MovementInfo.Transport.Pos.Orientation);
        Position = new WorldLocation(wObj.Location);
    }

    public WorldLocation Position { get; set; }
    public ObjectGuid TransportGuid { get; set; }
    public Position TransportOffset { get; set; }

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