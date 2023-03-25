﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class AddIgnore : ClientPacket
{
	public string Name;
	public ObjectGuid AccountGUID;
	public AddIgnore(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLength = _worldPacket.ReadBits<uint>(9);
		AccountGUID = _worldPacket.ReadPackedGuid();
		Name = _worldPacket.ReadString(nameLength);
	}
}