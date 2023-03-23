// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Toy;

public class ToyClearFanfare : ClientPacket
{
	public uint ItemID;
	public ToyClearFanfare(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ItemID = _worldPacket.ReadUInt32();
	}
}
