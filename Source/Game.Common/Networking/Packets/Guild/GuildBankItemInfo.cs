// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

public class GuildBankItemInfo
{
	public ItemInstance Item = new();
	public int Slot;
	public int Count;
	public int EnchantmentID;
	public int Charges;
	public int OnUseEnchantmentID;
	public uint Flags;
	public bool Locked;
	public List<ItemGemData> SocketEnchant = new();
}