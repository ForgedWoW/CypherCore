// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class ItemMod
{
	public uint Value;
	public ItemModifier Type;

	public ItemMod()
	{
		Type = ItemModifier.Max;
	}

	public ItemMod(uint value, ItemModifier type)
	{
		Value = value;
		Type = type;
	}

	public void Read(WorldPacket data)
	{
		Value = data.ReadUInt32();
		Type = (ItemModifier)data.ReadUInt8();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Value);
		data.WriteUInt8((byte)Type);
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode() ^ Type.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is ItemMod)
			return (ItemMod)obj == this;

		return false;
	}

	public static bool operator ==(ItemMod left, ItemMod right)
	{
		if (left.Value != right.Value)
			return false;

		return left.Type != right.Type;
	}

	public static bool operator !=(ItemMod left, ItemMod right)
	{
		return !(left == right);
	}
}