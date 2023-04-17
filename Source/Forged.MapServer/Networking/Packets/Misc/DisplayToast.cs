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
        WorldPacket.WriteUInt64(Quantity);
        WorldPacket.WriteUInt8((byte)DisplayToastMethod);
        WorldPacket.WriteUInt32(QuestID);

        WorldPacket.WriteBit(Mailed);
        WorldPacket.WriteBits((byte)Type, 2);
        WorldPacket.WriteBit(IsSecondaryResult);

        switch (Type)
        {
            case DisplayToastType.NewItem:
                WorldPacket.WriteBit(BonusRoll);
                Item.Write(WorldPacket);
                WorldPacket.WriteInt32(LootSpec);
                WorldPacket.WriteInt32((int)Gender);

                break;
            case DisplayToastType.NewCurrency:
                WorldPacket.WriteUInt32(CurrencyID);

                break;
        }

        WorldPacket.FlushBits();
    }
}