// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRoster : ServerPacket
{
    public uint CreateDate;
    public int GuildFlags;
    public string InfoText;
    public List<GuildRosterMemberData> MemberData;
    public int NumAccounts;
    public string WelcomeText;
    public GuildRoster() : base(ServerOpcodes.GuildRoster)
    {
        MemberData = new List<GuildRosterMemberData>();
    }

    public override void Write()
    {
        WorldPacket.WriteInt32(NumAccounts);
        WorldPacket.WritePackedTime(CreateDate);
        WorldPacket.WriteInt32(GuildFlags);
        WorldPacket.WriteInt32(MemberData.Count);
        WorldPacket.WriteBits(WelcomeText.GetByteCount(), 11);
        WorldPacket.WriteBits(InfoText.GetByteCount(), 10);
        WorldPacket.FlushBits();

        MemberData.ForEach(p => p.Write(WorldPacket));

        WorldPacket.WriteString(WelcomeText);
        WorldPacket.WriteString(InfoText);
    }
}