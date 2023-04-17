// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRewardItem
{
    public List<uint> AchievementsRequired = new();
    public ulong Cost;
    public uint ItemID;
    public int MinGuildLevel;
    public int MinGuildRep;
    public ulong RaceMask;
    public uint Unk4;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(ItemID);
        data.WriteUInt32(Unk4);
        data.WriteInt32(AchievementsRequired.Count);
        data.WriteUInt64(RaceMask);
        data.WriteInt32(MinGuildLevel);
        data.WriteInt32(MinGuildRep);
        data.WriteUInt64(Cost);

        foreach (var achievementId in AchievementsRequired)
            data.WriteUInt32(achievementId);
    }
}