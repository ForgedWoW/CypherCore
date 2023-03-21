// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class MailCreateTextItem : ClientPacket
{
	public ObjectGuid Mailbox;
	public ulong MailID;
	public MailCreateTextItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Mailbox = _worldPacket.ReadPackedGuid();
		MailID = _worldPacket.ReadUInt64();
	}
}