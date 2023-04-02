// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankItemInfo
{
    public int Charges;
    public int Count;
    public int EnchantmentID;
    public uint Flags;
    public ItemInstance Item = new();
    public bool Locked;
    public int OnUseEnchantmentID;
    public int Slot;
    public List<ItemGemData> SocketEnchant = new();
}