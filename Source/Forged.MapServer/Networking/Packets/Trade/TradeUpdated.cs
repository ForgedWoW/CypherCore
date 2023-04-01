// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Trade;

public class TradeUpdated : ServerPacket
{
    public ulong Gold;
    public uint CurrentStateIndex;
    public byte WhichPlayer;
    public uint ClientStateIndex;
    public List<TradeItem> Items = new();
    public int CurrencyType;
    public uint Id;
    public int ProposedEnchantment;
    public int CurrencyQuantity;
    public TradeUpdated() : base(ServerOpcodes.TradeUpdated, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt8(WhichPlayer);
        _worldPacket.WriteUInt32(Id);
        _worldPacket.WriteUInt32(ClientStateIndex);
        _worldPacket.WriteUInt32(CurrentStateIndex);
        _worldPacket.WriteUInt64(Gold);
        _worldPacket.WriteInt32(CurrencyType);
        _worldPacket.WriteInt32(CurrencyQuantity);
        _worldPacket.WriteInt32(ProposedEnchantment);
        _worldPacket.WriteInt32(Items.Count);

        Items.ForEach(item => item.Write(_worldPacket));
    }

    public class UnwrappedTradeItem
    {
        public ItemInstance Item;
        public int EnchantID;
        public int OnUseEnchantmentID;
        public ObjectGuid Creator;
        public int Charges;
        public bool Lock;
        public uint MaxDurability;
        public uint Durability;
        public List<ItemGemData> Gems = new();

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

    public class TradeItem
    {
        public byte Slot;
        public ItemInstance Item = new();
        public int StackCount;
        public ObjectGuid GiftCreator;
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
}