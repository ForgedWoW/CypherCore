// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Game.Common.Handlers;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Accounts;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Cache;
using Serilog;

namespace Forged.RealmServer;

public class SocialHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly WorldConfig _worldConfig;
    private readonly WhoListStorageManager _whoListStorageManager;
    private readonly AccountManager _accountManager;
    private readonly CliDB _cliDB;
    private readonly LoginDatabase _loginDatabase;
    private readonly WorldManager _worldManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly CharacterCache _characterCache;
    private readonly SocialManager _socialManager;

    public SocialHandler(WorldSession session, WorldConfig worldConfig, WhoListStorageManager whoListStorageManager,
		AccountManager accountManager, CliDB cliDB, LoginDatabase loginDatabase, WorldManager worldManager, ObjectAccessor objectAccessor,
		CharacterCache characterCache, SocialManager socialManager)
    {
        _session = session;
        _worldConfig = worldConfig;
        _whoListStorageManager = whoListStorageManager;
        _accountManager = accountManager;
        _cliDB = cliDB;
        _loginDatabase = loginDatabase;
        _worldManager = worldManager;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _socialManager = socialManager;
    }

    [WorldPacketHandler(ClientOpcodes.Who, Processing = PacketProcessing.ThreadSafe)]
	void HandleWho(WhoRequestPkt whoRequest)
	{
		var request = whoRequest.Request;

		// zones count, client limit = 10 (2.0.10)
		// can't be received from real client or broken packet
		if (whoRequest.Areas.Count > 10)
			return;

		// user entered strings count, client limit=4 (checked on 2.0.10)
		// can't be received from real client or broken packet
		if (request.Words.Count > 4)
			return;

		// @todo: handle following packet values
		// VirtualRealmNames
		// ShowEnemies
		// ShowArenaPlayers
		// ExactName
		// ServerInfo

		request.Words.ForEach(p => p = p.ToLower());

		request.Name = request.Name.ToLower();
		request.Guild = request.Guild.ToLower();

		// client send in case not set max level value 100 but we support 255 max level,
		// update it to show GMs with characters after 100 level
		if (whoRequest.Request.MaxLevel >= 100)
			whoRequest.Request.MaxLevel = 255;

		var team = _session.Player.Team;

		var gmLevelInWhoList = _worldConfig.GetUIntValue(WorldCfg.GmLevelInWhoList);

		WhoResponsePkt response = new();
		response.RequestID = whoRequest.RequestID;

		var whoList = _whoListStorageManager.GetWhoList();

		foreach (var target in whoList)
		{
			// player can see member of other team only if CONFIG_ALLOW_TWO_SIDE_WHO_LIST
			if (target.Team != team && !_session.HasPermission(RBACPermissions.TwoSideWhoList))
				continue;

			// player can see MODERATOR, GAME MASTER, ADMINISTRATOR only if CONFIG_GM_IN_WHO_LIST
			if (target.Security > (AccountTypes)gmLevelInWhoList && !_session.HasPermission(RBACPermissions.WhoSeeAllSecLevels))
				continue;

			// check if target is globally visible for player
			if (_session.Player.GUID != target.Guid && !target.IsVisible)
				if (_accountManager.IsPlayerAccount(_session.Player.Session.Security) || target.Security > _session.Player.Session.Security)
					continue;

			// check if target's level is in level range
			var lvl = target.Level;

			if (lvl < request.MinLevel || lvl > request.MaxLevel)
				continue;

			// check if class matches classmask
			if (!Convert.ToBoolean(request.ClassFilter & (1 << target.Class)))
				continue;

			// check if race matches racemask
			if (!Convert.ToBoolean(request.RaceFilter & (1 << target.Race)))
				continue;

			if (!whoRequest.Areas.Empty())
				if (whoRequest.Areas.Contains((int)target.ZoneId))
					continue;

			var wTargetName = target.PlayerName.ToLower();

			if (!(request.Name.IsEmpty() || wTargetName.Equals(request.Name)))
				continue;

			var wTargetGuildName = target.GuildName.ToLower();

			if (!request.Guild.IsEmpty() && !wTargetGuildName.Equals(request.Guild))
				continue;

			if (!request.Words.Empty())
			{
				var aname = "";
				var areaEntry = _cliDB.AreaTableStorage.LookupByKey(target.ZoneId);

				if (areaEntry != null)
					aname = areaEntry.AreaName[_session.SessionDbcLocale].ToLower();

				var show = false;

				for (var i = 0; i < request.Words.Count; ++i)
					if (!string.IsNullOrEmpty(request.Words[i]))
						if (wTargetName.Equals(request.Words[i]) ||
							wTargetGuildName.Equals(request.Words[i]) ||
							aname.Equals(request.Words[i]))
						{
							show = true;

							break;
						}

				if (!show)
					continue;
			}

			WhoEntry whoEntry = new();

			if (!whoEntry.PlayerData.Initialize(target.Guid, null))
				continue;

			if (!target.GuildGuid.IsEmpty)
			{
				whoEntry.GuildGUID = target.GuildGuid;
				whoEntry.GuildVirtualRealmAddress = _worldManager.VirtualRealmAddress;
				whoEntry.GuildName = target.GuildName;
			}

			whoEntry.AreaID = (int)target.ZoneId;
			whoEntry.IsGM = target.IsGamemaster;

			response.Response.Add(whoEntry);

			// 50 is maximum player count sent to client
			if (response.Response.Count >= 50)
				break;
		}

        _session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.WhoIs)]
	void HandleWhoIs(WhoIsRequest packet)
	{
		if (!_session.HasPermission(RBACPermissions.OpcodeWhois))
		{
			_session.SendNotification(CypherStrings.YouNotHavePermission);

			return;
		}

		if (!GameObjectManager.NormalizePlayerName(ref packet.CharName))
		{
            _session.SendNotification(CypherStrings.NeedCharacterName);

			return;
		}

		var player = _objectAccessor.FindPlayerByName(packet.CharName);

		if (!player)
		{
            _session.SendNotification(CypherStrings.PlayerNotExistOrOffline, packet.CharName);

			return;
		}

		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_WHOIS);
		stmt.AddValue(0, player.Session.AccountId);

		var result = _loginDatabase.Query(stmt);

		if (result.IsEmpty())
		{
            _session.SendNotification(CypherStrings.AccountForPlayerNotFound, packet.CharName);

			return;
		}

		var acc = result.Read<string>(0);

		if (string.IsNullOrEmpty(acc))
			acc = "Unknown";

		var email = result.Read<string>(1);

		if (string.IsNullOrEmpty(email))
			email = "Unknown";

		var lastip = result.Read<string>(2);

		if (string.IsNullOrEmpty(lastip))
			lastip = "Unknown";

		WhoIsResponse response = new();
		response.AccountName = packet.CharName + "'s " + "account is " + acc + ", e-mail: " + email + ", last ip: " + lastip;
        _session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.SendContactList)]
	void HandleContactList(SendContactList packet)
	{
        _session.Player.Social.SendSocialList(_session.Player, packet.Flags);
	}

	[WorldPacketHandler(ClientOpcodes.AddFriend)]
	void HandleAddFriend(AddFriend packet)
	{
		if (!GameObjectManager.NormalizePlayerName(ref packet.Name))
			return;

		var friendCharacterInfo = _characterCache.GetCharacterCacheByName(packet.Name);

		if (friendCharacterInfo == null)
		{
			_socialManager.SendFriendStatus(_session.Player, FriendsResult.NotFound, ObjectGuid.Empty);

			return;
		}

		void processFriendRequest()
		{
			var playerGuid = _session.Player.GUID;
			var friendGuid = friendCharacterInfo.Guid;
			var friendAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, friendCharacterInfo.AccountId);
			var team = Player.TeamForRace(friendCharacterInfo.RaceId);
			var friendNote = packet.Notes;

			if (playerGuid.Counter != _session.Player.GUID.Counter)
				return; // not the player initiating request, do nothing

			var friendResult = FriendsResult.NotFound;

			if (friendGuid == _session.Player.GUID)
			{
				friendResult = FriendsResult.Self;
			}
			else if (_session.Player.Team != team && !_session.HasPermission(RBACPermissions.TwoSideAddFriend))
			{
				friendResult = FriendsResult.Enemy;
			}
			else if (_session.Player.Social.HasFriend(friendGuid))
			{
				friendResult = FriendsResult.Already;
			}
			else
			{
				var pFriend = _objectAccessor.FindPlayer(friendGuid);

				if (pFriend != null && pFriend.IsVisibleGloballyFor(_session.Player))
					friendResult = FriendsResult.Online;
				else
					friendResult = FriendsResult.AddedOnline;

				if (_session.Player.Social.AddToSocialList(friendGuid, friendAccountGuid, SocialFlag.Friend))
                    _session.Player.Social.SetFriendNote(friendGuid, friendNote);
				else
					friendResult = FriendsResult.ListFull;
			}

			_socialManager.SendFriendStatus(_session.Player, friendResult, friendGuid);
		}

		if (_session.HasPermission(RBACPermissions.AllowGmFriend))
		{
			processFriendRequest();

			return;
		}

		// First try looking up friend candidate security from online object
		var friendPlayer = _objectAccessor.FindPlayer(friendCharacterInfo.Guid);

		if (friendPlayer != null)
		{
			if (!_accountManager.IsPlayerAccount(friendPlayer.Session.Security))
			{
				_socialManager.SendFriendStatus(_session.Player, FriendsResult.NotFound, ObjectGuid.Empty);

				return;
			}

			processFriendRequest();

			return;
		}

		// When not found, consult database
		_session.QueryProcessor.AddCallback(_accountManager.GetSecurityAsync(friendCharacterInfo.AccountId,
																	(int)_worldManager.RealmId.Index,
																	friendSecurity =>
																	{
																		if (!_accountManager.IsPlayerAccount((AccountTypes)friendSecurity))
																		{
																			_socialManager.SendFriendStatus(_session.Player, FriendsResult.NotFound, ObjectGuid.Empty);

																			return;
																		}

																		processFriendRequest();
																	}));
	}

	[WorldPacketHandler(ClientOpcodes.DelFriend)]
	void HandleDelFriend(DelFriend packet)
	{
        // @todo: handle VirtualRealmAddress
        _session.Player.Social.RemoveFromSocialList(packet.Player.Guid, SocialFlag.Friend);

		_socialManager.SendFriendStatus(_session.Player, FriendsResult.Removed, packet.Player.Guid);
	}

	[WorldPacketHandler(ClientOpcodes.AddIgnore)]
	void HandleAddIgnore(AddIgnore packet)
	{
		if (!GameObjectManager.NormalizePlayerName(ref packet.Name))
			return;

		var ignoreGuid = ObjectGuid.Empty;
		var ignoreResult = FriendsResult.IgnoreNotFound;

		var characterInfo = _characterCache.GetCharacterCacheByName(packet.Name);

		if (characterInfo != null)
		{
			ignoreGuid = characterInfo.Guid;
			var ignoreAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, characterInfo.AccountId);

			if (ignoreGuid == _session.Player.GUID) //not add yourself
			{
				ignoreResult = FriendsResult.IgnoreSelf;
			}
			else if (_session.Player.Social.HasIgnore(ignoreGuid, ignoreAccountGuid))
			{
				ignoreResult = FriendsResult.IgnoreAlready;
			}
			else
			{
				ignoreResult = FriendsResult.IgnoreAdded;

				// ignore list full
				if (!_session.Player.Social.AddToSocialList(ignoreGuid, ignoreAccountGuid, SocialFlag.Ignored))
					ignoreResult = FriendsResult.IgnoreFull;
			}
		}

		_socialManager.SendFriendStatus(_session.Player, ignoreResult, ignoreGuid);
	}

	[WorldPacketHandler(ClientOpcodes.DelIgnore)]
	void HandleDelIgnore(DelIgnore packet)
	{
		// @todo: handle VirtualRealmAddress
		Log.Logger.Debug("WorldSession.HandleDelIgnoreOpcode: {0}", packet.Player.Guid.ToString());

        _session.Player.Social.RemoveFromSocialList(packet.Player.Guid, SocialFlag.Ignored);

		_socialManager.SendFriendStatus(_session.Player, FriendsResult.IgnoreRemoved, packet.Player.Guid);
	}

	[WorldPacketHandler(ClientOpcodes.SetContactNotes)]
	void HandleSetContactNotes(SetContactNotes packet)
	{
		// @todo: handle VirtualRealmAddress
		Log.Logger.Debug("WorldSession.HandleSetContactNotesOpcode: Contact: {0}, Notes: {1}", packet.Player.Guid.ToString(), packet.Notes);
        _session.Player.Social.SetFriendNote(packet.Player.Guid, packet.Notes);
	}
}