﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class MailTakeMoney : ClientPacket
{
	public ObjectGuid Mailbox;
	public ulong MailID;
	public ulong Money;
	public MailTakeMoney(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Mailbox = _worldPacket.ReadPackedGuid();
		MailID = _worldPacket.ReadUInt64();
		Money = _worldPacket.ReadUInt64();
	}
}