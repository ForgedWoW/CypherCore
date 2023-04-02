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
        _worldPacket.WriteInt32(NumAccounts);
        _worldPacket.WritePackedTime(CreateDate);
        _worldPacket.WriteInt32(GuildFlags);
        _worldPacket.WriteInt32(MemberData.Count);
        _worldPacket.WriteBits(WelcomeText.GetByteCount(), 11);
        _worldPacket.WriteBits(InfoText.GetByteCount(), 10);
        _worldPacket.FlushBits();

        MemberData.ForEach(p => p.Write(_worldPacket));

        _worldPacket.WriteString(WelcomeText);
        _worldPacket.WriteString(InfoText);
    }
}