// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Character;

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
