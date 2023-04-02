// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailDelete : ClientPacket
{
    public int DeleteReason;
    public ulong MailID;
    public MailDelete(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        MailID = WorldPacket.ReadUInt64();
        DeleteReason = WorldPacket.ReadInt32();
    }
}