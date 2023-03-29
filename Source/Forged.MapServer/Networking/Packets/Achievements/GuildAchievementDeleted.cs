// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class GuildAchievementDeleted : ServerPacket
{
    public ObjectGuid GuildGUID;
    public uint AchievementID;
    public long TimeDeleted;
    public GuildAchievementDeleted() : base(ServerOpcodes.GuildAchievementDeleted) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(GuildGUID);
        _worldPacket.WriteUInt32(AchievementID);
        _worldPacket.WritePackedTime(TimeDeleted);
    }
}