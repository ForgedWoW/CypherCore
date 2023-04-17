// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Guilds;

public class GuildRankInfo
{
    private readonly GuildBankRightsAndSlots[] _bankTabRightsAndSlots = new GuildBankRightsAndSlots[GuildConst.MaxBankTabs];
    private readonly CharacterDatabase _characterDatabase;
    private readonly ulong _guildId;
    private uint _bankMoneyPerDay;

    public GuildRankInfo(CharacterDatabase characterDatabase, ulong guildId = 0)
    {
        _characterDatabase = characterDatabase;
        _guildId = guildId;
        Id = (GuildRankId)0xFF;
        Order = 0;
        AccessRights = GuildRankRights.None;
        _bankMoneyPerDay = 0;

        for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
            _bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
    }

    public GuildRankInfo(ulong guildId, GuildRankId rankId, GuildRankOrder rankOrder, string name, GuildRankRights rights, uint money)
    {
        _guildId = guildId;
        Id = rankId;
        Order = rankOrder;
        Name = name;
        AccessRights = rights;
        _bankMoneyPerDay = money;

        for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
            _bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
    }

    public GuildRankRights AccessRights { get; set; }

    public uint BankMoneyPerDay => Id != GuildRankId.GuildMaster ? _bankMoneyPerDay : GuildConst.WithdrawMoneyUnlimited;

    public GuildRankId Id { get; set; }

    public string Name { get; set; }

    public GuildRankOrder Order { get; set; }

    public void CreateMissingTabsIfNeeded(byte tabs, SQLTransaction trans, bool logOnCreate = false)
    {
        for (byte i = 0; i < tabs; ++i)
        {
            var rightsAndSlots = _bankTabRightsAndSlots[i];

            if (rightsAndSlots.TabId == i)
                continue;

            rightsAndSlots.TabId = i;

            if (Id == GuildRankId.GuildMaster)
                rightsAndSlots.SetGuildMasterValues();

            if (logOnCreate)
                Log.Logger.Error($"Guild {_guildId} has broken Tab {i} for rank {Id}. Created default tab.");

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
            stmt.AddValue(0, _guildId);
            stmt.AddValue(1, i);
            stmt.AddValue(2, (byte)Id);
            stmt.AddValue(3, (sbyte)rightsAndSlots.Rights);
            stmt.AddValue(4, rightsAndSlots.Slots);
            trans.Append(stmt);
        }
    }

    public GuildBankRights GetBankTabRights(byte tabId)
    {
        return tabId < GuildConst.MaxBankTabs ? _bankTabRightsAndSlots[tabId].Rights : 0;
    }

    public int GetBankTabSlotsPerDay(byte tabId)
    {
        return tabId < GuildConst.MaxBankTabs ? _bankTabRightsAndSlots[tabId].Slots : 0;
    }

    public void LoadFromDB(SQLFields field)
    {
        Id = (GuildRankId)field.Read<byte>(1);
        Order = (GuildRankOrder)field.Read<byte>(2);
        Name = field.Read<string>(3);
        AccessRights = (GuildRankRights)field.Read<uint>(4);
        _bankMoneyPerDay = field.Read<uint>(5);

        if (Id == GuildRankId.GuildMaster) // Prevent loss of leader rights
            AccessRights |= GuildRankRights.All;
    }

    public void SaveToDB(SQLTransaction trans)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_RANK);
        stmt.AddValue(0, _guildId);
        stmt.AddValue(1, (byte)Id);
        stmt.AddValue(2, (byte)Order);
        stmt.AddValue(3, Name);
        stmt.AddValue(4, (uint)AccessRights);
        stmt.AddValue(5, _bankMoneyPerDay);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public void SetBankMoneyPerDay(uint money)
    {
        if (_bankMoneyPerDay == money)
            return;

        _bankMoneyPerDay = money;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_BANK_MONEY);
        stmt.AddValue(0, money);
        stmt.AddValue(1, (byte)Id);
        stmt.AddValue(2, _guildId);
        _characterDatabase.Execute(stmt);
    }

    public void SetBankTabSlotsAndRights(GuildBankRightsAndSlots rightsAndSlots, bool saveToDB)
    {
        if (Id == GuildRankId.GuildMaster) // Prevent loss of leader rights
            rightsAndSlots.SetGuildMasterValues();

        _bankTabRightsAndSlots[rightsAndSlots.TabId] = rightsAndSlots;

        if (!saveToDB)
            return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
        stmt.AddValue(0, _guildId);
        stmt.AddValue(1, rightsAndSlots.TabId);
        stmt.AddValue(2, (byte)Id);
        stmt.AddValue(3, (sbyte)rightsAndSlots.Rights);
        stmt.AddValue(4, rightsAndSlots.Slots);
        _characterDatabase.Execute(stmt);
    }

    public void SetName(string name)
    {
        if (Name == name)
            return;

        Name = name;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_NAME);
        stmt.AddValue(0, Name);
        stmt.AddValue(1, (byte)Id);
        stmt.AddValue(2, _guildId);
        _characterDatabase.Execute(stmt);
    }

    public void SetOrder(GuildRankOrder rankOrder)
    {
        Order = rankOrder;
    }

    public void SetRights(GuildRankRights rights)
    {
        if (Id == GuildRankId.GuildMaster) // Prevent loss of leader rights
            rights = GuildRankRights.All;

        if (AccessRights == rights)
            return;

        AccessRights = rights;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_RIGHTS);
        stmt.AddValue(0, (uint)AccessRights);
        stmt.AddValue(1, (byte)Id);
        stmt.AddValue(2, _guildId);
        _characterDatabase.Execute(stmt);
    }
}