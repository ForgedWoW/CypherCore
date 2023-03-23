// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Item;

public class ReadItem : ClientPacket
{
	public byte PackSlot;
	public byte Slot;
	public ReadItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PackSlot = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}
