// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGPlayerReward : ServerPacket
{
    public uint QueuedSlot;
    public uint ActualSlot;
    public uint RewardMoney;
    public uint AddedXP;
    public List<LFGPlayerRewards> Rewards = new();
    public LFGPlayerReward() : base(ServerOpcodes.LfgPlayerReward) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(QueuedSlot);
        _worldPacket.WriteUInt32(ActualSlot);
        _worldPacket.WriteUInt32(RewardMoney);
        _worldPacket.WriteUInt32(AddedXP);
        _worldPacket.WriteInt32(Rewards.Count);

        foreach (var reward in Rewards)
            reward.Write(_worldPacket);
    }
}