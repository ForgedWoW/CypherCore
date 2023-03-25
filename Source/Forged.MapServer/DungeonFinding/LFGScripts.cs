// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IGroup;
using Game.Scripting.Interfaces.IPlayer;

namespace Game.DungeonFinding;

class LFGPlayerScript : ScriptObjectAutoAdd, IPlayerOnLogout, IPlayerOnLogin, IPlayerOnMapChanged
{
	public PlayerClass PlayerClass { get; } = PlayerClass.None;
	public LFGPlayerScript() : base("LFGPlayerScript") { }

	public void OnLogin(Player player)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		// Temporal: Trying to determine when group data and LFG data gets desynched
		var guid = player.GUID;
		var gguid = Global.LFGMgr.GetGroup(guid);

		var group = player.Group;

		if (group)
		{
			var gguid2 = group.GUID;

			if (gguid != gguid2)
			{
				Log.Logger.Error("{0} on group {1} but LFG has group {2} saved... Fixing.", player.Session.GetPlayerInfo(), gguid2.ToString(), gguid.ToString());
				Global.LFGMgr.SetupGroupMember(guid, group.GUID);
			}
		}

		Global.LFGMgr.SetTeam(player.GUID, player.Team);
		// @todo - Restore LfgPlayerData and send proper status to player if it was in a group
	}

	// Player Hooks
	public void OnLogout(Player player)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		if (!player.Group)
			Global.LFGMgr.LeaveLfg(player.GUID);
		else if (player.Session.PlayerDisconnected)
			Global.LFGMgr.LeaveLfg(player.GUID, true);
	}

	public void OnMapChanged(Player player)
	{
		var map = player.Map;

		if (Global.LFGMgr.InLfgDungeonMap(player.GUID, map.Id, map.DifficultyID))
		{
			var group = player.Group;

			// This function is also called when players log in
			// if for some reason the LFG system recognises the player as being in a LFG dungeon,
			// but the player was loaded without a valid group, we'll teleport to homebind to prevent
			// crashes or other undefined behaviour
			if (!group)
			{
				Global.LFGMgr.LeaveLfg(player.GUID);
				player.RemoveAura(SharedConst.LFGSpellLuckOfTheDraw);
				player.TeleportTo(player.Homebind);

				Log.Logger.Error(
							"LFGPlayerScript.OnMapChanged, Player {0} ({1}) is in LFG dungeon map but does not have a valid group! Teleporting to homebind.",
							player.GetName(),
							player.GUID.ToString());

				return;
			}

			QueryPlayerNamesResponse response = new();

			foreach (var memberSlot in group.MemberSlots)
			{
				player.Session.BuildNameQueryData(memberSlot.Guid, out var nameCacheLookupResult);
				response.Players.Add(nameCacheLookupResult);
			}

			player.SendPacket(response);

			if (Global.LFGMgr.SelectedRandomLfgDungeon(player.GUID))
				player.CastSpell(player, SharedConst.LFGSpellLuckOfTheDraw, true);
		}
		else
		{
			var group = player.Group;

			if (group && group.MembersCount == 1)
			{
				Global.LFGMgr.LeaveLfg(group.GUID);
				group.Disband();

				Log.Logger.Debug(
							"LFGPlayerScript::OnMapChanged, Player {0}({1}) is last in the lfggroup so we disband the group.",
							player.GetName(),
							player.GUID.ToString());
			}

			player.RemoveAura(SharedConst.LFGSpellLuckOfTheDraw);
		}
	}
}

class LFGGroupScript : ScriptObjectAutoAdd, IGroupOnAddMember, IGroupOnRemoveMember, IGroupOnDisband, IGroupOnChangeLeader, IGroupOnInviteMember
{
	public LFGGroupScript() : base("LFGGroupScript") { }

	// Group Hooks
	public void OnAddMember(PlayerGroup group, ObjectGuid guid)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		var gguid = group.GUID;
		var leader = group.LeaderGUID;

		if (leader == guid)
		{
			Log.Logger.Debug("LFGScripts.OnAddMember [{0}]: added [{1} leader {2}]", gguid, guid, leader);
			Global.LFGMgr.SetLeader(gguid, guid);
		}
		else
		{
			var gstate = Global.LFGMgr.GetState(gguid);
			var state = Global.LFGMgr.GetState(guid);
			Log.Logger.Debug("LFGScripts.OnAddMember [{0}]: added [{1} leader {2}] gstate: {3}, state: {4}", gguid, guid, leader, gstate, state);

			if (state == LfgState.Queued)
				Global.LFGMgr.LeaveLfg(guid);

			if (gstate == LfgState.Queued)
				Global.LFGMgr.LeaveLfg(gguid);
		}

		Global.LFGMgr.SetGroup(guid, gguid);
		Global.LFGMgr.AddPlayerToGroup(gguid, guid);
	}

	public void OnChangeLeader(PlayerGroup group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		var gguid = group.GUID;

		Log.Logger.Debug("LFGScripts.OnChangeLeader {0}: old {0} new {0}", gguid, newLeaderGuid, oldLeaderGuid);
		Global.LFGMgr.SetLeader(gguid, newLeaderGuid);
	}

	public void OnDisband(PlayerGroup group)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		var gguid = group.GUID;
		Log.Logger.Debug("LFGScripts.OnDisband {0}", gguid);

		Global.LFGMgr.RemoveGroupData(gguid);
	}

	public void OnInviteMember(PlayerGroup group, ObjectGuid guid)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		var gguid = group.GUID;
		var leader = group.LeaderGUID;
		Log.Logger.Debug("LFGScripts.OnInviteMember {0}: invite {0} leader {0}", gguid, guid, leader);

		// No gguid ==  new group being formed
		// No leader == after group creation first invite is new leader
		// leader and no gguid == first invite after leader is added to new group (this is the real invite)
		if (!leader.IsEmpty && gguid.IsEmpty)
			Global.LFGMgr.LeaveLfg(leader);
	}

	public void OnRemoveMember(PlayerGroup group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason)
	{
		if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
			return;

		var gguid = group.GUID;
		Log.Logger.Debug("LFGScripts.OnRemoveMember [{0}]: remove [{1}] Method: {2} Kicker: {3} Reason: {4}", gguid, guid, method, kicker, reason);

		var isLFG = group.IsLFGGroup;

		if (isLFG && method == RemoveMethod.Kick) // Player have been kicked
		{
			// @todo - Update internal kick cooldown of kicker
			var str_reason = "";

			if (!string.IsNullOrEmpty(reason))
				str_reason = reason;

			Global.LFGMgr.InitBoot(gguid, kicker, guid, str_reason);

			return;
		}

		var state = Global.LFGMgr.GetState(gguid);

		// If group is being formed after proposal success do nothing more
		if (state == LfgState.Proposal && method == RemoveMethod.Default)
		{
			// LfgData: Remove player from group
			Global.LFGMgr.SetGroup(guid, ObjectGuid.Empty);
			Global.LFGMgr.RemovePlayerFromGroup(gguid, guid);

			return;
		}

		Global.LFGMgr.LeaveLfg(guid);
		Global.LFGMgr.SetGroup(guid, ObjectGuid.Empty);
		var players = Global.LFGMgr.RemovePlayerFromGroup(gguid, guid);

		var player = Global.ObjAccessor.FindPlayer(guid);

		if (player)
		{
			if (method == RemoveMethod.Leave &&
				state == LfgState.Dungeon &&
				players >= SharedConst.LFGKickVotesNeeded)
				player.CastSpell(player, SharedConst.LFGSpellDungeonDeserter, true);
			else if (method == RemoveMethod.KickLFG)
				player.RemoveAura(SharedConst.LFGSpellDungeonCooldown);
			//else if (state == LFG_STATE_BOOT)
			// Update internal kick cooldown of kicked

			player. //else if (state == LFG_STATE_BOOT)
				// Update internal kick cooldown of kicked
				Session.SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.LeaderUnk1), true);

			if (isLFG && player.Map.IsDungeon) // Teleport player out the dungeon
				Global.LFGMgr.TeleportPlayer(player, true);
		}

		if (isLFG && state != LfgState.FinishedDungeon) // Need more players to finish the dungeon
		{
			var leader = Global.ObjAccessor.FindPlayer(Global.LFGMgr.GetLeader(gguid));

			if (leader)
				leader.Session.SendLfgOfferContinue(Global.LFGMgr.GetDungeon(gguid, false));
		}
	}
}