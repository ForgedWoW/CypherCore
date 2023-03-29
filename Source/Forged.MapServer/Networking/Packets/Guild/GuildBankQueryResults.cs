// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankQueryResults : ServerPacket
{
    public List<GuildBankItemInfo> ItemInfo;
    public List<GuildBankTabInfo> TabInfo;
    public int WithdrawalsRemaining;
    public int Tab;
    public ulong Money;
    public bool FullUpdate;

    public GuildBankQueryResults() : base(ServerOpcodes.GuildBankQueryResults)
    {
        ItemInfo = new List<GuildBankItemInfo>();
        TabInfo = new List<GuildBankTabInfo>();
    }

    public override void Write()
    {
        _worldPacket.WriteUInt64(Money);
        _worldPacket.WriteInt32(Tab);
        _worldPacket.WriteInt32(WithdrawalsRemaining);
        _worldPacket.WriteInt32(TabInfo.Count);
        _worldPacket.WriteInt32(ItemInfo.Count);
        _worldPacket.WriteBit(FullUpdate);
        _worldPacket.FlushBits();

        foreach (var tab in TabInfo)
        {
            _worldPacket.WriteInt32(tab.TabIndex);
            _worldPacket.WriteBits(tab.Name.GetByteCount(), 7);
            _worldPacket.WriteBits(tab.Icon.GetByteCount(), 9);

            _worldPacket.WriteString(tab.Name);
            _worldPacket.WriteString(tab.Icon);
        }

        foreach (var item in ItemInfo)
        {
            _worldPacket.WriteInt32(item.Slot);
            _worldPacket.WriteInt32(item.Count);
            _worldPacket.WriteInt32(item.EnchantmentID);
            _worldPacket.WriteInt32(item.Charges);
            _worldPacket.WriteInt32(item.OnUseEnchantmentID);
            _worldPacket.WriteUInt32(item.Flags);

            item.Item.Write(_worldPacket);

            _worldPacket.WriteBits(item.SocketEnchant.Count, 2);
            _worldPacket.WriteBit(item.Locked);
            _worldPacket.FlushBits();

            foreach (var socketEnchant in item.SocketEnchant)
                socketEnchant.Write(_worldPacket);
        }
    }
}