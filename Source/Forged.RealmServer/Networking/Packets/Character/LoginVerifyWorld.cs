// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class LoginVerifyWorld : ServerPacket
{
	public int MapID = -1;
	public Position Pos;
	public uint Reason = 0;
	public LoginVerifyWorld() : base(ServerOpcodes.LoginVerifyWorld, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteFloat(Pos.X);
		_worldPacket.WriteFloat(Pos.Y);
		_worldPacket.WriteFloat(Pos.Z);
		_worldPacket.WriteFloat(Pos.Orientation);
		_worldPacket.WriteUInt32(Reason);
	}
}