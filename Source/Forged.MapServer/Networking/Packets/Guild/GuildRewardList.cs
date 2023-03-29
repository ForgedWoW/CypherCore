// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRewardList : ServerPacket
{
    public List<GuildRewardItem> RewardItems;
    public long Version;

    public GuildRewardList() : base(ServerOpcodes.GuildRewardList)
    {
        RewardItems = new List<GuildRewardItem>();
    }

    public override void Write()
    {
        _worldPacket.WriteInt64(Version);
        _worldPacket.WriteInt32(RewardItems.Count);

        foreach (var item in RewardItems)
            item.Write(_worldPacket);
    }
}