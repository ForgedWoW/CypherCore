// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Spell;

public class SpellTargetData
{
	public SpellCastTargetFlags Flags;
	public ObjectGuid Unit;
	public ObjectGuid Item;
	public TargetLocation SrcLocation;
	public TargetLocation DstLocation;
	public float? Orientation;
	public int? MapID;
	public string Name = "";

	public void Read(WorldPacket data)
	{
		data.ResetBitPos();
		Flags = (SpellCastTargetFlags)data.ReadBits<uint>(28);

		if (data.HasBit())
			SrcLocation = new TargetLocation();

		if (data.HasBit())
			DstLocation = new TargetLocation();

		var hasOrientation = data.HasBit();
		var hasMapId = data.HasBit();

		var nameLength = data.ReadBits<uint>(7);

		Unit = data.ReadPackedGuid();
		Item = data.ReadPackedGuid();

		if (SrcLocation != null)
			SrcLocation.Read(data);

		if (DstLocation != null)
			DstLocation.Read(data);

		if (hasOrientation)
			Orientation = data.ReadFloat();

		if (hasMapId)
			MapID = data.ReadInt32();

		Name = data.ReadString(nameLength);
	}

	public void Write(WorldPacket data)
	{
		data.WriteBits((uint)Flags, 28);
		data.WriteBit(SrcLocation != null);
		data.WriteBit(DstLocation != null);
		data.WriteBit(Orientation.HasValue);
		data.WriteBit(MapID.HasValue);
		data.WriteBits(Name.GetByteCount(), 7);
		data.FlushBits();

		data.WritePackedGuid(Unit);
		data.WritePackedGuid(Item);

		if (SrcLocation != null)
			SrcLocation.Write(data);

		if (DstLocation != null)
			DstLocation.Write(data);

		if (Orientation.HasValue)
			data.WriteFloat(Orientation.Value);

		if (MapID.HasValue)
			data.WriteInt32(MapID.Value);

		data.WriteString(Name);
	}
}
