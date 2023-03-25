// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class MoveInitActiveMoverComplete : ClientPacket
{
	public uint Ticks;

	public MoveInitActiveMoverComplete(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Ticks = _worldPacket.ReadUInt32();
	}
}