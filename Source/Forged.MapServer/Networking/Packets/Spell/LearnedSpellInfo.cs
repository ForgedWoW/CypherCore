// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public struct LearnedSpellInfo
{
	public uint SpellID;
	public bool IsFavorite;
	public int? field_8;
	public int? Superceded;
	public int? TraitDefinitionID;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteBit(IsFavorite);
		data.WriteBit(field_8.HasValue);
		data.WriteBit(Superceded.HasValue);
		data.WriteBit(TraitDefinitionID.HasValue);
		data.FlushBits();

		if (field_8.HasValue)
			data.WriteInt32(field_8.Value);

		if (Superceded.HasValue)
			data.WriteInt32(Superceded.Value);

		if (TraitDefinitionID.HasValue)
			data.WriteInt32(TraitDefinitionID.Value);
	}
}