// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.IO;

namespace Forged.MapServer.Collision.Maps;

internal class TileFileOpenResult
{
    public FileStream File;
    public string Name;
    public uint UsedMapId;
}