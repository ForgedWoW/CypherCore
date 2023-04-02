// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class BroadcastAchievement : ServerPacket
{
    public uint AchievementID;
    public bool GuildAchievement;
    public string Name = "";
    public ObjectGuid PlayerGUID;
    public BroadcastAchievement() : base(ServerOpcodes.BroadcastAchievement) { }

    public override void Write()
    {
        _worldPacket.WriteBits(Name.GetByteCount(), 7);
        _worldPacket.WriteBit(GuildAchievement);
        _worldPacket.WritePackedGuid(PlayerGUID);
        _worldPacket.WriteUInt32(AchievementID);
        _worldPacket.WriteString(Name);
    }
}