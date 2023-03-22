﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class RepairItem : ClientPacket
{
	public ObjectGuid NpcGUID;
	public ObjectGuid ItemGUID;
	public bool UseGuildBank;
	public RepairItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		NpcGUID = _worldPacket.ReadPackedGuid();
		ItemGUID = _worldPacket.ReadPackedGuid();
		UseGuildBank = _worldPacket.HasBit();
	}
}