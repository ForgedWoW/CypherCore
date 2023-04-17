﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class ItemGemData
{
    public ItemInstance Item = new();
    public byte Slot;

    public void Read(WorldPacket data)
    {
        Slot = data.ReadUInt8();
        Item.Read(data);
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt8(Slot);
        Item.Write(data);
    }
}