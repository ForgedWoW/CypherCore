// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Framework.Constants;
using Framework.IO;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Forged.RealmServer.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Forged.RealmServer.Scripting.Interfaces.IConversation;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;

namespace Forged.RealmServer;

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

	[WorldPacketHandler(ClientOpcodes.UpdateAccountData, Status = SessionStatus.Authed)]
	void HandleUpdateAccountData(UserClientUpdateAccountData packet)
	{
		if (packet.DataType >= AccountDataTypes.Max)
			return;

		if (packet.Size == 0)
		{
			SetAccountData(packet.DataType, 0, "");

			return;
		}

		if (packet.Size > 0xFFFF)
		{
			Log.outError(LogFilter.Network, "UpdateAccountData: Account data packet too big, size {0}", packet.Size);

			return;
		}

		var data = ZLib.Decompress(packet.CompressedData.GetData(), packet.Size);
		SetAccountData(packet.DataType, packet.Time, Encoding.Default.GetString(data));
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

	[WorldPacketHandler(ClientOpcodes.SetActionButton)]
	void HandleSetActionButton(SetActionButton packet)
	{
		ulong action = packet.GetButtonAction();
		var type = packet.GetButtonType();

		if (packet.Action == 0)
			Player.RemoveActionButton(packet.Index);
		else
			Player.AddActionButton(packet.Index, action, type);
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

	[WorldPacketHandler(ClientOpcodes.MountSetFavorite)]
	void HandleMountSetFavorite(MountSetFavorite mountSetFavorite)
	{
		_collectionMgr.MountSetFavorite(mountSetFavorite.MountSpellID, mountSetFavorite.IsFavorite);
	}

	[WorldPacketHandler(ClientOpcodes.ChatRegisterAddonPrefixes)]
	void HandleAddonRegisteredPrefixes(ChatRegisterAddonPrefixes packet)
	{
		_registeredAddonPrefixes.AddRange(packet.Prefixes);

		if (_registeredAddonPrefixes.Count > 64) // shouldn't happen
		{
			_filterAddonMessages = false;

			return;
		}

		_filterAddonMessages = true;
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

	[WorldPacketHandler(ClientOpcodes.SetDungeonDifficulty)]
	void HandleSetDungeonDifficulty(SetDungeonDifficulty setDungeonDifficulty)
	{
		var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(setDungeonDifficulty.DifficultyID);

		if (difficultyEntry == null)
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an invalid instance mode {1}!",
						Player.GUID.ToString(),
						setDungeonDifficulty.DifficultyID);

			return;
		}

		if (difficultyEntry.InstanceType != MapTypes.Instance)
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an non-dungeon instance mode {1}!",
						Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
						Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		var difficultyID = (Difficulty)difficultyEntry.Id;

		if (difficultyID == Player.DungeonDifficultyId)
			return;

		// cannot reset while in an instance
		var map = Player.Map;

		if (map && map.Instanceable)
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession:HandleSetDungeonDifficulty: player (Name: {0}, {1}) tried to reset the instance while player is inside!",
						Player.GetName(),
						Player.GUID.ToString());

			return;
		}

		var group = Player.Group;

		if (group)
		{
			if (!group.IsLeader(_player.GUID))
				return;

			if (group.IsLFGGroup)
				return;

			// the difficulty is set even if the instances can't be reset
			group.ResetInstances(InstanceResetMethod.OnChangeDifficulty, _player);
			group.SetDungeonDifficultyID(difficultyID);
		}
		else
		{
			Player.ResetInstances(InstanceResetMethod.OnChangeDifficulty);
			Player.DungeonDifficultyId = difficultyID;
			Player.SendDungeonDifficulty();
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetRaidDifficulty)]
	void HandleSetRaidDifficulty(SetRaidDifficulty setRaidDifficulty)
	{
		var difficultyEntry = CliDB.DifficultyStorage.LookupByKey(setRaidDifficulty.DifficultyID);

		if (difficultyEntry == null)
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an invalid instance mode {1}!",
						Player.GUID.ToString(),
						setRaidDifficulty.DifficultyID);

			return;
		}

		if (difficultyEntry.InstanceType != MapTypes.Raid)
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an non-dungeon instance mode {1}!",
						Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
						Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		if (((int)(difficultyEntry.Flags & DifficultyFlags.Legacy) != 0) != (setRaidDifficulty.Legacy != 0))
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession.HandleSetDungeonDifficulty: {0} sent not matching legacy difficulty {1}!",
						Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		var difficultyID = (Difficulty)difficultyEntry.Id;

		if (difficultyID == (setRaidDifficulty.Legacy != 0 ? Player.LegacyRaidDifficultyId : Player.RaidDifficultyId))
			return;

		// cannot reset while in an instance
		var map = Player.Map;

		if (map && map.Instanceable)
		{
			Log.outDebug(LogFilter.Network,
						"WorldSession:HandleSetRaidDifficulty: player (Name: {0}, {1} tried to reset the instance while inside!",
						Player.GetName(),
						Player.GUID.ToString());

			return;
		}

		var group = Player.Group;

		if (group)
		{
			if (!group.IsLeader(_player.GUID))
				return;

			if (group.IsLFGGroup)
				return;

			// the difficulty is set even if the instances can't be reset
			group.ResetInstances(InstanceResetMethod.OnChangeDifficulty, _player);

			if (setRaidDifficulty.Legacy != 0)
				group.SetLegacyRaidDifficultyID(difficultyID);
			else
				group.SetRaidDifficultyID(difficultyID);
		}
		else
		{
			Player.ResetInstances(InstanceResetMethod.OnChangeDifficulty);

			if (setRaidDifficulty.Legacy != 0)
				Player.LegacyRaidDifficultyId = difficultyID;
			else
				Player.RaidDifficultyId = difficultyID;

			Player.SendRaidDifficulty(setRaidDifficulty.Legacy != 0);
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
}