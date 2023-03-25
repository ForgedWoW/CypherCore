// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.LFG;

public class LfgPlayerDungeonInfo
{
	public uint Slot;
	public int CompletionQuantity;
	public int CompletionLimit;
	public int CompletionCurrencyID;
	public int SpecificQuantity;
	public int SpecificLimit;
	public int OverallQuantity;
	public int OverallLimit;
	public int PurseWeeklyQuantity;
	public int PurseWeeklyLimit;
	public int PurseQuantity;
	public int PurseLimit;
	public int Quantity;
	public uint CompletedMask;
	public uint EncounterMask;
	public bool FirstReward;
	public bool ShortageEligible;
	public LfgPlayerQuestReward Rewards = new();
	public List<LfgPlayerQuestReward> ShortageReward = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Slot);
		data.WriteInt32(CompletionQuantity);
		data.WriteInt32(CompletionLimit);
		data.WriteInt32(CompletionCurrencyID);
		data.WriteInt32(SpecificQuantity);
		data.WriteInt32(SpecificLimit);
		data.WriteInt32(OverallQuantity);
		data.WriteInt32(OverallLimit);
		data.WriteInt32(PurseWeeklyQuantity);
		data.WriteInt32(PurseWeeklyLimit);
		data.WriteInt32(PurseQuantity);
		data.WriteInt32(PurseLimit);
		data.WriteInt32(Quantity);
		data.WriteUInt32(CompletedMask);
		data.WriteUInt32(EncounterMask);
		data.WriteInt32(ShortageReward.Count);
		data.WriteBit(FirstReward);
		data.WriteBit(ShortageEligible);
		data.FlushBits();

		Rewards.Write(data);

		foreach (var shortageReward in ShortageReward)
			shortageReward.Write(data);
	}
}