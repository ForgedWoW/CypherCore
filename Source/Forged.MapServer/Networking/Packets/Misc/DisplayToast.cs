// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class DisplayToast : ServerPacket
{
    public bool BonusRoll;
    public uint CurrencyID;
    public DisplayToastMethod DisplayToastMethod;
    public Gender Gender = Gender.None;
    public bool IsSecondaryResult;
    public ItemInstance Item;
    public int LootSpec;
    public bool Mailed;
    public ulong Quantity;
    public uint QuestID;
    public DisplayToastType Type = DisplayToastType.Money;
    public DisplayToast() : base(ServerOpcodes.DisplayToast, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(Quantity);
        _worldPacket.WriteUInt8((byte)DisplayToastMethod);
        _worldPacket.WriteUInt32(QuestID);

        _worldPacket.WriteBit(Mailed);
        _worldPacket.WriteBits((byte)Type, 2);
        _worldPacket.WriteBit(IsSecondaryResult);

        switch (Type)
        {
            case DisplayToastType.NewItem:
                _worldPacket.WriteBit(BonusRoll);
                Item.Write(_worldPacket);
                _worldPacket.WriteInt32(LootSpec);
                _worldPacket.WriteInt32((int)Gender);

                break;
            case DisplayToastType.NewCurrency:
                _worldPacket.WriteUInt32(CurrencyID);

                break;
            default:
                break;
        }

        _worldPacket.FlushBits();
    }
}