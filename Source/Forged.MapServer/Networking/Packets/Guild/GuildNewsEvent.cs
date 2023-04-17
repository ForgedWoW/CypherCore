// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildNewsEvent
{
    public uint CompletedDate;
    public int[] Data = new int[2];
    public int Flags;
    public int Id;
    public ItemInstance Item;
    public ObjectGuid MemberGuid;
    public List<ObjectGuid> MemberList = new();
    public int Type;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(Id);
        data.WritePackedTime(CompletedDate);
        data.WriteInt32(Type);
        data.WriteInt32(Flags);

        for (byte i = 0; i < 2; i++)
            data.WriteInt32(Data[i]);

        data.WritePackedGuid(MemberGuid);
        data.WriteInt32(MemberList.Count);

        foreach (var memberGuid in MemberList)
            data.WritePackedGuid(memberGuid);

        data.WriteBit(Item != null);
        data.FlushBits();

        if (Item != null)
            Item.Write(data);
    }
}