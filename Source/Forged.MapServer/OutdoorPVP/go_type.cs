// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.OutdoorPVP;

public class go_type
{
    public uint entry;
    public uint map;
    public Position pos;
    public Quaternion rot;

    public go_type(uint _entry, uint _map, float _x, float _y, float _z, float _o, float _rot0, float _rot1, float _rot2, float _rot3)
    {
        entry = _entry;
        map = _map;
        pos = new Position(_x, _y, _z, _o);
        rot = new Quaternion(_rot0, _rot1, _rot2, _rot3);
    }
}