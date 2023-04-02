// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

public class LootResponse : ServerPacket
{
    public bool Acquired;
    public byte AcquireReason;
    public bool AELooting;
    public uint Coins;
    public List<LootCurrency> Currencies = new();
    public LootError FailureReason = LootError.NoLoot;
    // Most common value
    public List<LootItemData> Items = new();

    public LootMethod LootMethod;
    public ObjectGuid LootObj;
    public ObjectGuid Owner;
    public byte Threshold = 2; // Most common value, 2 = Uncommon
    public LootResponse() : base(ServerOpcodes.LootResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Owner);
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WriteUInt8((byte)FailureReason);
        WorldPacket.WriteUInt8(AcquireReason);
        WorldPacket.WriteUInt8((byte)LootMethod);
        WorldPacket.WriteUInt8(Threshold);
        WorldPacket.WriteUInt32(Coins);
        WorldPacket.WriteInt32(Items.Count);
        WorldPacket.WriteInt32(Currencies.Count);
        WorldPacket.WriteBit(Acquired);
        WorldPacket.WriteBit(AELooting);
        WorldPacket.FlushBits();

        foreach (var item in Items)
            item.Write(WorldPacket);

        foreach (var currency in Currencies)
        {
            WorldPacket.WriteUInt32(currency.CurrencyID);
            WorldPacket.WriteUInt32(currency.Quantity);
            WorldPacket.WriteUInt8(currency.LootListID);
            WorldPacket.WriteBits(currency.UIType, 3);
            WorldPacket.FlushBits();
        }
    }
}