// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Authentication;

class ConnectToFailed : ClientPacket
{
	public ConnectToSerial Serial;
	byte Con;
	public ConnectToFailed(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Serial = (ConnectToSerial)_worldPacket.ReadUInt32();
		Con = _worldPacket.ReadUInt8();
	}
}