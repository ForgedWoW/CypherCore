// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailCommandResult : ServerPacket
{
    public ulong AttachID;
    public int BagResult;
    public int Command;
    public int ErrorCode;
    public ulong MailID;
    public int QtyInInventory;
    public MailCommandResult() : base(ServerOpcodes.MailCommandResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt64(MailID);
        WorldPacket.WriteInt32(Command);
        WorldPacket.WriteInt32(ErrorCode);
        WorldPacket.WriteInt32(BagResult);
        WorldPacket.WriteUInt64(AttachID);
        WorldPacket.WriteInt32(QtyInInventory);
    }
}