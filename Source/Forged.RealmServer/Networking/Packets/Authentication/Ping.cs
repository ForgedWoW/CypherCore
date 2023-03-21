// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class Ping : ClientPacket
{
	public uint Serial;
	public uint Latency;
	public Ping(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Serial = _worldPacket.ReadUInt32();
		Latency = _worldPacket.ReadUInt32();
	}
}

//Structs