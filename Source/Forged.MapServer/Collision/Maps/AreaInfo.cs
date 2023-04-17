// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Collision.Maps;

public class AreaInfo
{
    public AreaInfo()
    {
        GroundZ = float.NegativeInfinity;
    }

    public int AdtId { get; set; }
    public uint Flags { get; set; }
    public float GroundZ { get; set; }
    public int GroupId { get; set; }
    public bool Result { get; set; }
    public int RootId { get; set; }
}