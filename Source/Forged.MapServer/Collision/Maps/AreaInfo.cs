// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Collision.Maps;

public class AreaInfo
{
    public bool Result;
    public float GroundZ;
    public uint Flags;
    public int AdtId;
    public int RootId;
    public int GroupId;

    public AreaInfo()
    {
        GroundZ = float.NegativeInfinity;
    }
}