// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Authentication;

internal class Pong : ServerPacket
{
    private readonly uint Serial;

	public Pong(uint serial) : base(ServerOpcodes.Pong)
	{
		Serial = serial;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(Serial);
	}
}