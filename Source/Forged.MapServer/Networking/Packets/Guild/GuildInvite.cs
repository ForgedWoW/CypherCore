// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildInvite : ServerPacket
{
    public int AchievementPoints;
    public uint Background;
    public uint BorderColor;
    public uint BorderStyle;
    public uint EmblemColor;
    public uint EmblemStyle;
    public ObjectGuid GuildGUID;
    public string GuildName;
    public uint GuildVirtualRealmAddress;
    public string InviterName;
    public uint InviterVirtualRealmAddress;
    public ObjectGuid OldGuildGUID;
    public string OldGuildName;
    public uint OldGuildVirtualRealmAddress;
    public GuildInvite() : base(ServerOpcodes.GuildInvite) { }

    public override void Write()
    {
        _worldPacket.WriteBits(InviterName.GetByteCount(), 6);
        _worldPacket.WriteBits(GuildName.GetByteCount(), 7);
        _worldPacket.WriteBits(OldGuildName.GetByteCount(), 7);

        _worldPacket.WriteUInt32(InviterVirtualRealmAddress);
        _worldPacket.WriteUInt32(GuildVirtualRealmAddress);
        _worldPacket.WritePackedGuid(GuildGUID);
        _worldPacket.WriteUInt32(OldGuildVirtualRealmAddress);
        _worldPacket.WritePackedGuid(OldGuildGUID);
        _worldPacket.WriteUInt32(EmblemStyle);
        _worldPacket.WriteUInt32(EmblemColor);
        _worldPacket.WriteUInt32(BorderStyle);
        _worldPacket.WriteUInt32(BorderColor);
        _worldPacket.WriteUInt32(Background);
        _worldPacket.WriteInt32(AchievementPoints);

        _worldPacket.WriteString(InviterName);
        _worldPacket.WriteString(GuildName);
        _worldPacket.WriteString(OldGuildName);
    }
}