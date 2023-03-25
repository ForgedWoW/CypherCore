﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class GuildSetGuildMaster : ClientPacket
{
	public string NewMasterName;
	public GuildSetGuildMaster(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(9);
		NewMasterName = _worldPacket.ReadString(nameLen);
	}
}