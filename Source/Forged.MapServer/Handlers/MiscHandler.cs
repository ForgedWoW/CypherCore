// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IConversation;
using Game.Scripting.Interfaces.IPlayer;

namespace Game;

public partial class WorldSession
{
	public void SendLoadCUFProfiles()
	{
		var player = Player;

		LoadCUFProfiles loadCUFProfiles = new();

		for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
		{
			var cufProfile = player.GetCUFProfile(i);

			if (cufProfile != null)
				loadCUFProfiles.CUFProfiles.Add(cufProfile);
		}

		SendPacket(loadCUFProfiles);
	}

	[WorldPacketHandler(ClientOpcodes.RequestAccountData, Status = SessionStatus.Authed)]
	void HandleRequestAccountData(RequestAccountData request)
	{
		if (request.DataType > AccountDataTypes.Max)
			return;

		var adata = GetAccountData(request.DataType);

		UpdateAccountData data = new();
		data.Player = Player ? Player.GUID : ObjectGuid.Empty;
		data.Time = (uint)adata.Time;
		data.DataType = request.DataType;

		if (!adata.Data.IsEmpty())
		{
			data.Size = (uint)adata.Data.Length;
			data.CompressedData = new ByteBuffer(ZLib.Compress(Encoding.UTF8.GetBytes(adata.Data)));
		}

		SendPacket(data);
	}

	[WorldPacketHandler(ClientOpcodes.SetSelection)]
	void HandleSetSelection(SetSelection packet)
	{
		Player.SetSelection(packet.Selection);
	}

	[WorldPacketHandler(ClientOpcodes.ObjectUpdateFailed, Processing = PacketProcessing.Inplace)]
	void HandleObjectUpdateFailed(ObjectUpdateFailed objectUpdateFailed)
	{
		Log.outError(LogFilter.Network, "Object update failed for {0} for player {1} ({2})", objectUpdateFailed.ObjectGUID.ToString(), PlayerName, Player.GUID.ToString());

		// If create object failed for current player then client will be stuck on loading screen
		if (Player.GUID == objectUpdateFailed.ObjectGUID)
		{
			LogoutPlayer(true);

			return;
		}

		// Pretend we've never seen this object
		Player.ClientGuiDs.Remove(objectUpdateFailed.ObjectGUID);
	}

	[WorldPacketHandler(ClientOpcodes.ObjectUpdateRescued, Processing = PacketProcessing.Inplace)]
	void HandleObjectUpdateRescued(ObjectUpdateRescued objectUpdateRescued)
	{
		Log.outError(LogFilter.Network, "Object update rescued for {0} for player {1} ({2})", objectUpdateRescued.ObjectGUID.ToString(), PlayerName, Player.GUID.ToString());

		// Client received values update after destroying object
		// re-register object in m_clientGUIDs to send DestroyObject on next visibility update
		lock (Player.ClientGuiDs)
		{
			Player.ClientGuiDs.Add(objectUpdateRescued.ObjectGUID);
		}
	}


	[WorldPacketHandler(ClientOpcodes.SetActionBarToggles)]
	void HandleSetActionBarToggles(SetActionBarToggles packet)
	{
		if (!Player) // ignore until not logged (check needed because STATUS_AUTHED)
		{
			if (packet.Mask != 0)
				Log.outError(LogFilter.Network, "WorldSession.HandleSetActionBarToggles in not logged state with value: {0}, ignored", packet.Mask);

			return;
		}

		Player.SetMultiActionBars(packet.Mask);
	}

	[WorldPacketHandler(ClientOpcodes.CompleteCinematic)]
	void HandleCompleteCinematic(CompleteCinematic packet)
	{
		// If player has sight bound to visual waypoint NPC we should remove it
		Player. // If player has sight bound to visual waypoint NPC we should remove it
			CinematicMgr.EndCinematic();
	}

	[WorldPacketHandler(ClientOpcodes.NextCinematicCamera)]
	void HandleNextCinematicCamera(NextCinematicCamera packet)
	{
		// Sent by client when cinematic actually begun. So we begin the server side process
		Player. // Sent by client when cinematic actually begun. So we begin the server side process
			CinematicMgr.NextCinematicCamera();
	}

	[WorldPacketHandler(ClientOpcodes.CompleteMovie)]
	void HandleCompleteMovie(CompleteMovie packet)
	{
		var movie = _player.Movie;

		if (movie == 0)
			return;

		_player.Movie = 0;
		Global.ScriptMgr.ForEach<IPlayerOnMovieComplete>(p => p.OnMovieComplete(_player, movie));
	}

	[WorldPacketHandler(ClientOpcodes.ViolenceLevel, Processing = PacketProcessing.Inplace, Status = SessionStatus.Authed)]
	void HandleViolenceLevel(ViolenceLevel violenceLevel)
	{
		// do something?
	}

	[WorldPacketHandler(ClientOpcodes.AreaTrigger, Processing = PacketProcessing.Inplace)]
	void HandleAreaTrigger(AreaTriggerPkt packet)
	{
		var player = Player;

		if (player.IsInFlight)
		{
			Log.outDebug(LogFilter.Network,
						"HandleAreaTrigger: Player '{0}' (GUID: {1}) in flight, ignore Area Trigger ID:{2}",
						player.GetName(),
						player.GUID.ToString(),
						packet.AreaTriggerID);

			return;
		}

		var atEntry = CliDB.AreaTriggerStorage.LookupByKey(packet.AreaTriggerID);

		if (atEntry == null)
		{
			Log.outDebug(LogFilter.Network,
						"HandleAreaTrigger: Player '{0}' (GUID: {1}) send unknown (by DBC) Area Trigger ID:{2}",
						player.GetName(),
						player.GUID.ToString(),
						packet.AreaTriggerID);

			return;
		}

		if (packet.Entered && !player.IsInAreaTriggerRadius(atEntry))
		{
			Log.outDebug(LogFilter.Network,
						"HandleAreaTrigger: Player '{0}' ({1}) too far, ignore Area Trigger ID: {2}",
						player.GetName(),
						player.GUID.ToString(),
						packet.AreaTriggerID);

			return;
		}

		if (player.IsDebugAreaTriggers)
			player.SendSysMessage(packet.Entered ? CypherStrings.DebugAreatriggerEntered : CypherStrings.DebugAreatriggerLeft, packet.AreaTriggerID);

		if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.AreatriggerClientTriggered, atEntry.Id, player))
			return;

		if (Global.ScriptMgr.OnAreaTrigger(player, atEntry, packet.Entered))
			return;

		if (player.IsAlive)
		{
			// not using Player.UpdateQuestObjectiveProgress, ObjectID in quest_objectives can be set to -1, areatrigger_involvedrelation then holds correct id
			var quests = Global.ObjectMgr.GetQuestsForAreaTrigger(packet.AreaTriggerID);

			if (quests != null)
			{
				var anyObjectiveChangedCompletionState = false;

				foreach (var questId in quests)
				{
					var qInfo = Global.ObjectMgr.GetQuestTemplate(questId);
					var slot = player.FindQuestSlot(questId);

					if (qInfo != null && slot < SharedConst.MaxQuestLogSize && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
					{
						foreach (var obj in qInfo.Objectives)
						{
							if (obj.Type != QuestObjectiveType.AreaTrigger)
								continue;

							if (!player.IsQuestObjectiveCompletable(slot, qInfo, obj))
								continue;

							if (player.IsQuestObjectiveComplete(slot, qInfo, obj))
								continue;

							if (obj.ObjectID != -1 && obj.ObjectID != packet.AreaTriggerID)
								continue;

							player.SetQuestObjectiveData(obj, 1);
							player.SendQuestUpdateAddCreditSimple(obj);
							anyObjectiveChangedCompletionState = true;

							break;
						}

						player.AreaExploredOrEventHappens(questId);

						if (player.CanCompleteQuest(questId))
							player.CompleteQuest(questId);
					}
				}

				if (anyObjectiveChangedCompletionState)
					player.UpdateVisibleGameobjectsOrSpellClicks();
			}
		}

		if (Global.ObjectMgr.IsTavernAreaTrigger(packet.AreaTriggerID))
		{
			// set resting flag we are in the inn
			player. // set resting flag we are in the inn
				RestMgr.SetRestFlag(RestFlag.Tavern, atEntry.Id);

			if (Global.WorldMgr.IsFFAPvPRealm)
				player.RemovePvpFlag(UnitPVPStateFlags.FFAPvp);

			return;
		}

		var bg = player.Battleground;

		if (bg)
			bg.HandleAreaTrigger(player, packet.AreaTriggerID, packet.Entered);

		var pvp = player.GetOutdoorPvP();

		if (pvp != null)
			if (pvp.HandleAreaTrigger(player, packet.AreaTriggerID, packet.Entered))
				return;

		var at = Global.ObjectMgr.GetAreaTrigger(packet.AreaTriggerID);

		if (at == null)
			return;

		var teleported = false;

		if (player.Location.MapId != at.target_mapId)
		{
			if (!player.IsAlive)
			{
				if (player.HasCorpse)
				{
					// let enter in ghost mode in instance that connected to inner instance with corpse
					var corpseMap = player.CorpseLocation.MapId;

					do
					{
						if (corpseMap == at.target_mapId)
							break;

						var corpseInstance = Global.ObjectMgr.GetInstanceTemplate(corpseMap);
						corpseMap = corpseInstance != null ? corpseInstance.Parent : 0;
					} while (corpseMap != 0);

					if (corpseMap == 0)
					{
						SendPacket(new AreaTriggerNoCorpse());

						return;
					}

					Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' has corpse in instance {at.target_mapId} and can enter.");
				}
				else
				{
					Log.outDebug(LogFilter.Maps, $"Map::CanPlayerEnter - player '{player.GetName()}' is dead but does not have a corpse!");
				}
			}

			var denyReason = Map.PlayerCannotEnter(at.target_mapId, player);

			if (denyReason != null)
			{
				switch (denyReason.Reason)
				{
					case TransferAbortReason.MapNotAllowed:
						Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' attempted to enter map with id {at.target_mapId} which has no entry");

						break;
					case TransferAbortReason.Difficulty:
						Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' attempted to enter instance map {at.target_mapId} but the requested difficulty was not found");

						break;
					case TransferAbortReason.NeedGroup:
						Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' must be in a raid group to enter map {at.target_mapId}");
						player.SendRaidGroupOnlyMessage(RaidGroupReason.Only, 0);

						break;
					case TransferAbortReason.LockedToDifferentInstance:
						Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' cannot enter instance map {at.target_mapId} because their permanent bind is incompatible with their group's");

						break;
					case TransferAbortReason.AlreadyCompletedEncounter:
						Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' cannot enter instance map {at.target_mapId} because their permanent bind is incompatible with their group's");

						break;
					case TransferAbortReason.TooManyInstances:
						Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' cannot enter instance map {1} because he has exceeded the maximum number of instances per hour.", player.GetName(), at.target_mapId);

						break;
					case TransferAbortReason.MaxPlayers:
					case TransferAbortReason.ZoneInCombat:
						break;
					case TransferAbortReason.NotFound:
						Log.outDebug(LogFilter.Maps, $"MAP: Player '{player.GetName()}' cannot enter instance map {at.target_mapId} because instance is resetting.");

						break;
					default:
						break;
				}

				if (denyReason.Reason != TransferAbortReason.NeedGroup)
					player.SendTransferAborted(at.target_mapId, denyReason.Reason, denyReason.Arg, denyReason.MapDifficultyXConditionId);

				if (!player.IsAlive && player.HasCorpse)
					if (player.CorpseLocation.MapId == at.target_mapId)
					{
						player.ResurrectPlayer(0.5f);
						player.SpawnCorpseBones();
					}

				return;
			}

			var group = player.Group;

			if (group)
				if (group.IsLFGGroup && player.Map.IsDungeon)
					teleported = player.TeleportToBGEntryPoint();
		}

		if (!teleported)
		{
			WorldSafeLocsEntry entranceLocation = null;
			var mapEntry = CliDB.MapStorage.LookupByKey(at.target_mapId);

			if (mapEntry.Instanceable())
			{
				// Check if we can contact the instancescript of the instance for an updated entrance location
				var targetInstanceId = Global.MapMgr.FindInstanceIdForPlayer(at.target_mapId, _player);

				if (targetInstanceId != 0)
				{
					var map = Global.MapMgr.FindMap(at.target_mapId, targetInstanceId);

					if (map != null)
					{
						var instanceMap = map.ToInstanceMap;

						if (instanceMap)
						{
							var instanceScript = instanceMap.InstanceScript;

							if (instanceScript != null)
								entranceLocation = Global.ObjectMgr.GetWorldSafeLoc(instanceScript.GetEntranceLocation());
						}
					}
				}

				// Finally check with the instancesave for an entrance location if we did not get a valid one from the instancescript
				if (entranceLocation == null)
				{
					var group = player.Group;
					var difficulty = group ? group.GetDifficultyID(mapEntry) : player.GetDifficultyId(mapEntry);
					var instanceOwnerGuid = group ? group.GetRecentInstanceOwner(at.target_mapId) : player.GUID;
					var instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(instanceOwnerGuid, new MapDb2Entries(mapEntry, Global.DB2Mgr.GetDownscaledMapDifficultyData(at.target_mapId, ref difficulty)));

					if (instanceLock != null)
						entranceLocation = Global.ObjectMgr.GetWorldSafeLoc(instanceLock.GetData().EntranceWorldSafeLocId);
				}
			}

			if (entranceLocation != null)
				player.TeleportTo(entranceLocation.Loc, TeleportToOptions.NotLeaveTransport);
			else
				player.TeleportTo(at.target_mapId, at.target_X, at.target_Y, at.target_Z, at.target_Orientation, TeleportToOptions.NotLeaveTransport);
		}
	}

	[WorldPacketHandler(ClientOpcodes.SaveCufProfiles, Processing = PacketProcessing.Inplace)]
	void HandleSaveCUFProfiles(SaveCUFProfiles packet)
	{
		if (packet.CUFProfiles.Count > PlayerConst.MaxCUFProfiles)
		{
			Log.outError(LogFilter.Player, "HandleSaveCUFProfiles - {0} tried to save more than {1} CUF profiles. Hacking attempt?", PlayerName, PlayerConst.MaxCUFProfiles);

			return;
		}

		for (byte i = 0; i < packet.CUFProfiles.Count; ++i)
			Player.SaveCUFProfile(i, packet.CUFProfiles[i]);

		for (var i = (byte)packet.CUFProfiles.Count; i < PlayerConst.MaxCUFProfiles; ++i)
			Player.SaveCUFProfile(i, null);
	}

	[WorldPacketHandler(ClientOpcodes.SetAdvancedCombatLogging, Processing = PacketProcessing.Inplace)]
	void HandleSetAdvancedCombatLogging(SetAdvancedCombatLogging setAdvancedCombatLogging)
	{
		Player.SetAdvancedCombatLogging(setAdvancedCombatLogging.Enable);
	}

	[WorldPacketHandler(ClientOpcodes.MountSpecialAnim)]
	void HandleMountSpecialAnim(MountSpecial mountSpecial)
	{
		SpecialMountAnim specialMountAnim = new();
		specialMountAnim.UnitGUID = _player.GUID;
		specialMountAnim.SpellVisualKitIDs.AddRange(mountSpecial.SpellVisualKitIDs);
		specialMountAnim.SequenceVariation = mountSpecial.SequenceVariation;
		Player.SendMessageToSet(specialMountAnim, false);
	}

	[WorldPacketHandler(ClientOpcodes.CloseInteraction)]
	void HandleCloseInteraction(CloseInteraction closeInteraction)
	{
		if (_player.PlayerTalkClass.GetInteractionData().SourceGuid == closeInteraction.SourceGuid)
			_player.PlayerTalkClass.GetInteractionData().Reset();
	}

	[WorldPacketHandler(ClientOpcodes.ConversationLineStarted)]
	void HandleConversationLineStarted(ConversationLineStarted conversationLineStarted)
	{
		var convo = ObjectAccessor.GetConversation(_player, conversationLineStarted.ConversationGUID);

		if (convo != null)
			Global.ScriptMgr.RunScript<IConversationOnConversationLineStarted>(script => script.OnConversationLineStarted(convo, conversationLineStarted.LineID, _player), convo.GetScriptId());
	}

	[WorldPacketHandler(ClientOpcodes.RequestLatestSplashScreen)]
	void HandleRequestLatestSplashScreen(RequestLatestSplashScreen requestLatestSplashScreen)
	{
		UISplashScreenRecord splashScreen = null;

		foreach (var itr in CliDB.UISplashScreenStorage.Values)
		{
			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(itr.CharLevelConditionID);

			if (playerCondition != null)
				if (!ConditionManager.IsPlayerMeetingCondition(_player, playerCondition))
					continue;

			splashScreen = itr;
		}

		SplashScreenShowLatest splashScreenShowLatest = new();
		splashScreenShowLatest.UISplashScreenID = splashScreen != null ? splashScreen.Id : 0;
		SendPacket(splashScreenShowLatest);
	}

	[WorldPacketHandler(ClientOpcodes.ChatUnregisterAllAddonPrefixes)]
	void HandleUnregisterAllAddonPrefixes(ChatUnregisterAllAddonPrefixes packet)
	{
		_registeredAddonPrefixes.Clear();
	}

	[WorldPacketHandler(ClientOpcodes.TogglePvp)]
	void HandleTogglePvP(TogglePvP packet)
	{
		if (!Player.HasPlayerFlag(PlayerFlags.InPVP))
		{
			Player.SetPlayerFlag(PlayerFlags.InPVP);
			Player.RemovePlayerFlag(PlayerFlags.PVPTimer);

			if (!Player.IsPvP || Player.PvpInfo.EndTimer != 0)
				Player.UpdatePvP(true, true);
		}
		else if (!Player.IsWarModeLocalActive)
		{
			Player.RemovePlayerFlag(PlayerFlags.InPVP);
			Player.SetPlayerFlag(PlayerFlags.PVPTimer);

			if (!Player.PvpInfo.IsHostile && Player.IsPvP)
				Player.PvpInfo.EndTimer = GameTime.GetGameTime(); // start toggle-off
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetPvp)]
	void HandleSetPvP(SetPvP packet)
	{
		if (packet.EnablePVP)
		{
			Player.SetPlayerFlag(PlayerFlags.InPVP);
			Player.RemovePlayerFlag(PlayerFlags.PVPTimer);

			if (!Player.IsPvP || Player.PvpInfo.EndTimer != 0)
				Player.UpdatePvP(true, true);
		}
		else if (!Player.IsWarModeLocalActive)
		{
			Player.RemovePlayerFlag(PlayerFlags.InPVP);
			Player.SetPlayerFlag(PlayerFlags.PVPTimer);

			if (!Player.PvpInfo.IsHostile && Player.IsPvP)
				Player.PvpInfo.EndTimer = GameTime.GetGameTime(); // start toggle-off
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetWarMode)]
	void HandleSetWarMode(SetWarMode packet)
	{
		_player.SetWarModeDesired(packet.Enable);
	}

	[WorldPacketHandler(ClientOpcodes.FarSight)]
	void HandleFarSight(FarSight farSight)
	{
		if (farSight.Enable)
		{
			Log.outDebug(LogFilter.Network, "Added FarSight {0} to player {1}", Player.ActivePlayerData.FarsightObject.ToString(), Player.GUID.ToString());
			var target = Player.Viewpoint;

			if (target)
				Player.SetSeer(target);
			else
				Log.outDebug(LogFilter.Network, "Player {0} (GUID: {1}) requests non-existing seer {2}", Player.GetName(), Player.GUID.ToString(), Player.ActivePlayerData.FarsightObject.ToString());
		}
		else
		{
			Log.outDebug(LogFilter.Network, "Player {0} set vision to self", Player.GUID.ToString());
			Player.SetSeer(Player);
		}

		Player.UpdateVisibilityForPlayer();
	}

	[WorldPacketHandler(ClientOpcodes.SetTitle, Processing = PacketProcessing.Inplace)]
	void HandleSetTitle(SetTitle packet)
	{
		// -1 at none
		if (packet.TitleID > 0)
		{
			if (!Player.HasTitle((uint)packet.TitleID))
				return;
		}
		else
		{
			packet.TitleID = 0;
		}

		Player.SetChosenTitle((uint)packet.TitleID);
	}

	[WorldPacketHandler(ClientOpcodes.ResetInstances)]
	void HandleResetInstances(ResetInstances packet)
	{
		var map = _player.Map;

		if (map != null && map.Instanceable)
			return;

		var group = Player.Group;

		if (group)
		{
			if (!group.IsLeader(Player.GUID))
				return;

			if (group.IsLFGGroup)
				return;

			group.ResetInstances(InstanceResetMethod.Manual, _player);
		}
		else
		{
			Player.ResetInstances(InstanceResetMethod.Manual);
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetTaxiBenchmarkMode, Processing = PacketProcessing.Inplace)]
	void HandleSetTaxiBenchmark(SetTaxiBenchmarkMode packet)
	{
		if (packet.Enable)
			_player.SetPlayerFlag(PlayerFlags.TaxiBenchmark);
		else
			_player.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
	}

	[WorldPacketHandler(ClientOpcodes.GuildSetFocusedAchievement)]
	void HandleGuildSetFocusedAchievement(GuildSetFocusedAchievement setFocusedAchievement)
	{
		var guild = Global.GuildMgr.GetGuildById(Player.GuildId);

		if (guild)
			guild.GetAchievementMgr().SendAchievementInfo(Player, setFocusedAchievement.AchievementID);
	}

	[WorldPacketHandler(ClientOpcodes.InstanceLockResponse)]
	void HandleInstanceLockResponse(InstanceLockResponse packet)
	{
		if (!Player.HasPendingBind)
		{
			Log.outInfo(LogFilter.Network,
						"InstanceLockResponse: Player {0} (guid {1}) tried to bind himself/teleport to graveyard without a pending bind!",
						Player.GetName(),
						Player.GUID.ToString());

			return;
		}

		if (packet.AcceptLock)
			Player.ConfirmPendingBind();
		else
			Player.RepopAtGraveyard();

		Player.SetPendingBind(0, 0);
	}

	[WorldPacketHandler(ClientOpcodes.Warden3Data)]
	void HandleWarden3Data(WardenData packet)
	{
		if (_warden == null || packet.Data.GetSize() == 0)
			return;

		_warden.DecryptData(packet.Data.GetData());
		var opcode = (WardenOpcodes)packet.Data.ReadUInt8();

		switch (opcode)
		{
			case WardenOpcodes.CmsgModuleMissing:
				_warden.SendModuleToClient();

				break;
			case WardenOpcodes.CmsgModuleOk:
				_warden.RequestHash();

				break;
			case WardenOpcodes.SmsgCheatChecksRequest:
				_warden.HandleData(packet.Data);

				break;
			case WardenOpcodes.CmsgMemChecksResult:
				Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");

				break;
			case WardenOpcodes.CmsgHashResult:
				_warden.HandleHashResult(packet.Data);
				_warden.InitializeModule();

				break;
			case WardenOpcodes.CmsgModuleFailed:
				Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MODULE_FAILED received!");

				break;
			default:
				Log.outDebug(LogFilter.Warden, "Got unknown warden opcode {0} of size {1}.", opcode, packet.Data.GetSize() - 1);

				break;
		}
	}
}