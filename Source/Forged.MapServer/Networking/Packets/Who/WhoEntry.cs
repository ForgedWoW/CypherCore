// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Query;

namespace Forged.MapServer.Networking.Packets.Who;

public class WhoEntry
{
    public int AreaID;
    public ObjectGuid GuildGUID;
    public string GuildName = "";
    public uint GuildVirtualRealmAddress;
    public bool IsGM;
    public PlayerGuidLookupData PlayerData = new();

    public void Write(WorldPacket data)
    {
        PlayerData.Write(data);

        data.WritePackedGuid(GuildGUID);
        data.WriteUInt32(GuildVirtualRealmAddress);
        data.WriteInt32(AreaID);

        data.WriteBits(GuildName.GetByteCount(), 7);
        data.WriteBit(IsGM);
        data.WriteString(GuildName);

        data.FlushBits();
    }
}