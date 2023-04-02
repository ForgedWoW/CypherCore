// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventNewLeader : ServerPacket
{
    public ObjectGuid NewLeaderGUID;
    public string NewLeaderName;
    public uint NewLeaderVirtualRealmAddress;
    public ObjectGuid OldLeaderGUID;
    public string OldLeaderName = "";
    public uint OldLeaderVirtualRealmAddress;
    public bool SelfPromoted;
    public GuildEventNewLeader() : base(ServerOpcodes.GuildEventNewLeader) { }

    public override void Write()
    {
        WorldPacket.WriteBit(SelfPromoted);
        WorldPacket.WriteBits(OldLeaderName.GetByteCount(), 6);
        WorldPacket.WriteBits(NewLeaderName.GetByteCount(), 6);

        WorldPacket.WritePackedGuid(OldLeaderGUID);
        WorldPacket.WriteUInt32(OldLeaderVirtualRealmAddress);
        WorldPacket.WritePackedGuid(NewLeaderGUID);
        WorldPacket.WriteUInt32(NewLeaderVirtualRealmAddress);

        WorldPacket.WriteString(OldLeaderName);
        WorldPacket.WriteString(NewLeaderName);
    }
}