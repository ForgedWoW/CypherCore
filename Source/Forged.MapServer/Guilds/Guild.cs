// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Forged.MapServer.Achievements;
using Forged.MapServer.Cache;
using Forged.MapServer.Calendar;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Calendar;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.Guild;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IGuild;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Guilds;

public class Guild
{
    private readonly GuildLogHolder<GuildBankEventLogEntry>[] _bankEventLogs = new GuildLogHolder<GuildBankEventLogEntry>[GuildConst.MaxBankTabs + 1];
    private readonly List<GuildBankTab> _bankTabs = new();
    private readonly CalendarManager _calendar;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;

    private readonly CriteriaManager _criteriaManager;

    // These are actually ordered lists. The first element is the oldest entry.
    private readonly GuildLogHolder<GuildEventLogEntry> _eventLog;

    private readonly GuildAchievementMgr _guildAchievementMgr;
    private readonly GuildManager _guildManager;
    private readonly Dictionary<ObjectGuid, GuildMember> _members = new();
    private readonly GuildLogHolder<GuildNewsLogEntry> _newsLog;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly PlayerComputators _playerComputators;
    private readonly List<GuildRankInfo> _ranks = new();
    private readonly ScriptManager _scriptManager;
    private uint _accountsNumber;

    private ulong _bankMoney;

    private long _createdDate;

    private GuildEmblemInfo _emblemInfo;

    private ulong _id;

    private string _info;

    private ObjectGuid _leaderGuid;

    private string _motd;

    private string _name;

    public Guild(CharacterDatabase characterDatabase, ObjectAccessor objectAccessor, CharacterCache characterCache, IConfiguration configuration, CliDB cliDB, ClassFactory classFactory,
                 PlayerComputators playerComputators, ScriptManager scriptManager, GuildManager guildManager, CalendarManager calendar, CriteriaManager criteriaManager, GameObjectManager objectManager)
    {
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _configuration = configuration;
        _cliDB = cliDB;
        _playerComputators = playerComputators;
        _scriptManager = scriptManager;
        _guildManager = guildManager;
        _calendar = calendar;
        _criteriaManager = criteriaManager;
        _objectManager = objectManager;
        _eventLog = new GuildLogHolder<GuildEventLogEntry>(configuration);
        _newsLog = new GuildLogHolder<GuildNewsLogEntry>(configuration);
        _emblemInfo = new GuildEmblemInfo(characterDatabase, cliDB);
        _guildAchievementMgr = classFactory.Resolve<GuildAchievementMgr>(new PositionalParameter(0, this));

        for (var i = 0; i < _bankEventLogs.Length; ++i)
            _bankEventLogs[i] = new GuildLogHolder<GuildBankEventLogEntry>(configuration);
    }

    public static void SendCommandResult(WorldSession session, GuildCommandType type, GuildCommandError errCode, string param = "")
    {
        GuildCommandResult resultPacket = new()
        {
            Command = type,
            Result = errCode,
            Name = param
        };

        session.SendPacket(resultPacket);
    }

    public static void SendSaveEmblemResult(WorldSession session, GuildEmblemError errCode)
    {
        PlayerSaveGuildEmblem saveResponse = new()
        {
            Error = errCode
        };

        session.SendPacket(saveResponse);
    }

    public void AddGuildNews(GuildNews type, ObjectGuid guid, uint flags, uint value)
    {
        SQLTransaction trans = new();
        var news = _newsLog.AddEvent(trans, new GuildNewsLogEntry(_id, _newsLog.GetNextGUID(), type, guid, flags, value, _characterDatabase));
        _characterDatabase.CommitTransaction(trans);

        GuildNewsPkt newsPacket = new();
        news.WritePacket(newsPacket);
        BroadcastPacket(newsPacket);
    }

    public bool AddMember(SQLTransaction trans, ObjectGuid guid, GuildRankId? rankId = null)
    {
        var player = _objectAccessor.FindPlayer(guid);

        // Player cannot be in guild
        if (player != null)
        {
            if (player.GuildId != 0)
                return false;
        }
        else if (_characterCache.GetCharacterGuildIdByGuid(guid) != 0)
            return false;

        // Remove all player signs from another petitions
        // This will be prevent attempt to join many guilds and corrupt guild data integrity
        _playerComputators.RemovePetitionsAndSigns(guid);

        var lowguid = guid.Counter;

        // If rank was not passed, assign lowest possible rank
        rankId ??= GetLowestRankId();

        GuildMember member = new(_id, guid, rankId.Value, _characterDatabase, _objectAccessor, _playerComputators, _cliDB);
        var isNew = _members.TryAdd(guid, member);

        if (!isNew)
        {
            Log.Logger.Error($"Tried to add {guid} to guild '{_name}'. Member already exists.");

            return false;
        }

        var name = "";

        if (player != null)
        {
            _members[guid] = member;
            player.SetInGuild(_id);
            player.GuildIdInvited = 0;
            player.SetGuildRank((byte)rankId);
            player.GuildLevel = GetLevel();
            member.SetStats(player);
            SendLoginInfo(player.Session);
            name = player.GetName();
        }
        else
        {
            member.ResetFlags();

            var ok = false;
            // Player must exist
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_DATA_FOR_GUILD);
            stmt.AddValue(0, lowguid);
            var result = _characterDatabase.Query(stmt);

            if (!result.IsEmpty())
            {
                name = result.Read<string>(0);

                member.SetStats(name,
                                result.Read<byte>(1),
                                (Race)result.Read<byte>(2),
                                (PlayerClass)result.Read<byte>(3),
                                (Gender)result.Read<byte>(4),
                                result.Read<ushort>(5),
                                result.Read<uint>(6),
                                0);

                ok = member.CheckStats();
            }

            if (!ok)
                return false;

            _members[guid] = member;
            _characterCache.UpdateCharacterGuildId(guid, GetId());
        }

        member.SaveToDB(trans);

        UpdateAccountsNumber();
        LogEvent(GuildEventLogTypes.JoinGuild, lowguid);

        GuildEventPlayerJoined joinNotificationPacket = new()
        {
            Guid = guid,
            Name = name,
            VirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress
        };

        BroadcastPacket(joinNotificationPacket);

        // Call scripts if member was succesfully added (and stored to database)
        _scriptManager.ForEach<IGuildOnAddMember>(p => p.OnAddMember(this, player, (byte)rankId));

        return true;
    }

    public void BroadcastAddonToGuild(WorldSession session, bool officerOnly, string msg, string prefix, bool isLogged)
    {
        if (session?.Player == null || !HasRankRight(session.Player, officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
            return;

        ChatPkt data = new();
        data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, isLogged ? Language.AddonLogged : Language.Addon, session.Player, null, msg, 0, "", Locale.enUS, prefix);

        foreach (var player in _members.Values.Select(member => member.FindPlayer())
                                       .Where(player => player?.Session != null &&
                                                        HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
                                                        !player.Social.HasIgnore(session.Player.GUID, session.AccountGUID) &&
                                                        player.Session.IsAddonRegistered(prefix)))
            player.SendPacket(data);
    }

    public void BroadcastPacket(ServerPacket packet)
    {
        foreach (var member in _members.Values)
            member.FindPlayer()?.SendPacket(packet);
    }

    public void BroadcastPacketIfTrackingAchievement(ServerPacket packet, uint criteriaId)
    {
        foreach (var member in _members.Values.Where(member => member.IsTrackingCriteriaId(criteriaId)))
            member.FindPlayer()?.SendPacket(packet);
    }

    public void BroadcastPacketToRank(ServerPacket packet, GuildRankId rankId)
    {
        foreach (var member in _members.Values.Where(member => member.IsRank(rankId)))
            member.FindPlayer()?.SendPacket(packet);
    }

    public void BroadcastToGuild(WorldSession session, bool officerOnly, string msg, Language language)
    {
        if (session?.Player == null || !HasRankRight(session.Player, officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
            return;

        ChatPkt data = new();
        data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, language, session.Player, null, msg);

        foreach (var member in _members.Values)
        {
            var player = member.FindPlayer();

            if (player?.Session != null &&
                HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
                !player.Social.HasIgnore(session.Player.GUID, session.AccountGUID))
                player.SendPacket(data);
        }
    }

    public void BroadcastWorker(IDoWork<Player> @do, Player except = null)
    {
        foreach (var member in _members.Values)
        {
            var player = member.FindPlayer();

            if (player == null)
                continue;

            if (player != except)
                @do.Invoke(player);
        }
    }

    public bool ChangeMemberRank(SQLTransaction trans, ObjectGuid guid, GuildRankId newRank)
    {
        if (GetRankInfo(newRank) == null) // Validate rank (allow only existing ranks)
            return false;

        var member = GetMember(guid);

        if (member == null)
            return false;

        member.ChangeRank(trans, newRank);

        return true;
    }

    public bool Create(Player pLeader, string name)
    {
        // Check if guild with such name already exists
        if (_guildManager.GetGuildByName(name) != null)
            return false;

        var pLeaderSession = pLeader.Session;

        if (pLeaderSession == null)
            return false;

        _id = _guildManager.GenerateGuildId();
        _leaderGuid = pLeader.GUID;
        _name = name;
        _info = "";
        _motd = "No message set.";
        _bankMoney = 0;
        _createdDate = GameTime.CurrentTime;

        Log.Logger.Debug("GUILD: creating guild [{0}] for leader {1} ({2})",
                         name,
                         pLeader.GetName(),
                         _leaderGuid);

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBERS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        byte index = 0;
        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD);
        stmt.AddValue(index, _id);
        stmt.AddValue(++index, name);
        stmt.AddValue(++index, _leaderGuid.Counter);
        stmt.AddValue(++index, _info);
        stmt.AddValue(++index, _motd);
        stmt.AddValue(++index, _createdDate);
        stmt.AddValue(++index, _emblemInfo.Style);
        stmt.AddValue(++index, _emblemInfo.Color);
        stmt.AddValue(++index, _emblemInfo.BorderStyle);
        stmt.AddValue(++index, _emblemInfo.BorderColor);
        stmt.AddValue(++index, _emblemInfo.BackgroundColor);
        stmt.AddValue(++index, _bankMoney);
        trans.Append(stmt);

        CreateDefaultGuildRanks(trans, pLeaderSession.SessionDbLocaleIndex); // Create default ranks
        var ret = AddMember(trans, _leaderGuid, GuildRankId.GuildMaster);    // Add guildmaster

        _characterDatabase.CommitTransaction(trans);

        if (!ret)
            return false;

        var leader = GetMember(_leaderGuid);

        if (leader != null)
            SendEventNewLeader(leader, null);

        _scriptManager.ForEach<IGuildOnCreate>(p => p.OnCreate(this, pLeader, name));

        return true;
    }

    public void DeleteMember(SQLTransaction trans, ObjectGuid guid, bool isDisbanding = false, bool isKicked = false, bool canDeleteGuild = false)
    {
        var player = _objectAccessor.FindPlayer(guid);

        // Guild master can be deleted when loading guild and guid doesn't exist in characters table
        // or when he is removed from guild by gm command
        if (_leaderGuid == guid && !isDisbanding)
        {
            GuildMember oldLeader = null;
            GuildMember newLeader = null;

            foreach (var (memberGuid, member) in _members)
                if (memberGuid == guid)
                    oldLeader = member;
                else if (newLeader == null || newLeader.RankId > member.RankId)
                    newLeader = member;

            if (newLeader == null)
            {
                Disband();

                return;
            }

            SetLeader(trans, newLeader);

            // If leader does not exist (at guild loading with deleted leader) do not send broadcasts
            if (oldLeader != null)
            {
                SendEventNewLeader(newLeader, oldLeader, true);
                SendEventPlayerLeft(player);
            }
        }

        // Call script on remove before member is actually removed from guild (and database)
        _scriptManager.ForEach<IGuildOnRemoveMember>(p => p.OnRemoveMember(this, player, isDisbanding, isKicked));

        _members.Remove(guid);

        // If player not online data in data field will be loaded from guild tabs no need to update it !!
        if (player != null)
        {
            player.SetInGuild(0);
            player.SetGuildRank(0);
            player.GuildLevel = 0;

            foreach (var entry in _cliDB.GuildPerkSpellsStorage.Values)
                player.RemoveSpell(entry.SpellID, false, false);
        }
        else
            _characterCache.UpdateCharacterGuildId(guid, 0);

        DeleteMemberFromDB(trans, guid.Counter);

        if (!isDisbanding)
            UpdateAccountsNumber();
    }

    public void Disband()
    {
        _scriptManager.ForEach<IGuildOnDisband>(p => p.OnDisband(this));

        BroadcastPacket(new GuildEventDisbanded());

        SQLTransaction trans = new();

        while (!_members.Empty())
        {
            var member = _members.First();
            DeleteMember(trans, member.Value.GUID, true);
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_TABS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        // Free bank tab used memory and delete items stored in them
        DeleteBankItems(trans, true);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEMS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOGS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOGS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        _guildManager.RemoveGuild(_id);
    }

    public GuildAchievementMgr GetAchievementMgr()
    {
        return _guildAchievementMgr;
    }

    public ulong GetBankMoney()
    {
        return _bankMoney;
    }

    public GuildBankTab GetBankTab(byte tabId)
    {
        return tabId < _bankTabs.Count ? _bankTabs[tabId] : null;
    }

    public long GetCreatedDate()
    {
        return _createdDate;
    }

    public GuildEmblemInfo GetEmblemInfo()
    {
        return _emblemInfo;
    }

    public ObjectGuid GetGUID()
    {
        return ObjectGuid.Create(HighGuid.Guild, _id);
    }

    public ulong GetId()
    {
        return _id;
    }

    public string GetInfo()
    {
        return _info;
    }

    public Item GetItem(byte tabId, byte slotId)
    {
        var tab = GetBankTab(tabId);

        return tab?.GetItem(slotId);
    }

    public ObjectGuid GetLeaderGUID()
    {
        return _leaderGuid;
    }

    // Pre-6.x guild leveling
    public byte GetLevel()
    {
        return GuildConst.OldMaxLevel;
    }

    public GuildMember GetMember(ObjectGuid guid)
    {
        return _members.LookupByKey(guid);
    }

    public GuildMember GetMember(string name)
    {
        foreach (var member in _members.Values)
            if (member.Name == name)
                return member;

        return null;
    }

    public ulong GetMemberAvailableMoneyForRepairItems(ObjectGuid guid)
    {
        var member = GetMember(guid);

        if (member == null)
            return 0;

        return Math.Min(_bankMoney, (ulong)GetMemberRemainingMoney(member));
    }

    public int GetMemberRemainingSlots(GuildMember member, byte tabId)
    {
        var rankId = member.RankId;

        if (rankId == GuildRankId.GuildMaster)
            return GuildConst.WithdrawSlotUnlimited;

        if ((GetRankBankTabRights(rankId, tabId) & GuildBankRights.ViewTab) == 0)
            return 0;

        var remaining = GetRankBankTabSlotsPerDay(rankId, tabId) - (int)member.GetBankTabWithdrawValue(tabId);

        return remaining > 0 ? remaining : 0;
    }

    public int GetMembersCount()
    {
        return _members.Count;
    }

    public string GetMotd()
    {
        return _motd;
    }

    public string GetName()
    {
        return _name;
    }

    public byte GetPurchasedTabsSize()
    {
        return (byte)_bankTabs.Count;
    }

    public void HandleAcceptMember(WorldSession session)
    {
        var player = session.Player;

        if (!_configuration.GetDefaultValue("AllowTwoSide:Interaction:Guild", false) &&
            player.Team != _characterCache.GetCharacterTeamByGuid(GetLeaderGUID()))
            return;

        AddMember(null, player.GUID);
    }

    public void HandleAddNewRank(WorldSession session, string name)
    {
        var size = GetRanksSize();

        if (size >= GuildConst.MaxRanks)
            return;

        // Only leader can add new rank
        if (IsLeader(session.Player))
            if (CreateRank(null, name, GuildRankRights.GChatListen | GuildRankRights.GChatSpeak))
                BroadcastPacket(new GuildEventRanksUpdated());
    }

    public void HandleBuyBankTab(WorldSession session, byte tabId)
    {
        var player = session.Player;

        if (player == null)
            return;

        var member = GetMember(player.GUID);

        if (member == null)
            return;

        if (GetPurchasedTabsSize() >= GuildConst.MaxBankTabs)
            return;

        if (tabId != GetPurchasedTabsSize())
            return;

        if (tabId >= GuildConst.MaxBankTabs)
            return;

        // Do not get money for bank tabs that the GM bought, we had to buy them already.
        // This is just a speedup check, GetGuildBankTabPrice will return 0.
        if (tabId < GuildConst.MaxBankTabs - 2) // 7th tab is actually the 6th
        {
            var tabCost = (long)(GetGuildBankTabPrice(tabId) * MoneyConstants.Gold);

            if (!player.HasEnoughMoney(tabCost)) // Should not happen, this is checked by client
                return;

            player.ModifyMoney(-tabCost);
        }

        CreateNewBankTab();

        BroadcastPacket(new GuildEventTabAdded());

        SendPermissions(session); //Hack to force client to update permissions
    }

    public void HandleDelete(WorldSession session)
    {
        // Only leader can disband guild
        if (IsLeader(session.Player))
        {
            Disband();
            Log.Logger.Debug("Guild Successfully Disbanded");
        }
    }

    public void HandleGetAchievementMembers(WorldSession session, uint achievementId)
    {
        GetAchievementMgr().SendAchievementMembers(session.Player, achievementId);
    }

    public void HandleGuildPartyRequest(WorldSession session)
    {
        // Make sure player is a member of the guild and that he is in a group.
        if (!IsMember(session.Player.GUID) || session.Player.Group == null)
            return;

        GuildPartyState partyStateResponse = new()
        {
            InGuildParty = session.Player.Location.Map.GetOwnerGuildId(session.Player.Team) == GetId(),
            NumMembers = 0,
            NumRequired = 0,
            GuildXPEarnedMult = 0.0f
        };

        session.SendPacket(partyStateResponse);
    }

    public void HandleGuildRequestChallengeUpdate(WorldSession session)
    {
        GuildChallengeUpdate updatePacket = new();

        for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
            updatePacket.CurrentCount[i] = 0; // @todo current count

        for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
            updatePacket.MaxCount[i] = GuildConst.ChallengesMaxCount[i];

        for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
            updatePacket.MaxLevelGold[i] = GuildConst.ChallengeMaxLevelGoldReward[i];

        for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
            updatePacket.Gold[i] = GuildConst.ChallengeGoldReward[i];

        session.SendPacket(updatePacket);
    }

    public void HandleInviteMember(WorldSession session, string name)
    {
        var pInvitee = _objectAccessor.FindPlayerByName(name);

        if (pInvitee == null)
        {
            SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.PlayerNotFound_S, name);

            return;
        }

        var player = session.Player;

        // Do not show invitations from ignored players
        if (pInvitee.Social.HasIgnore(player.GUID, player.Session.AccountGUID))
            return;

        if (!_configuration.GetDefaultValue("AllowTwoSide:Interaction:Guild", false) && pInvitee.Team != player.Team)
        {
            SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.NotAllied, name);

            return;
        }

        // Invited player cannot be in another guild
        if (pInvitee.GuildId != 0)
        {
            SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, name);

            return;
        }

        // Invited player cannot be invited
        if (pInvitee.GuildIdInvited != 0)
        {
            SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, name);

            return;
        }

        // Inviting player must have rights to invite
        if (!HasRankRight(player, GuildRankRights.Invite))
        {
            SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.Permissions);

            return;
        }

        SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.Success, name);

        Log.Logger.Debug("Player {0} invited {1} to join his Guild", player.GetName(), name);

        pInvitee.GuildIdInvited = _id;
        LogEvent(GuildEventLogTypes.InvitePlayer, player.GUID.Counter, pInvitee.GUID.Counter);

        GuildInvite invite = new()
        {
            InviterVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
            GuildVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
            GuildGUID = GetGUID(),
            EmblemStyle = _emblemInfo.Style,
            EmblemColor = _emblemInfo.Color,
            BorderStyle = _emblemInfo.BorderStyle,
            BorderColor = _emblemInfo.BorderColor,
            Background = _emblemInfo.BackgroundColor,
            AchievementPoints = (int)GetAchievementMgr().AchievementPoints,
            InviterName = player.GetName(),
            GuildName = GetName()
        };

        var oldGuild = pInvitee.Guild;

        if (oldGuild != null)
        {
            invite.OldGuildGUID = oldGuild.GetGUID();
            invite.OldGuildName = oldGuild.GetName();
            invite.OldGuildVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress;
        }

        pInvitee.SendPacket(invite);
    }

    public void HandleLeaveMember(WorldSession session)
    {
        var player = session.Player;

        // If leader is leaving
        if (IsLeader(player))
        {
            if (_members.Count > 1)
                // Leader cannot leave if he is not the last member
                SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.LeaderLeave);
            else
                // Guild is disbanded if leader leaves.
                Disband();
        }
        else
        {
            DeleteMember(null, player.GUID);

            LogEvent(GuildEventLogTypes.LeaveGuild, player.GUID.Counter);
            SendEventPlayerLeft(player);

            SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.Success, _name);
        }

        _calendar.RemovePlayerGuildEventsAndSignups(player.GUID, GetId());
    }

    public void HandleMemberDepositMoney(WorldSession session, ulong amount, bool cashFlow = false)
    {
        // guild bank cannot have more than MAX_MONEY_AMOUNT
        amount = Math.Min(amount, PlayerConst.MaxMoneyAmount - _bankMoney);

        if (amount == 0)
            return;

        var player = session.Player;

        // Call script after validation and before money transfer.
        _scriptManager.ForEach<IGuildOnMemberDepositMoney>(p => p.OnMemberDepositMoney(this, player, amount));

        if (_bankMoney > GuildConst.MoneyLimit - amount)
        {
            if (!cashFlow)
                SendCommandResult(session, GuildCommandType.MoveItem, GuildCommandError.TooMuchMoney);

            return;
        }

        SQLTransaction trans = new();
        ModifyBankMoney(trans, amount, true);

        if (!cashFlow)
        {
            player.ModifyMoney(-(long)amount);
            player.SaveGoldToDB(trans);
        }

        LogBankEvent(trans, cashFlow ? GuildBankEventLogTypes.CashFlowDeposit : GuildBankEventLogTypes.DepositMoney, 0, player.GUID.Counter, (uint)amount);
        _characterDatabase.CommitTransaction(trans);

        SendEventBankMoneyChanged();

        if (player.Session.HasPermission(RBACPermissions.LogGmTrade))
            Log.Logger.ForContext<GMCommands>()
               .Information("GM {0} (Account: {1}) deposit money (Amount: {2}) to guild bank (Guild ID {3})",
                            player.GetName(),
                            player.Session.AccountId,
                            amount,
                            _id);
    }

    public void HandleMemberLogout(WorldSession session)
    {
        var player = session.Player;
        var member = GetMember(player.GUID);

        if (member != null)
        {
            member.SetStats(player);
            member.UpdateLogoutTime();
            member.ResetFlags();
        }

        SendEventPresenceChanged(session, false, true);
        SaveToDB();
    }

    public bool HandleMemberWithdrawMoney(WorldSession session, ulong amount, bool repair = false)
    {
        // clamp amount to MAX_MONEY_AMOUNT, Players can't hold more than that anyway
        amount = Math.Min(amount, PlayerConst.MaxMoneyAmount);

        if (_bankMoney < amount) // Not enough money in bank
            return false;

        var player = session.Player;

        var member = GetMember(player.GUID);

        if (member == null)
            return false;

        if (!HasRankRight(player, repair ? GuildRankRights.WithdrawRepair : GuildRankRights.WithdrawGold))
            return false;

        if (GetMemberRemainingMoney(member) < (long)amount) // Check if we have enough slot/money today
            return false;

        // Call script after validation and before money transfer.
        _scriptManager.ForEach<IGuildOnMemberWithDrawMoney>(p => p.OnMemberWitdrawMoney(this, player, amount, repair));

        SQLTransaction trans = new();

        // Add money to player (if required)
        if (!repair)
        {
            if (!player.ModifyMoney((long)amount))
                return false;

            player.SaveGoldToDB(trans);
        }

        // Update remaining money amount
        member.UpdateBankMoneyWithdrawValue(trans, amount);
        // Remove money from bank
        ModifyBankMoney(trans, amount, false);

        // Log guild bank event
        LogBankEvent(trans, repair ? GuildBankEventLogTypes.RepairMoney : GuildBankEventLogTypes.WithdrawMoney, 0, player.GUID.Counter, (uint)amount);
        _characterDatabase.CommitTransaction(trans);

        SendEventBankMoneyChanged();

        return true;
    }

    public void HandleNewsSetSticky(WorldSession session, uint newsId, bool sticky)
    {
        var newsLog = _newsLog.GetGuildLog().Find(p => p.GUID == newsId);

        if (newsLog == null)
        {
            Log.Logger.Debug("HandleNewsSetSticky: [{0}] requested unknown newsId {1} - Sticky: {2}", session.GetPlayerInfo(), newsId, sticky);

            return;
        }

        newsLog.SetSticky(sticky);

        GuildNewsPkt newsPacket = new();
        newsLog.WritePacket(newsPacket);
        session.SendPacket(newsPacket);
    }

    public void HandleRemoveMember(WorldSession session, ObjectGuid guid)
    {
        var player = session.Player;

        // Player must have rights to remove members
        if (!HasRankRight(player, GuildRankRights.Remove))
            SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.Permissions);

        var member = GetMember(guid);

        if (member != null)
        {
            var name = member.Name;

            // Guild masters cannot be removed
            if (member.IsRank(GuildRankId.GuildMaster))
                SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.LeaderLeave);
            // Do not allow to remove player with the same rank or higher
            else
            {
                var memberMe = GetMember(player.GUID);
                var myRank = GetRankInfo(memberMe.RankId);
                var targetRank = GetRankInfo(member.RankId);

                if (targetRank.Order <= myRank.Order)
                    SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.RankTooHigh_S, name);
                else
                {
                    DeleteMember(null, guid, false, true);
                    LogEvent(GuildEventLogTypes.UninvitePlayer, player.GUID.Counter, guid.Counter);

                    var pMember = _objectAccessor.FindConnectedPlayer(guid);
                    SendEventPlayerLeft(pMember, player, true);

                    SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.Success, name);
                }
            }
        }
    }

    public void HandleRemoveRank(WorldSession session, GuildRankOrder rankOrder)
    {
        // Cannot remove rank if total count is minimum allowed by the client or is not leader
        if (GetRanksSize() <= GuildConst.MinRanks || !IsLeader(session.Player))
            return;

        var rankInfo = _ranks.Find(rank => rank.Order == rankOrder);

        if (rankInfo == null)
            return;

        var trans = new SQLTransaction();

        // Delete bank rights for rank
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS_FOR_RANK);
        stmt.AddValue(0, _id);
        stmt.AddValue(1, (byte)rankInfo.Id);
        trans.Append(stmt);

        // Delete rank
        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_RANK);
        stmt.AddValue(0, _id);
        stmt.AddValue(1, (byte)rankInfo.Id);
        trans.Append(stmt);

        _ranks.Remove(rankInfo);

        // correct order of other ranks
        foreach (var otherRank in _ranks)
        {
            if (otherRank.Order < rankOrder)
                continue;

            otherRank.SetOrder(otherRank.Order - 1);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
            stmt.AddValue(0, (byte)otherRank.Order);
            stmt.AddValue(1, (byte)otherRank.Id);
            stmt.AddValue(2, _id);
            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);

        BroadcastPacket(new GuildEventRanksUpdated());
    }

    public void HandleRoster(WorldSession session)
    {
        GuildRoster roster = new()
        {
            NumAccounts = (int)_accountsNumber,
            CreateDate = (uint)_createdDate,
            GuildFlags = 0
        };

        var sendOfficerNote = HasRankRight(session.Player, GuildRankRights.ViewOffNote);

        foreach (var member in _members.Values)
        {
            GuildRosterMemberData memberData = new()
            {
                Guid = member.GUID,
                RankID = (int)member.RankId,
                AreaID = (int)member.ZoneId,
                PersonalAchievementPoints = (int)member.AchievementPoints,
                GuildReputation = (int)member.TotalReputation,
                LastSave = member.InactiveDays,
                //GuildRosterProfessionData
                VirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
                Status = (byte)member.Flags,
                Level = member.Level,
                ClassID = (byte)member.Class,
                Gender = (byte)member.Gender,
                RaceID = (byte)member.Race,
                Authenticated = false,
                SorEligible = false,
                Name = member.Name,
                Note = member.PublicNote
            };

            if (sendOfficerNote)
                memberData.OfficerNote = member.OfficerNote;

            roster.MemberData.Add(memberData);
        }

        roster.WelcomeText = _motd;
        roster.InfoText = _info;

        session.SendPacket(roster);
    }

    public void HandleSetAchievementTracking(WorldSession session, List<uint> achievementIds)
    {
        var player = session.Player;

        var member = GetMember(player.GUID);

        if (member == null)
            return;

        List<uint> criteriaIds = new();

        foreach (var achievementId in achievementIds)
        {
            if (!_cliDB.AchievementStorage.TryGetValue(achievementId, out var achievement))
                continue;

            var tree = _criteriaManager.GetCriteriaTree(achievement.CriteriaTree);

            if (tree != null)
                CriteriaManager.WalkCriteriaTree(tree,
                                                 node =>
                                                 {
                                                     if (node.Criteria != null)
                                                         criteriaIds.Add(node.Criteria.Id);
                                                 });
        }

        member.SetTrackedCriteriaIds(criteriaIds);
        GetAchievementMgr().SendAllTrackedCriterias(player, member.TrackedCriteriaIds);
    }

    public void HandleSetBankTabInfo(WorldSession session, byte tabId, string name, string icon)
    {
        var tab = GetBankTab(tabId);

        if (tab == null)
        {
            Log.Logger.Error("Guild.HandleSetBankTabInfo: Player {0} trying to change bank tab info from unexisting tab {1}.",
                             session.Player.GetName(),
                             tabId);

            return;
        }

        tab.SetInfo(name, icon);

        GuildEventTabModified packet = new()
        {
            Tab = tabId,
            Name = name,
            Icon = icon
        };

        BroadcastPacket(packet);
    }

    public void HandleSetEmblem(WorldSession session, GuildEmblemInfo emblemInfo)
    {
        var player = session.Player;

        if (!IsLeader(player))
            SendSaveEmblemResult(session, GuildEmblemError.NotGuildMaster); // "Only guild leaders can create emblems."
        else if (!player.HasEnoughMoney(10 * MoneyConstants.Gold))
            SendSaveEmblemResult(session, GuildEmblemError.NotEnoughMoney); // "You can't afford to do that."
        else
        {
            player.ModifyMoney(-(long)10 * MoneyConstants.Gold);

            _emblemInfo = emblemInfo;
            _emblemInfo.SaveToDB(_id);

            SendSaveEmblemResult(session, GuildEmblemError.Success); // "Guild Emblem saved."

            SendQueryResponse(session);
        }
    }

    public void HandleSetInfo(WorldSession session, string info)
    {
        if (_info == info)
            return;

        // Player must have rights to set guild's info
        if (HasRankRight(session.Player, GuildRankRights.ModifyGuildInfo))
        {
            _info = info;

            _scriptManager.ForEach<IGuildOnInfoChanged>(p => p.OnInfoChanged(this, info));

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_INFO);
            stmt.AddValue(0, info);
            stmt.AddValue(1, _id);
            _characterDatabase.Execute(stmt);
        }
    }

    public void HandleSetMemberNote(WorldSession session, string note, ObjectGuid guid, bool isPublic)
    {
        // Player must have rights to set public/officer note
        if (!HasRankRight(session.Player, isPublic ? GuildRankRights.EditPublicNote : GuildRankRights.EOffNote))
            SendCommandResult(session, GuildCommandType.EditPublicNote, GuildCommandError.Permissions);

        var member = GetMember(guid);

        if (member != null)
        {
            if (isPublic)
                member.SetPublicNote(note);
            else
                member.SetOfficerNote(note);

            GuildMemberUpdateNote updateNote = new()
            {
                Member = guid,
                IsPublic = isPublic,
                Note = note
            };

            BroadcastPacket(updateNote);
        }
    }

    public void HandleSetMemberRank(WorldSession session, ObjectGuid targetGuid, ObjectGuid setterGuid, GuildRankOrder rank)
    {
        var player = session.Player;
        var member = GetMember(targetGuid);
        var rights = GuildRankRights.Promote;
        var type = GuildCommandType.PromotePlayer;

        var oldRank = GetRankInfo(member.RankId);
        var newRank = GetRankInfo(rank);

        if (oldRank == null || newRank == null)
            return;

        if (rank > oldRank.Order)
        {
            rights = GuildRankRights.Demote;
            type = GuildCommandType.DemotePlayer;
        }

        // Promoted player must be a member of guild
        if (!HasRankRight(player, rights))
        {
            SendCommandResult(session, type, GuildCommandError.Permissions);

            return;
        }

        // Player cannot promote himself
        if (member.IsSamePlayer(player.GUID))
        {
            SendCommandResult(session, type, GuildCommandError.NameInvalid);

            return;
        }

        SendGuildRanksUpdate(setterGuid, targetGuid, newRank.Id);
    }

    public void HandleSetMotd(WorldSession session, string motd)
    {
        if (_motd == motd)
            return;

        // Player must have rights to set MOTD
        if (!HasRankRight(session.Player, GuildRankRights.SetMotd))
            SendCommandResult(session, GuildCommandType.EditMOTD, GuildCommandError.Permissions);
        else
        {
            _motd = motd;

            _scriptManager.ForEach<IGuildOnMOTDChanged>(p => p.OnMOTDChanged(this, motd));

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MOTD);
            stmt.AddValue(0, motd);
            stmt.AddValue(1, _id);
            _characterDatabase.Execute(stmt);

            SendEventMotd(session, true);
        }
    }

    public void HandleSetNewGuildMaster(WorldSession session, string name, bool isSelfPromote)
    {
        var player = session.Player;
        var oldGuildMaster = GetMember(GetLeaderGUID());

        GuildMember newGuildMaster;

        if (isSelfPromote)
        {
            newGuildMaster = GetMember(player.GUID);

            if (newGuildMaster == null)
                return;

            var oldRank = GetRankInfo(newGuildMaster.RankId);

            // only second highest rank can take over guild
            if (oldRank.Order != (GuildRankOrder)1 || oldGuildMaster.InactiveDays < GuildConst.MasterDethroneInactiveDays)
            {
                SendCommandResult(session, GuildCommandType.ChangeLeader, GuildCommandError.Permissions);

                return;
            }
        }
        else
        {
            if (!IsLeader(player))
            {
                SendCommandResult(session, GuildCommandType.ChangeLeader, GuildCommandError.Permissions);

                return;
            }

            newGuildMaster = GetMember(name);

            if (newGuildMaster == null)
                return;
        }

        SQLTransaction trans = new();

        SetLeader(trans, newGuildMaster);
        oldGuildMaster.ChangeRank(trans, GetLowestRankId());

        SendEventNewLeader(newGuildMaster, oldGuildMaster, isSelfPromote);

        _characterDatabase.CommitTransaction(trans);
    }

    public void HandleSetRankInfo(WorldSession session, GuildRankId rankId, string name, GuildRankRights rights, uint moneyPerDay, GuildBankRightsAndSlots[] rightsAndSlots)
    {
        // Only leader can modify ranks
        if (!IsLeader(session.Player))
            SendCommandResult(session, GuildCommandType.ChangeRank, GuildCommandError.Permissions);

        var rankInfo = GetRankInfo(rankId);

        if (rankInfo != null)
        {
            rankInfo.SetName(name);
            rankInfo.SetRights(rights);
            SetRankBankMoneyPerDay(rankId, moneyPerDay * MoneyConstants.Gold);

            foreach (var rightsAndSlot in rightsAndSlots)
                SetRankBankTabRightsAndSlots(rankId, rightsAndSlot);

            GuildEventRankChanged packet = new()
            {
                RankID = (byte)rankId
            };

            BroadcastPacket(packet);
        }
    }

    public void HandleShiftRank(WorldSession session, GuildRankOrder rankOrder, bool shiftUp)
    {
        // Only leader can modify ranks
        if (!IsLeader(session.Player))
            return;

        var otherRankOrder = rankOrder + (shiftUp ? -1 : 1);

        var rankInfo = GetRankInfo(rankOrder);
        var otherRankInfo = GetRankInfo(otherRankOrder);

        if (rankInfo == null || otherRankInfo == null)
            return;

        // can't shift guild master rank (rank id = 0) - there's already a client-side limitation for it so that's just a safe-guard
        if (rankInfo.Id == GuildRankId.GuildMaster || otherRankInfo.Id == GuildRankId.GuildMaster)
            return;

        rankInfo.SetOrder(otherRankOrder);
        otherRankInfo.SetOrder(rankOrder);

        var trans = new SQLTransaction();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
        stmt.AddValue(0, (byte)rankInfo.Order);
        stmt.AddValue(1, (byte)rankInfo.Id);
        stmt.AddValue(2, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
        stmt.AddValue(0, (byte)otherRankInfo.Order);
        stmt.AddValue(1, (byte)otherRankInfo.Id);
        stmt.AddValue(2, _id);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        // force client to re-request SMSG_GUILD_RANKS
        BroadcastPacket(new GuildEventRanksUpdated());
    }

    public void HandleUpdateMemberRank(WorldSession session, ObjectGuid guid, bool demote)
    {
        var player = session.Player;
        var type = demote ? GuildCommandType.DemotePlayer : GuildCommandType.PromotePlayer;
        // Player must have rights to promote
        GuildMember member;

        if (!HasRankRight(player, demote ? GuildRankRights.Demote : GuildRankRights.Promote))
            SendCommandResult(session, type, GuildCommandError.LeaderLeave);
        // Promoted player must be a member of guild
        else if ((member = GetMember(guid)) != null)
        {
            var name = member.Name;

            // Player cannot promote himself
            if (member.IsSamePlayer(player.GUID))
            {
                SendCommandResult(session, type, GuildCommandError.NameInvalid);

                return;
            }

            var memberMe = GetMember(player.GUID);
            var myRank = GetRankInfo(memberMe.RankId);
            var oldRank = GetRankInfo(member.RankId);
            GuildRankId newRankId;

            if (demote)
            {
                // Player can demote only lower rank members
                if (oldRank.Order <= myRank.Order)
                {
                    SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);

                    return;
                }

                // Lowest rank cannot be demoted
                var newRank = GetRankInfo(oldRank.Order + 1);

                if (newRank == null)
                {
                    SendCommandResult(session, type, GuildCommandError.RankTooLow_S, name);

                    return;
                }

                newRankId = newRank.Id;
            }
            else
            {
                // Allow to promote only to lower rank than member's rank
                // memberMe.GetRankId() + 1 is the highest rank that current player can promote to
                if (oldRank.Order - 1 <= myRank.Order)
                {
                    SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);

                    return;
                }

                newRankId = GetRankInfo(oldRank.Order - 1).Id;
            }

            member.ChangeRank(null, newRankId);
            LogEvent(demote ? GuildEventLogTypes.DemotePlayer : GuildEventLogTypes.PromotePlayer, player.GUID.Counter, member.GUID.Counter, (byte)newRankId);
            //_BroadcastEvent(demote ? GuildEvents.Demotion : GuildEvents.Promotion, ObjectGuid.Empty, player.GetName(), name, _GetRankName((byte)newRankId));
        }
    }

    public bool IsMember(ObjectGuid guid)
    {
        return _members.ContainsKey(guid);
    }

    public bool LoadBankEventLogFromDB(SQLFields field)
    {
        var dbTabId = field.Read<byte>(1);
        var isMoneyTab = dbTabId == GuildConst.BankMoneyLogsTab;

        if (dbTabId < GetPurchasedTabsSize() || isMoneyTab)
        {
            var tabId = isMoneyTab ? (byte)GuildConst.MaxBankTabs : dbTabId;
            var pLog = _bankEventLogs[tabId];

            if (pLog.CanInsert())
            {
                var guid = field.Read<uint>(2);
                var eventType = (GuildBankEventLogTypes)field.Read<byte>(3);

                if (GuildBankEventLogEntry.IsMoneyEvent(eventType))
                {
                    if (!isMoneyTab)
                    {
                        Log.Logger.Error("GuildBankEventLog ERROR: MoneyEvent(LogGuid: {0}, Guild: {1}) does not belong to money tab ({2}), ignoring...", guid, _id, dbTabId);

                        return false;
                    }
                }
                else if (isMoneyTab)
                {
                    Log.Logger.Error("GuildBankEventLog ERROR: non-money event (LogGuid: {0}, Guild: {1}) belongs to money tab, ignoring...", guid, _id);

                    return false;
                }

                pLog.LoadEvent(new GuildBankEventLogEntry(_id,              // guild id
                                                     guid,                  // guid
                                                     field.Read<long>(8),   // timestamp
                                                     dbTabId,               // tab id
                                                     eventType,             // event type
                                                     field.Read<ulong>(4),  // player guid
                                                     field.Read<ulong>(5),  // item or money
                                                     field.Read<ushort>(6), // itam stack count
                                                     field.Read<byte>(7),   // dest tab id
                                                     _characterDatabase)); 
            }
        }

        return true;
    }

    public bool LoadBankItemFromDB(SQLFields field)
    {
        var tabId = field.Read<byte>(52);

        if (tabId >= GetPurchasedTabsSize())
        {
            Log.Logger.Error("Invalid tab for item (GUID: {0}, id: {1}) in guild bank, skipped.",
                             field.Read<uint>(0),
                             field.Read<uint>(1));

            return false;
        }

        return _bankTabs[tabId].LoadItemFromDB(field);
    }

    public void LoadBankRightFromDB(SQLFields field)
    {
        // tabId              rights                slots
        GuildBankRightsAndSlots rightsAndSlots = new(field.Read<byte>(1), field.Read<sbyte>(3), field.Read<int>(4));
        // rankId
        SetRankBankTabRightsAndSlots((GuildRankId)field.Read<byte>(2), rightsAndSlots, false);
    }

    public void LoadBankTabFromDB(SQLFields field)
    {
        var tabId = field.Read<byte>(1);

        if (tabId >= GetPurchasedTabsSize())
            Log.Logger.Error("Invalid tab (tabId: {0}) in guild bank, skipped.", tabId);
        else
            _bankTabs[tabId].LoadFromDB(field);
    }

    public bool LoadEventLogFromDB(SQLFields field)
    {
        if (!_eventLog.CanInsert())
            return false;

        _eventLog.LoadEvent(new GuildEventLogEntry(_id,                                     // guild id
                                                   field.Read<uint>(1),                     // guid
                                                   field.Read<long>(6),                     // timestamp
                                                   (GuildEventLogTypes)field.Read<byte>(2), // event type
                                                   field.Read<ulong>(3),                    // player guid 1
                                                   field.Read<ulong>(4),                    // player guid 2
                                                   field.Read<byte>(5),                     // rank
                                                    _characterDatabase)); 

        return true;
    }

    public bool LoadFromDB(SQLFields fields)
    {
        _id = fields.Read<uint>(0);
        _name = fields.Read<string>(1);
        _leaderGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(2));

        if (!_emblemInfo.LoadFromDB(fields))
        {
            Log.Logger.Error("Guild {0} has invalid emblem colors (Background: {1}, Border: {2}, Emblem: {3}), skipped.",
                             _id,
                             _emblemInfo.BackgroundColor,
                             _emblemInfo.BorderColor,
                             _emblemInfo.Color);

            return false;
        }

        _info = fields.Read<string>(8);
        _motd = fields.Read<string>(9);
        _createdDate = fields.Read<uint>(10);
        _bankMoney = fields.Read<ulong>(11);

        var purchasedTabs = (byte)fields.Read<uint>(12);

        if (purchasedTabs > GuildConst.MaxBankTabs)
            purchasedTabs = GuildConst.MaxBankTabs;

        _bankTabs.Clear();

        for (byte i = 0; i < purchasedTabs; ++i)
            _bankTabs.Add(new GuildBankTab(_id, i, _objectManager, _characterDatabase));

        return true;
    }

    public void LoadGuildNewsLogFromDB(SQLFields field)
    {
        if (!_newsLog.CanInsert())
            return;

        var news = new GuildNewsLogEntry(_id,                                                      // guild id
                                         field.Read<uint>(1),                                      // guid
                                         field.Read<long>(6),                                      // timestamp //64 bits?
                                         (GuildNews)field.Read<byte>(2),                           // type
                                         ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(3)), // player guid
                                         field.Read<uint>(4),                                      // Flags
                                         field.Read<uint>(5),
                                         _characterDatabase); // value)

        _newsLog.LoadEvent(news);
    }

    public bool LoadMemberFromDB(SQLFields field)
    {
        var lowguid = field.Read<ulong>(1);
        var playerGuid = ObjectGuid.Create(HighGuid.Player, lowguid);

        GuildMember member = new(_id, playerGuid, (GuildRankId)field.Read<byte>(2), _characterDatabase, _objectAccessor, _playerComputators, _cliDB);
        var isNew = _members.TryAdd(playerGuid, member);

        if (!isNew)
        {
            Log.Logger.Error($"Tried to add {playerGuid} to guild '{_name}'. Member already exists.");

            return false;
        }

        if (!member.LoadFromDB(field))
        {
            DeleteMemberFromDB(null, lowguid);

            return false;
        }

        _characterCache.UpdateCharacterGuildId(playerGuid, GetId());
        _members[member.GUID] = member;

        return true;
    }

    public void LoadRankFromDB(SQLFields field)
    {
        GuildRankInfo rankInfo = new(_characterDatabase, _id);

        rankInfo.LoadFromDB(field);

        _ranks.Add(rankInfo);
    }

    public void LogBankEvent(SQLTransaction trans, GuildBankEventLogTypes eventType, byte tabId, ulong lowguid, uint itemOrMoney, ushort itemStackCount = 0, byte destTabId = 0)
    {
        if (tabId > GuildConst.MaxBankTabs)
            return;

        // not logging moves within the same tab
        if (eventType == GuildBankEventLogTypes.MoveItem && tabId == destTabId)
            return;

        var dbTabId = tabId;

        if (GuildBankEventLogEntry.IsMoneyEvent(eventType))
        {
            tabId = GuildConst.MaxBankTabs;
            dbTabId = GuildConst.BankMoneyLogsTab;
        }

        var pLog = _bankEventLogs[tabId];
        pLog.AddEvent(trans, new GuildBankEventLogEntry(_id, pLog.GetNextGUID(), eventType, dbTabId, lowguid, itemOrMoney, itemStackCount, destTabId, _characterDatabase));

        _scriptManager.ForEach<IGuildOnBankEvent>(p => p.OnBankEvent(this, (byte)eventType, tabId, lowguid, itemOrMoney, itemStackCount, destTabId));
    }

    public void LogEvent(GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2 = 0, byte newRank = 0)
    {
        SQLTransaction trans = new();
        _eventLog.AddEvent(trans, new GuildEventLogEntry(_id, _eventLog.GetNextGUID(), eventType, playerGuid1, playerGuid2, newRank, _characterDatabase));
        _characterDatabase.CommitTransaction(trans);

        _scriptManager.ForEach<IGuildOnEvent>(p => p.OnEvent(this, (byte)eventType, playerGuid1, playerGuid2, newRank));
    }

    public void MassInviteToEvent(WorldSession session, uint minLevel, uint maxLevel, GuildRankOrder minRank)
    {
        CalendarCommunityInvite packet = new();

        foreach (var (guid, member) in _members)
        {
            // not sure if needed, maybe client checks it as well
            if (packet.Invites.Count >= SharedConst.CalendarMaxInvites)
            {
                var player = session.Player;

                if (player != null)
                    _calendar.SendCalendarCommandResult(player.GUID, CalendarError.InvitesExceeded);

                return;
            }

            if (guid == session.Player.GUID)
                continue;

            uint level = _characterCache.GetCharacterLevelByGuid(guid);

            if (level < minLevel || level > maxLevel)
                continue;

            var rank = GetRankInfo(member.RankId);

            if (rank.Order > minRank)
                continue;

            packet.Invites.Add(new CalendarEventInitialInviteInfo(guid, (byte)level));
        }

        session.SendPacket(packet);
    }

    public bool MemberHasTabRights(ObjectGuid guid, byte tabId, GuildBankRights rights)
    {
        var member = GetMember(guid);

        if (member == null)
            return false;

        // Leader always has full rights
        if (member.IsRank(GuildRankId.GuildMaster) || _leaderGuid == guid)
            return true;

        return (GetRankBankTabRights(member.RankId, tabId) & rights) == rights;
    }

    public void RemoveItem(SQLTransaction trans, byte tabId, byte slotId)
    {
        var pTab = GetBankTab(tabId);

        pTab?.SetItem(trans, slotId, null);
    }

    public void ResetTimes(bool weekly)
    {
        foreach (var member in _members.Values)
        {
            member.ResetValues(weekly);
            var player = member.FindPlayer();

            player?.SendPacket(new GuildMemberDailyReset());
        }
    }

    public void SaveToDB()
    {
        SQLTransaction trans = new();

        GetAchievementMgr().SaveToDB(trans);

        _characterDatabase.CommitTransaction(trans);
    }

    public void SendBankList(WorldSession session, byte tabId, bool fullUpdate)
    {
        var member = GetMember(session.Player.GUID);

        if (member == null) // Shouldn't happen, just in case
            return;

        GuildBankQueryResults packet = new()
        {
            Money = _bankMoney,
            WithdrawalsRemaining = GetMemberRemainingSlots(member, tabId),
            Tab = tabId,
            FullUpdate = fullUpdate
        };

        // TabInfo
        if (fullUpdate)
            for (byte i = 0; i < GetPurchasedTabsSize(); ++i)
            {
                GuildBankTabInfo tabInfo;
                tabInfo.TabIndex = i;
                tabInfo.Name = _bankTabs[i].Name;
                tabInfo.Icon = _bankTabs[i].Icon;
                packet.TabInfo.Add(tabInfo);
            }

        if (fullUpdate && MemberHasTabRights(session.Player.GUID, tabId, GuildBankRights.ViewTab))
        {
            var tab = GetBankTab(tabId);

            if (tab != null)
                for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
                {
                    var tabItem = tab.GetItem(slotId);

                    if (tabItem == null)
                        continue;

                    GuildBankItemInfo itemInfo = new()
                    {
                        Slot = slotId,
                        Item =
                        {
                            ItemID = tabItem.Entry
                        },
                        Count = (int)tabItem.Count,
                        Charges = Math.Abs(tabItem.GetSpellCharges()),
                        EnchantmentID = (int)tabItem.GetEnchantmentId(EnchantmentSlot.Perm),
                        OnUseEnchantmentID = (int)tabItem.GetEnchantmentId(EnchantmentSlot.Use),
                        Flags = tabItem.ItemData.DynamicFlags
                    };

                    byte i = 0;

                    foreach (var gemData in tabItem.ItemData.Gems)
                    {
                        if (gemData.ItemId != 0)
                        {
                            ItemGemData gem = new()
                            {
                                Slot = i,
                                Item = new ItemInstance(gemData)
                            };

                            itemInfo.SocketEnchant.Add(gem);
                        }

                        ++i;
                    }

                    itemInfo.Locked = false;

                    packet.ItemInfo.Add(itemInfo);
                }
        }

        session.SendPacket(packet);
    }

    public void SendBankLog(WorldSession session, byte tabId)
    {
        // GuildConst.MaxBankTabs send by client for money log
        if (tabId < GetPurchasedTabsSize() || tabId == GuildConst.MaxBankTabs)
        {
            var bankEventLog = _bankEventLogs[tabId].GetGuildLog();

            GuildBankLogQueryResults packet = new()
            {
                Tab = tabId
            };

            //if (tabId == GUILD_BANK_MAX_TABS && hasCashFlow)
            //    packet.WeeklyBonusMoney.Set(uint64(weeklyBonusMoney));

            foreach (var entry in bankEventLog)
                entry.WritePacket(packet);

            session.SendPacket(packet);
        }
    }

    public void SendBankTabText(WorldSession session, byte tabId)
    {
        var tab = GetBankTab(tabId);

        tab?.SendText(this, session);
    }

    public void SendEventAwayChanged(ObjectGuid memberGuid, bool afk, bool dnd)
    {
        var member = GetMember(memberGuid);

        if (member == null)
            return;

        if (afk)
            member.AddFlag(GuildMemberFlags.AFK);
        else
            member.RemoveFlag(GuildMemberFlags.AFK);

        if (dnd)
            member.AddFlag(GuildMemberFlags.DND);
        else
            member.RemoveFlag(GuildMemberFlags.DND);

        GuildEventStatusChange statusChange = new()
        {
            Guid = memberGuid,
            AFK = afk,
            DND = dnd
        };

        BroadcastPacket(statusChange);
    }

    public void SendEventLog(WorldSession session)
    {
        var eventLog = _eventLog.GetGuildLog();

        GuildEventLogQueryResults packet = new();

        foreach (var entry in eventLog)
            entry.WritePacket(packet);

        session.SendPacket(packet);
    }

    public void SendGuildRankInfo(WorldSession session)
    {
        GuildRanks ranks = new();

        foreach (var rankInfo in _ranks)
        {
            GuildRankData rankData = new()
            {
                RankID = (byte)rankInfo.Id,
                RankOrder = (byte)rankInfo.Order,
                Flags = (uint)rankInfo.AccessRights,
                WithdrawGoldLimit = rankInfo.Id == GuildRankId.GuildMaster ? uint.MaxValue : rankInfo.BankMoneyPerDay / MoneyConstants.Gold,
                RankName = rankInfo.Name
            };

            for (byte j = 0; j < GuildConst.MaxBankTabs; ++j)
            {
                rankData.TabFlags[j] = (uint)rankInfo.GetBankTabRights(j);
                rankData.TabWithdrawItemLimit[j] = (uint)rankInfo.GetBankTabSlotsPerDay(j);
            }

            ranks.Ranks.Add(rankData);
        }

        session.SendPacket(ranks);
    }

    public void SendLoginInfo(WorldSession session)
    {
        var player = session.Player;
        var member = GetMember(player.GUID);

        if (member == null)
            return;

        SendEventMotd(session);
        SendGuildRankInfo(session);
        SendEventPresenceChanged(session, true, true); // Broadcast

        // Send to self separately, player is not in world yet and is not found by _BroadcastEvent
        SendEventPresenceChanged(session, true);

        if (member.GUID == GetLeaderGUID())
        {
            GuildFlaggedForRename renameFlag = new()
            {
                FlagSet = false
            };

            player.SendPacket(renameFlag);
        }

        foreach (var entry in _cliDB.GuildPerkSpellsStorage.Values)
            player.LearnSpell(entry.SpellID, true);

        GetAchievementMgr().SendAllData(player);

        // tells the client to request bank withdrawal limit
        player.SendPacket(new GuildMemberDailyReset());

        member.SetStats(player);
        member.AddFlag(GuildMemberFlags.Online);
    }

    public void SendMoneyInfo(WorldSession session)
    {
        var member = GetMember(session.Player.GUID);

        if (member == null)
            return;

        var amount = GetMemberRemainingMoney(member);

        GuildBankRemainingWithdrawMoney packet = new()
        {
            RemainingWithdrawMoney = amount
        };

        session.SendPacket(packet);
    }

    public void SendNewsUpdate(WorldSession session)
    {
        var newsLog = _newsLog.GetGuildLog();

        GuildNewsPkt packet = new();

        foreach (var newsLogEntry in newsLog)
            newsLogEntry.WritePacket(packet);

        session.SendPacket(packet);
    }

    public void SendPermissions(WorldSession session)
    {
        var member = GetMember(session.Player.GUID);

        if (member == null)
            return;

        var rankId = member.RankId;

        GuildPermissionsQueryResults queryResult = new()
        {
            RankID = (byte)rankId,
            WithdrawGoldLimit = (int)GetMemberRemainingMoney(member),
            Flags = (int)GetRankRights(rankId),
            NumTabs = GetPurchasedTabsSize()
        };

        for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
        {
            GuildPermissionsQueryResults.GuildRankTabPermissions tabPerm;
            tabPerm.Flags = (int)GetRankBankTabRights(rankId, tabId);
            tabPerm.WithdrawItemLimit = GetMemberRemainingSlots(member, tabId);
            queryResult.Tab.Add(tabPerm);
        }

        session.SendPacket(queryResult);
    }

    public void SendQueryResponse(WorldSession session)
    {
        QueryGuildInfoResponse response = new()
        {
            GuildGUID = GetGUID(),
            HasGuildInfo = true,
            Info =
            {
                GuildGuid = GetGUID(),
                VirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
                EmblemStyle = _emblemInfo.Style,
                EmblemColor = _emblemInfo.Color,
                BorderStyle = _emblemInfo.BorderStyle,
                BorderColor = _emblemInfo.BorderColor,
                BackgroundColor = _emblemInfo.BackgroundColor
            }
        };

        foreach (var rankInfo in _ranks)
            response.Info.Ranks.Add(new QueryGuildInfoResponse.GuildInfo.RankInfo((byte)rankInfo.Id, (byte)rankInfo.Order, rankInfo.Name));

        response.Info.GuildName = _name;

        session.SendPacket(response);
    }

    public void SetBankTabText(byte tabId, string text)
    {
        var pTab = GetBankTab(tabId);

        if (pTab != null)
        {
            pTab.SetText(text);
            pTab.SendText(this);

            GuildEventTabTextChanged eventPacket = new()
            {
                Tab = tabId
            };

            BroadcastPacket(eventPacket);
        }
    }

    public bool SetName(string name)
    {
        if (_name == name || string.IsNullOrEmpty(name) || name.Length > 24 || _objectManager.IsReservedName(name) || !_objectManager.IsValidCharterName(name))
            return false;

        _name = name;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_NAME);
        stmt.AddValue(0, _name);
        stmt.AddValue(1, GetId());
        _characterDatabase.Execute(stmt);

        GuildNameChanged guildNameChanged = new()
        {
            GuildGUID = GetGUID(),
            GuildName = _name
        };

        BroadcastPacket(guildNameChanged);

        return true;
    }

    public void SwapItems(Player player, byte tabId, byte slotId, byte destTabId, byte destSlotId, uint splitedAmount)
    {
        if (tabId >= GetPurchasedTabsSize() ||
            slotId >= GuildConst.MaxBankSlots ||
            destTabId >= GetPurchasedTabsSize() ||
            destSlotId >= GuildConst.MaxBankSlots)
            return;

        if (tabId == destTabId && slotId == destSlotId)
            return;

        GuildBankMoveItemData from = new(this, player, tabId, slotId, _scriptManager);
        GuildBankMoveItemData to = new(this, player, destTabId, destSlotId, _scriptManager);
        MoveItems(from, to, splitedAmount);
    }

    public void SwapItemsWithInventory(Player player, bool toChar, byte tabId, byte slotId, byte playerBag, byte playerSlotId, uint splitedAmount)
    {
        if ((slotId >= GuildConst.MaxBankSlots && slotId != ItemConst.NullSlot) || tabId >= GetPurchasedTabsSize())
            return;

        GuildBankMoveItemData bankData = new(this, player, tabId, slotId, _scriptManager);
        GuildMemberMoveItemData charData = new(this, player, playerBag, playerSlotId, _scriptManager);

        if (toChar)
            MoveItems(bankData, charData, splitedAmount);
        else
            MoveItems(charData, bankData, splitedAmount);
    }

    public void UpdateCriteria(CriteriaType type, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player player)
    {
        GetAchievementMgr().UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, player);
    }

    public void UpdateMemberData(Player player, GuildMemberData dataid, uint value)
    {
        var member = GetMember(player.GUID);

        if (member == null)
            return;

        switch (dataid)
        {
            case GuildMemberData.ZoneId:
                member.ZoneId = value;

                break;
            case GuildMemberData.AchievementPoints:
                member.AchievementPoints = value;

                break;
            case GuildMemberData.Level:
                member.Level = (byte)value;

                break;
            default:
                Log.Logger.Error("Guild.UpdateMemberData: Called with incorrect DATAID {0} (value {1})", dataid, value);

                return;
        }
    }

    public void UpdateMemberWithdrawSlots(SQLTransaction trans, ObjectGuid guid, byte tabId)
    {
        var member = GetMember(guid);

        member?.UpdateBankTabWithdrawValue(trans, tabId, 1);
    }

    public bool Validate()
    {
        // Validate ranks data
        // GUILD RANKS represent a sequence starting from 0 = GUILD_MASTER (ALL PRIVILEGES) to max 9 (lowest privileges).
        // The lower rank id is considered higher rank - so promotion does rank-- and demotion does rank++
        // Between ranks in sequence cannot be gaps - so 0, 1, 2, 4 is impossible
        // Min ranks count is 2 and max is 10.
        var brokenRanks = false;
        var ranks = GetRanksSize();

        SQLTransaction trans = new();

        if (ranks is < GuildConst.MinRanks or > GuildConst.MaxRanks)
        {
            Log.Logger.Error("Guild {0} has invalid number of ranks, creating new...", _id);
            brokenRanks = true;
        }
        else
            for (byte rankId = 0; rankId < ranks; ++rankId)
            {
                var rankInfo = GetRankInfo((GuildRankId)rankId);

                if (rankInfo.Id != (GuildRankId)rankId)
                {
                    Log.Logger.Error("Guild {0} has broken rank id {1}, creating default set of ranks...", _id, rankId);
                    brokenRanks = true;
                }
                else
                    rankInfo.CreateMissingTabsIfNeeded(GetPurchasedTabsSize(), trans, true);
            }

        if (brokenRanks)
        {
            _ranks.Clear();
            CreateDefaultGuildRanks(trans);
        }

        // Validate members' data
        foreach (var member in _members.Values)
            if (GetRankInfo(member.RankId) == null)
                member.ChangeRank(trans, GetLowestRankId());

        // Repair the structure of the guild.
        // If the guildmaster doesn't exist or isn't member of the guild
        // attempt to promote another member.
        var leader = GetMember(_leaderGuid);

        if (leader == null)
        {
            DeleteMember(trans, _leaderGuid);

            // If no more members left, disband guild
            if (_members.Empty())
            {
                Disband();

                return false;
            }
        }
        else if (!leader.IsRank(GuildRankId.GuildMaster))
            SetLeader(trans, leader);

        if (trans.commands.Count > 0)
            _characterDatabase.CommitTransaction(trans);

        UpdateAccountsNumber();

        return true;
    }

    private void CreateDefaultGuildRanks(SQLTransaction trans, Locale loc = Locale.enUS)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
        stmt.AddValue(0, _id);
        trans.Append(stmt);

        CreateRank(trans, _objectManager.GetCypherString(CypherStrings.GuildMaster, loc), GuildRankRights.All);
        CreateRank(trans, _objectManager.GetCypherString(CypherStrings.GuildOfficer, loc), GuildRankRights.All);
        CreateRank(trans, _objectManager.GetCypherString(CypherStrings.GuildVeteran, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
        CreateRank(trans, _objectManager.GetCypherString(CypherStrings.GuildMember, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
        CreateRank(trans, _objectManager.GetCypherString(CypherStrings.GuildInitiate, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
    }

    // Private methods
    private void CreateNewBankTab()
    {
        var tabId = GetPurchasedTabsSize(); // Next free id
        _bankTabs.Add(new GuildBankTab(_id, tabId, _objectManager, _characterDatabase));

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_TAB);
        stmt.AddValue(0, _id);
        stmt.AddValue(1, tabId);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_TAB);
        stmt.AddValue(0, _id);
        stmt.AddValue(1, tabId);
        trans.Append(stmt);

        ++tabId;

        foreach (var rank in _ranks)
            rank.CreateMissingTabsIfNeeded(tabId, trans);

        _characterDatabase.CommitTransaction(trans);
    }

    private bool CreateRank(SQLTransaction trans, string name, GuildRankRights rights)
    {
        if (_ranks.Count >= GuildConst.MaxRanks)
            return false;

        byte newRankId = 0;

        while (GetRankInfo((GuildRankId)newRankId) != null)
            ++newRankId;

        // Ranks represent sequence 0, 1, 2, ... where 0 means guildmaster
        GuildRankInfo info = new(_id, (GuildRankId)newRankId, (GuildRankOrder)_ranks.Count, name, rights, 0);
        _ranks.Add(info);

        var isInTransaction = trans != null;

        if (!isInTransaction)
            trans = new SQLTransaction();

        info.CreateMissingTabsIfNeeded(GetPurchasedTabsSize(), trans);
        info.SaveToDB(trans);
        _characterDatabase.CommitTransaction(trans);

        if (!isInTransaction)
            _characterDatabase.CommitTransaction(trans);

        return true;
    }

    private void DeleteBankItems(SQLTransaction trans, bool removeItemsFromDB)
    {
        for (byte tabId = 0; tabId < GetPurchasedTabsSize(); ++tabId)
        {
            _bankTabs[tabId].Delete(trans, removeItemsFromDB);
            _bankTabs[tabId] = null;
        }

        _bankTabs.Clear();
    }

    private void DeleteMemberFromDB(SQLTransaction trans, ulong lowguid)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBER);
        stmt.AddValue(0, lowguid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    private InventoryResult DoItemsMove(GuildMoveItemData pSrc, GuildMoveItemData pDest, bool sendError, uint splitedAmount = 0)
    {
        var pDestItem = pDest.GetItem();
        var swap = pDestItem != null;

        var pSrcItem = pSrc.GetItem(splitedAmount != 0);
        // 1. Can store source item in destination
        var destResult = pDest.CanStore(pSrcItem, swap, sendError);

        if (destResult != InventoryResult.Ok)
            return destResult;

        // 2. Can store destination item in source
        if (swap)
        {
            var srcResult = pSrc.CanStore(pDestItem, true, true);

            if (srcResult != InventoryResult.Ok)
                return srcResult;
        }

        // GM LOG (@todo move to scripts)
        pDest.LogAction(pSrc);

        if (swap)
            pSrc.LogAction(pDest);

        SQLTransaction trans = new();
        // 3. Log bank events
        pDest.LogBankEvent(trans, pSrc, pSrcItem.Count);

        if (swap)
            pSrc.LogBankEvent(trans, pDest, pDestItem.Count);

        // 4. Remove item from source
        pSrc.RemoveItem(trans, pDest, splitedAmount);

        // 5. Remove item from destination
        if (swap)
            pDest.RemoveItem(trans, pSrc);

        // 6. Store item in destination
        pDest.StoreItem(trans, pSrcItem);

        // 7. Store item in source
        if (swap)
            pSrc.StoreItem(trans, pDestItem);

        _characterDatabase.CommitTransaction(trans);

        return InventoryResult.Ok;
    }

    private ulong GetGuildBankTabPrice(byte tabId)
    {
        // these prices are in gold units, not copper
        return tabId switch
        {
            0 => 100,
            1 => 250,
            2 => 500,
            3 => 1000,
            4 => 2500,
            5 => 5000,
            _ => 0
        };
    }

    private GuildRankId GetLowestRankId()
    {
        return _ranks.Last().Id;
    }

    private long GetMemberRemainingMoney(GuildMember member)
    {
        var rankId = member.RankId;

        if (rankId == GuildRankId.GuildMaster)
            return long.MaxValue;

        if ((GetRankRights(rankId) & (GuildRankRights.WithdrawRepair | GuildRankRights.WithdrawGold)) != 0)
        {
            var remaining = (long)(GetRankBankMoneyPerDay(rankId) * MoneyConstants.Gold - member.BankMoneyWithdrawValue);

            if (remaining > 0)
                return remaining;
        }

        return 0;
    }

    private uint GetRankBankMoneyPerDay(GuildRankId rankId)
    {
        return GetRankInfo(rankId)?.BankMoneyPerDay ?? 0;
    }

    private GuildBankRights GetRankBankTabRights(GuildRankId rankId, byte tabId)
    {
        return GetRankInfo(rankId)?.GetBankTabRights(tabId) ?? 0;
    }

    private int GetRankBankTabSlotsPerDay(GuildRankId rankId, byte tabId)
    {
        if (tabId >= GetPurchasedTabsSize())
            return 0;

        return GetRankInfo(rankId)?.GetBankTabSlotsPerDay(tabId) ?? 0;
    }

    private GuildRankInfo GetRankInfo(GuildRankId rankId)
    {
        return _ranks.Find(rank => rank.Id == rankId);
    }

    private GuildRankInfo GetRankInfo(GuildRankOrder rankOrder)
    {
        return _ranks.Find(rank => rank.Order == rankOrder);
    }

    private GuildRankRights GetRankRights(GuildRankId rankId)
    {
        return GetRankInfo(rankId)?.AccessRights ?? 0;
    }

    private byte GetRanksSize()
    {
        return (byte)_ranks.Count;
    }

    private bool HasRankRight(Player player, GuildRankRights right)
    {
        if (player == null)
            return false;

        var member = GetMember(player.GUID);

        if (member != null)
            return (GetRankRights(member.RankId) & right) != GuildRankRights.None;

        return false;
    }

    private bool IsLeader(Player player)
    {
        if (player.GUID == _leaderGuid)
            return true;

        var member = GetMember(player.GUID);

        return member != null && member.IsRank(GuildRankId.GuildMaster);
    }

    private void ModifyBankMoney(SQLTransaction trans, ulong amount, bool add)
    {
        if (add)
            _bankMoney += amount;
        else
        {
            // Check if there is enough money in bank.
            if (_bankMoney < amount)
                return;

            _bankMoney -= amount;
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_MONEY);
        stmt.AddValue(0, _bankMoney);
        stmt.AddValue(1, _id);
        trans.Append(stmt);
    }

    private void MoveItems(GuildMoveItemData pSrc, GuildMoveItemData pDest, uint splitedAmount)
    {
        // 1. Initialize source item
        if (!pSrc.InitItem())
            return; // No source item

        // 2. Check source item
        if (!pSrc.CheckItem(ref splitedAmount))
            return; // Source item or splited amount is invalid

        // 3. Check destination rights
        if (!pDest.HasStoreRights(pSrc))
            return; // Player has no rights to store item in destination

        // 4. Check source withdraw rights
        if (!pSrc.HasWithdrawRights(pDest))
            return; // Player has no rights to withdraw items from source

        // 5. Check split
        if (splitedAmount != 0)
        {
            // 5.1. Clone source item
            if (!pSrc.CloneItem(splitedAmount))
                return; // Item could not be cloned

            // 5.2. Move splited item to destination
            DoItemsMove(pSrc, pDest, true, splitedAmount);
        }
        else // 6. No split
        {
            // 6.1. Try to merge items in destination (pDest.GetItem() == NULL)
            var mergeAttemptResult = DoItemsMove(pSrc, pDest, false);

            if (mergeAttemptResult != InventoryResult.Ok) // Item could not be merged
            {
                // 6.2. Try to swap items
                // 6.2.1. Initialize destination item
                if (!pDest.InitItem())
                {
                    pSrc.SendEquipError(mergeAttemptResult, pSrc.GetItem());

                    return;
                }

                // 6.2.2. Check rights to store item in source (opposite direction)
                if (!pSrc.HasStoreRights(pDest))
                    return; // Player has no rights to store item in source (opposite direction)

                if (!pDest.HasWithdrawRights(pSrc))
                    return; // Player has no rights to withdraw item from destination (opposite direction)

                // 6.2.3. Swap items (pDest.GetItem() != NULL)
                DoItemsMove(pSrc, pDest, true);
            }
        }

        // 7. Send changes
        SendBankContentUpdate(pSrc, pDest);
    }

    private void SendBankContentUpdate(GuildMoveItemData pSrc, GuildMoveItemData pDest)
    {
        byte tabId = 0;
        List<byte> slots = new();

        if (pSrc.IsBank()) // B .
        {
            tabId = pSrc.Container;
            slots.Insert(0, pSrc.SlotId);

            if (pDest.IsBank()) // B . B
            {
                // Same tab - add destination slots to collection
                if (pDest.Container == pSrc.Container)
                    pDest.CopySlots(slots);
                else // Different tabs - send second message
                {
                    List<byte> destSlots = new();
                    pDest.CopySlots(destSlots);
                    SendBankContentUpdate(pDest.Container, destSlots);
                }
            }
        }
        else if (pDest.IsBank()) // C . B
        {
            tabId = pDest.Container;
            pDest.CopySlots(slots);
        }

        SendBankContentUpdate(tabId, slots);
    }

    private void SendBankContentUpdate(byte tabId, List<byte> slots)
    {
        var tab = GetBankTab(tabId);

        if (tab == null)
            return;

        GuildBankQueryResults packet = new()
        {
            FullUpdate = true, // @todo
            Tab = tabId,
            Money = _bankMoney
        };

        foreach (var slot in slots)
        {
            var tabItem = tab.GetItem(slot);

            GuildBankItemInfo itemInfo = new()
            {
                Slot = slot,
                Item =
                {
                    ItemID = tabItem?.Entry ?? 0
                },
                Count = (int)(tabItem?.Count ?? 0),
                EnchantmentID = (int)(tabItem?.GetEnchantmentId(EnchantmentSlot.Perm) ?? 0),
                Charges = tabItem != null ? Math.Abs(tabItem.GetSpellCharges()) : 0,
                OnUseEnchantmentID = (int)(tabItem?.GetEnchantmentId(EnchantmentSlot.Use) ?? 0),
                Flags = 0,
                Locked = false
            };

            if (tabItem != null)
            {
                byte i = 0;

                foreach (var gemData in tabItem.ItemData.Gems)
                {
                    if (gemData.ItemId != 0)
                    {
                        ItemGemData gem = new()
                        {
                            Slot = i,
                            Item = new ItemInstance(gemData)
                        };

                        itemInfo.SocketEnchant.Add(gem);
                    }

                    ++i;
                }
            }

            packet.ItemInfo.Add(itemInfo);
        }

        foreach (var (guid, member) in _members)
        {
            if (!MemberHasTabRights(guid, tabId, GuildBankRights.ViewTab))
                continue;

            var player = member.FindPlayer();

            if (player == null)
                continue;

            packet.WithdrawalsRemaining = GetMemberRemainingSlots(member, tabId);
            player.SendPacket(packet);
        }
    }

    private void SendEventBankMoneyChanged()
    {
        GuildEventBankMoneyChanged eventPacket = new()
        {
            Money = GetBankMoney()
        };

        BroadcastPacket(eventPacket);
    }

    private void SendEventMotd(WorldSession session, bool broadcast = false)
    {
        GuildEventMotd eventPacket = new()
        {
            MotdText = GetMotd()
        };

        if (broadcast)
            BroadcastPacket(eventPacket);
        else
        {
            session.SendPacket(eventPacket);
            Log.Logger.Debug("SMSG_GUILD_EVENT_MOTD [{0}] ", session.GetPlayerInfo());
        }
    }

    private void SendEventNewLeader(GuildMember newLeader, GuildMember oldLeader, bool isSelfPromoted = false)
    {
        GuildEventNewLeader eventPacket = new()
        {
            SelfPromoted = isSelfPromoted
        };

        if (newLeader != null)
        {
            eventPacket.NewLeaderGUID = newLeader.GUID;
            eventPacket.NewLeaderName = newLeader.Name;
            eventPacket.NewLeaderVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress;
        }

        if (oldLeader != null)
        {
            eventPacket.OldLeaderGUID = oldLeader.GUID;
            eventPacket.OldLeaderName = oldLeader.Name;
            eventPacket.OldLeaderVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress;
        }

        BroadcastPacket(eventPacket);
    }

    private void SendEventPlayerLeft(Player leaver, Player remover = null, bool isRemoved = false)
    {
        GuildEventPlayerLeft eventPacket = new()
        {
            Removed = isRemoved,
            LeaverGUID = leaver.GUID,
            LeaverName = leaver.GetName(),
            LeaverVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress
        };

        if (isRemoved && remover != null)
        {
            eventPacket.RemoverGUID = remover.GUID;
            eventPacket.RemoverName = remover.GetName();
            eventPacket.RemoverVirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress;
        }

        BroadcastPacket(eventPacket);
    }

    private void SendEventPresenceChanged(WorldSession session, bool loggedOn, bool broadcast = false)
    {
        var player = session.Player;

        GuildEventPresenceChange eventPacket = new()
        {
            Guid = player.GUID,
            Name = player.GetName(),
            VirtualRealmAddress = WorldManager.Realm.Id.VirtualRealmAddress,
            LoggedOn = loggedOn,
            Mobile = false
        };

        if (broadcast)
            BroadcastPacket(eventPacket);
        else
            session.SendPacket(eventPacket);
    }

    private void SendGuildRanksUpdate(ObjectGuid setterGuid, ObjectGuid targetGuid, GuildRankId rank)
    {
        var member = GetMember(targetGuid);

        GuildSendRankChange rankChange = new()
        {
            Officer = setterGuid,
            Other = targetGuid,
            RankID = (byte)rank,
            Promote = rank < member.RankId
        };

        BroadcastPacket(rankChange);

        member.ChangeRank(null, rank);

        Log.Logger.Debug("SMSG_GUILD_RANKS_UPDATE [Broadcast] Target: {0}, Issuer: {1}, RankId: {2}", targetGuid.ToString(), setterGuid.ToString(), rank);
    }

    private void SetLeader(SQLTransaction trans, GuildMember leader)
    {
        var isInTransaction = trans != null;

        if (!isInTransaction)
            trans = new SQLTransaction();

        _leaderGuid = leader.GUID;
        leader.ChangeRank(trans, GuildRankId.GuildMaster);

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_LEADER);
        stmt.AddValue(0, _leaderGuid.Counter);
        stmt.AddValue(1, _id);
        trans.Append(stmt);

        if (!isInTransaction)
            _characterDatabase.CommitTransaction(trans);
    }

    private void SetRankBankMoneyPerDay(GuildRankId rankId, uint moneyPerDay)
    {
        var rankInfo = GetRankInfo(rankId);

        rankInfo?.SetBankMoneyPerDay(moneyPerDay);
    }

    private void SetRankBankTabRightsAndSlots(GuildRankId rankId, GuildBankRightsAndSlots rightsAndSlots, bool saveToDB = true)
    {
        if (rightsAndSlots.TabId >= GetPurchasedTabsSize())
            return;

        var rankInfo = GetRankInfo(rankId);

        rankInfo?.SetBankTabSlotsAndRights(rightsAndSlots, saveToDB);
    }

    private void UpdateAccountsNumber()
    {
        // We use a set to be sure each element will be unique
        var accountsIdSet = _members.Values.Select(member => member.AccountId).ToList();

        _accountsNumber = (uint)accountsIdSet.Count;
    }
}