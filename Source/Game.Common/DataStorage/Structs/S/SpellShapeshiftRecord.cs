using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellShapeshiftRecord
{
	public uint Id;
	public uint SpellID;
	public sbyte StanceBarOrder;
	public uint[] ShapeshiftExclude = new uint[2];
	public uint[] ShapeshiftMask = new uint[2];
}
