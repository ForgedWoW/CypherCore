// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Players;

namespace Game.Common.Networking.Packets.Social;

public class ContactList : ServerPacket
{
	public List<ContactInfo> Contacts;
	public SocialFlag Flags;

	public ContactList() : base(ServerOpcodes.ContactList)
	{
		Contacts = new List<ContactInfo>();
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WriteBits(Contacts.Count, 8);
		_worldPacket.FlushBits();

		foreach (var contact in Contacts)
			contact.Write(_worldPacket);
	}
}
