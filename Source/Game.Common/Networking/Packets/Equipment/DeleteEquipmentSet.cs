﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Equipment;

public class DeleteEquipmentSet : ClientPacket
{
	public ulong ID;
	public DeleteEquipmentSet(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ID = _worldPacket.ReadUInt64();
	}
}
