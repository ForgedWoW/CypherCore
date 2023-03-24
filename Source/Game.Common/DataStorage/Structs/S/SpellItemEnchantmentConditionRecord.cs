// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellItemEnchantmentConditionRecord
{
	public uint Id;
	public byte[] LtOperandType = new byte[5];
	public uint[] LtOperand = new uint[5];
	public byte[] Operator = new byte[5];
	public byte[] RtOperandType = new byte[5];
	public byte[] RtOperand = new byte[5];
	public byte[] Logic = new byte[5];
}
