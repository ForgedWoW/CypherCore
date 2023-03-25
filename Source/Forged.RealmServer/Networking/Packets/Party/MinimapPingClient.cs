// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class MinimapPingClient : ClientPacket
{
	public sbyte PartyIndex;
	public float PositionX;
	public float PositionY;
	public MinimapPingClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PositionX = _worldPacket.ReadFloat();
		PositionY = _worldPacket.ReadFloat();
		PartyIndex = _worldPacket.ReadInt8();
	}
}