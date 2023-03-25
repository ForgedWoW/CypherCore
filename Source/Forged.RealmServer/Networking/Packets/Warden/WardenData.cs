// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.IO;

namespace Forged.RealmServer.Networking.Packets;

class WardenData : ClientPacket
{
	public ByteBuffer Data;
	public WardenData(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var size = _worldPacket.ReadUInt32();

		if (size != 0)
			Data = new ByteBuffer(_worldPacket.ReadBytes(size));
	}
}