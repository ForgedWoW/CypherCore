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
        WorldPacket.WriteBits(InviterName.GetByteCount(), 6);
        WorldPacket.WriteBits(GuildName.GetByteCount(), 7);
        WorldPacket.WriteBits(OldGuildName.GetByteCount(), 7);

        WorldPacket.WriteUInt32(InviterVirtualRealmAddress);
        WorldPacket.WriteUInt32(GuildVirtualRealmAddress);
        WorldPacket.WritePackedGuid(GuildGUID);
        WorldPacket.WriteUInt32(OldGuildVirtualRealmAddress);
        WorldPacket.WritePackedGuid(OldGuildGUID);
        WorldPacket.WriteUInt32(EmblemStyle);
        WorldPacket.WriteUInt32(EmblemColor);
        WorldPacket.WriteUInt32(BorderStyle);
        WorldPacket.WriteUInt32(BorderColor);
        WorldPacket.WriteUInt32(Background);
        WorldPacket.WriteInt32(AchievementPoints);

        WorldPacket.WriteString(InviterName);
        WorldPacket.WriteString(GuildName);
        WorldPacket.WriteString(OldGuildName);
    }
}