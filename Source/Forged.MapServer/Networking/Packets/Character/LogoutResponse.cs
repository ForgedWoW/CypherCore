// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class LogoutResponse : ServerPacket
{
	public int LogoutResult;
	public bool Instant = false;
	public LogoutResponse() : base(ServerOpcodes.LogoutResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(LogoutResult);
		_worldPacket.WriteBit(Instant);
		_worldPacket.FlushBits();
	}
}