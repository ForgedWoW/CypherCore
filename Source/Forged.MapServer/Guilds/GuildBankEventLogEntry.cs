// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Guild;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public class GuildBankEventLogEntry : GuildLogEntry
{
    private readonly byte _bankTabId;
    private readonly CharacterDatabase _characterDatabase;
    private readonly byte _destTabId;
    private readonly GuildBankEventLogTypes _eventType;
    private readonly ulong _itemOrMoney;
    private readonly ushort _itemStackCount;
    private readonly ulong _playerGuid;

    public GuildBankEventLogEntry(ulong guildId, uint guid, GuildBankEventLogTypes eventType, byte tabId, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId, CharacterDatabase characterDatabase)
        : base(guildId, guid)
    {
        _eventType = eventType;
        _bankTabId = tabId;
        _playerGuid = playerGuid;
        _itemOrMoney = itemOrMoney;
        _itemStackCount = itemStackCount;
        _destTabId = destTabId;
        _characterDatabase = characterDatabase;
    }

    public GuildBankEventLogEntry(ulong guildId, uint guid, long timestamp, byte tabId, GuildBankEventLogTypes eventType, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId, CharacterDatabase characterDatabase)
        : base(guildId, guid, timestamp)
    {
        _eventType = eventType;
        _bankTabId = tabId;
        _playerGuid = playerGuid;
        _itemOrMoney = itemOrMoney;
        _itemStackCount = itemStackCount;
        _destTabId = destTabId;
        _characterDatabase = characterDatabase;
    }

    public static bool IsMoneyEvent(GuildBankEventLogTypes eventType)
    {
        return
            eventType is GuildBankEventLogTypes.DepositMoney or GuildBankEventLogTypes.WithdrawMoney or GuildBankEventLogTypes.RepairMoney or GuildBankEventLogTypes.CashFlowDeposit;
    }

    public override void SaveToDB(SQLTransaction trans)
    {
        byte index = 0;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG);
        stmt.AddValue(index, GuildId);
        stmt.AddValue(++index, GUID);
        stmt.AddValue(++index, _bankTabId);
        trans.Append(stmt);

        index = 0;
        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_EVENTLOG);
        stmt.AddValue(index, GuildId);
        stmt.AddValue(++index, GUID);
        stmt.AddValue(++index, _bankTabId);
        stmt.AddValue(++index, (byte)_eventType);
        stmt.AddValue(++index, _playerGuid);
        stmt.AddValue(++index, _itemOrMoney);
        stmt.AddValue(++index, _itemStackCount);
        stmt.AddValue(++index, _destTabId);
        stmt.AddValue(++index, Timestamp);
        trans.Append(stmt);
    }

    public void WritePacket(GuildBankLogQueryResults packet)
    {
        var logGuid = ObjectGuid.Create(HighGuid.Player, _playerGuid);

        var hasItem = _eventType is GuildBankEventLogTypes.DepositItem or GuildBankEventLogTypes.WithdrawItem or GuildBankEventLogTypes.MoveItem or GuildBankEventLogTypes.MoveItem2;

        var itemMoved = _eventType is GuildBankEventLogTypes.MoveItem or GuildBankEventLogTypes.MoveItem2;

        var hasStack = (hasItem && _itemStackCount > 1) || itemMoved;

        GuildBankLogEntry bankLogEntry = new()
        {
            PlayerGUID = logGuid,
            TimeOffset = (uint)(GameTime.CurrentTime - Timestamp),
            EntryType = (sbyte)_eventType
        };

        if (hasStack)
            bankLogEntry.Count = _itemStackCount;

        if (IsMoneyEvent())
            bankLogEntry.Money = _itemOrMoney;

        if (hasItem)
            bankLogEntry.ItemID = (int)_itemOrMoney;

        if (itemMoved)
            bankLogEntry.OtherTab = (sbyte)_destTabId;

        packet.Entry.Add(bankLogEntry);
    }

    private bool IsMoneyEvent()
    {
        return IsMoneyEvent(_eventType);
    }
}