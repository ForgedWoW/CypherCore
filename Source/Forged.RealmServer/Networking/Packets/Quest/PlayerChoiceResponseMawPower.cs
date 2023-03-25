// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

struct PlayerChoiceResponseMawPower
{
	public int Unused901_1;
	public int TypeArtFileID;
	public int? Rarity;
	public uint? RarityColor;
	public int Unused901_2;
	public int SpellID;
	public int MaxStacks;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Unused901_1);
		data.WriteInt32(TypeArtFileID);
		data.WriteInt32(Unused901_2);
		data.WriteInt32(SpellID);
		data.WriteInt32(MaxStacks);
		data.WriteBit(Rarity.HasValue);
		data.WriteBit(RarityColor.HasValue);
		data.FlushBits();

		if (Rarity.HasValue)
			data.WriteInt32(Rarity.Value);

		if (RarityColor.HasValue)
			data.WriteUInt32(RarityColor.Value);
	}
}