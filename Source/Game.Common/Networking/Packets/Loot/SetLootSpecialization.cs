// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class SetLootSpecialization : ClientPacket
{
	public uint SpecID;
	public SetLootSpecialization(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpecID = _worldPacket.ReadUInt32();
	}
}