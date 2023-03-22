﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class MailGetList : ClientPacket
{
	public ObjectGuid Mailbox;
	public MailGetList(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Mailbox = _worldPacket.ReadPackedGuid();
	}
}

//Structs