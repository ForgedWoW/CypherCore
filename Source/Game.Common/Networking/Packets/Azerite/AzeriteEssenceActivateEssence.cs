// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class AzeriteEssenceActivateEssence : ClientPacket
{
	public uint AzeriteEssenceID;
	public byte Slot;
	public AzeriteEssenceActivateEssence(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AzeriteEssenceID = _worldPacket.ReadUInt32();
		Slot = _worldPacket.ReadUInt8();
	}
}