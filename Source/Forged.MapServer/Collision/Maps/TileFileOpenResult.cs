// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.IO;

namespace Game.Collision;

class TileFileOpenResult
{
	public string Name;
	public FileStream File;
	public uint UsedMapId;
}