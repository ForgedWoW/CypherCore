// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankQueryResults : ServerPacket
{
    public bool FullUpdate;
    public List<GuildBankItemInfo> ItemInfo;
    public ulong Money;
    public int Tab;
    public List<GuildBankTabInfo> TabInfo;
    public int WithdrawalsRemaining;
    public GuildBankQueryResults() : base(ServerOpcodes.GuildBankQueryResults)
    {
        ItemInfo = new List<GuildBankItemInfo>();
        TabInfo = new List<GuildBankTabInfo>();
    }

    public override void Write()
    {
        WorldPacket.WriteUInt64(Money);
        WorldPacket.WriteInt32(Tab);
        WorldPacket.WriteInt32(WithdrawalsRemaining);
        WorldPacket.WriteInt32(TabInfo.Count);
        WorldPacket.WriteInt32(ItemInfo.Count);
        WorldPacket.WriteBit(FullUpdate);
        WorldPacket.FlushBits();

        foreach (var tab in TabInfo)
        {
            WorldPacket.WriteInt32(tab.TabIndex);
            WorldPacket.WriteBits(tab.Name.GetByteCount(), 7);
            WorldPacket.WriteBits(tab.Icon.GetByteCount(), 9);

            WorldPacket.WriteString(tab.Name);
            WorldPacket.WriteString(tab.Icon);
        }

        foreach (var item in ItemInfo)
        {
            WorldPacket.WriteInt32(item.Slot);
            WorldPacket.WriteInt32(item.Count);
            WorldPacket.WriteInt32(item.EnchantmentID);
            WorldPacket.WriteInt32(item.Charges);
            WorldPacket.WriteInt32(item.OnUseEnchantmentID);
            WorldPacket.WriteUInt32(item.Flags);

            item.Item.Write(WorldPacket);

            WorldPacket.WriteBits(item.SocketEnchant.Count, 2);
            WorldPacket.WriteBit(item.Locked);
            WorldPacket.FlushBits();

            foreach (var socketEnchant in item.SocketEnchant)
                socketEnchant.Write(WorldPacket);
        }
    }
}