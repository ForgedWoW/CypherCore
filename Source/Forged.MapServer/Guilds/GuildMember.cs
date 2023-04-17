// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Guilds;

public class GuildMember
{
    private readonly uint[] _bankWithdraw = new uint[GuildConst.MaxBankTabs];

    private readonly CharacterDatabase _characterDatabase;

    private readonly CliDB _cliDB;

    private readonly ulong _guildId;

    private readonly ObjectAccessor _objectAccessor;

    private readonly PlayerComputators _playerComputators;

    private ObjectGuid _guid;

    public GuildMember(ulong guildId, ObjectGuid guid, GuildRankId rankId, CharacterDatabase characterDatabase, ObjectAccessor objectAccessor, PlayerComputators playerComputators, CliDB cliDB)
    {
        _guildId = guildId;
        _guid = guid;
        Flags = GuildMemberFlags.None;
        LogoutTime = (ulong)GameTime.CurrentTime;
        RankId = rankId;
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
        _playerComputators = playerComputators;
        _cliDB = cliDB;
    }

    public uint AccountId { get; set; }

    public uint AchievementPoints { get; set; }

    public ulong BankMoneyWithdrawValue { get; set; }

    public PlayerClass Class { get; set; }

    public GuildMemberFlags Flags { get; set; }

    public Gender Gender { get; set; }

    public ObjectGuid GUID => _guid;

    public float InactiveDays
    {
        get
        {
            if (IsOnline)
                return 0.0f;

            return (GameTime.CurrentTime - (long)LogoutTime) / (float)Time.DAY;
        }
    }

    public bool IsOnline => Flags.HasFlag(GuildMemberFlags.Online);

    public byte Level { get; set; }

    public ulong LogoutTime { get; set; }

    public string Name { get; set; }

    public string OfficerNote { get; set; } = "";

    public string PublicNote { get; set; } = "";

    public Race Race { get; set; }

    public GuildRankId RankId { get; set; }

    public ulong TotalActivity { get; set; }

    public uint TotalReputation { get; set; }

    public List<uint> TrackedCriteriaIds { get; set; } = new();

    public ulong WeekActivity { get; set; }

    public uint WeekReputation { get; set; }

    public uint ZoneId { get; set; }

    public void AddFlag(GuildMemberFlags var)
    {
        Flags |= var;
    }

    public void ChangeRank(SQLTransaction trans, GuildRankId newRank)
    {
        RankId = newRank;

        // Update rank information in player's field, if he is online.
        var player = FindConnectedPlayer();

        player?.SetGuildRank((byte)newRank);

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_RANK);
        stmt.AddValue(0, (byte)newRank);
        stmt.AddValue(1, _guid.Counter);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public bool CheckStats()
    {
        if (Level < 1)
        {
            Log.Logger.Error($"{_guid} has a broken data in field `characters`.`level`, deleting him from guild!");

            return false;
        }

        if (!_cliDB.ChrRacesStorage.ContainsKey((uint)Race))
        {
            Log.Logger.Error($"{_guid} has a broken data in field `characters`.`race`, deleting him from guild!");

            return false;
        }

        if (_cliDB.ChrClassesStorage.ContainsKey((uint)Class))
            return true;

        Log.Logger.Error($"{_guid} has a broken data in field `characters`.`class`, deleting him from guild!");

        return false;
    }

    public Player FindPlayer()
    {
        return _objectAccessor.FindPlayer(_guid);
    }

    public uint GetBankTabWithdrawValue(byte tabId)
    {
        return _bankWithdraw[tabId];
    }

    public bool IsRank(GuildRankId rankId)
    {
        return RankId == rankId;
    }

    public bool IsSamePlayer(ObjectGuid guid)
    {
        return _guid == guid;
    }

    public bool IsTrackingCriteriaId(uint criteriaId)
    {
        return TrackedCriteriaIds.Contains(criteriaId);
    }

    public bool LoadFromDB(SQLFields field)
    {
        PublicNote = field.Read<string>(3);
        OfficerNote = field.Read<string>(4);

        for (byte i = 0; i < GuildConst.MaxBankTabs; ++i)
            _bankWithdraw[i] = field.Read<uint>(5 + i);

        BankMoneyWithdrawValue = field.Read<ulong>(13);

        SetStats(field.Read<string>(14),
                 field.Read<byte>(15),              // characters.level
                 (Race)field.Read<byte>(16),        // characters.race
                 (PlayerClass)field.Read<byte>(17), // characters.class
                 (Gender)field.Read<byte>(18),      // characters.gender
                 field.Read<ushort>(19),            // characters.zone
                 field.Read<uint>(20),              // characters.account
                 0);

        LogoutTime = field.Read<ulong>(21); // characters.logout_time
        TotalActivity = 0;
        WeekActivity = 0;
        WeekReputation = 0;

        if (!CheckStats())
            return false;

        if (ZoneId == 0)
        {
            Log.Logger.Error("Player ({0}) has broken zone-data", _guid.ToString());
            ZoneId = _playerComputators.GetZoneIdFromDB(_guid);
        }

        ResetFlags();

        return true;
    }

    public void RemoveFlag(GuildMemberFlags var)
    {
        Flags &= ~var;
    }

    public void ResetFlags()
    {
        Flags = GuildMemberFlags.None;
    }

    public void ResetValues(bool weekly = false)
    {
        for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
            _bankWithdraw[tabId] = 0;

        BankMoneyWithdrawValue = 0;

        if (!weekly)
            return;

        WeekActivity = 0;
        WeekReputation = 0;
    }

    public void SaveToDB(SQLTransaction trans)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER);
        stmt.AddValue(0, _guildId);
        stmt.AddValue(1, _guid.Counter);
        stmt.AddValue(2, (byte)RankId);
        stmt.AddValue(3, PublicNote);
        stmt.AddValue(4, OfficerNote);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }


    public void SetOfficerNote(string officerNote)
    {
        if (OfficerNote == officerNote)
            return;

        OfficerNote = officerNote;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_OFFNOTE);
        stmt.AddValue(0, officerNote);
        stmt.AddValue(1, _guid.Counter);
        _characterDatabase.Execute(stmt);
    }

    public void SetPublicNote(string publicNote)
    {
        if (PublicNote == publicNote)
            return;

        PublicNote = publicNote;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_PNOTE);
        stmt.AddValue(0, publicNote);
        stmt.AddValue(1, _guid.Counter);
        _characterDatabase.Execute(stmt);
    }

    public void SetStats(Player player)
    {
        Name = player.GetName();
        Level = (byte)player.Level;
        Race = player.Race;
        Class = player.Class;
        Gender = player.NativeGender;
        ZoneId = player.Location.Zone;
        AccountId = player.Session.AccountId;
        AchievementPoints = player.AchievementPoints;
    }

    public void SetStats(string name, byte level, Race race, PlayerClass playerClass, Gender gender, uint zoneId, uint accountId, uint reputation)
    {
        Name = name;
        Level = level;
        Race = race;
        Class = playerClass;
        Gender = gender;
        ZoneId = zoneId;
        AccountId = accountId;
        TotalReputation = reputation;
    }

    public void SetTrackedCriteriaIds(List<uint> criteriaIds)
    {
        TrackedCriteriaIds = criteriaIds;
    }

    // Decreases amount of money left for today.
    public void UpdateBankMoneyWithdrawValue(SQLTransaction trans, ulong amount)
    {
        BankMoneyWithdrawValue += amount;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_MONEY);
        stmt.AddValue(0, _guid.Counter);
        stmt.AddValue(1, BankMoneyWithdrawValue);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    // Decreases amount of slots left for today.
    public void UpdateBankTabWithdrawValue(SQLTransaction trans, byte tabId, uint amount)
    {
        _bankWithdraw[tabId] += amount;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_TABS);
        stmt.AddValue(0, _guid.Counter);

        for (byte i = 0; i < GuildConst.MaxBankTabs;)
        {
            var withdraw = _bankWithdraw[i++];
            stmt.AddValue(i, withdraw);
        }

        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public void UpdateLogoutTime()
    {
        LogoutTime = (ulong)GameTime.CurrentTime;
    }

    private Player FindConnectedPlayer()
    {
        return _objectAccessor.FindConnectedPlayer(_guid);
    }
}