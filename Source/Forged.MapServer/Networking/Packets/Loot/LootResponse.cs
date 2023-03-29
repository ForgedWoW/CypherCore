// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

public class LootResponse : ServerPacket
{
    public ObjectGuid LootObj;
    public ObjectGuid Owner;
    public byte Threshold = 2; // Most common value, 2 = Uncommon
    public LootMethod LootMethod;
    public byte AcquireReason;
    public LootError FailureReason = LootError.NoLoot; // Most common value
    public uint Coins;
    public List<LootItemData> Items = new();
    public List<LootCurrency> Currencies = new();
    public bool Acquired;
    public bool AELooting;
    public LootResponse() : base(ServerOpcodes.LootResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Owner);
        _worldPacket.WritePackedGuid(LootObj);
        _worldPacket.WriteUInt8((byte)FailureReason);
        _worldPacket.WriteUInt8(AcquireReason);
        _worldPacket.WriteUInt8((byte)LootMethod);
        _worldPacket.WriteUInt8(Threshold);
        _worldPacket.WriteUInt32(Coins);
        _worldPacket.WriteInt32(Items.Count);
        _worldPacket.WriteInt32(Currencies.Count);
        _worldPacket.WriteBit(Acquired);
        _worldPacket.WriteBit(AELooting);
        _worldPacket.FlushBits();

        foreach (var item in Items)
            item.Write(_worldPacket);

        foreach (var currency in Currencies)
        {
            _worldPacket.WriteUInt32(currency.CurrencyID);
            _worldPacket.WriteUInt32(currency.Quantity);
            _worldPacket.WriteUInt8(currency.LootListID);
            _worldPacket.WriteBits(currency.UIType, 3);
            _worldPacket.FlushBits();
        }
    }
}