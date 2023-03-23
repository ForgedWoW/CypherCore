// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Loot;

public class SetLootSpecialization : ClientPacket
{
	public uint SpecID;
	public SetLootSpecialization(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpecID = _worldPacket.ReadUInt32();
	}
}
