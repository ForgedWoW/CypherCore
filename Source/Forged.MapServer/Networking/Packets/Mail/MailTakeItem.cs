// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailTakeItem : ClientPacket
{
    public ulong AttachID;
    public ObjectGuid Mailbox;
    public ulong MailID;
    public MailTakeItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Mailbox = WorldPacket.ReadPackedGuid();
        MailID = WorldPacket.ReadUInt64();
        AttachID = WorldPacket.ReadUInt64();
    }
}