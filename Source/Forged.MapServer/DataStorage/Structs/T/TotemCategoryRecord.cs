// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TotemCategoryRecord
{
	public uint Id;
	public string Name;
	public byte TotemCategoryType;
	public int TotemCategoryMask;
}