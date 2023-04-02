// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Social;

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
        WorldPacket.WriteUInt32((uint)Flags);
        WorldPacket.WriteBits(Contacts.Count, 8);
        WorldPacket.FlushBits();

        foreach (var contact in Contacts)
            contact.Write(WorldPacket);
    }
}