// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Achievements;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Scripting.Interfaces.IGuild;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Serilog;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Globals;
using Forged.RealmServer.World;
using Forged.RealmServer.Cache;
using Forged.RealmServer.Conditions;

namespace Forged.RealmServer.Guilds;

public class Guild
{
	public Guild(ClassFactory classFactory)
    {
        _classFactory = classFactory;
        _criteriaManager = _classFactory.Resolve<CriteriaManager>();
        _gameTime = _classFactory.Resolve<GameTime>();
        _characterDatabase = _classFactory.Resolve<CharacterDatabase>();
        _cliDB = _classFactory.Resolve<CliDB>();
        _scriptManager = _classFactory.Resolve<ScriptManager>();
        _guildManager = _classFactory.Resolve<GuildManager>();
        _worldManager = _classFactory.Resolve<WorldManager>();
        _gameObjectManager = _classFactory.Resolve<GameObjectManager>();
        _objectAccessor = _classFactory.Resolve<ObjectAccessor>();
        _worldConfig = _classFactory.Resolve<WorldConfig>();
        _characterCache = _classFactory.Resolve<CharacterCache>();
        _calendarManager = _classFactory.Resolve<CalendarManager>();

		_eventLog = new(_worldConfig);
        _newsLog = new(_worldConfig);
        _emblemInfo = new(_cliDB, _characterDatabase);
        _achievementSys = new GuildAchievementMgr(this);

		for (var i = 0; i < _bankEventLog.Length; ++i)
			_bankEventLog[i] = new LogHolder<BankEventLogEntry>(_worldConfig);
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
		_createdDate = _gameTime.CurrentGameTime;

		Log.Logger.Debug(
					"GUILD: creating guild [{0}] for leader {1} ({2})",
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
		stmt.AddValue(++index, _emblemInfo.GetStyle());
		stmt.AddValue(++index, _emblemInfo.GetColor());
		stmt.AddValue(++index, _emblemInfo.GetBorderStyle());
		stmt.AddValue(++index, _emblemInfo.GetBorderColor());
		stmt.AddValue(++index, _emblemInfo.GetBackgroundColor());
		stmt.AddValue(++index, _bankMoney);
		trans.Append(stmt);

		_CreateDefaultGuildRanks(trans, pLeaderSession.SessionDbLocaleIndex); // Create default ranks
		var ret = AddMember(trans, _leaderGuid, GuildRankId.GuildMaster);    // Add guildmaster

		_characterDatabase.CommitTransaction(trans);

		if (ret)
		{
			var leader = GetMember(_leaderGuid);

			if (leader != null)
				SendEventNewLeader(leader, null);

			_scriptManager.ForEach<IGuildOnCreate>(p => p.OnCreate(this, pLeader, name));
		}

		return ret;
	}

	public void Disband()
	{
		_scriptManager.ForEach<IGuildOnDisband>(p => p.OnDisband(this));

		BroadcastPacket(new GuildEventDisbanded());

		SQLTransaction trans = new();

		while (!_members.Empty())
		{
			var member = _members.First();
			DeleteMember(trans, member.Value.GetGUID(), true);
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
		_DeleteBankItems(trans, true);

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

	public void SaveToDB()
	{
		SQLTransaction trans = new();

		GetAchievementMgr().SaveToDB(trans);

		_characterDatabase.CommitTransaction(trans);
	}

	public void UpdateMemberData(Player player, GuildMemberData dataid, uint value)
	{
		var member = GetMember(player.GUID);

		if (member != null)
			switch (dataid)
			{
				case GuildMemberData.ZoneId:
					member.SetZoneId(value);

					break;
				case GuildMemberData.AchievementPoints:
					member.SetAchievementPoints(value);

					break;
				case GuildMemberData.Level:
					member.SetLevel(value);

					break;
				default:
					Log.Logger.Error("Guild.UpdateMemberData: Called with incorrect DATAID {0} (value {1})", dataid, value);

					return;
			}
	}

	public bool SetName(string name)
	{
		if (_name == name || string.IsNullOrEmpty(name) || name.Length > 24 || _gameObjectManager.IsReservedName(name) || !GameObjectManager.IsValidCharterName(name))
			return false;

		_name = name;
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_NAME);
		stmt.AddValue(0, _name);
		stmt.AddValue(1, GetId());
		_characterDatabase.Execute(stmt);

		GuildNameChanged guildNameChanged = new();
		guildNameChanged.GuildGUID = GetGUID();
		guildNameChanged.GuildName = _name;
		BroadcastPacket(guildNameChanged);

		return true;
	}

	public void HandleRoster(WorldSession session = null)
	{
		GuildRoster roster = new();
		roster.NumAccounts = (int)_accountsNumber;
		roster.CreateDate = (uint)_createdDate;
		roster.GuildFlags = 0;

		var sendOfficerNote = _HasRankRight(session.Player, GuildRankRights.ViewOffNote);

		foreach (var member in _members.Values)
		{
			GuildRosterMemberData memberData = new();

			memberData.Guid = member.GetGUID();
			memberData.RankID = (int)member.GetRankId();
			memberData.AreaID = (int)member.GetZoneId();
			memberData.PersonalAchievementPoints = (int)member.GetAchievementPoints();
			memberData.GuildReputation = (int)member.GetTotalReputation();
			memberData.LastSave = member.GetInactiveDays();

			//GuildRosterProfessionData

			memberData.VirtualRealmAddress = _worldManager.VirtualRealmAddress;
			memberData.Status = (byte)member.GetFlags();
			memberData.Level = member.GetLevel();
			memberData.ClassID = (byte)member.GetClass();
			memberData.Gender = (byte)member.GetGender();
			memberData.RaceID = (byte)member.GetRace();

			memberData.Authenticated = false;
			memberData.SorEligible = false;

			memberData.Name = member.GetName();
			memberData.Note = member.GetPublicNote();

			if (sendOfficerNote)
				memberData.OfficerNote = member.GetOfficerNote();

			roster.MemberData.Add(memberData);
		}

		roster.WelcomeText = _motd;
		roster.InfoText = _info;

		if (session != null)
			session.SendPacket(roster);
	}

	public void SendQueryResponse(WorldSession session)
	{
		QueryGuildInfoResponse response = new();
		response.GuildGUID = GetGUID();
		response.HasGuildInfo = true;

		response.Info.GuildGuid = GetGUID();
		response.Info.VirtualRealmAddress = _worldManager.VirtualRealmAddress;

		response.Info.EmblemStyle = _emblemInfo.GetStyle();
		response.Info.EmblemColor = _emblemInfo.GetColor();
		response.Info.BorderStyle = _emblemInfo.GetBorderStyle();
		response.Info.BorderColor = _emblemInfo.GetBorderColor();
		response.Info.BackgroundColor = _emblemInfo.GetBackgroundColor();

		foreach (var rankInfo in _ranks)
			response.Info.Ranks.Add(new QueryGuildInfoResponse.GuildInfo.RankInfo((byte)rankInfo.GetId(), (byte)rankInfo.GetOrder(), rankInfo.GetName()));

		response.Info.GuildName = _name;

		session.SendPacket(response);
	}

	public void SendGuildRankInfo(WorldSession session)
	{
		GuildRanks ranks = new();

		foreach (var rankInfo in _ranks)
		{
			GuildRankData rankData = new();

			rankData.RankID = (byte)rankInfo.GetId();
			rankData.RankOrder = (byte)rankInfo.GetOrder();
			rankData.Flags = (uint)rankInfo.GetRights();
			rankData.WithdrawGoldLimit = (rankInfo.GetId() == GuildRankId.GuildMaster ? uint.MaxValue : (rankInfo.GetBankMoneyPerDay() / MoneyConstants.Gold));
			rankData.RankName = rankInfo.GetName();

			for (byte j = 0; j < GuildConst.MaxBankTabs; ++j)
			{
				rankData.TabFlags[j] = (uint)rankInfo.GetBankTabRights(j);
				rankData.TabWithdrawItemLimit[j] = (uint)rankInfo.GetBankTabSlotsPerDay(j);
			}

			ranks.Ranks.Add(rankData);
		}

		session.SendPacket(ranks);
	}

	public void HandleSetAchievementTracking(WorldSession session, List<uint> achievementIds)
	{
		var player = session.Player;

		var member = GetMember(player.GUID);

		if (member != null)
		{
			List<uint> criteriaIds = new();

			foreach (var achievementId in achievementIds)
			{
				var achievement = _cliDB.AchievementStorage.LookupByKey(achievementId);

				if (achievement != null)
				{
					var tree = _criteriaManager.GetCriteriaTree(achievement.CriteriaTree);

					if (tree != null)
						CriteriaManager.WalkCriteriaTree(tree,
														node =>
														{
															if (node.Criteria != null)
																criteriaIds.Add(node.Criteria.Id);
														});
				}
			}

			member.SetTrackedCriteriaIds(criteriaIds);
			GetAchievementMgr().SendAllTrackedCriterias(player, member.GetTrackedCriteriaIds());
		}
	}

	public void HandleGetAchievementMembers(WorldSession session, uint achievementId)
	{
		GetAchievementMgr().SendAchievementMembers(session.Player, achievementId);
	}

	public void HandleSetMOTD(WorldSession session, string motd)
	{
		if (_motd == motd)
			return;

		// Player must have rights to set MOTD
		if (!_HasRankRight(session.Player, GuildRankRights.SetMotd))
		{
			SendCommandResult(session, GuildCommandType.EditMOTD, GuildCommandError.Permissions);
		}
		else
		{
			_motd = motd;

			_scriptManager.ForEach<IGuildOnMOTDChanged>(p => p.OnMOTDChanged(this, motd));

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MOTD);
			stmt.AddValue(0, motd);
			stmt.AddValue(1, _id);
			_characterDatabase.Execute(stmt);

			SendEventMOTD(session, true);
		}
	}

	public void HandleSetInfo(WorldSession session, string info)
	{
		if (_info == info)
			return;

		// Player must have rights to set guild's info
		if (_HasRankRight(session.Player, GuildRankRights.ModifyGuildInfo))
		{
			_info = info;

			_scriptManager.ForEach<IGuildOnInfoChanged>(p => p.OnInfoChanged(this, info));

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_INFO);
			stmt.AddValue(0, info);
			stmt.AddValue(1, _id);
			_characterDatabase.Execute(stmt);
		}
	}

	public void HandleSetEmblem(WorldSession session, EmblemInfo emblemInfo)
	{
		var player = session.Player;

		if (!_IsLeader(player))
		{
			SendSaveEmblemResult(session, GuildEmblemError.NotGuildMaster); // "Only guild leaders can create emblems."
		}
		else if (!player.HasEnoughMoney(10 * MoneyConstants.Gold))
		{
			SendSaveEmblemResult(session, GuildEmblemError.NotEnoughMoney); // "You can't afford to do that."
		}
		else
		{
			player.ModifyMoney(-(long)10 * MoneyConstants.Gold);

			_emblemInfo = emblemInfo;
			_emblemInfo.SaveToDB(_id);

			SendSaveEmblemResult(session, GuildEmblemError.Success); // "Guild Emblem saved."

			SendQueryResponse(session);
		}
	}

	public void HandleSetNewGuildMaster(WorldSession session, string name, bool isSelfPromote)
	{
		var player = session.Player;
		var oldGuildMaster = GetMember(GetLeaderGUID());

		Member newGuildMaster;

		if (isSelfPromote)
		{
			newGuildMaster = GetMember(player.GUID);

			if (newGuildMaster == null)
				return;

			var oldRank = GetRankInfo(newGuildMaster.GetRankId());

			// only second highest rank can take over guild
			if (oldRank.GetOrder() != (GuildRankOrder)1 || oldGuildMaster.GetInactiveDays() < GuildConst.MasterDethroneInactiveDays)
			{
				SendCommandResult(session, GuildCommandType.ChangeLeader, GuildCommandError.Permissions);

				return;
			}
		}
		else
		{
			if (!_IsLeader(player))
			{
				SendCommandResult(session, GuildCommandType.ChangeLeader, GuildCommandError.Permissions);

				return;
			}

			newGuildMaster = GetMember(name);

			if (newGuildMaster == null)
				return;
		}

		SQLTransaction trans = new();

		_SetLeader(trans, newGuildMaster);
		oldGuildMaster.ChangeRank(trans, _GetLowestRankId());

		SendEventNewLeader(newGuildMaster, oldGuildMaster, isSelfPromote);

		_characterDatabase.CommitTransaction(trans);
	}

	public void HandleSetBankTabInfo(WorldSession session, byte tabId, string name, string icon)
	{
		var tab = GetBankTab(tabId);

		if (tab == null)
		{
			Log.Logger.Error(
						"Guild.HandleSetBankTabInfo: Player {0} trying to change bank tab info from unexisting tab {1}.",
						session.Player.GetName(),
						tabId);

			return;
		}

		tab.SetInfo(name, icon);

		GuildEventTabModified packet = new();
		packet.Tab = tabId;
		packet.Name = name;
		packet.Icon = icon;
		BroadcastPacket(packet);
	}

	public void HandleSetMemberNote(WorldSession session, string note, ObjectGuid guid, bool isPublic)
	{
		// Player must have rights to set public/officer note
		if (!_HasRankRight(session.Player, isPublic ? GuildRankRights.EditPublicNote : GuildRankRights.EOffNote))
			SendCommandResult(session, GuildCommandType.EditPublicNote, GuildCommandError.Permissions);

		var member = GetMember(guid);

		if (member != null)
		{
			if (isPublic)
				member.SetPublicNote(note);
			else
				member.SetOfficerNote(note);

			GuildMemberUpdateNote updateNote = new();
			updateNote.Member = guid;
			updateNote.IsPublic = isPublic;
			updateNote.Note = note;
			BroadcastPacket(updateNote);
		}
	}

	public void HandleSetRankInfo(WorldSession session, GuildRankId rankId, string name, GuildRankRights rights, uint moneyPerDay, GuildBankRightsAndSlots[] rightsAndSlots)
	{
		// Only leader can modify ranks
		if (!_IsLeader(session.Player))
			SendCommandResult(session, GuildCommandType.ChangeRank, GuildCommandError.Permissions);

		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
		{
			rankInfo.SetName(name);
			rankInfo.SetRights(rights);
			_SetRankBankMoneyPerDay(rankId, moneyPerDay * MoneyConstants.Gold);

			foreach (var rightsAndSlot in rightsAndSlots)
				_SetRankBankTabRightsAndSlots(rankId, rightsAndSlot);

			GuildEventRankChanged packet = new();
			packet.RankID = (byte)rankId;
			BroadcastPacket(packet);
		}
	}

	public void HandleBuyBankTab(WorldSession session, byte tabId)
	{
		var player = session.Player;

		if (player == null)
			return;

		var member = GetMember(player.GUID);

		if (member == null)
			return;

		if (_GetPurchasedTabsSize() >= GuildConst.MaxBankTabs)
			return;

		if (tabId != _GetPurchasedTabsSize())
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

		_CreateNewBankTab();

		BroadcastPacket(new GuildEventTabAdded());

		SendPermissions(session); //Hack to force client to update permissions
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

		if (!_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && pInvitee.Team != player.Team)
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
		if (!_HasRankRight(player, GuildRankRights.Invite))
		{
			SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.Permissions);

			return;
		}

		SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.Success, name);

		Log.Logger.Debug("Player {0} invited {1} to join his Guild", player.GetName(), name);

		pInvitee.GuildIdInvited = _id;
		_LogEvent(GuildEventLogTypes.InvitePlayer, player.GUID.Counter, pInvitee.GUID.Counter);

		GuildInvite invite = new();

		invite.InviterVirtualRealmAddress = _worldManager.VirtualRealmAddress;
		invite.GuildVirtualRealmAddress = _worldManager.VirtualRealmAddress;
		invite.GuildGUID = GetGUID();

		invite.EmblemStyle = _emblemInfo.GetStyle();
		invite.EmblemColor = _emblemInfo.GetColor();
		invite.BorderStyle = _emblemInfo.GetBorderStyle();
		invite.BorderColor = _emblemInfo.GetBorderColor();
		invite.Background = _emblemInfo.GetBackgroundColor();
		invite.AchievementPoints = (int)GetAchievementMgr().AchievementPoints;

		invite.InviterName = player.GetName();
		invite.GuildName = GetName();

		var oldGuild = pInvitee.Guild;

		if (oldGuild)
		{
			invite.OldGuildGUID = oldGuild.GetGUID();
			invite.OldGuildName = oldGuild.GetName();
			invite.OldGuildVirtualRealmAddress = _worldManager.VirtualRealmAddress;
		}

		pInvitee.SendPacket(invite);
	}

	public void HandleAcceptMember(WorldSession session)
	{
		var player = session.Player;

		if (!_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) &&
			player.Team != _characterCache.GetCharacterTeamByGuid(GetLeaderGUID()))
			return;

		AddMember(null, player.GUID);
	}

	public void HandleLeaveMember(WorldSession session)
	{
		var player = session.Player;

		// If leader is leaving
		if (_IsLeader(player))
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
			DeleteMember(null, player.GUID, false, false);

			_LogEvent(GuildEventLogTypes.LeaveGuild, player.GUID.Counter);
			SendEventPlayerLeft(player);

			SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.Success, _name);
		}

		_calendarManager.RemovePlayerGuildEventsAndSignups(player.GUID, GetId());
	}

	public void HandleRemoveMember(WorldSession session, ObjectGuid guid)
	{
		var player = session.Player;

		// Player must have rights to remove members
		if (!_HasRankRight(player, GuildRankRights.Remove))
			SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.Permissions);

		var member = GetMember(guid);

		if (member != null)
		{
			var name = member.GetName();

			// Guild masters cannot be removed
			if (member.IsRank(GuildRankId.GuildMaster))
			{
				SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.LeaderLeave);
			}
			// Do not allow to remove player with the same rank or higher
			else
			{
				var memberMe = GetMember(player.GUID);
				var myRank = GetRankInfo(memberMe.GetRankId());
				var targetRank = GetRankInfo(member.GetRankId());

				if (memberMe == null || targetRank.GetOrder() <= myRank.GetOrder())
				{
					SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.RankTooHigh_S, name);
				}
				else
				{
					DeleteMember(null, guid, false, true);
					_LogEvent(GuildEventLogTypes.UninvitePlayer, player.GUID.Counter, guid.Counter);

					var pMember = _objectAccessor.FindConnectedPlayer(guid);
					SendEventPlayerLeft(pMember, player, true);

					SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.Success, name);
				}
			}
		}
	}

	public void HandleUpdateMemberRank(WorldSession session, ObjectGuid guid, bool demote)
	{
		var player = session.Player;
		var type = demote ? GuildCommandType.DemotePlayer : GuildCommandType.PromotePlayer;
		// Player must have rights to promote
		Member member;

		if (!_HasRankRight(player, demote ? GuildRankRights.Demote : GuildRankRights.Promote))
		{
			SendCommandResult(session, type, GuildCommandError.LeaderLeave);
		}
		// Promoted player must be a member of guild
		else if ((member = GetMember(guid)) != null)
		{
			var name = member.GetName();

			// Player cannot promote himself
			if (member.IsSamePlayer(player.GUID))
			{
				SendCommandResult(session, type, GuildCommandError.NameInvalid);

				return;
			}

			var memberMe = GetMember(player.GUID);
			var myRank = GetRankInfo(memberMe.GetRankId());
			var oldRank = GetRankInfo(member.GetRankId());
			GuildRankId newRankId;

			if (demote)
			{
				// Player can demote only lower rank members
				if (oldRank.GetOrder() <= myRank.GetOrder())
				{
					SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);

					return;
				}

				// Lowest rank cannot be demoted
				var newRank = GetRankInfo(oldRank.GetOrder() + 1);

				if (newRank == null)
				{
					SendCommandResult(session, type, GuildCommandError.RankTooLow_S, name);

					return;
				}

				newRankId = newRank.GetId();
			}
			else
			{
				// Allow to promote only to lower rank than member's rank
				// memberMe.GetRankId() + 1 is the highest rank that current player can promote to
				if ((oldRank.GetOrder() - 1) <= myRank.GetOrder())
				{
					SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);

					return;
				}

				newRankId = GetRankInfo((oldRank.GetOrder() - 1)).GetId();
			}

			member.ChangeRank(null, newRankId);
			_LogEvent(demote ? GuildEventLogTypes.DemotePlayer : GuildEventLogTypes.PromotePlayer, player.GUID.Counter, member.GetGUID().Counter, (byte)newRankId);
			//_BroadcastEvent(demote ? GuildEvents.Demotion : GuildEvents.Promotion, ObjectGuid.Empty, player.GetName(), name, _GetRankName((byte)newRankId));
		}
	}

	public void HandleSetMemberRank(WorldSession session, ObjectGuid targetGuid, ObjectGuid setterGuid, GuildRankOrder rank)
	{
		var player = session.Player;
		var member = GetMember(targetGuid);
		var rights = GuildRankRights.Promote;
		var type = GuildCommandType.PromotePlayer;

		var oldRank = GetRankInfo(member.GetRankId());
		var newRank = GetRankInfo(rank);

		if (oldRank == null || newRank == null)
			return;

		if (rank > oldRank.GetOrder())
		{
			rights = GuildRankRights.Demote;
			type = GuildCommandType.DemotePlayer;
		}

		// Promoted player must be a member of guild
		if (!_HasRankRight(player, rights))
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

		SendGuildRanksUpdate(setterGuid, targetGuid, newRank.GetId());
	}

	public void HandleAddNewRank(WorldSession session, string name)
	{
		var size = _GetRanksSize();

		if (size >= GuildConst.MaxRanks)
			return;

		// Only leader can add new rank
		if (_IsLeader(session.Player))
			if (_CreateRank(null, name, GuildRankRights.GChatListen | GuildRankRights.GChatSpeak))
				BroadcastPacket(new GuildEventRanksUpdated());
	}

	public void HandleRemoveRank(WorldSession session, GuildRankOrder rankOrder)
	{
		// Cannot remove rank if total count is minimum allowed by the client or is not leader
		if (_GetRanksSize() <= GuildConst.MinRanks || !_IsLeader(session.Player))
			return;

		var rankInfo = _ranks.Find(rank => rank.GetOrder() == rankOrder);

		if (rankInfo == null)
			return;

		var trans = new SQLTransaction();

		// Delete bank rights for rank
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS_FOR_RANK);
		stmt.AddValue(0, _id);
		stmt.AddValue(1, (byte)rankInfo.GetId());
		trans.Append(stmt);

		// Delete rank
		stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_RANK);
		stmt.AddValue(0, _id);
		stmt.AddValue(1, (byte)rankInfo.GetId());
		trans.Append(stmt);

		_ranks.Remove(rankInfo);

		// correct order of other ranks
		foreach (var otherRank in _ranks)
		{
			if (otherRank.GetOrder() < rankOrder)
				continue;

			otherRank.SetOrder(otherRank.GetOrder() - 1);

			stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
			stmt.AddValue(0, (byte)otherRank.GetOrder());
			stmt.AddValue(1, (byte)otherRank.GetId());
			stmt.AddValue(2, _id);
			trans.Append(stmt);
		}

		_characterDatabase.CommitTransaction(trans);

		BroadcastPacket(new GuildEventRanksUpdated());
	}

	public void HandleShiftRank(WorldSession session, GuildRankOrder rankOrder, bool shiftUp)
	{
		// Only leader can modify ranks
		if (!_IsLeader(session.Player))
			return;

		var otherRankOrder = (GuildRankOrder)(rankOrder + (shiftUp ? -1 : 1));

		var rankInfo = GetRankInfo(rankOrder);
		var otherRankInfo = GetRankInfo(otherRankOrder);

		if (rankInfo == null || otherRankInfo == null)
			return;

		// can't shift guild master rank (rank id = 0) - there's already a client-side limitation for it so that's just a safe-guard
		if (rankInfo.GetId() == GuildRankId.GuildMaster || otherRankInfo.GetId() == GuildRankId.GuildMaster)
			return;

		rankInfo.SetOrder(otherRankOrder);
		otherRankInfo.SetOrder(rankOrder);

		var trans = new SQLTransaction();

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
		stmt.AddValue(0, (byte)rankInfo.GetOrder());
		stmt.AddValue(1, (byte)rankInfo.GetId());
		stmt.AddValue(2, _id);
		trans.Append(stmt);

		stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
		stmt.AddValue(0, (byte)otherRankInfo.GetOrder());
		stmt.AddValue(1, (byte)otherRankInfo.GetId());
		stmt.AddValue(2, _id);
		trans.Append(stmt);

		_characterDatabase.CommitTransaction(trans);

		// force client to re-request SMSG_GUILD_RANKS
		BroadcastPacket(new GuildEventRanksUpdated());
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
		_ModifyBankMoney(trans, amount, true);

		if (!cashFlow)
		{
			player.ModifyMoney(-(long)amount);
			player.SaveGoldToDB(trans);
		}

		_LogBankEvent(trans, cashFlow ? GuildBankEventLogTypes.CashFlowDeposit : GuildBankEventLogTypes.DepositMoney, 0, player.GUID.Counter, (uint)amount);
		_characterDatabase.CommitTransaction(trans);

		SendEventBankMoneyChanged();

		if (player.Session.HasPermission(RBACPermissions.LogGmTrade))
			Log.Logger.Information("GM {0} (Account: {1}) deposit money (Amount: {2}) to guild bank (Guild ID {3})",
							player.GetName(),
							player.Session.AccountId,
							amount,
							_id);
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

		if (!_HasRankRight(player, repair ? GuildRankRights.WithdrawRepair : GuildRankRights.WithdrawGold))
			return false;

		if (_GetMemberRemainingMoney(member) < (long)amount) // Check if we have enough slot/money today
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
		_ModifyBankMoney(trans, amount, false);

		// Log guild bank event
		_LogBankEvent(trans, repair ? GuildBankEventLogTypes.RepairMoney : GuildBankEventLogTypes.WithdrawMoney, 0, player.GUID.Counter, (uint)amount);
		_characterDatabase.CommitTransaction(trans);

		SendEventBankMoneyChanged();

		return true;
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

	public void HandleDelete(WorldSession session)
	{
		// Only leader can disband guild
		if (_IsLeader(session.Player))
		{
			Disband();
			Log.Logger.Debug("Guild Successfully Disbanded");
		}
	}

	public void HandleGuildPartyRequest(WorldSession session)
	{
		var player = session.Player;
		var group = player.Group;

		// Make sure player is a member of the guild and that he is in a group.
		if (!IsMember(player.GUID) || !group)
			return;

		GuildPartyState partyStateResponse = new();
		partyStateResponse.InGuildParty = (player.Map.GetOwnerGuildId(player.Team) == GetId());
		partyStateResponse.NumMembers = 0;
		partyStateResponse.NumRequired = 0;
		partyStateResponse.GuildXPEarnedMult = 0.0f;
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

	public void SendEventLog(WorldSession session)
	{
		var eventLog = _eventLog.GetGuildLog();

		GuildEventLogQueryResults packet = new();

		foreach (var entry in eventLog)
			entry.WritePacket(packet);

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

	public void SendBankLog(WorldSession session, byte tabId)
	{
		// GuildConst.MaxBankTabs send by client for money log
		if (tabId < _GetPurchasedTabsSize() || tabId == GuildConst.MaxBankTabs)
		{
			var bankEventLog = _bankEventLog[tabId].GetGuildLog();

			GuildBankLogQueryResults packet = new();
			packet.Tab = tabId;

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

		if (tab != null)
			tab.SendText(this, session);
	}

	public void SendPermissions(WorldSession session)
	{
		var member = GetMember(session.Player.GUID);

		if (member == null)
			return;

		var rankId = member.GetRankId();

		GuildPermissionsQueryResults queryResult = new();
		queryResult.RankID = (byte)rankId;
		queryResult.WithdrawGoldLimit = (int)_GetMemberRemainingMoney(member);
		queryResult.Flags = (int)_GetRankRights(rankId);
		queryResult.NumTabs = _GetPurchasedTabsSize();

		for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
		{
			GuildPermissionsQueryResults.GuildRankTabPermissions tabPerm;
			tabPerm.Flags = (int)_GetRankBankTabRights(rankId, tabId);
			tabPerm.WithdrawItemLimit = _GetMemberRemainingSlots(member, tabId);
			queryResult.Tab.Add(tabPerm);
		}

		session.SendPacket(queryResult);
	}

	public void SendMoneyInfo(WorldSession session)
	{
		var member = GetMember(session.Player.GUID);

		if (member == null)
			return;

		var amount = _GetMemberRemainingMoney(member);

		GuildBankRemainingWithdrawMoney packet = new();
		packet.RemainingWithdrawMoney = amount;
		session.SendPacket(packet);
	}

	public void SendLoginInfo(WorldSession session)
	{
		var player = session.Player;
		var member = GetMember(player.GUID);

		if (member == null)
			return;

		SendEventMOTD(session);
		SendGuildRankInfo(session);
		SendEventPresenceChanged(session, true, true); // Broadcast

		// Send to self separately, player is not in world yet and is not found by _BroadcastEvent
		SendEventPresenceChanged(session, true);

		if (member.GetGUID() == GetLeaderGUID())
		{
			GuildFlaggedForRename renameFlag = new();
			renameFlag.FlagSet = false;
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

		GuildEventStatusChange statusChange = new();
		statusChange.Guid = memberGuid;
		statusChange.AFK = afk;
		statusChange.DND = dnd;
		BroadcastPacket(statusChange);
	}

	public bool LoadFromDB(SQLFields fields)
	{
		_id = fields.Read<uint>(0);
		_name = fields.Read<string>(1);
		_leaderGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(2));

		if (!_emblemInfo.LoadFromDB(fields))
		{
			Log.Logger.Error(
						"Guild {0} has invalid emblem colors (Background: {1}, Border: {2}, Emblem: {3}), skipped.",
						_id,
						_emblemInfo.GetBackgroundColor(),
						_emblemInfo.GetBorderColor(),
						_emblemInfo.GetColor());

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
			_bankTabs.Add(new BankTab(_id, i, _gameObjectManager, _characterDatabase));

		return true;
	}

	public void LoadRankFromDB(SQLFields field)
	{
		RankInfo rankInfo = new(_characterDatabase, _id);

		rankInfo.LoadFromDB(field);

		_ranks.Add(rankInfo);
	}

	public bool LoadMemberFromDB(SQLFields field)
	{
		var lowguid = field.Read<ulong>(1);
		var playerGuid = ObjectGuid.Create(HighGuid.Player, lowguid);

		Member member = new(_id, playerGuid, (GuildRankId)field.Read<byte>(2), _gameTime, _characterDatabase, _objectAccessor, _cliDB);
		var isNew = _members.TryAdd(playerGuid, member);

		if (!isNew)
		{
			Log.Logger.Error($"Tried to add {playerGuid} to guild '{_name}'. Member already exists.");

			return false;
		}

		if (!member.LoadFromDB(field))
		{
			_DeleteMemberFromDB(null, lowguid);

			return false;
		}

		_characterCache.UpdateCharacterGuildId(playerGuid, GetId());
		_members[member.GetGUID()] = member;

		return true;
	}

	public void LoadBankRightFromDB(SQLFields field)
	{
		// tabId              rights                slots
		GuildBankRightsAndSlots rightsAndSlots = new(field.Read<byte>(1), field.Read<sbyte>(3), field.Read<int>(4));
		// rankId
		_SetRankBankTabRightsAndSlots((GuildRankId)field.Read<byte>(2), rightsAndSlots, false);
	}

	public bool LoadEventLogFromDB(SQLFields field)
	{
		if (_eventLog.CanInsert())
		{
			_eventLog.LoadEvent(new EventLogEntry(_id,                                     // guild id
													field.Read<uint>(1),                     // guid
													field.Read<long>(6),                     // timestamp
													(GuildEventLogTypes)field.Read<byte>(2), // event type
													field.Read<ulong>(3),                    // player guid 1
													field.Read<ulong>(4),                    // player guid 2
													field.Read<byte>(5),                    // rank
                                                    _gameTime, 
													_characterDatabase));

			return true;
		}

		return false;
	}

	public bool LoadBankEventLogFromDB(SQLFields field)
	{
		var dbTabId = field.Read<byte>(1);
		var isMoneyTab = (dbTabId == GuildConst.BankMoneyLogsTab);

		if (dbTabId < _GetPurchasedTabsSize() || isMoneyTab)
		{
			var tabId = isMoneyTab ? (byte)GuildConst.MaxBankTabs : dbTabId;
			var pLog = _bankEventLog[tabId];

			if (pLog.CanInsert())
			{
				var guid = field.Read<uint>(2);
				var eventType = (GuildBankEventLogTypes)field.Read<byte>(3);

				if (BankEventLogEntry.IsMoneyEvent(eventType))
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

				pLog.LoadEvent(new BankEventLogEntry(_id,                 // guild id
													guid,                  // guid
													field.Read<long>(8),   // timestamp
													dbTabId,               // tab id
													eventType,             // event type
													field.Read<ulong>(4),  // player guid
													field.Read<ulong>(5),  // item or money
													field.Read<ushort>(6), // itam stack count
													field.Read<byte>(7),  // dest tab id
                                                    _gameTime, 
													_characterDatabase));
			}
		}

		return true;
	}

	public void LoadGuildNewsLogFromDB(SQLFields field)
	{
		if (!_newsLog.CanInsert())
			return;

		var news = new NewsLogEntry(_id,                                                     // guild id
									field.Read<uint>(1),                                      // guid
									field.Read<long>(6),                                      // timestamp //64 bits?
									(GuildNews)field.Read<byte>(2),                           // type
									ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(3)), // player guid
									field.Read<uint>(4),                                      // Flags
									field.Read<uint>(5),                                      // value
                                    _gameTime, 
									_characterDatabase);

		_newsLog.LoadEvent(news);
	}

	public void LoadBankTabFromDB(SQLFields field)
	{
		var tabId = field.Read<byte>(1);

		if (tabId >= _GetPurchasedTabsSize())
			Log.Logger.Error("Invalid tab (tabId: {0}) in guild bank, skipped.", tabId);
		else
			_bankTabs[tabId].LoadFromDB(field);
	}

	public bool LoadBankItemFromDB(SQLFields field)
	{
		var tabId = field.Read<byte>(52);

		if (tabId >= _GetPurchasedTabsSize())
		{
			Log.Logger.Error(
						"Invalid tab for item (GUID: {0}, id: {1}) in guild bank, skipped.",
						field.Read<uint>(0),
						field.Read<uint>(1));

			return false;
		}

		return _bankTabs[tabId].LoadItemFromDB(field);
	}

	public bool Validate()
	{
		// Validate ranks data
		// GUILD RANKS represent a sequence starting from 0 = GUILD_MASTER (ALL PRIVILEGES) to max 9 (lowest privileges).
		// The lower rank id is considered higher rank - so promotion does rank-- and demotion does rank++
		// Between ranks in sequence cannot be gaps - so 0, 1, 2, 4 is impossible
		// Min ranks count is 2 and max is 10.
		var broken_ranks = false;
		var ranks = _GetRanksSize();

		SQLTransaction trans = new();

		if (ranks < GuildConst.MinRanks || ranks > GuildConst.MaxRanks)
		{
			Log.Logger.Error("Guild {0} has invalid number of ranks, creating new...", _id);
			broken_ranks = true;
		}
		else
		{
			for (byte rankId = 0; rankId < ranks; ++rankId)
			{
				var rankInfo = GetRankInfo((GuildRankId)rankId);

				if (rankInfo.GetId() != (GuildRankId)rankId)
				{
					Log.Logger.Error("Guild {0} has broken rank id {1}, creating default set of ranks...", _id, rankId);
					broken_ranks = true;
				}
				else
				{
					rankInfo.CreateMissingTabsIfNeeded(_GetPurchasedTabsSize(), trans, true);
				}
			}
		}

		if (broken_ranks)
		{
			_ranks.Clear();
			_CreateDefaultGuildRanks(trans, SharedConst.DefaultLocale);
		}

		// Validate members' data
		foreach (var member in _members.Values)
			if (GetRankInfo(member.GetRankId()) == null)
				member.ChangeRank(trans, _GetLowestRankId());

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
		{
			_SetLeader(trans, leader);
		}

		if (trans.commands.Count > 0)
			_characterDatabase.CommitTransaction(trans);

		_UpdateAccountsNumber();

		return true;
	}

	public void BroadcastToGuild(WorldSession session, bool officerOnly, string msg, Language language)
	{
		if (session != null && session.Player != null && _HasRankRight(session.Player, officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
		{
			ChatPkt data = new();
			data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, language, session.Player, null, msg);

			foreach (var member in _members.Values)
			{
				var player = member.FindPlayer();

				if (player != null)
					if (player.Session != null &&
						_HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
						!player.Social.HasIgnore(session.Player.GUID, session.AccountGUID))
						player.SendPacket(data);
			}
		}
	}

	public void BroadcastAddonToGuild(WorldSession session, bool officerOnly, string msg, string prefix, bool isLogged)
	{
		if (session != null && session.Player != null && _HasRankRight(session.Player, officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
		{
			ChatPkt data = new();
			data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, isLogged ? Language.AddonLogged : Language.Addon, session.Player, null, msg, 0, "", Locale.enUS, prefix);

			foreach (var member in _members.Values)
			{
				var player = member.FindPlayer();

				if (player)
					if (player.Session != null &&
						_HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
						!player.Social.HasIgnore(session.Player.GUID, session.AccountGUID) &&
						player.Session.IsAddonRegistered(prefix))
						player.SendPacket(data);
			}
		}
	}

	public void BroadcastPacketToRank(ServerPacket packet, GuildRankId rankId)
	{
		foreach (var member in _members.Values)
			if (member.IsRank(rankId))
			{
				var player = member.FindPlayer();

				if (player != null)
					player.SendPacket(packet);
			}
	}

	public void BroadcastPacket(ServerPacket packet)
	{
		foreach (var member in _members.Values)
		{
			var player = member.FindPlayer();

			if (player != null)
				player.SendPacket(packet);
		}
	}

	public void BroadcastPacketIfTrackingAchievement(ServerPacket packet, uint criteriaId)
	{
		foreach (var member in _members.Values)
			if (member.IsTrackingCriteriaId(criteriaId))
			{
				var player = member.FindPlayer();

				if (player)
					player.SendPacket(packet);
			}
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
					_calendarManager.SendCalendarCommandResult(player.GUID, CalendarError.InvitesExceeded);

				return;
			}

			if (guid == session.Player.GUID)
				continue;

			uint level = _characterCache.GetCharacterLevelByGuid(guid);

			if (level < minLevel || level > maxLevel)
				continue;

			var rank = GetRankInfo(member.GetRankId());

			if (rank.GetOrder() > minRank)
				continue;

			packet.Invites.Add(new CalendarEventInitialInviteInfo(guid, (byte)level));
		}

		session.SendPacket(packet);
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
		{
			return false;
		}

		// Remove all player signs from another petitions
		// This will be prevent attempt to join many guilds and corrupt guild data integrity
		Player.RemovePetitionsAndSigns(guid);

		var lowguid = guid.Counter;

		// If rank was not passed, assign lowest possible rank
		if (!rankId.HasValue)
			rankId = _GetLowestRankId();

		Member member = new(_id, guid, rankId.Value, _gameTime, _characterDatabase, _objectAccessor, _cliDB);
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

		_UpdateAccountsNumber();
		_LogEvent(GuildEventLogTypes.JoinGuild, lowguid);

		GuildEventPlayerJoined joinNotificationPacket = new();
		joinNotificationPacket.Guid = guid;
		joinNotificationPacket.Name = name;
		joinNotificationPacket.VirtualRealmAddress = _worldManager.VirtualRealmAddress;
		BroadcastPacket(joinNotificationPacket);

		// Call scripts if member was succesfully added (and stored to database)
		_scriptManager.ForEach<IGuildOnAddMember>(p => p.OnAddMember(this, player, (byte)rankId));

		return true;
	}

	public void DeleteMember(SQLTransaction trans, ObjectGuid guid, bool isDisbanding = false, bool isKicked = false, bool canDeleteGuild = false)
	{
		var player = _objectAccessor.FindPlayer(guid);

		// Guild master can be deleted when loading guild and guid doesn't exist in characters table
		// or when he is removed from guild by gm command
		if (_leaderGuid == guid && !isDisbanding)
		{
			Member oldLeader = null;
			Member newLeader = null;

			foreach (var (memberGuid, member) in _members)
				if (memberGuid == guid)
					oldLeader = member;
				else if (newLeader == null || newLeader.GetRankId() > member.GetRankId())
					newLeader = member;

			if (newLeader == null)
			{
				Disband();

				return;
			}

			_SetLeader(trans, newLeader);

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
		{
			_characterCache.UpdateCharacterGuildId(guid, 0);
		}

		_DeleteMemberFromDB(trans, guid.Counter);

		if (!isDisbanding)
			_UpdateAccountsNumber();
	}

	public bool ChangeMemberRank(SQLTransaction trans, ObjectGuid guid, GuildRankId newRank)
	{
		if (GetRankInfo(newRank) != null) // Validate rank (allow only existing ranks)
		{
			var member = GetMember(guid);

			if (member != null)
			{
				member.ChangeRank(trans, newRank);

				return true;
			}
		}

		return false;
	}

	public bool IsMember(ObjectGuid guid)
	{
		return _members.ContainsKey(guid);
	}

	public ulong GetMemberAvailableMoneyForRepairItems(ObjectGuid guid)
	{
		var member = GetMember(guid);

		if (member == null)
			return 0;

		return Math.Min(_bankMoney, (ulong)_GetMemberRemainingMoney(member));
	}

	public void SwapItems(Player player, byte tabId, byte slotId, byte destTabId, byte destSlotId, uint splitedAmount)
	{
		if (tabId >= _GetPurchasedTabsSize() ||
			slotId >= GuildConst.MaxBankSlots ||
			destTabId >= _GetPurchasedTabsSize() ||
			destSlotId >= GuildConst.MaxBankSlots)
			return;

		if (tabId == destTabId && slotId == destSlotId)
			return;

		BankMoveItemData from = new(this, player, tabId, slotId, _scriptManager);
		BankMoveItemData to = new(this, player, destTabId, destSlotId, _scriptManager);
		_MoveItems(from, to, splitedAmount);
	}

	public void SwapItemsWithInventory(Player player, bool toChar, byte tabId, byte slotId, byte playerBag, byte playerSlotId, uint splitedAmount)
	{
		if ((slotId >= GuildConst.MaxBankSlots && slotId != ItemConst.NullSlot) || tabId >= _GetPurchasedTabsSize())
			return;

		BankMoveItemData bankData = new(this, player, tabId, slotId, _scriptManager);
		PlayerMoveItemData charData = new(this, player, playerBag, playerSlotId, _scriptManager);

		if (toChar)
			_MoveItems(bankData, charData, splitedAmount);
		else
			_MoveItems(charData, bankData, splitedAmount);
	}

	public void SetBankTabText(byte tabId, string text)
	{
		var pTab = GetBankTab(tabId);

		if (pTab != null)
		{
			pTab.SetText(text);
			pTab.SendText(this);

			GuildEventTabTextChanged eventPacket = new();
			eventPacket.Tab = tabId;
			BroadcastPacket(eventPacket);
		}
	}

	public void SendBankList(WorldSession session, byte tabId, bool fullUpdate)
	{
		var member = GetMember(session.Player.GUID);

		if (member == null) // Shouldn't happen, just in case
			return;

		GuildBankQueryResults packet = new();

		packet.Money = _bankMoney;
		packet.WithdrawalsRemaining = _GetMemberRemainingSlots(member, tabId);
		packet.Tab = tabId;
		packet.FullUpdate = fullUpdate;

		// TabInfo
		if (fullUpdate)
			for (byte i = 0; i < _GetPurchasedTabsSize(); ++i)
			{
				GuildBankTabInfo tabInfo;
				tabInfo.TabIndex = i;
				tabInfo.Name = _bankTabs[i].GetName();
				tabInfo.Icon = _bankTabs[i].GetIcon();
				packet.TabInfo.Add(tabInfo);
			}

		if (fullUpdate && _MemberHasTabRights(session.Player.GUID, tabId, GuildBankRights.ViewTab))
		{
			var tab = GetBankTab(tabId);

			if (tab != null)
				for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
				{
					var tabItem = tab.GetItem(slotId);

					if (tabItem)
					{
						GuildBankItemInfo itemInfo = new();

						itemInfo.Slot = slotId;
						itemInfo.Item.ItemID = tabItem.Entry;
						itemInfo.Count = (int)tabItem.Count;
						itemInfo.Charges = Math.Abs(tabItem.GetSpellCharges());
						itemInfo.EnchantmentID = (int)tabItem.GetEnchantmentId(EnchantmentSlot.Perm);
						itemInfo.OnUseEnchantmentID = (int)tabItem.GetEnchantmentId(EnchantmentSlot.Use);
						itemInfo.Flags = tabItem.ItemData.DynamicFlags;

						byte i = 0;

						foreach (var gemData in tabItem.ItemData.Gems)
						{
							if (gemData.ItemId != 0)
							{
								ItemGemData gem = new();
								gem.Slot = i;
								gem.Item = new ItemInstance(gemData);
								itemInfo.SocketEnchant.Add(gem);
							}

							++i;
						}

						itemInfo.Locked = false;

						packet.ItemInfo.Add(itemInfo);
					}
				}
		}

		session.SendPacket(packet);
	}

	public void ResetTimes(bool weekly)
	{
		foreach (var member in _members.Values)
		{
			member.ResetValues(weekly);
			var player = member.FindPlayer();

			if (player != null)
				// tells the client to request bank withdrawal limit
				player.SendPacket(new GuildMemberDailyReset());
		}
	}

	public void AddGuildNews(GuildNews type, ObjectGuid guid, uint flags, uint value)
	{
		SQLTransaction trans = new();
		var news = _newsLog.AddEvent(trans, new NewsLogEntry(_id, _newsLog.GetNextGUID(), type, guid, flags, value, _gameTime, _characterDatabase));
		_characterDatabase.CommitTransaction(trans);

		GuildNewsPkt newsPacket = new();
		news.WritePacket(newsPacket);
		BroadcastPacket(newsPacket);
	}

	public void UpdateCriteria(CriteriaType type, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player player)
	{
		GetAchievementMgr().UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, player);
	}

	public void HandleNewsSetSticky(WorldSession session, uint newsId, bool sticky)
	{
		var newsLog = _newsLog.GetGuildLog().Find(p => p.GetGUID() == newsId);

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

	public ulong GetId()
	{
		return _id;
	}

	public ObjectGuid GetGUID()
	{
		return ObjectGuid.Create(HighGuid.Guild, _id);
	}

	public ObjectGuid GetLeaderGUID()
	{
		return _leaderGuid;
	}

	public string GetName()
	{
		return _name;
	}

	public string GetMOTD()
	{
		return _motd;
	}

	public string GetInfo()
	{
		return _info;
	}

	public long GetCreatedDate()
	{
		return _createdDate;
	}

	public ulong GetBankMoney()
	{
		return _bankMoney;
	}

	public void BroadcastWorker(IDoWork<Player> _do, Player except = null)
	{
		foreach (var member in _members.Values)
		{
			var player = member.FindPlayer();

			if (player != null)
				if (player != except)
					_do.Invoke(player);
		}
	}

	public int GetMembersCount()
	{
		return _members.Count;
	}

	public GuildAchievementMgr GetAchievementMgr()
	{
		return _achievementSys;
	}

	// Pre-6.x guild leveling
	public byte GetLevel()
	{
		return GuildConst.OldMaxLevel;
	}

	public EmblemInfo GetEmblemInfo()
	{
		return _emblemInfo;
	}

	public Member GetMember(ObjectGuid guid)
	{
		return _members.LookupByKey(guid);
	}

	public Member GetMember(string name)
	{
		foreach (var member in _members.Values)
			if (member.GetName() == name)
				return member;

		return null;
	}

	public static void SendCommandResult(WorldSession session, GuildCommandType type, GuildCommandError errCode, string param = "")
	{
		GuildCommandResult resultPacket = new();
		resultPacket.Command = type;
		resultPacket.Result = errCode;
		resultPacket.Name = param;
		session.SendPacket(resultPacket);
	}

	public static void SendSaveEmblemResult(WorldSession session, GuildEmblemError errCode)
	{
		PlayerSaveGuildEmblem saveResponse = new();
		saveResponse.Error = errCode;
		session.SendPacket(saveResponse);
	}

	public static implicit operator bool(Guild guild)
	{
		return guild != null;
	}

	void OnPlayerStatusChange(Player player, GuildMemberFlags flag, bool state)
	{
		var member = GetMember(player.GUID);

		if (member != null)
		{
			if (state)
				member.AddFlag(flag);
			else
				member.RemoveFlag(flag);
		}
	}

	void SendEventBankMoneyChanged()
	{
		GuildEventBankMoneyChanged eventPacket = new();
		eventPacket.Money = GetBankMoney();
		BroadcastPacket(eventPacket);
	}

	void SendEventMOTD(WorldSession session, bool broadcast = false)
	{
		GuildEventMotd eventPacket = new();
		eventPacket.MotdText = GetMOTD();

		if (broadcast)
		{
			BroadcastPacket(eventPacket);
		}
		else
		{
			session.SendPacket(eventPacket);
			Log.Logger.Debug("SMSG_GUILD_EVENT_MOTD [{0}] ", session.GetPlayerInfo());
		}
	}

	void SendEventNewLeader(Member newLeader, Member oldLeader, bool isSelfPromoted = false)
	{
		GuildEventNewLeader eventPacket = new();
		eventPacket.SelfPromoted = isSelfPromoted;

		if (newLeader != null)
		{
			eventPacket.NewLeaderGUID = newLeader.GetGUID();
			eventPacket.NewLeaderName = newLeader.GetName();
			eventPacket.NewLeaderVirtualRealmAddress = _worldManager.VirtualRealmAddress;
		}

		if (oldLeader != null)
		{
			eventPacket.OldLeaderGUID = oldLeader.GetGUID();
			eventPacket.OldLeaderName = oldLeader.GetName();
			eventPacket.OldLeaderVirtualRealmAddress = _worldManager.VirtualRealmAddress;
		}

		BroadcastPacket(eventPacket);
	}

	void SendEventPlayerLeft(Player leaver, Player remover = null, bool isRemoved = false)
	{
		GuildEventPlayerLeft eventPacket = new();
		eventPacket.Removed = isRemoved;
		eventPacket.LeaverGUID = leaver.GUID;
		eventPacket.LeaverName = leaver.GetName();
		eventPacket.LeaverVirtualRealmAddress = _worldManager.VirtualRealmAddress;

		if (isRemoved && remover)
		{
			eventPacket.RemoverGUID = remover.GUID;
			eventPacket.RemoverName = remover.GetName();
			eventPacket.RemoverVirtualRealmAddress = _worldManager.VirtualRealmAddress;
		}

		BroadcastPacket(eventPacket);
	}

	void SendEventPresenceChanged(WorldSession session, bool loggedOn, bool broadcast = false)
	{
		var player = session.Player;

		GuildEventPresenceChange eventPacket = new();
		eventPacket.Guid = player.GUID;
		eventPacket.Name = player.GetName();
		eventPacket.VirtualRealmAddress = _worldManager.VirtualRealmAddress;
		eventPacket.LoggedOn = loggedOn;
		eventPacket.Mobile = false;

		if (broadcast)
			BroadcastPacket(eventPacket);
		else
			session.SendPacket(eventPacket);
	}

	RankInfo GetRankInfo(GuildRankId rankId)
	{
		return _ranks.Find(rank => rank.GetId() == rankId);
	}

	RankInfo GetRankInfo(GuildRankOrder rankOrder)
	{
		return _ranks.Find(rank => rank.GetOrder() == rankOrder);
	}

	// Private methods
	void _CreateNewBankTab()
	{
		var tabId = _GetPurchasedTabsSize(); // Next free id
		_bankTabs.Add(new BankTab(_id, tabId, _gameObjectManager, _characterDatabase));

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
			rank.CreateMissingTabsIfNeeded(tabId, trans, false);

		_characterDatabase.CommitTransaction(trans);
	}

	void _CreateDefaultGuildRanks(SQLTransaction trans, Locale loc = Locale.enUS)
	{
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
		stmt.AddValue(0, _id);
		trans.Append(stmt);

		stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
		stmt.AddValue(0, _id);
		trans.Append(stmt);

		_CreateRank(trans, _gameObjectManager.GetCypherString(CypherStrings.GuildMaster, loc), GuildRankRights.All);
		_CreateRank(trans, _gameObjectManager.GetCypherString(CypherStrings.GuildOfficer, loc), GuildRankRights.All);
		_CreateRank(trans, _gameObjectManager.GetCypherString(CypherStrings.GuildVeteran, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
		_CreateRank(trans, _gameObjectManager.GetCypherString(CypherStrings.GuildMember, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
		_CreateRank(trans, _gameObjectManager.GetCypherString(CypherStrings.GuildInitiate, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
	}

	bool _CreateRank(SQLTransaction trans, string name, GuildRankRights rights)
	{
		if (_ranks.Count >= GuildConst.MaxRanks)
			return false;

		byte newRankId = 0;

		while (GetRankInfo((GuildRankId)newRankId) != null)
			++newRankId;

		// Ranks represent sequence 0, 1, 2, ... where 0 means guildmaster
		RankInfo info = new(_id, (GuildRankId)newRankId, (GuildRankOrder)_ranks.Count, name, rights, 0, _characterDatabase);
		_ranks.Add(info);

		var isInTransaction = trans != null;

		if (!isInTransaction)
			trans = new SQLTransaction();

		info.CreateMissingTabsIfNeeded(_GetPurchasedTabsSize(), trans);
		info.SaveToDB(trans);
		_characterDatabase.CommitTransaction(trans);

		if (!isInTransaction)
			_characterDatabase.CommitTransaction(trans);

		return true;
	}

	void _UpdateAccountsNumber()
	{
		// We use a set to be sure each element will be unique
		List<uint> accountsIdSet = new();

		foreach (var member in _members.Values)
			accountsIdSet.Add(member.GetAccountId());

		_accountsNumber = (uint)accountsIdSet.Count;
	}

	bool _IsLeader(Player player)
	{
		if (player.GUID == _leaderGuid)
			return true;

		var member = GetMember(player.GUID);

		if (member != null)
			return member.IsRank(GuildRankId.GuildMaster);

		return false;
	}

	void _DeleteBankItems(SQLTransaction trans, bool removeItemsFromDB)
	{
		for (byte tabId = 0; tabId < _GetPurchasedTabsSize(); ++tabId)
		{
			_bankTabs[tabId].Delete(trans, removeItemsFromDB);
			_bankTabs[tabId] = null;
		}

		_bankTabs.Clear();
	}

	bool _ModifyBankMoney(SQLTransaction trans, ulong amount, bool add)
	{
		if (add)
		{
			_bankMoney += amount;
		}
		else
		{
			// Check if there is enough money in bank.
			if (_bankMoney < amount)
				return false;

			_bankMoney -= amount;
		}

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_MONEY);
		stmt.AddValue(0, _bankMoney);
		stmt.AddValue(1, _id);
		trans.Append(stmt);

		return true;
	}

	void _SetLeader(SQLTransaction trans, Member leader)
	{
		var isInTransaction = trans != null;

		if (!isInTransaction)
			trans = new SQLTransaction();

		_leaderGuid = leader.GetGUID();
		leader.ChangeRank(trans, GuildRankId.GuildMaster);

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_LEADER);
		stmt.AddValue(0, _leaderGuid.Counter);
		stmt.AddValue(1, _id);
		trans.Append(stmt);

		if (!isInTransaction)
			_characterDatabase.CommitTransaction(trans);
	}

	void _SetRankBankMoneyPerDay(GuildRankId rankId, uint moneyPerDay)
	{
		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
			rankInfo.SetBankMoneyPerDay(moneyPerDay);
	}

	void _SetRankBankTabRightsAndSlots(GuildRankId rankId, GuildBankRightsAndSlots rightsAndSlots, bool saveToDB = true)
	{
		if (rightsAndSlots.GetTabId() >= _GetPurchasedTabsSize())
			return;

		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
			rankInfo.SetBankTabSlotsAndRights(rightsAndSlots, saveToDB);
	}

	string _GetRankName(GuildRankId rankId)
	{
		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
			return rankInfo.GetName();

		return "<unknown>";
	}

	GuildRankRights _GetRankRights(GuildRankId rankId)
	{
		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
			return rankInfo.GetRights();

		return 0;
	}

	uint _GetRankBankMoneyPerDay(GuildRankId rankId)
	{
		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
			return rankInfo.GetBankMoneyPerDay();

		return 0;
	}

	int _GetRankBankTabSlotsPerDay(GuildRankId rankId, byte tabId)
	{
		if (tabId < _GetPurchasedTabsSize())
		{
			var rankInfo = GetRankInfo(rankId);

			if (rankInfo != null)
				return rankInfo.GetBankTabSlotsPerDay(tabId);
		}

		return 0;
	}

	GuildBankRights _GetRankBankTabRights(GuildRankId rankId, byte tabId)
	{
		var rankInfo = GetRankInfo(rankId);

		if (rankInfo != null)
			return rankInfo.GetBankTabRights(tabId);

		return 0;
	}

	int _GetMemberRemainingSlots(Member member, byte tabId)
	{
		var rankId = member.GetRankId();

		if (rankId == GuildRankId.GuildMaster)
			return GuildConst.WithdrawSlotUnlimited;

		if ((_GetRankBankTabRights(rankId, tabId) & GuildBankRights.ViewTab) != 0)
		{
			var remaining = _GetRankBankTabSlotsPerDay(rankId, tabId) - (int)member.GetBankTabWithdrawValue(tabId);

			if (remaining > 0)
				return remaining;
		}

		return 0;
	}

	long _GetMemberRemainingMoney(Member member)
	{
		var rankId = member.GetRankId();

		if (rankId == GuildRankId.GuildMaster)
			return long.MaxValue;

		if ((_GetRankRights(rankId) & (GuildRankRights.WithdrawRepair | GuildRankRights.WithdrawGold)) != 0)
		{
			var remaining = (long)((_GetRankBankMoneyPerDay(rankId) * MoneyConstants.Gold) - member.GetBankMoneyWithdrawValue());

			if (remaining > 0)
				return remaining;
		}

		return 0;
	}

	void _UpdateMemberWithdrawSlots(SQLTransaction trans, ObjectGuid guid, byte tabId)
	{
		var member = GetMember(guid);

		if (member != null)
			member.UpdateBankTabWithdrawValue(trans, tabId, 1);
	}

	bool _MemberHasTabRights(ObjectGuid guid, byte tabId, GuildBankRights rights)
	{
		var member = GetMember(guid);

		if (member != null)
		{
			// Leader always has full rights
			if (member.IsRank(GuildRankId.GuildMaster) || _leaderGuid == guid)
				return true;

			return (_GetRankBankTabRights(member.GetRankId(), tabId) & rights) == rights;
		}

		return false;
	}

	void _LogEvent(GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2 = 0, byte newRank = 0)
	{
		SQLTransaction trans = new();
		_eventLog.AddEvent(trans, new EventLogEntry(_id, _eventLog.GetNextGUID(), eventType, playerGuid1, playerGuid2, newRank, _gameTime, _characterDatabase));
		_characterDatabase.CommitTransaction(trans);

		_scriptManager.ForEach<IGuildOnEvent>(p => p.OnEvent(this, (byte)eventType, playerGuid1, playerGuid2, newRank));
	}

	void _LogBankEvent(SQLTransaction trans, GuildBankEventLogTypes eventType, byte tabId, ulong lowguid, uint itemOrMoney, ushort itemStackCount = 0, byte destTabId = 0)
	{
		if (tabId > GuildConst.MaxBankTabs)
			return;

		// not logging moves within the same tab
		if (eventType == GuildBankEventLogTypes.MoveItem && tabId == destTabId)
			return;

		var dbTabId = tabId;

		if (BankEventLogEntry.IsMoneyEvent(eventType))
		{
			tabId = GuildConst.MaxBankTabs;
			dbTabId = GuildConst.BankMoneyLogsTab;
		}

		var pLog = _bankEventLog[tabId];
		pLog.AddEvent(trans, new BankEventLogEntry(_id, pLog.GetNextGUID(), eventType, dbTabId, lowguid, itemOrMoney, itemStackCount, destTabId, _gameTime, _characterDatabase));

		_scriptManager.ForEach<IGuildOnBankEvent>(p => p.OnBankEvent(this, (byte)eventType, tabId, lowguid, itemOrMoney, itemStackCount, destTabId));
	}

	Item _GetItem(byte tabId, byte slotId)
	{
		var tab = GetBankTab(tabId);

		if (tab != null)
			return tab.GetItem(slotId);

		return null;
	}

	void _RemoveItem(SQLTransaction trans, byte tabId, byte slotId)
	{
		var pTab = GetBankTab(tabId);

		if (pTab != null)
			pTab.SetItem(trans, slotId, null);
	}

	void _MoveItems(MoveItemData pSrc, MoveItemData pDest, uint splitedAmount)
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
			_DoItemsMove(pSrc, pDest, true, splitedAmount);
		}
		else // 6. No split
		{
			// 6.1. Try to merge items in destination (pDest.GetItem() == NULL)
			var mergeAttemptResult = _DoItemsMove(pSrc, pDest, false);

			if (mergeAttemptResult != InventoryResult.Ok) // Item could not be merged
			{
				// 6.2. Try to swap items
				// 6.2.1. Initialize destination item
				if (!pDest.InitItem())
				{
					pSrc.SendEquipError(mergeAttemptResult, pSrc.GetItem(false));

					return;
				}

				// 6.2.2. Check rights to store item in source (opposite direction)
				if (!pSrc.HasStoreRights(pDest))
					return; // Player has no rights to store item in source (opposite direction)

				if (!pDest.HasWithdrawRights(pSrc))
					return; // Player has no rights to withdraw item from destination (opposite direction)

				// 6.2.3. Swap items (pDest.GetItem() != NULL)
				_DoItemsMove(pSrc, pDest, true);
			}
		}

		// 7. Send changes
		_SendBankContentUpdate(pSrc, pDest);
	}

	InventoryResult _DoItemsMove(MoveItemData pSrc, MoveItemData pDest, bool sendError, uint splitedAmount = 0)
	{
		var pDestItem = pDest.GetItem();
		var swap = (pDestItem != null);

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

	void _SendBankContentUpdate(MoveItemData pSrc, MoveItemData pDest)
	{
		byte tabId = 0;
		List<byte> slots = new();

		if (pSrc.IsBank()) // B .
		{
			tabId = pSrc.GetContainer();
			slots.Insert(0, pSrc.GetSlotId());

			if (pDest.IsBank()) // B . B
			{
				// Same tab - add destination slots to collection
				if (pDest.GetContainer() == pSrc.GetContainer())
				{
					pDest.CopySlots(slots);
				}
				else // Different tabs - send second message
				{
					List<byte> destSlots = new();
					pDest.CopySlots(destSlots);
					_SendBankContentUpdate(pDest.GetContainer(), destSlots);
				}
			}
		}
		else if (pDest.IsBank()) // C . B
		{
			tabId = pDest.GetContainer();
			pDest.CopySlots(slots);
		}

		_SendBankContentUpdate(tabId, slots);
	}
	
	void _SendBankContentUpdate(byte tabId, List<byte> slots)
	{
		var tab = GetBankTab(tabId);

		if (tab != null)
		{
			GuildBankQueryResults packet = new();
			packet.FullUpdate = true; // @todo
			packet.Tab = tabId;
			packet.Money = _bankMoney;

			foreach (var slot in slots)
			{
				var tabItem = tab.GetItem(slot);

				GuildBankItemInfo itemInfo = new();

				itemInfo.Slot = slot;
				itemInfo.Item.ItemID = tabItem ? tabItem.Entry : 0;
				itemInfo.Count = (int)(tabItem ? tabItem.Count : 0);
				itemInfo.EnchantmentID = (int)(tabItem ? tabItem.GetEnchantmentId(EnchantmentSlot.Perm) : 0);
				itemInfo.Charges = tabItem ? Math.Abs(tabItem.GetSpellCharges()) : 0;
				itemInfo.OnUseEnchantmentID = (int)(tabItem ? tabItem.GetEnchantmentId(EnchantmentSlot.Use) : 0);
				itemInfo.Flags = 0;
				itemInfo.Locked = false;

				if (tabItem != null)
				{
					byte i = 0;

					foreach (var gemData in tabItem.ItemData.Gems)
					{
						if (gemData.ItemId != 0)
						{
							ItemGemData gem = new();
							gem.Slot = i;
							gem.Item = new ItemInstance(gemData);
							itemInfo.SocketEnchant.Add(gem);
						}

						++i;
					}
				}

				packet.ItemInfo.Add(itemInfo);
			}

			foreach (var (guid, member) in _members)
			{
				if (!_MemberHasTabRights(guid, tabId, GuildBankRights.ViewTab))
					continue;

				var player = member.FindPlayer();

				if (player == null)
					continue;

				packet.WithdrawalsRemaining = _GetMemberRemainingSlots(member, tabId);
				player.SendPacket(packet);
			}
		}
	}

	void SendGuildRanksUpdate(ObjectGuid setterGuid, ObjectGuid targetGuid, GuildRankId rank)
	{
		var member = GetMember(targetGuid);

		GuildSendRankChange rankChange = new();
		rankChange.Officer = setterGuid;
		rankChange.Other = targetGuid;
		rankChange.RankID = (byte)rank;
		rankChange.Promote = (rank < member.GetRankId());
		BroadcastPacket(rankChange);

		member.ChangeRank(null, rank);

		Log.Logger.Debug("SMSG_GUILD_RANKS_UPDATE [Broadcast] Target: {0}, Issuer: {1}, RankId: {2}", targetGuid.ToString(), setterGuid.ToString(), rank);
	}

	bool HasAchieved(uint achievementId)
	{
		return GetAchievementMgr().HasAchieved(achievementId);
	}

	byte _GetRanksSize()
	{
		return (byte)_ranks.Count;
	}

	RankInfo GetRankInfo(uint rankId)
	{
		return rankId < _GetRanksSize() ? _ranks[(int)rankId] : null;
	}

	bool _HasRankRight(Player player, GuildRankRights right)
	{
		if (player != null)
		{
			var member = GetMember(player.GUID);

			if (member != null)
				return (_GetRankRights(member.GetRankId()) & right) != GuildRankRights.None;

			return false;
		}

		return false;
	}

	GuildRankId _GetLowestRankId()
	{
		return _ranks.Last().GetId();
	}

	byte _GetPurchasedTabsSize()
	{
		return (byte)_bankTabs.Count;
	}

	BankTab GetBankTab(byte tabId)
	{
		return tabId < _bankTabs.Count ? _bankTabs[tabId] : null;
	}

	void _DeleteMemberFromDB(SQLTransaction trans, ulong lowguid)
	{
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBER);
		stmt.AddValue(0, lowguid);
		_characterDatabase.ExecuteOrAppend(trans, stmt);
	}

	ulong GetGuildBankTabPrice(byte tabId)
	{
		// these prices are in gold units, not copper
		switch (tabId)
		{
			case 0:  return 100;
			case 1:  return 250;
			case 2:  return 500;
			case 3:  return 1000;
			case 4:  return 2500;
			case 5:  return 5000;
			default: return 0;
		}
	}

	#region Fields

	ulong _id;
	string _name;
	ObjectGuid _leaderGuid;
	string _motd;
	string _info;
	long _createdDate;

	EmblemInfo _emblemInfo;
	uint _accountsNumber;
	ulong _bankMoney;
	readonly List<RankInfo> _ranks = new();
	readonly Dictionary<ObjectGuid, Member> _members = new();
	readonly List<BankTab> _bankTabs = new();

	// These are actually ordered lists. The first element is the oldest entry.
	readonly LogHolder<EventLogEntry> _eventLog;
	readonly LogHolder<BankEventLogEntry>[] _bankEventLog = new LogHolder<BankEventLogEntry>[GuildConst.MaxBankTabs + 1];
	readonly LogHolder<NewsLogEntry> _newsLog;
	readonly GuildAchievementMgr _achievementSys;
    private readonly GameTime _gameTime;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly ScriptManager _scriptManager;
    private readonly GuildManager _guildManager;
    private readonly WorldManager _worldManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldConfig _worldConfig;
    private readonly CharacterCache _characterCache;
    private readonly CalendarManager _calendarManager;
    private readonly ClassFactory _classFactory;
    private readonly CriteriaManager _criteriaManager;

    #endregion

    #region Classes

    public class Member
	{
		public Member(ulong guildId, ObjectGuid guid, GuildRankId rankId, GameTime gameTime, CharacterDatabase characterDatabase, ObjectAccessor objectAccessor,
			CliDB cliDB)
        {
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
            _objectAccessor = objectAccessor;
            _cliDB = cliDB;
            _guildId = guildId;
			_guid = guid;
			_zoneId = 0;
			_level = 0;
			_class = 0;
			_flags = GuildMemberFlags.None;
			_logoutTime = (ulong)_gameTime.CurrentGameTime;
			_accountId = 0;
			_rankId = rankId;
            _achievementPoints = 0;
			_totalActivity = 0;
			_weekActivity = 0;
			_totalReputation = 0;
			_weekReputation = 0;
        }

		public void SetStats(Player player)
		{
			_name = player.GetName();
			_level = (byte)player.Level;
			_race = player.Race;
			_class = player.Class;
			_gender = player.NativeGender;
			_zoneId = player.Zone;
			_accountId = player.Session.AccountId;
			_achievementPoints = player.AchievementPoints;
		}

		public void SetStats(string name, byte level, Race race, PlayerClass _class, Gender gender, uint zoneId, uint accountId, uint reputation)
		{
			_name = name;
			_level = level;
			_race = race;
			_class = _class;
			_gender = gender;
			_zoneId = zoneId;
			_accountId = accountId;
			_totalReputation = reputation;
		}

		public void SetPublicNote(string publicNote)
		{
			if (_publicNote == publicNote)
				return;

			_publicNote = publicNote;
			
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_PNOTE);
			stmt.AddValue(0, publicNote);
			stmt.AddValue(1, _guid.Counter);
			_characterDatabase.Execute(stmt);
		}

		public void SetOfficerNote(string officerNote)
		{
			if (_officerNote == officerNote)
				return;

			_officerNote = officerNote;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_OFFNOTE);
			stmt.AddValue(0, officerNote);
			stmt.AddValue(1, _guid.Counter);
			_characterDatabase.Execute(stmt);
		}
		
		public void ChangeRank(SQLTransaction trans, GuildRankId newRank)
		{
			_rankId = newRank;

			// Update rank information in player's field, if he is online.
			var player = FindConnectedPlayer();

			if (player != null)
				player.SetGuildRank((byte)newRank);

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_RANK);
			stmt.AddValue(0, (byte)newRank);
			stmt.AddValue(1, _guid.Counter);
			_characterDatabase.ExecuteOrAppend(trans, stmt);
		}

		public void SaveToDB(SQLTransaction trans)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER);
			stmt.AddValue(0, _guildId);
			stmt.AddValue(1, _guid.Counter);
			stmt.AddValue(2, (byte)_rankId);
			stmt.AddValue(3, _publicNote);
			stmt.AddValue(4, _officerNote);
			_characterDatabase.ExecuteOrAppend(trans, stmt);
		}

		public bool LoadFromDB(SQLFields field)
		{
			_publicNote = field.Read<string>(3);
			_officerNote = field.Read<string>(4);

			for (byte i = 0; i < GuildConst.MaxBankTabs; ++i)
				_bankWithdraw[i] = field.Read<uint>(5 + i);

			_bankWithdrawMoney = field.Read<ulong>(13);

			SetStats(field.Read<string>(14),
					field.Read<byte>(15),              // characters.level
					(Race)field.Read<byte>(16),        // characters.race
					(PlayerClass)field.Read<byte>(17), // characters.class
					(Gender)field.Read<byte>(18),      // characters.gender
					field.Read<ushort>(19),            // characters.zone
					field.Read<uint>(20),              // characters.account
					0);

			_logoutTime = field.Read<ulong>(21); // characters.logout_time
			_totalActivity = 0;
			_weekActivity = 0;
			_weekReputation = 0;

			if (!CheckStats())
				return false;

			if (_zoneId == 0)
			{
				Log.Logger.Error("Player ({0}) has broken zone-data", _guid.ToString());
				_zoneId = Player.GetZoneIdFromDB(_guid);
			}

			ResetFlags();

			return true;
		}

		public bool CheckStats()
		{
			if (_level < 1)
			{
				Log.Logger.Error($"{_guid} has a broken data in field `characters`.`level`, deleting him from guild!");

				return false;
			}

			if (!_cliDB.ChrRacesStorage.ContainsKey((uint)_race))
			{
				Log.Logger.Error($"{_guid} has a broken data in field `characters`.`race`, deleting him from guild!");

				return false;
			}

			if (!_cliDB.ChrClassesStorage.ContainsKey((uint)_class))
			{
				Log.Logger.Error($"{_guid} has a broken data in field `characters`.`class`, deleting him from guild!");

				return false;
			}

			return true;
		}

		public float GetInactiveDays()
		{
			if (IsOnline())
				return 0.0f;

			return (float)((_gameTime.CurrentGameTime - (long)GetLogoutTime()) / (float)Time.Day);
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

		// Decreases amount of money left for today.
		public void UpdateBankMoneyWithdrawValue(SQLTransaction trans, ulong amount)
		{
			_bankWithdrawMoney += amount;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_MONEY);
			stmt.AddValue(0, _guid.Counter);
			stmt.AddValue(1, _bankWithdrawMoney);
			_characterDatabase.ExecuteOrAppend(trans, stmt);
		}

		public void ResetValues(bool weekly = false)
		{
			for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
				_bankWithdraw[tabId] = 0;

			_bankWithdrawMoney = 0;

			if (weekly)
			{
				_weekActivity = 0;
				_weekReputation = 0;
			}
		}

		public void SetZoneId(uint id)
		{
			_zoneId = id;
		}

		public void SetAchievementPoints(uint val)
		{
			_achievementPoints = val;
		}

		public void SetLevel(uint var)
		{
			_level = (byte)var;
		}

		public void AddFlag(GuildMemberFlags var)
		{
			_flags |= var;
		}

		public void RemoveFlag(GuildMemberFlags var)
		{
			_flags &= ~var;
		}

		public void ResetFlags()
		{
			_flags = GuildMemberFlags.None;
		}

		public ObjectGuid GetGUID()
		{
			return _guid;
		}

		public string GetName()
		{
			return _name;
		}

		public uint GetAccountId()
		{
			return _accountId;
		}

		public GuildRankId GetRankId()
		{
			return _rankId;
		}

		public ulong GetLogoutTime()
		{
			return _logoutTime;
		}

		public string GetPublicNote()
		{
			return _publicNote;
		}

		public string GetOfficerNote()
		{
			return _officerNote;
		}

		public Race GetRace()
		{
			return _race;
		}

		public PlayerClass GetClass()
		{
			return _class;
		}

		public Gender GetGender()
		{
			return _gender;
		}

		public byte GetLevel()
		{
			return _level;
		}

		public GuildMemberFlags GetFlags()
		{
			return _flags;
		}

		public uint GetZoneId()
		{
			return _zoneId;
		}

		public uint GetAchievementPoints()
		{
			return _achievementPoints;
		}

		public ulong GetTotalActivity()
		{
			return _totalActivity;
		}

		public ulong GetWeekActivity()
		{
			return _weekActivity;
		}

		public uint GetTotalReputation()
		{
			return _totalReputation;
		}

		public uint GetWeekReputation()
		{
			return _weekReputation;
		}

		public List<uint> GetTrackedCriteriaIds()
		{
			return _trackedCriteriaIds;
		}

		public void SetTrackedCriteriaIds(List<uint> criteriaIds)
		{
			_trackedCriteriaIds = criteriaIds;
		}

		public bool IsTrackingCriteriaId(uint criteriaId)
		{
			return _trackedCriteriaIds.Contains(criteriaId);
		}

		public bool IsOnline()
		{
			return _flags.HasFlag(GuildMemberFlags.Online);
		}

		public void UpdateLogoutTime()
		{
			_logoutTime = (ulong)_gameTime.CurrentGameTime;
		}

		public bool IsRank(GuildRankId rankId)
		{
			return _rankId == rankId;
		}

		public bool IsSamePlayer(ObjectGuid guid)
		{
			return _guid == guid;
		}

		public uint GetBankTabWithdrawValue(byte tabId)
		{
			return _bankWithdraw[tabId];
		}

		public ulong GetBankMoneyWithdrawValue()
		{
			return _bankWithdrawMoney;
		}

		public Player FindPlayer()
		{
			return _objectAccessor.FindPlayer(_guid);
		}

		Player FindConnectedPlayer()
		{
			return _objectAccessor.FindConnectedPlayer(_guid);
		}

		#region Fields

		readonly ulong _guildId;
		ObjectGuid _guid;
		string _name;
		uint _zoneId;
		byte _level;
		Race _race;
		PlayerClass _class;
		Gender _gender;
		GuildMemberFlags _flags;
		ulong _logoutTime;
		uint _accountId;
		GuildRankId _rankId;
        private readonly GameTime _gameTime;
        private readonly CharacterDatabase _characterDatabase;
        private readonly ObjectAccessor _objectAccessor;
        private readonly CliDB _cliDB;
        string _publicNote = "";
		string _officerNote = "";

		List<uint> _trackedCriteriaIds = new();
		readonly uint[] _bankWithdraw = new uint[GuildConst.MaxBankTabs];
		ulong _bankWithdrawMoney;
		uint _achievementPoints;
		ulong _totalActivity;
		ulong _weekActivity;
		uint _totalReputation;
		uint _weekReputation;

		#endregion
	}

	public class LogEntry
	{
		public ulong _guildId;
		public uint _guid;
		public long _timestamp;

		public LogEntry(ulong guildId, uint guid, GameTime gameTime)
		{
			_guildId = guildId;
			_guid = guid;
			_timestamp = gameTime.CurrentGameTime;
		}

		public LogEntry(ulong guildId, uint guid, long timestamp)
		{
			_guildId = guildId;
			_guid = guid;
			_timestamp = timestamp;
		}

		public uint GetGUID()
		{
			return _guid;
		}

		public long GetTimestamp()
		{
			return _timestamp;
		}

		public virtual void SaveToDB(SQLTransaction trans) { }
	}

	public class EventLogEntry : LogEntry
	{
		readonly GuildEventLogTypes _eventType;
		readonly ulong _playerGuid1;
		readonly ulong _playerGuid2;
		readonly byte _newRank;
        private readonly GameTime _gameTime;
        private readonly CharacterDatabase _characterDatabase;

        public EventLogEntry(ulong guildId, uint guid, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank,
            GameTime gameTime, CharacterDatabase characterDatabase)
			: base(guildId, guid, gameTime)
		{
			_eventType = eventType;
			_playerGuid1 = playerGuid1;
			_playerGuid2 = playerGuid2;
			_newRank = newRank;
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
        }

		public EventLogEntry(ulong guildId, uint guid, long timestamp, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank,
            GameTime gameTime, CharacterDatabase characterDatabase)
            : base(guildId, guid, timestamp)
		{
			_eventType = eventType;
			_playerGuid1 = playerGuid1;
			_playerGuid2 = playerGuid2;
			_newRank = newRank;
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
        }

		public override void SaveToDB(SQLTransaction trans)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG);
			stmt.AddValue(0, _guildId);
			stmt.AddValue(1, _guid);
			trans.Append(stmt);

			byte index = 0;
			stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_EVENTLOG);
			stmt.AddValue(index, _guildId);
			stmt.AddValue(++index, _guid);
			stmt.AddValue(++index, (byte)_eventType);
			stmt.AddValue(++index, _playerGuid1);
			stmt.AddValue(++index, _playerGuid2);
			stmt.AddValue(++index, _newRank);
			stmt.AddValue(++index, _timestamp);
			trans.Append(stmt);
		}

		public void WritePacket(GuildEventLogQueryResults packet)
		{
			var playerGUID = ObjectGuid.Create(HighGuid.Player, _playerGuid1);
			var otherGUID = ObjectGuid.Create(HighGuid.Player, _playerGuid2);

			GuildEventEntry eventEntry = new();
			eventEntry.PlayerGUID = playerGUID;
			eventEntry.OtherGUID = otherGUID;
			eventEntry.TransactionType = (byte)_eventType;
			eventEntry.TransactionDate = (uint)(_gameTime.CurrentGameTime - _timestamp);
			eventEntry.RankID = _newRank;
			packet.Entry.Add(eventEntry);
		}
	}

	public class BankEventLogEntry : LogEntry
	{
		readonly GuildBankEventLogTypes _eventType;
		readonly byte _bankTabId;
		readonly ulong _playerGuid;
		readonly ulong _itemOrMoney;
		readonly ushort _itemStackCount;
		readonly byte _destTabId;
        private readonly GameTime _gameTime;
        private readonly CharacterDatabase _characterDatabase;

        public BankEventLogEntry(ulong guildId, uint guid, GuildBankEventLogTypes eventType, byte tabId, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId,
			GameTime gameTime, CharacterDatabase characterDatabase)
			: base(guildId, guid, gameTime)
		{
			_eventType = eventType;
			_bankTabId = tabId;
			_playerGuid = playerGuid;
			_itemOrMoney = itemOrMoney;
			_itemStackCount = itemStackCount;
			_destTabId = destTabId;
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
        }

		public BankEventLogEntry(ulong guildId, uint guid, long timestamp, byte tabId, GuildBankEventLogTypes eventType, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId,
            GameTime gameTime, CharacterDatabase characterDatabase)
			: base(guildId, guid, timestamp)
		{
			_eventType = eventType;
			_bankTabId = tabId;
			_playerGuid = playerGuid;
			_itemOrMoney = itemOrMoney;
			_itemStackCount = itemStackCount;
			_destTabId = destTabId;
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
        }

		public static bool IsMoneyEvent(GuildBankEventLogTypes eventType)
		{
			return
				eventType == GuildBankEventLogTypes.DepositMoney ||
				eventType == GuildBankEventLogTypes.WithdrawMoney ||
				eventType == GuildBankEventLogTypes.RepairMoney ||
				eventType == GuildBankEventLogTypes.CashFlowDeposit;
		}

		public override void SaveToDB(SQLTransaction trans)
		{
			byte index = 0;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG);
			stmt.AddValue(index, _guildId);
			stmt.AddValue(++index, _guid);
			stmt.AddValue(++index, _bankTabId);
			trans.Append(stmt);

			index = 0;
			stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_EVENTLOG);
			stmt.AddValue(index, _guildId);
			stmt.AddValue(++index, _guid);
			stmt.AddValue(++index, _bankTabId);
			stmt.AddValue(++index, (byte)_eventType);
			stmt.AddValue(++index, _playerGuid);
			stmt.AddValue(++index, _itemOrMoney);
			stmt.AddValue(++index, _itemStackCount);
			stmt.AddValue(++index, _destTabId);
			stmt.AddValue(++index, _timestamp);
			trans.Append(stmt);
		}

		public void WritePacket(GuildBankLogQueryResults packet)
		{
			var logGuid = ObjectGuid.Create(HighGuid.Player, _playerGuid);

			var hasItem = _eventType == GuildBankEventLogTypes.DepositItem ||
						_eventType == GuildBankEventLogTypes.WithdrawItem ||
						_eventType == GuildBankEventLogTypes.MoveItem ||
						_eventType == GuildBankEventLogTypes.MoveItem2;

			var itemMoved = (_eventType == GuildBankEventLogTypes.MoveItem || _eventType == GuildBankEventLogTypes.MoveItem2);

			var hasStack = (hasItem && _itemStackCount > 1) || itemMoved;

			GuildBankLogEntry bankLogEntry = new();
			bankLogEntry.PlayerGUID = logGuid;
			bankLogEntry.TimeOffset = (uint)(_gameTime.CurrentGameTime - _timestamp);
			bankLogEntry.EntryType = (sbyte)_eventType;

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

		bool IsMoneyEvent()
		{
			return IsMoneyEvent(_eventType);
		}
	}

	public class NewsLogEntry : LogEntry
	{
		readonly GuildNews _type;
		readonly uint _value;
        private readonly GameTime _gameTime;
        private readonly CharacterDatabase _characterDatabase;
        readonly ObjectGuid _playerGuid;
		int _flags;

		public NewsLogEntry(ulong guildId, uint guid, GuildNews type, ObjectGuid playerGuid, uint flags, uint value,
            GameTime gameTime, CharacterDatabase characterDatabase)
			: base(guildId, guid, gameTime)
		{
			_type = type;
			_playerGuid = playerGuid;
			_flags = (int)flags;
			_value = value;
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
        }

		public NewsLogEntry(ulong guildId, uint guid, long timestamp, GuildNews type, ObjectGuid playerGuid, uint flags, uint value,
            GameTime gameTime, CharacterDatabase characterDatabase)
			: base(guildId, guid, timestamp)
		{
			_type = type;
			_playerGuid = playerGuid;
			_flags = (int)flags;
			_value = value;
            _gameTime = gameTime;
            _characterDatabase = characterDatabase;
        }

		public GuildNews GetNewsType()
		{
			return _type;
		}

		public ObjectGuid GetPlayerGuid()
		{
			return _playerGuid;
		}

		public uint GetValue()
		{
			return _value;
		}

		public int GetFlags()
		{
			return _flags;
		}

		public void SetSticky(bool sticky)
		{
			if (sticky)
				_flags |= 1;
			else
				_flags &= ~1;
		}

		public override void SaveToDB(SQLTransaction trans)
		{
			byte index = 0;
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_NEWS);
			stmt.AddValue(index, _guildId);
			stmt.AddValue(++index, GetGUID());
			stmt.AddValue(++index, (byte)GetNewsType());
			stmt.AddValue(++index, GetPlayerGuid().Counter);
			stmt.AddValue(++index, GetFlags());
			stmt.AddValue(++index, GetValue());
			stmt.AddValue(++index, GetTimestamp());
			_characterDatabase.ExecuteOrAppend(trans, stmt);
		}

		public void WritePacket(GuildNewsPkt newsPacket)
		{
			GuildNewsEvent newsEvent = new();
			newsEvent.Id = (int)GetGUID();
			newsEvent.MemberGuid = GetPlayerGuid();
			newsEvent.CompletedDate = (uint)GetTimestamp();
			newsEvent.Flags = GetFlags();
			newsEvent.Type = (int)GetNewsType();

			//for (public byte i = 0; i < 2; i++)
			//    newsEvent.Data[i] =

			//newsEvent.MemberList.push_back(MemberGuid);

			if (GetNewsType() == GuildNews.ItemLooted || GetNewsType() == GuildNews.ItemCrafted || GetNewsType() == GuildNews.ItemPurchased)
			{
				ItemInstance itemInstance = new();
				itemInstance.ItemID = GetValue();
				newsEvent.Item = itemInstance;
			}

			newsPacket.NewsEvents.Add(newsEvent);
		}
	}

	public class LogHolder<T> where T : LogEntry
	{
		readonly List<T> _log = new();
		readonly uint _maxRecords;
		uint _nextGUID;

		public LogHolder(WorldConfig worldConfig)
		{
			_maxRecords = worldConfig.GetUIntValue(typeof(T) == typeof(BankEventLogEntry) ? WorldCfg.GuildBankEventLogCount : WorldCfg.GuildEventLogCount);
			_nextGUID = GuildConst.EventLogGuidUndefined;
		}

		// Checks if new log entry can be added to holder
		public bool CanInsert()
		{
			return _log.Count < _maxRecords;
		}

		public byte GetSize()
		{
			return (byte)_log.Count;
		}

		public void LoadEvent(T entry)
		{
			if (_nextGUID == GuildConst.EventLogGuidUndefined)
				_nextGUID = entry.GetGUID();

			_log.Insert(0, entry);
		}

		public T AddEvent(SQLTransaction trans, T entry)
		{
			// Check max records limit
			if (!CanInsert())
				_log.RemoveAt(0);

			// Add event to list
			_log.Add(entry);

			// Save to DB
			entry.SaveToDB(trans);

			return entry;
		}

		public uint GetNextGUID()
		{
			if (_nextGUID == GuildConst.EventLogGuidUndefined)
				_nextGUID = 0;
			else
				_nextGUID = (_nextGUID + 1) % _maxRecords;

			return _nextGUID;
		}

		public List<T> GetGuildLog()
		{
			return _log;
		}
	}

	public class RankInfo
	{
        private readonly CharacterDatabase _characterDatabase;
        readonly ulong _guildId;
		readonly GuildBankRightsAndSlots[] _bankTabRightsAndSlots = new GuildBankRightsAndSlots[GuildConst.MaxBankTabs];
		GuildRankId _rankId;
		GuildRankOrder _rankOrder;
		string _name;
		GuildRankRights _rights;
		uint _bankMoneyPerDay;

		public RankInfo(CharacterDatabase characterDatabase, ulong guildId = 0)
		{
            _characterDatabase = characterDatabase;
            _guildId = guildId;
			_rankId = (GuildRankId)0xFF;
			_rankOrder = 0;
			_rights = GuildRankRights.None;
			_bankMoneyPerDay = 0;

			for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
				_bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
		}

		public RankInfo(ulong guildId, GuildRankId rankId, GuildRankOrder rankOrder, string name, GuildRankRights rights, uint money, CharacterDatabase characterDatabase)
		{
			_guildId = guildId;
			_rankId = rankId;
			_rankOrder = rankOrder;
			_name = name;
			_rights = rights;
			_bankMoneyPerDay = money;
            _characterDatabase = characterDatabase;

            for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
				_bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
		}

		public void LoadFromDB(SQLFields field)
		{
			_rankId = (GuildRankId)field.Read<byte>(1);
			_rankOrder = (GuildRankOrder)field.Read<byte>(2);
			_name = field.Read<string>(3);
			_rights = (GuildRankRights)field.Read<uint>(4);
			_bankMoneyPerDay = field.Read<uint>(5);

			if (_rankId == GuildRankId.GuildMaster) // Prevent loss of leader rights
				_rights |= GuildRankRights.All;
		}

		public void SaveToDB(SQLTransaction trans)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_RANK);
			stmt.AddValue(0, _guildId);
			stmt.AddValue(1, (byte)_rankId);
			stmt.AddValue(2, (byte)_rankOrder);
			stmt.AddValue(3, _name);
			stmt.AddValue(4, (uint)_rights);
			stmt.AddValue(5, _bankMoneyPerDay);
			_characterDatabase.ExecuteOrAppend(trans, stmt);
		}

		public void CreateMissingTabsIfNeeded(byte tabs, SQLTransaction trans, bool logOnCreate = false)
		{
			for (byte i = 0; i < tabs; ++i)
			{
				var rightsAndSlots = _bankTabRightsAndSlots[i];

				if (rightsAndSlots.GetTabId() == i)
					continue;

				rightsAndSlots.SetTabId(i);

				if (_rankId == GuildRankId.GuildMaster)
					rightsAndSlots.SetGuildMasterValues();

				if (logOnCreate)
					Log.Logger.Error($"Guild {_guildId} has broken Tab {i} for rank {_rankId}. Created default tab.");

				var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
				stmt.AddValue(0, _guildId);
				stmt.AddValue(1, i);
				stmt.AddValue(2, (byte)_rankId);
				stmt.AddValue(3, (sbyte)rightsAndSlots.GetRights());
				stmt.AddValue(4, rightsAndSlots.GetSlots());
				trans.Append(stmt);
			}
		}

		public void SetName(string name)
		{
			if (_name == name)
				return;

			_name = name;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_NAME);
			stmt.AddValue(0, _name);
			stmt.AddValue(1, (byte)_rankId);
			stmt.AddValue(2, _guildId);
			_characterDatabase.Execute(stmt);
		}

		public void SetRights(GuildRankRights rights)
		{
			if (_rankId == GuildRankId.GuildMaster) // Prevent loss of leader rights
				rights = GuildRankRights.All;

			if (_rights == rights)
				return;

			_rights = rights;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_RIGHTS);
			stmt.AddValue(0, (uint)_rights);
			stmt.AddValue(1, (byte)_rankId);
			stmt.AddValue(2, _guildId);
			_characterDatabase.Execute(stmt);
		}

		public void SetBankMoneyPerDay(uint money)
		{
			if (_bankMoneyPerDay == money)
				return;

			_bankMoneyPerDay = money;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_BANK_MONEY);
			stmt.AddValue(0, money);
			stmt.AddValue(1, (byte)_rankId);
			stmt.AddValue(2, _guildId);
			_characterDatabase.Execute(stmt);
		}

		public void SetBankTabSlotsAndRights(GuildBankRightsAndSlots rightsAndSlots, bool saveToDB)
		{
			if (_rankId == GuildRankId.GuildMaster) // Prevent loss of leader rights
				rightsAndSlots.SetGuildMasterValues();

			_bankTabRightsAndSlots[rightsAndSlots.GetTabId()] = rightsAndSlots;

			if (saveToDB)
			{
				var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
				stmt.AddValue(0, _guildId);
				stmt.AddValue(1, rightsAndSlots.GetTabId());
				stmt.AddValue(2, (byte)_rankId);
				stmt.AddValue(3, (sbyte)rightsAndSlots.GetRights());
				stmt.AddValue(4, rightsAndSlots.GetSlots());
				_characterDatabase.Execute(stmt);
			}
		}

		public GuildRankId GetId()
		{
			return _rankId;
		}

		public GuildRankOrder GetOrder()
		{
			return _rankOrder;
		}

		public void SetOrder(GuildRankOrder rankOrder)
		{
			_rankOrder = rankOrder;
		}

		public string GetName()
		{
			return _name;
		}

		public GuildRankRights GetRights()
		{
			return _rights;
		}

		public uint GetBankMoneyPerDay()
		{
			return _rankId != GuildRankId.GuildMaster ? _bankMoneyPerDay : GuildConst.WithdrawMoneyUnlimited;
		}

		public GuildBankRights GetBankTabRights(byte tabId)
		{
			return tabId < GuildConst.MaxBankTabs ? _bankTabRightsAndSlots[tabId].GetRights() : 0;
		}

		public int GetBankTabSlotsPerDay(byte tabId)
		{
			return tabId < GuildConst.MaxBankTabs ? _bankTabRightsAndSlots[tabId].GetSlots() : 0;
		}
	}

	public class BankTab
	{
		readonly ulong _guildId;
		readonly byte _tabId;
        private readonly GameObjectManager _gameObjectManager;
        private readonly CharacterDatabase _characterDatabase;
        readonly Item[] _items = new Item[GuildConst.MaxBankSlots];
		string _name;
		string _icon;
		string _text;

		public BankTab(ulong guildId, byte tabId, GameObjectManager gameObjectManager, CharacterDatabase characterDatabase)
		{
			_guildId = guildId;
			_tabId = tabId;
            _gameObjectManager = gameObjectManager;
            _characterDatabase = characterDatabase;
        }

		public void LoadFromDB(SQLFields field)
		{
			_name = field.Read<string>(2);
			_icon = field.Read<string>(3);
			_text = field.Read<string>(4);
		}

		public bool LoadItemFromDB(SQLFields field)
		{
			var slotId = field.Read<byte>(53);
			var itemGuid = field.Read<uint>(0);
			var itemEntry = field.Read<uint>(1);

			if (slotId >= GuildConst.MaxBankSlots)
			{
				Log.Logger.Error("Invalid slot for item (GUID: {0}, id: {1}) in guild bank, skipped.", itemGuid, itemEntry);

				return false;
			}

			var proto = _gameObjectManager.GetItemTemplate(itemEntry);

			if (proto == null)
			{
				Log.Logger.Error("Unknown item (GUID: {0}, id: {1}) in guild bank, skipped.", itemGuid, itemEntry);

				return false;
			}

			var pItem = Item.NewItemOrBag(proto);

			if (!pItem.LoadFromDB(itemGuid, ObjectGuid.Empty, field, itemEntry))
			{
				Log.Logger.Error("Item (GUID {0}, id: {1}) not found in ite_instance, deleting from guild bank!", itemGuid, itemEntry);

				var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_NONEXISTENT_GUILD_BANK_ITEM);
				stmt.AddValue(0, _guildId);
				stmt.AddValue(1, _tabId);
				stmt.AddValue(2, slotId);
				_characterDatabase.Execute(stmt);

				return false;
			}

			pItem.AddToWorld();
			_items[slotId] = pItem;

			return true;
		}

		public void Delete(SQLTransaction trans, bool removeItemsFromDB = false)
		{
			for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
			{
				var pItem = _items[slotId];

				if (pItem != null)
				{
					pItem.RemoveFromWorld();

					if (removeItemsFromDB)
						pItem.DeleteFromDB(trans);
				}
			}
		}

		public void SetInfo(string name, string icon)
		{
			if (_name == name && _icon == icon)
				return;

			_name = name;
			_icon = icon;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_INFO);
			stmt.AddValue(0, _name);
			stmt.AddValue(1, _icon);
			stmt.AddValue(2, _guildId);
			stmt.AddValue(3, _tabId);
			_characterDatabase.Execute(stmt);
		}

		public void SetText(string text)
		{
			if (_text == text)
				return;

			_text = text;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_TEXT);
			stmt.AddValue(0, _text);
			stmt.AddValue(1, _guildId);
			stmt.AddValue(2, _tabId);
			_characterDatabase.Execute(stmt);
		}

		public void SendText(Guild guild, WorldSession session = null)
		{
			GuildBankTextQueryResult textQuery = new();
			textQuery.Tab = _tabId;
			textQuery.Text = _text;

			if (session != null)
			{
				Log.Logger.Debug("SMSG_GUILD_BANK_QUERY_TEXT_RESULT [{0}]: Tabid: {1}, Text: {2}", session.GetPlayerInfo(), _tabId, _text);
				session.SendPacket(textQuery);
			}
			else
			{
				Log.Logger.Debug("SMSG_GUILD_BANK_QUERY_TEXT_RESULT [Broadcast]: Tabid: {0}, Text: {1}", _tabId, _text);
				guild.BroadcastPacket(textQuery);
			}
		}

		public string GetName()
		{
			return _name;
		}

		public string GetIcon()
		{
			return _icon;
		}

		public string GetText()
		{
			return _text;
		}

		public Item GetItem(byte slotId)
		{
			return slotId < GuildConst.MaxBankSlots ? _items[slotId] : null;
		}

		public bool SetItem(SQLTransaction trans, byte slotId, Item item)
		{
			if (slotId >= GuildConst.MaxBankSlots)
				return false;

			_items[slotId] = item;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEM);
			stmt.AddValue(0, _guildId);
			stmt.AddValue(1, _tabId);
			stmt.AddValue(2, slotId);
			trans.Append(stmt);

			if (item != null)
			{
				stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_ITEM);
				stmt.AddValue(0, _guildId);
				stmt.AddValue(1, _tabId);
				stmt.AddValue(2, slotId);
				stmt.AddValue(3, item.GUID.Counter);
				trans.Append(stmt);

				item.SetContainedIn(ObjectGuid.Empty);
				item.SetOwnerGUID(ObjectGuid.Empty);
				item.FSetState(ItemUpdateState.New);
				item.SaveToDB(trans); // Not in inventory and can be saved standalone
			}

			return true;
		}
	}

	public class GuildBankRightsAndSlots
	{
		byte tabId;
		GuildBankRights rights;
		int slots;

		public GuildBankRightsAndSlots(byte _tabId = 0xFF, sbyte _rights = 0, int _slots = 0)
		{
			tabId = _tabId;
			rights = (GuildBankRights)_rights;
			slots = _slots;
		}

		public void SetGuildMasterValues()
		{
			rights = GuildBankRights.Full;
			slots = Convert.ToInt32(GuildConst.WithdrawSlotUnlimited);
		}

		public void SetTabId(byte _tabId)
		{
			tabId = _tabId;
		}

		public void SetSlots(int _slots)
		{
			slots = _slots;
		}

		public void SetRights(GuildBankRights _rights)
		{
			rights = _rights;
		}

		public byte GetTabId()
		{
			return tabId;
		}

		public int GetSlots()
		{
			return slots;
		}

		public GuildBankRights GetRights()
		{
			return rights;
		}
	}

	public class EmblemInfo
	{
		uint _style;
		uint _color;
		uint _borderStyle;
		uint _borderColor;
		uint _backgroundColor;
        private readonly CliDB _cliDB;
        private readonly CharacterDatabase _characterDatabase;

        public EmblemInfo(CliDB cliDB, CharacterDatabase characterDatabase)
		{
			_style = 0;
			_color = 0;
			_borderStyle = 0;
			_borderColor = 0;
			_backgroundColor = 0;

            _cliDB = cliDB;
            _characterDatabase = characterDatabase;
        }

		public void ReadPacket(SaveGuildEmblem packet)
		{
			_style = packet.EStyle;
			_color = packet.EColor;
			_borderStyle = packet.BStyle;
			_borderColor = packet.BColor;
			_backgroundColor = packet.Bg;
		}

		public bool ValidateEmblemColors()
		{
			return _cliDB.GuildColorBackgroundStorage.ContainsKey(_backgroundColor) &&
					_cliDB.GuildColorBorderStorage.ContainsKey(_borderColor) &&
					_cliDB.GuildColorEmblemStorage.ContainsKey(_color);
		}

		public bool LoadFromDB(SQLFields field)
		{
			_style = field.Read<byte>(3);
			_color = field.Read<byte>(4);
			_borderStyle = field.Read<byte>(5);
			_borderColor = field.Read<byte>(6);
			_backgroundColor = field.Read<byte>(7);

			return ValidateEmblemColors();
		}

		public void SaveToDB(ulong guildId)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_EMBLEM_INFO);
			stmt.AddValue(0, _style);
			stmt.AddValue(1, _color);
			stmt.AddValue(2, _borderStyle);
			stmt.AddValue(3, _borderColor);
			stmt.AddValue(4, _backgroundColor);
			stmt.AddValue(5, guildId);
			_characterDatabase.Execute(stmt);
		}

		public uint GetStyle()
		{
			return _style;
		}

		public uint GetColor()
		{
			return _color;
		}

		public uint GetBorderStyle()
		{
			return _borderStyle;
		}

		public uint GetBorderColor()
		{
			return _borderColor;
		}

		public uint GetBackgroundColor()
		{
			return _backgroundColor;
		}
	}

	public abstract class MoveItemData
	{
		public Guild _pGuild;
		public Player _pPlayer;
		public byte _container;
		public byte _slotId;
        private readonly ScriptManager _scriptManager;
        public Item _pItem;
		public Item _pClonedItem;
		public List<ItemPosCount> _vec = new();

		protected MoveItemData(Guild guild, Player player, byte container, byte slotId, ScriptManager scriptManager)
		{
			_pGuild = guild;
			_pPlayer = player;
			_container = container;
			_slotId = slotId;
            _scriptManager = scriptManager;
            _pItem = null;
			_pClonedItem = null;
		}

		public virtual bool CheckItem(ref uint splitedAmount)
		{
			if (splitedAmount > _pItem.Count)
				return false;

			if (splitedAmount == _pItem.Count)
				splitedAmount = 0;

			return true;
		}

		public InventoryResult CanStore(Item pItem, bool swap, bool sendError)
		{
			_vec.Clear();
			var msg = CanStore(pItem, swap);

			if (sendError && msg != InventoryResult.Ok)
				SendEquipError(msg, pItem);

			return msg;
		}

		public bool CloneItem(uint count)
		{
			_pClonedItem = _pItem.CloneItem(count);

			if (_pClonedItem == null)
			{
				SendEquipError(InventoryResult.ItemNotFound, _pItem);

				return false;
			}

			return true;
		}

		public virtual void LogAction(MoveItemData pFrom)
		{
			_scriptManager.ForEach<IGuildOnItemMove>(p => p.OnItemMove(_pGuild,
																		_pPlayer,
																		pFrom.GetItem(),
																		pFrom.IsBank(),
																		pFrom.GetContainer(),
																		pFrom.GetSlotId(),
																		IsBank(),
																		GetContainer(),
																		GetSlotId()));
		}

		public void CopySlots(List<byte> ids)
		{
			foreach (var item in _vec)
				ids.Add((byte)item.Pos);
		}

		public void SendEquipError(InventoryResult result, Item item)
		{
			_pPlayer.SendEquipError(result, item);
		}

		public abstract bool IsBank();

		// Initializes item. Returns true, if item exists, false otherwise.
		public abstract bool InitItem();

		// Checks splited amount against item. Splited amount cannot be more that number of items in stack.
		// Defines if player has rights to save item in container
		public virtual bool HasStoreRights(MoveItemData pOther)
		{
			return true;
		}

		// Defines if player has rights to withdraw item from container
		public virtual bool HasWithdrawRights(MoveItemData pOther)
		{
			return true;
		}

		// Remove item from container (if splited update items fields)
		public abstract void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0);

		// Saves item to container
		public abstract Item StoreItem(SQLTransaction trans, Item pItem);

		// Log bank event
		public abstract void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count);

		public abstract InventoryResult CanStore(Item pItem, bool swap);

		public Item GetItem(bool isCloned = false)
		{
			return isCloned ? _pClonedItem : _pItem;
		}

		public byte GetContainer()
		{
			return _container;
		}

		public byte GetSlotId()
		{
			return _slotId;
		}
	}
	
	public class PlayerMoveItemData : MoveItemData
	{
		public PlayerMoveItemData(Guild guild, Player player, byte container, byte slotId, ScriptManager scriptManager)
			: base(guild, player, container, slotId, scriptManager) { }

		public override bool IsBank()
		{
			return false;
		}

		public override bool InitItem()
		{
			_pItem = _pPlayer.GetItemByPos(_container, _slotId);

			if (_pItem != null)
			{
				// Anti-WPE protection. Do not move non-empty bags to bank.
				if (_pItem.IsNotEmptyBag)
				{
					SendEquipError(InventoryResult.DestroyNonemptyBag, _pItem);
					_pItem = null;
				}
				// Bound items cannot be put into bank.
				else if (!_pItem.CanBeTraded())
				{
					SendEquipError(InventoryResult.CantSwap, _pItem);
					_pItem = null;
				}
			}

			return (_pItem != null);
		}

		public override void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0)
		{
			if (splitedAmount != 0)
			{
				_pItem.SetCount(_pItem.Count - splitedAmount);
				_pItem.SetState(ItemUpdateState.Changed, _pPlayer);
				_pPlayer.SaveInventoryAndGoldToDB(trans);
			}
			else
			{
				_pPlayer.MoveItemFromInventory(_container, _slotId, true);
				_pItem.DeleteFromInventoryDB(trans);
				_pItem = null;
			}
		}

		public override Item StoreItem(SQLTransaction trans, Item pItem)
		{
			_pPlayer.MoveItemToInventory(_vec, pItem, true);
			_pPlayer.SaveInventoryAndGoldToDB(trans);

			return pItem;
		}

		public override void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count)
		{
			// Bank . Char
			_pGuild._LogBankEvent(trans,
									GuildBankEventLogTypes.WithdrawItem,
									pFrom.GetContainer(),
									_pPlayer.GUID.Counter,
									pFrom.GetItem().Entry,
									(ushort)count);
		}

		public override InventoryResult CanStore(Item pItem, bool swap)
		{
			return _pPlayer.CanStoreItem(_container, _slotId, _vec, pItem, swap);
		}
	}

	public class BankMoveItemData : MoveItemData
	{
		public BankMoveItemData(Guild guild, Player player, byte container, byte slotId, ScriptManager scriptManager)
			: base(guild, player, container, slotId, scriptManager) { }

		public override bool IsBank()
		{
			return true;
		}

		public override bool InitItem()
		{
			_pItem = _pGuild._GetItem(_container, _slotId);

			return (_pItem != null);
		}

		public override bool HasStoreRights(MoveItemData pOther)
		{
			// Do not check rights if item is being swapped within the same bank tab
			if (pOther.IsBank() && pOther.GetContainer() == _container)
				return true;

			return _pGuild._MemberHasTabRights(_pPlayer.GUID, _container, GuildBankRights.DepositItem);
		}

		public override bool HasWithdrawRights(MoveItemData pOther)
		{
			// Do not check rights if item is being swapped within the same bank tab
			if (pOther.IsBank() && pOther.GetContainer() == _container)
				return true;

			var slots = 0;
			var member = _pGuild.GetMember(_pPlayer.GUID);

			if (member != null)
				slots = _pGuild._GetMemberRemainingSlots(member, _container);

			return slots != 0;
		}

		public override void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0)
		{
			if (splitedAmount != 0)
			{
				_pItem.SetCount(_pItem.Count - splitedAmount);
				_pItem.FSetState(ItemUpdateState.Changed);
				_pItem.SaveToDB(trans);
			}
			else
			{
				_pGuild._RemoveItem(trans, _container, _slotId);
				_pItem = null;
			}

			// Decrease amount of player's remaining items (if item is moved to different tab or to player)
			if (!pOther.IsBank() || pOther.GetContainer() != _container)
				_pGuild._UpdateMemberWithdrawSlots(trans, _pPlayer.GUID, _container);
		}

		public override Item StoreItem(SQLTransaction trans, Item pItem)
		{
			if (pItem == null)
				return null;

			var pTab = _pGuild.GetBankTab(_container);

			if (pTab == null)
				return null;

			var pLastItem = pItem;

			foreach (var pos in _vec)
			{
				Log.Logger.Debug(
							"GUILD STORAGE: StoreItem tab = {0}, slot = {1}, item = {2}, count = {3}",
							_container,
							_slotId,
							pItem.Entry,
							pItem.Count);

				pLastItem = _StoreItem(trans, pTab, pItem, pos, pos.Equals(_vec.Last()));
			}

			return pLastItem;
		}

		public override void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count)
		{
			if (pFrom.IsBank())
				// Bank . Bank
				_pGuild._LogBankEvent(trans,
										GuildBankEventLogTypes.MoveItem,
										pFrom.GetContainer(),
										_pPlayer.GUID.Counter,
										pFrom.GetItem().Entry,
										(ushort)count,
										_container);
			else
				// Char . Bank
				_pGuild._LogBankEvent(trans,
										GuildBankEventLogTypes.DepositItem,
										_container,
										_pPlayer.GUID.Counter,
										pFrom.GetItem().Entry,
										(ushort)count);
		}

		public override void LogAction(MoveItemData pFrom)
		{
			base.LogAction(pFrom);

			if (!pFrom.IsBank() && _pPlayer.Session.HasPermission(RBACPermissions.LogGmTrade)) // @todo Move this to scripts
				Log.Logger.Information("GM {0} ({1}) (Account: {2}) deposit item: {3} (Entry: {4} Count: {5}) to guild bank named: {6} (Guild ID: {7})",
								_pPlayer.GetName(),
								_pPlayer.GUID.ToString(),
								_pPlayer.Session.AccountId,
								pFrom.GetItem().Template.GetName(),
								pFrom.GetItem().Entry,
								pFrom.GetItem().Count,
								_pGuild.GetName(),
								_pGuild.GetId());
		}

		public override InventoryResult CanStore(Item pItem, bool swap)
		{
			Log.Logger.Debug(
						"GUILD STORAGE: CanStore() tab = {0}, slot = {1}, item = {2}, count = {3}",
						_container,
						_slotId,
						pItem.Entry,
						pItem.Count);

			var count = pItem.Count;

			// Soulbound items cannot be moved
			if (pItem.IsSoulBound)
				return InventoryResult.DropBoundItem;

			// Make sure destination bank tab exists
			if (_container >= _pGuild._GetPurchasedTabsSize())
				return InventoryResult.WrongBagType;

			// Slot explicitely specified. Check it.
			if (_slotId != ItemConst.NullSlot)
			{
				var pItemDest = _pGuild._GetItem(_container, _slotId);

				// Ignore swapped item (this slot will be empty after move)
				if ((pItemDest == pItem) || swap)
					pItemDest = null;

				if (!_ReserveSpace(_slotId, pItem, pItemDest, ref count))
					return InventoryResult.CantStack;

				if (count == 0)
					return InventoryResult.Ok;
			}

			// Slot was not specified or it has not enough space for all the items in stack
			// Search for stacks to merge with
			if (pItem.MaxStackCount > 1)
			{
				CanStoreItemInTab(pItem, _slotId, true, ref count);

				if (count == 0)
					return InventoryResult.Ok;
			}

			// Search free slot for item
			CanStoreItemInTab(pItem, _slotId, false, ref count);

			if (count == 0)
				return InventoryResult.Ok;

			return InventoryResult.BankFull;
		}

		Item _StoreItem(SQLTransaction trans, BankTab pTab, Item pItem, ItemPosCount pos, bool clone)
		{
			var slotId = (byte)pos.Pos;
			var count = pos.Count;
			var pItemDest = pTab.GetItem(slotId);

			if (pItemDest != null)
			{
				pItemDest.SetCount(pItemDest.Count + count);
				pItemDest.FSetState(ItemUpdateState.Changed);
				pItemDest.SaveToDB(trans);

				if (!clone)
				{
					pItem.RemoveFromWorld();
					pItem.DeleteFromDB(trans);
				}

				return pItemDest;
			}

			if (clone)
				pItem = pItem.CloneItem(count);
			else
				pItem.SetCount(count);

			if (pItem != null && pTab.SetItem(trans, slotId, pItem))
				return pItem;

			return null;
		}

		bool _ReserveSpace(byte slotId, Item pItem, Item pItemDest, ref uint count)
		{
			var requiredSpace = pItem.MaxStackCount;

			if (pItemDest != null)
			{
				// Make sure source and destination items match and destination item has space for more stacks.
				if (pItemDest.Entry != pItem.Entry || pItemDest.Count >= pItem.MaxStackCount)
					return false;

				requiredSpace -= pItemDest.Count;
			}

			// Let's not be greedy, reserve only required space
			requiredSpace = Math.Min(requiredSpace, count);

			// Reserve space
			ItemPosCount pos = new(slotId, requiredSpace);

			if (!pos.IsContainedIn(_vec))
			{
				_vec.Add(pos);
				count -= requiredSpace;
			}

			return true;
		}

		void CanStoreItemInTab(Item pItem, byte skipSlotId, bool merge, ref uint count)
		{
			for (byte slotId = 0; (slotId < GuildConst.MaxBankSlots) && (count > 0); ++slotId)
			{
				// Skip slot already processed in CanStore (when destination slot was specified)
				if (slotId == skipSlotId)
					continue;

				var pItemDest = _pGuild._GetItem(_container, slotId);

				if (pItemDest == pItem)
					pItemDest = null;

				// If merge skip empty, if not merge skip non-empty
				if ((pItemDest != null) != merge)
					continue;

				_ReserveSpace(slotId, pItem, pItemDest, ref count);
			}
		}
	}

	#endregion
}