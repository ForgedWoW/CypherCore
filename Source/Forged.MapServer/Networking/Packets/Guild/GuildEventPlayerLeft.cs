// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventPlayerLeft : ServerPacket
{
    public ObjectGuid LeaverGUID;
    public string LeaverName;
    public uint LeaverVirtualRealmAddress;
    public bool Removed;
    public ObjectGuid RemoverGUID;
    public string RemoverName;
    public uint RemoverVirtualRealmAddress;
    public GuildEventPlayerLeft() : base(ServerOpcodes.GuildEventPlayerLeft) { }

    public override void Write()
    {
        WorldPacket.WriteBit(Removed);
        WorldPacket.WriteBits(LeaverName.GetByteCount(), 6);

        if (Removed)
        {
            WorldPacket.WriteBits(RemoverName.GetByteCount(), 6);
            WorldPacket.WritePackedGuid(RemoverGUID);
            WorldPacket.WriteUInt32(RemoverVirtualRealmAddress);
            WorldPacket.WriteString(RemoverName);
        }

        WorldPacket.WritePackedGuid(LeaverGUID);
        WorldPacket.WriteUInt32(LeaverVirtualRealmAddress);
        WorldPacket.WriteString(LeaverName);
    }
}