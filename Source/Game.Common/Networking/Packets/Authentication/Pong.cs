// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class Pong : ServerPacket
{
	readonly uint Serial;

	public Pong(uint serial) : base(ServerOpcodes.Pong)
	{
		Serial = serial;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(Serial);
	}
}