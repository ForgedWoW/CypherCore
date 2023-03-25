// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Battlenet;

class Response : ServerPacket
{
	public BattlenetRpcErrorCode BnetStatus = BattlenetRpcErrorCode.Ok;
	public MethodCall Method;
	public ByteBuffer Data = new();
	public Response() : base(ServerOpcodes.BattlenetResponse) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)BnetStatus);
		Method.Write(_worldPacket);
		_worldPacket.WriteUInt32(Data.GetSize());
		_worldPacket.WriteBytes(Data);
	}
}