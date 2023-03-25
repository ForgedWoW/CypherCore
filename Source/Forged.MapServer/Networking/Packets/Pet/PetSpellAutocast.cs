// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Pet;

class PetSpellAutocast : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint SpellID;
	public bool AutocastEnabled;
	public PetSpellAutocast(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
		AutocastEnabled = _worldPacket.HasBit();
	}
}