﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
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

		var team = Player.Team;

		var gmLevelInWhoList = WorldConfig.GetUIntValue(WorldCfg.GmLevelInWhoList);

		WhoResponsePkt response = new();
		response.RequestID = whoRequest.RequestID;

		var whoList = Global.WhoListStorageMgr.GetWhoList();

		foreach (var target in whoList)
		{
			// player can see member of other team only if CONFIG_ALLOW_TWO_SIDE_WHO_LIST
			if (target.Team != team && !HasPermission(RBACPermissions.TwoSideWhoList))
				continue;

			// player can see MODERATOR, GAME MASTER, ADMINISTRATOR only if CONFIG_GM_IN_WHO_LIST
			if (target.Security > (AccountTypes)gmLevelInWhoList && !HasPermission(RBACPermissions.WhoSeeAllSecLevels))
				continue;

			// check if target is globally visible for player
			if (_player.GUID != target.Guid && !target.IsVisible)
				if (Global.AccountMgr.IsPlayerAccount(_player.Session.Security) || target.Security > _player.Session.Security)
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
				var areaEntry = CliDB.AreaTableStorage.LookupByKey(target.ZoneId);

				if (areaEntry != null)
					aname = areaEntry.AreaName[SessionDbcLocale].ToLower();

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
				whoEntry.GuildVirtualRealmAddress = Global.WorldMgr.VirtualRealmAddress;
				whoEntry.GuildName = target.GuildName;
			}

			whoEntry.AreaID = (int)target.ZoneId;
			whoEntry.IsGM = target.IsGamemaster;

			response.Response.Add(whoEntry);

			// 50 is maximum player count sent to client
			if (response.Response.Count >= 50)
				break;
		}

		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.WhoIs)]
	void HandleWhoIs(WhoIsRequest packet)
	{
		if (!HasPermission(RBACPermissions.OpcodeWhois))
		{
			SendNotification(CypherStrings.YouNotHavePermission);

			return;
		}

		if (!ObjectManager.NormalizePlayerName(ref packet.CharName))
		{
			SendNotification(CypherStrings.NeedCharacterName);

			return;
		}

		var player = Global.ObjAccessor.FindPlayerByName(packet.CharName);

		if (!player)
		{
			SendNotification(CypherStrings.PlayerNotExistOrOffline, packet.CharName);

			return;
		}

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_WHOIS);
		stmt.AddValue(0, player.Session.AccountId);

		var result = DB.Login.Query(stmt);

		if (result.IsEmpty())
		{
			SendNotification(CypherStrings.AccountForPlayerNotFound, packet.CharName);

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
		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.SendContactList)]
	void HandleContactList(SendContactList packet)
	{
		Player.Social.SendSocialList(Player, packet.Flags);
	}

	[WorldPacketHandler(ClientOpcodes.AddFriend)]
	void HandleAddFriend(AddFriend packet)
	{
		if (!ObjectManager.NormalizePlayerName(ref packet.Name))
			return;

		var friendCharacterInfo = Global.CharacterCacheStorage.GetCharacterCacheByName(packet.Name);

		if (friendCharacterInfo == null)
		{
			Global.SocialMgr.SendFriendStatus(Player, FriendsResult.NotFound, ObjectGuid.Empty);

			return;
		}

		void processFriendRequest()
		{
			var playerGuid = _player.GUID;
			var friendGuid = friendCharacterInfo.Guid;
			var friendAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, friendCharacterInfo.AccountId);
			var team = Player.TeamForRace(friendCharacterInfo.RaceId);
			var friendNote = packet.Notes;

			if (playerGuid.Counter != _guidLow)
				return; // not the player initiating request, do nothing

			var friendResult = FriendsResult.NotFound;

			if (friendGuid == Player.GUID)
			{
				friendResult = FriendsResult.Self;
			}
			else if (Player.Team != team && !HasPermission(RBACPermissions.TwoSideAddFriend))
			{
				friendResult = FriendsResult.Enemy;
			}
			else if (Player.Social.HasFriend(friendGuid))
			{
				friendResult = FriendsResult.Already;
			}
			else
			{
				var pFriend = Global.ObjAccessor.FindPlayer(friendGuid);

				if (pFriend != null && pFriend.IsVisibleGloballyFor(Player))
					friendResult = FriendsResult.Online;
				else
					friendResult = FriendsResult.AddedOnline;

				if (Player.Social.AddToSocialList(friendGuid, friendAccountGuid, SocialFlag.Friend))
					Player.Social.SetFriendNote(friendGuid, friendNote);
				else
					friendResult = FriendsResult.ListFull;
			}

			Global.SocialMgr.SendFriendStatus(Player, friendResult, friendGuid);
		}

		if (HasPermission(RBACPermissions.AllowGmFriend))
		{
			processFriendRequest();

			return;
		}

		// First try looking up friend candidate security from online object
		var friendPlayer = Global.ObjAccessor.FindPlayer(friendCharacterInfo.Guid);

		if (friendPlayer != null)
		{
			if (!Global.AccountMgr.IsPlayerAccount(friendPlayer.Session.Security))
			{
				Global.SocialMgr.SendFriendStatus(Player, FriendsResult.NotFound, ObjectGuid.Empty);

				return;
			}

			processFriendRequest();

			return;
		}

		// When not found, consult database
		QueryProcessor.AddCallback(Global.AccountMgr.GetSecurityAsync(friendCharacterInfo.AccountId,
																	(int)Global.WorldMgr.RealmId.Index,
																	friendSecurity =>
																	{
																		if (!Global.AccountMgr.IsPlayerAccount((AccountTypes)friendSecurity))
																		{
																			Global.SocialMgr.SendFriendStatus(Player, FriendsResult.NotFound, ObjectGuid.Empty);

																			return;
																		}

																		processFriendRequest();
																	}));
	}

	[WorldPacketHandler(ClientOpcodes.DelFriend)]
	void HandleDelFriend(DelFriend packet)
	{
		// @todo: handle VirtualRealmAddress
		Player. // @todo: handle VirtualRealmAddress
			Social.RemoveFromSocialList(packet.Player.Guid, SocialFlag.Friend);

		Global.SocialMgr.SendFriendStatus(Player, FriendsResult.Removed, packet.Player.Guid);
	}

	[WorldPacketHandler(ClientOpcodes.AddIgnore)]
	void HandleAddIgnore(AddIgnore packet)
	{
		if (!ObjectManager.NormalizePlayerName(ref packet.Name))
			return;

		var ignoreGuid = ObjectGuid.Empty;
		var ignoreResult = FriendsResult.IgnoreNotFound;

		var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByName(packet.Name);

		if (characterInfo != null)
		{
			ignoreGuid = characterInfo.Guid;
			var ignoreAccountGuid = ObjectGuid.Create(HighGuid.WowAccount, characterInfo.AccountId);

			if (ignoreGuid == Player.GUID) //not add yourself
			{
				ignoreResult = FriendsResult.IgnoreSelf;
			}
			else if (Player.Social.HasIgnore(ignoreGuid, ignoreAccountGuid))
			{
				ignoreResult = FriendsResult.IgnoreAlready;
			}
			else
			{
				ignoreResult = FriendsResult.IgnoreAdded;

				// ignore list full
				if (!Player.Social.AddToSocialList(ignoreGuid, ignoreAccountGuid, SocialFlag.Ignored))
					ignoreResult = FriendsResult.IgnoreFull;
			}
		}

		Global.SocialMgr.SendFriendStatus(Player, ignoreResult, ignoreGuid);
	}

	[WorldPacketHandler(ClientOpcodes.DelIgnore)]
	void HandleDelIgnore(DelIgnore packet)
	{
		// @todo: handle VirtualRealmAddress
		Log.outDebug(LogFilter.Network, "WorldSession.HandleDelIgnoreOpcode: {0}", packet.Player.Guid.ToString());

		Player.Social.RemoveFromSocialList(packet.Player.Guid, SocialFlag.Ignored);

		Global.SocialMgr.SendFriendStatus(Player, FriendsResult.IgnoreRemoved, packet.Player.Guid);
	}

	[WorldPacketHandler(ClientOpcodes.SetContactNotes)]
	void HandleSetContactNotes(SetContactNotes packet)
	{
		// @todo: handle VirtualRealmAddress
		Log.outDebug(LogFilter.Network, "WorldSession.HandleSetContactNotesOpcode: Contact: {0}, Notes: {1}", packet.Player.Guid.ToString(), packet.Notes);
		Player.Social.SetFriendNote(packet.Player.Guid, packet.Notes);
	}

	[WorldPacketHandler(ClientOpcodes.SocialContractRequest)]
	void HandleSocialContractRequest(SocialContractRequest socialContractRequest)
	{
		SocialContractRequestResponse response = new();
		response.ShowSocialContract = false;
		SendPacket(response);
	}
}