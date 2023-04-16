// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.OutdoorPVP;

internal class CreatureType
{
    public uint Entry;
    public uint Map;
    private readonly Position _pos;

    public CreatureType(uint entry, uint map, float x, float y, float z, float o)
    {
        Entry = entry;
        Map = map;
        _pos = new Position(x, y, z, o);
    }
}