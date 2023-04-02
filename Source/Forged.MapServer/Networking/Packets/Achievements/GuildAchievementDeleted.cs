// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class GuildAchievementDeleted : ServerPacket
{
    public uint AchievementID;
    public ObjectGuid GuildGUID;
    public long TimeDeleted;
    public GuildAchievementDeleted() : base(ServerOpcodes.GuildAchievementDeleted) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(GuildGUID);
        WorldPacket.WriteUInt32(AchievementID);
        WorldPacket.WritePackedTime(TimeDeleted);
    }
}