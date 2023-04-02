// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

internal class GuildAchievementMembers : ServerPacket
{
    public uint AchievementID;
    public ObjectGuid GuildGUID;
    public List<ObjectGuid> Member = new();
    public GuildAchievementMembers() : base(ServerOpcodes.GuildAchievementMembers) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(GuildGUID);
        _worldPacket.WriteUInt32(AchievementID);
        _worldPacket.WriteInt32(Member.Count);

        foreach (var guid in Member)
            _worldPacket.WritePackedGuid(guid);
    }
}