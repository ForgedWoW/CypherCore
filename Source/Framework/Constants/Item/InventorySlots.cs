// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public struct InventorySlots
{
    public const byte BagStart = 30;
    public const byte BagEnd = 34;

    public const byte ReagentBagStart = 34;
    public const byte ReagentBagEnd = 35;

    public const byte ItemStart = 35;
    public const byte ItemEnd = 63;

    public const byte BankItemStart = 63;
    public const byte BankItemEnd = 91;

    public const byte BankBagStart = 91;
    public const byte BankBagEnd = 98;

    public const byte BuyBackStart = 98;
    public const byte BuyBackEnd = 110;

    public const byte ReagentStart = 110;
    public const byte ReagentEnd = 208;

    public const byte ChildEquipmentStart = 208;
    public const byte ChildEquipmentEnd = 211;

    public const byte Bag0 = 255;
    public const byte DefaultSize = 16;
}