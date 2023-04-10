// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Collision.Models;

namespace Forged.MapServer.Collision.Maps;

public class LocationInfo
{
    public float GroundZ { get; set; }
    public ModelInstance HitInstance { get; set; }
    public GroupModel HitModel { get; set; }
    public int RootId { get; set; }

    public LocationInfo()
    {
        GroundZ = float.NegativeInfinity;
    }
}