// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets;

public class QuestGiverOfferReward
{
	public ObjectGuid QuestGiverGUID;
	public uint QuestGiverCreatureID = 0;
	public uint QuestID = 0;
	public bool AutoLaunched = false;
	public uint SuggestedPartyMembers = 0;
	public QuestRewards Rewards = new();
	public List<QuestDescEmote> Emotes = new();
	public uint[] QuestFlags = new uint[3]; // Flags and FlagsEx

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(QuestGiverGUID);
		data.WriteUInt32(QuestGiverCreatureID);
		data.WriteUInt32(QuestID);
		data.WriteUInt32(QuestFlags[0]); // Flags
		data.WriteUInt32(QuestFlags[1]); // FlagsEx
		data.WriteUInt32(QuestFlags[2]); // FlagsEx2
		data.WriteUInt32(SuggestedPartyMembers);

		data.WriteInt32(Emotes.Count);

		foreach (var emote in Emotes)
		{
			data.WriteInt32(emote.Type);
			data.WriteUInt32(emote.Delay);
		}

		data.WriteBit(AutoLaunched);
		data.WriteBit(false); // Unused
		data.FlushBits();

		Rewards.Write(data);
	}
}