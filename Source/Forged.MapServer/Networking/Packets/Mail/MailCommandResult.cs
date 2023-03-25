// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class MailCommandResult : ServerPacket
{
	public ulong MailID;
	public int Command;
	public int ErrorCode;
	public int BagResult;
	public ulong AttachID;
	public int QtyInInventory;
	public MailCommandResult() : base(ServerOpcodes.MailCommandResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(MailID);
		_worldPacket.WriteInt32(Command);
		_worldPacket.WriteInt32(ErrorCode);
		_worldPacket.WriteInt32(BagResult);
		_worldPacket.WriteUInt64(AttachID);
		_worldPacket.WriteInt32(QtyInInventory);
	}
}