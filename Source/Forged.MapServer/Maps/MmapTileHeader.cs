﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps;

public struct MmapTileHeader
{
    public uint dtVersion;
    public uint mmapMagic;
    public uint mmapVersion;
    public uint size;
    public byte usesLiquids;
}