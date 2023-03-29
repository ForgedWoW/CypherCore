// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class GuildAchievementEarned : ServerPacket
{
    public uint AchievementID;
    public ObjectGuid GuildGUID;
    public long TimeEarned;
    public GuildAchievementEarned() : base(ServerOpcodes.GuildAchievementEarned) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(GuildGUID);
        _worldPacket.WriteUInt32(AchievementID);
        _worldPacket.WritePackedTime(TimeEarned);
    }
}