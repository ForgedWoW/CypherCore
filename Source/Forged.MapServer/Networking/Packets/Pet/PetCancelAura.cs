// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class PetCancelAura : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint SpellID;
	public PetCancelAura(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
	}
}