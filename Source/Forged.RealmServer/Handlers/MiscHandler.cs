// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Framework.Constants;
using Framework.IO;
using Game.Common.Handlers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Forged.RealmServer;

public class MiscHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly CliDB _cliDb;
    private readonly CollectionMgr _collectionMgr;
    private readonly GuildManager _guildManager;
    private readonly GameTime _gameTime;

    public MiscHandler(WorldSession session, CliDB cliDb, CollectionMgr collectionMgr,
		GuildManager guildManager, GameTime gameTime)
    {
        _session = session;
        _cliDb = cliDb;
        _collectionMgr = collectionMgr;
        _guildManager = guildManager;
        _gameTime = gameTime;
    }

    public void SendLoadCUFProfiles()
	{
		var player = _session.Player;

		LoadCUFProfiles loadCUFProfiles = new();

		for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
		{
			var cufProfile = player.GetCUFProfile(i);

			if (cufProfile != null)
				loadCUFProfiles.CUFProfiles.Add(cufProfile);
		}

        _session.SendPacket(loadCUFProfiles);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateAccountData, Status = SessionStatus.Authed)]
	void HandleUpdateAccountData(UserClientUpdateAccountData packet)
	{
		if (packet.DataType >= AccountDataTypes.Max)
			return;

		if (packet.Size == 0)
		{
            _session.SetAccountData(packet.DataType, 0, "");

			return;
		}

		if (packet.Size > 0xFFFF)
		{
			Log.Logger.Error("UpdateAccountData: Account data packet too big, size {0}", packet.Size);

			return;
		}

		var data = ZLib.Decompress(packet.CompressedData.GetData(), packet.Size);
        _session.SetAccountData(packet.DataType, packet.Time, Encoding.Default.GetString(data));
	}

	[WorldPacketHandler(ClientOpcodes.ObjectUpdateFailed, Processing = PacketProcessing.Inplace)]
	void HandleObjectUpdateFailed(ObjectUpdateFailed objectUpdateFailed)
	{
		Log.Logger.Error("Object update failed for {0} for player {1} ({2})", objectUpdateFailed.ObjectGUID.ToString(), _session.PlayerName, _session.Player.GUID.ToString());

		// If create object failed for current player then client will be stuck on loading screen
		if (_session.Player.GUID == objectUpdateFailed.ObjectGUID)
		{
            _session.LogoutPlayer(true);

			return;
		}

        // Pretend we've never seen this object
        _session.Player.ClientGuiDs.Remove(objectUpdateFailed.ObjectGUID);
	}

	[WorldPacketHandler(ClientOpcodes.ObjectUpdateRescued, Processing = PacketProcessing.Inplace)]
	void HandleObjectUpdateRescued(ObjectUpdateRescued objectUpdateRescued)
	{
		Log.Logger.Error("Object update rescued for {0} for player {1} ({2})", objectUpdateRescued.ObjectGUID.ToString(), _session.PlayerName, _session.Player.GUID.ToString());

		// Client received values update after destroying object
		// re-register object in m_clientGUIDs to send DestroyObject on next visibility update
		lock (_session.Player.ClientGuiDs)
		{
            _session.Player.ClientGuiDs.Add(objectUpdateRescued.ObjectGUID);
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetActionButton)]
	void HandleSetActionButton(SetActionButton packet)
	{
		ulong action = packet.GetButtonAction();
		var type = packet.GetButtonType();

		if (packet.Action == 0)
            _session.Player.RemoveActionButton(packet.Index);
		else
            _session.Player.AddActionButton(packet.Index, action, type);
	}

	[WorldPacketHandler(ClientOpcodes.SaveCufProfiles, Processing = PacketProcessing.Inplace)]
	void HandleSaveCUFProfiles(SaveCUFProfiles packet)
	{
		if (packet.CUFProfiles.Count > PlayerConst.MaxCUFProfiles)
		{
			Log.Logger.Error("HandleSaveCUFProfiles - {0} tried to save more than {1} CUF profiles. Hacking attempt?", _session.PlayerName, PlayerConst.MaxCUFProfiles);

			return;
		}

		for (byte i = 0; i < packet.CUFProfiles.Count; ++i)
            _session.Player.SaveCUFProfile(i, packet.CUFProfiles[i]);

		for (var i = (byte)packet.CUFProfiles.Count; i < PlayerConst.MaxCUFProfiles; ++i)
            _session.Player.SaveCUFProfile(i, null);
	}

	[WorldPacketHandler(ClientOpcodes.SetAdvancedCombatLogging, Processing = PacketProcessing.Inplace)]
	void HandleSetAdvancedCombatLogging(SetAdvancedCombatLogging setAdvancedCombatLogging)
	{
        _session.Player.SetAdvancedCombatLogging(setAdvancedCombatLogging.Enable);
	}

	[WorldPacketHandler(ClientOpcodes.MountSetFavorite)]
	void HandleMountSetFavorite(MountSetFavorite mountSetFavorite)
	{
		_collectionMgr.MountSetFavorite(mountSetFavorite.MountSpellID, mountSetFavorite.IsFavorite);
	}

	[WorldPacketHandler(ClientOpcodes.ChatRegisterAddonPrefixes)]
	void HandleAddonRegisteredPrefixes(ChatRegisterAddonPrefixes packet)
	{
        _session.RegisteredAddonPrefixes.AddRange(packet.Prefixes);

		if (_session.RegisteredAddonPrefixes.Count > 64) // shouldn't happen
		{
            _session.FilterAddonMessages = false;

			return;
		}

        _session.FilterAddonMessages = true;
	}

	[WorldPacketHandler(ClientOpcodes.TogglePvp)]
	void HandleTogglePvP(TogglePvP packet)
	{
		if (!_session.Player.HasPlayerFlag(PlayerFlags.InPVP))
		{
            _session.Player.SetPlayerFlag(PlayerFlags.InPVP);
            _session.Player.RemovePlayerFlag(PlayerFlags.PVPTimer);

			if (!_session.Player.IsPvP || _session.Player.PvpInfo.EndTimer != 0)
                _session.Player.UpdatePvP(true, true);
		}
		else if (!_session.Player.IsWarModeLocalActive)
		{
            _session.Player.RemovePlayerFlag(PlayerFlags.InPVP);
            _session.Player.SetPlayerFlag(PlayerFlags.PVPTimer);

			if (!_session.Player.PvpInfo.IsHostile && _session.Player.IsPvP)
                _session.Player.PvpInfo.EndTimer = _gameTime.CurrentGameTime; // start toggle-off
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetPvp)]
	void HandleSetPvP(SetPvP packet)
	{
		if (packet.EnablePVP)
		{
            _session.Player.SetPlayerFlag(PlayerFlags.InPVP);
            _session.Player.RemovePlayerFlag(PlayerFlags.PVPTimer);

			if (!_session.Player.IsPvP || _session.Player.PvpInfo.EndTimer != 0)
                _session.Player.UpdatePvP(true, true);
		}
		else if (!_session.Player.IsWarModeLocalActive)
		{
            _session.Player.RemovePlayerFlag(PlayerFlags.InPVP);
            _session.Player.SetPlayerFlag(PlayerFlags.PVPTimer);

			if (!_session.Player.PvpInfo.IsHostile && _session.Player.IsPvP)
                _session.Player.PvpInfo.EndTimer = _gameTime.CurrentGameTime; // start toggle-off
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetWarMode)]
	void HandleSetWarMode(SetWarMode packet)
	{
		_session.Player.SetWarModeDesired(packet.Enable);
	}

	[WorldPacketHandler(ClientOpcodes.ResetInstances)]
	void HandleResetInstances(ResetInstances packet)
	{
		var map = _session.Player.Map;

		if (map != null && map.Instanceable)
			return;

		var group = _session.Player.Group;

		if (group)
		{
			if (!group.IsLeader(_session.Player.GUID))
				return;

			if (group.IsLFGGroup)
				return;

			group.ResetInstances(InstanceResetMethod.Manual, _session.Player);
		}
		else
		{
            _session.Player.ResetInstances(InstanceResetMethod.Manual);
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetDungeonDifficulty)]
	void HandleSetDungeonDifficulty(SetDungeonDifficulty setDungeonDifficulty)
	{
		var difficultyEntry = _cliDb.DifficultyStorage.LookupByKey(setDungeonDifficulty.DifficultyID);

		if (difficultyEntry == null)
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an invalid instance mode {1}!",
                        _session.Player.GUID.ToString(),
						setDungeonDifficulty.DifficultyID);

			return;
		}

		if (difficultyEntry.InstanceType != MapTypes.Instance)
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an non-dungeon instance mode {1}!",
                        _session.Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
                        _session.Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		var difficultyID = (Difficulty)difficultyEntry.Id;

		if (difficultyID == _session.Player.DungeonDifficultyId)
			return;

		// cannot reset while in an instance
		var map = _session.Player.Map;

		if (map && map.Instanceable)
		{
			Log.Logger.Debug(
						"WorldSession:HandleSetDungeonDifficulty: player (Name: {0}, {1}) tried to reset the instance while player is inside!",
                        _session.Player.GetName(),
                        _session.Player.GUID.ToString());

			return;
		}

		var group = _session.Player.Group;

		if (group)
		{
			if (!group.IsLeader(_session.Player.GUID))
				return;

			if (group.IsLFGGroup)
				return;

			// the difficulty is set even if the instances can't be reset
			group.ResetInstances(InstanceResetMethod.OnChangeDifficulty, _session.Player);
			group.SetDungeonDifficultyID(difficultyID);
		}
		else
		{
            _session.Player.ResetInstances(InstanceResetMethod.OnChangeDifficulty);
            _session.Player.DungeonDifficultyId = difficultyID;
            _session.Player.SendDungeonDifficulty();
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetRaidDifficulty)]
	void HandleSetRaidDifficulty(SetRaidDifficulty setRaidDifficulty)
	{
		var difficultyEntry = _cliDb.DifficultyStorage.LookupByKey((uint)setRaidDifficulty.DifficultyID);

		if (difficultyEntry == null)
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an invalid instance mode {1}!",
                        _session.Player.GUID.ToString(),
						setRaidDifficulty.DifficultyID);

			return;
		}

		if (difficultyEntry.InstanceType != MapTypes.Raid)
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent an non-dungeon instance mode {1}!",
                        _session.Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent unselectable instance mode {1}!",
                        _session.Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		if (((int)(difficultyEntry.Flags & DifficultyFlags.Legacy) != 0) != (setRaidDifficulty.Legacy != 0))
		{
			Log.Logger.Debug(
						"WorldSession.HandleSetDungeonDifficulty: {0} sent not matching legacy difficulty {1}!",
                        _session.Player.GUID.ToString(),
						difficultyEntry.Id);

			return;
		}

		var difficultyID = (Difficulty)difficultyEntry.Id;

		if (difficultyID == (setRaidDifficulty.Legacy != 0 ? _session.Player.LegacyRaidDifficultyId : _session.Player.RaidDifficultyId))
			return;

		// cannot reset while in an instance
		var map = _session.Player.Map;

		if (map && map.Instanceable)
		{
			Log.Logger.Debug(
						"WorldSession:HandleSetRaidDifficulty: player (Name: {0}, {1} tried to reset the instance while inside!",
                        _session.Player.GetName(),
                        _session.Player.GUID.ToString());

			return;
		}

		var group = _session.Player.Group;

		if (group)
		{
			if (!group.IsLeader(_session.Player.GUID))
				return;

			if (group.IsLFGGroup)
				return;

			// the difficulty is set even if the instances can't be reset
			group.ResetInstances(InstanceResetMethod.OnChangeDifficulty, _session.Player);

			if (setRaidDifficulty.Legacy != 0)
				group.SetLegacyRaidDifficultyID(difficultyID);
			else
				group.SetRaidDifficultyID(difficultyID);
		}
		else
		{
            _session.Player.ResetInstances(InstanceResetMethod.OnChangeDifficulty);

			if (setRaidDifficulty.Legacy != 0)
                _session.Player.LegacyRaidDifficultyId = difficultyID;
			else
                _session.Player.RaidDifficultyId = difficultyID;

            _session.Player.SendRaidDifficulty(setRaidDifficulty.Legacy != 0);
		}
	}

	[WorldPacketHandler(ClientOpcodes.SetTaxiBenchmarkMode, Processing = PacketProcessing.Inplace)]
	void HandleSetTaxiBenchmark(SetTaxiBenchmarkMode packet)
	{
		if (packet.Enable)
			_session.Player.SetPlayerFlag(PlayerFlags.TaxiBenchmark);
		else
			_session.Player.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
	}

	[WorldPacketHandler(ClientOpcodes.GuildSetFocusedAchievement)]
	void HandleGuildSetFocusedAchievement(GuildSetFocusedAchievement setFocusedAchievement)
	{
		var guild = _guildManager.GetGuildById(_session.Player.GuildId);

		if (guild)
			guild.GetAchievementMgr().SendAchievementInfo(_session.Player, setFocusedAchievement.AchievementID);
	}

	[WorldPacketHandler(ClientOpcodes.InstanceLockResponse)]
	void HandleInstanceLockResponse(InstanceLockResponse packet)
	{
		if (!_session.Player.HasPendingBind)
		{
			Log.Logger.Information(
						"InstanceLockResponse: Player {0} (guid {1}) tried to bind himself/teleport to graveyard without a pending bind!",
                        _session.Player.GetName(),
                        _session.Player.GUID.ToString());

			return;
		}

		if (packet.AcceptLock)
            _session.Player.ConfirmPendingBind();
		else
            _session.Player.RepopAtGraveyard();

        _session.Player.SetPendingBind(0, 0);
	}
}