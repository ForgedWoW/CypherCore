// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public class LiquidData
{
    public float DepthLevel { get; set; }
    public uint Entry { get; set; }
    public float Level { get; set; }
    public LiquidHeaderTypeFlags TypeFlags { get; set; }
}