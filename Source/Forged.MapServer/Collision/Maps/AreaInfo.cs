// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Collision.Maps;

public class AreaInfo
{
    public int AdtId;
    public uint Flags;
    public float GroundZ;
    public int GroupId;
    public bool Result;
    public int RootId;
    public AreaInfo()
    {
        GroundZ = float.NegativeInfinity;
    }
}