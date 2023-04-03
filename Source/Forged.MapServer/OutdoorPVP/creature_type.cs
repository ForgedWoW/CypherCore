// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.OutdoorPVP;

internal class creature_type
{
    public uint entry;
    public uint map;
    private readonly Position pos;

    public creature_type(uint _entry, uint _map, float _x, float _y, float _z, float _o)
    {
        entry = _entry;
        map = _map;
        pos = new Position(_x, _y, _z, _o);
    }
}