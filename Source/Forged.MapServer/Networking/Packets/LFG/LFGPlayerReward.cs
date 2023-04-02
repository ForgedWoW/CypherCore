// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGPlayerReward : ServerPacket
{
    public uint ActualSlot;
    public uint AddedXP;
    public uint QueuedSlot;
    public uint RewardMoney;
    public List<LFGPlayerRewards> Rewards = new();
    public LFGPlayerReward() : base(ServerOpcodes.LfgPlayerReward) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QueuedSlot);
        WorldPacket.WriteUInt32(ActualSlot);
        WorldPacket.WriteUInt32(RewardMoney);
        WorldPacket.WriteUInt32(AddedXP);
        WorldPacket.WriteInt32(Rewards.Count);

        foreach (var reward in Rewards)
            reward.Write(WorldPacket);
    }
}