// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class SendContactList : ClientPacket
{
	public SocialFlag Flags;
	public SendContactList(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Flags = (SocialFlag)_worldPacket.ReadUInt32();
	}
}

//Structs