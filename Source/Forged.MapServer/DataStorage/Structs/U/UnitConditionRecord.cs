// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UnitConditionRecord
{
	public uint Id;
	public byte Flags;
	public byte[] Variable = new byte[8];
	public sbyte[] Op = new sbyte[8];
	public int[] Value = new int[8];

	public UnitConditionFlags GetFlags()
	{
		return (UnitConditionFlags)Flags;
	}
}