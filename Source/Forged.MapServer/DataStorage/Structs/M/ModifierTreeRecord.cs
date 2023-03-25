// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class ModifierTreeRecord
{
	public uint Id;
	public uint Parent;
	public sbyte Operator;
	public sbyte Amount;
	public uint Type;
	public uint Asset;
	public int SecondaryAsset;
	public int TertiaryAsset;
}