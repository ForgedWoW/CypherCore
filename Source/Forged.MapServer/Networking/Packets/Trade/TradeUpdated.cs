// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Trade;

public class TradeUpdated : ServerPacket
{
    public uint ClientStateIndex;
    public int CurrencyQuantity;
    public int CurrencyType;
    public uint CurrentStateIndex;
    public ulong Gold;
    public uint Id;
    public List<TradeItem> Items = new();
    public int ProposedEnchantment;
    public byte WhichPlayer;
    public TradeUpdated() : base(ServerOpcodes.TradeUpdated, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(WhichPlayer);
        WorldPacket.WriteUInt32(Id);
        WorldPacket.WriteUInt32(ClientStateIndex);
        WorldPacket.WriteUInt32(CurrentStateIndex);
        WorldPacket.WriteUInt64(Gold);
        WorldPacket.WriteInt32(CurrencyType);
        WorldPacket.WriteInt32(CurrencyQuantity);
        WorldPacket.WriteInt32(ProposedEnchantment);
        WorldPacket.WriteInt32(Items.Count);

        Items.ForEach(item => item.Write(WorldPacket));
    }

    public class TradeItem
    {
        public ObjectGuid GiftCreator;
        public ItemInstance Item = new();
        public byte Slot;
        public int StackCount;
        public UnwrappedTradeItem Unwrapped;

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Slot);
            data.WriteInt32(StackCount);
            data.WritePackedGuid(GiftCreator);
            Item.Write(data);
            data.WriteBit(Unwrapped != null);
            data.FlushBits();

            Unwrapped?.Write(data);
        }
    }

    public class UnwrappedTradeItem
    {
        public int Charges;
        public ObjectGuid Creator;
        public uint Durability;
        public int EnchantID;
        public List<ItemGemData> Gems = new();
        public ItemInstance Item;
        public bool Lock;
        public uint MaxDurability;
        public int OnUseEnchantmentID;
        public void Write(WorldPacket data)
        {
            data.WriteInt32(EnchantID);
            data.WriteInt32(OnUseEnchantmentID);
            data.WritePackedGuid(Creator);
            data.WriteInt32(Charges);
            data.WriteUInt32(MaxDurability);
            data.WriteUInt32(Durability);
            data.WriteBits(Gems.Count, 2);
            data.WriteBit(Lock);
            data.FlushBits();

            foreach (var gem in Gems)
                gem.Write(data);
        }
    }
}