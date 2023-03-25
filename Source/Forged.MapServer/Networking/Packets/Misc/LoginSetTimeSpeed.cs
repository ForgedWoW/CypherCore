// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class LoginSetTimeSpeed : ServerPacket
{
	public float NewSpeed;
	public int ServerTimeHolidayOffset;
	public uint GameTime;
	public uint ServerTime;
	public int GameTimeHolidayOffset;
	public LoginSetTimeSpeed() : base(ServerOpcodes.LoginSetTimeSpeed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedTime(ServerTime);
		_worldPacket.WritePackedTime(GameTime);
		_worldPacket.WriteFloat(NewSpeed);
		_worldPacket.WriteInt32(ServerTimeHolidayOffset);
		_worldPacket.WriteInt32(GameTimeHolidayOffset);
	}
}