// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Quest;

class PlayerChoiceResponseReward
{
	public int TitleID;
	public int PackageID;
	public int SkillLineID;
	public uint SkillPointCount;
	public uint ArenaPointCount;
	public uint HonorPointCount;
	public ulong Money;
	public uint Xp;
	public List<PlayerChoiceResponseRewardEntry> Items = new();
	public List<PlayerChoiceResponseRewardEntry> Currencies = new();
	public List<PlayerChoiceResponseRewardEntry> Factions = new();
	public List<PlayerChoiceResponseRewardEntry> ItemChoices = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(TitleID);
		data.WriteInt32(PackageID);
		data.WriteInt32(SkillLineID);
		data.WriteUInt32(SkillPointCount);
		data.WriteUInt32(ArenaPointCount);
		data.WriteUInt32(HonorPointCount);
		data.WriteUInt64(Money);
		data.WriteUInt32(Xp);

		data.WriteInt32(Items.Count);
		data.WriteInt32(Currencies.Count);
		data.WriteInt32(Factions.Count);
		data.WriteInt32(ItemChoices.Count);

		foreach (var item in Items)
			item.Write(data);

		foreach (var currency in Currencies)
			currency.Write(data);

		foreach (var faction in Factions)
			faction.Write(data);

		foreach (var itemChoice in ItemChoices)
			itemChoice.Write(data);
	}
}