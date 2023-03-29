// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Collision.Models;

namespace Forged.MapServer.Collision.Maps;

public class LocationInfo
{
    public int RootId;
    public ModelInstance HitInstance;
    public GroupModel HitModel;
    public float GroundZ;

    public LocationInfo()
    {
        GroundZ = float.NegativeInfinity;
    }
}