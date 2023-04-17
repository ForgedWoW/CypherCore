// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.LFG;

public class LfgPlayerDungeonInfo
{
    public uint CompletedMask;
    public int CompletionCurrencyID;
    public int CompletionLimit;
    public int CompletionQuantity;
    public uint EncounterMask;
    public bool FirstReward;
    public int OverallLimit;
    public int OverallQuantity;
    public int PurseLimit;
    public int PurseQuantity;
    public int PurseWeeklyLimit;
    public int PurseWeeklyQuantity;
    public int Quantity;
    public LfgPlayerQuestReward Rewards = new();
    public bool ShortageEligible;
    public List<LfgPlayerQuestReward> ShortageReward = new();
    public uint Slot;
    public int SpecificLimit;
    public int SpecificQuantity;

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