// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

public class LootItemData
{
    public bool CanTradeToTapList;
    public ItemInstance Loot;
    public byte LootItemType;
    public byte LootListID;
    public uint Quantity;
    public byte Type;
    public LootSlotType UIType;

    public void Write(WorldPacket data)
    {
        data.WriteBits(Type, 2);
        data.WriteBits(UIType, 3);
        data.WriteBit(CanTradeToTapList);
        data.FlushBits();
        Loot.Write(data); // WorldPackets::Item::ItemInstance
        data.WriteUInt32(Quantity);
        data.WriteUInt8(LootItemType);
        data.WriteUInt8(LootListID);
    }
}