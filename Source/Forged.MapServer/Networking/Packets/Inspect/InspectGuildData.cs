// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Inspect;

public struct InspectGuildData
{
    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(GuildGUID);
        data.WriteInt32(NumGuildMembers);
        data.WriteInt32(AchievementPoints);
    }

    public ObjectGuid GuildGUID;
    public int NumGuildMembers;
    public int AchievementPoints;
}