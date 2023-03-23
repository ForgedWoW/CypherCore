// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public class OpenItem : ClientPacket
{
	public byte Slot;
	public byte PackSlot;
	public OpenItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Slot = _worldPacket.ReadUInt8();
		PackSlot = _worldPacket.ReadUInt8();
	}
}
