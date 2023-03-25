﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class SellItem : ClientPacket
{
	public ObjectGuid VendorGUID;
	public ObjectGuid ItemGUID;
	public uint Amount;
	public SellItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		VendorGUID = _worldPacket.ReadPackedGuid();
		ItemGUID = _worldPacket.ReadPackedGuid();
		Amount = _worldPacket.ReadUInt32();
	}
}