// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Achievements;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Forged.RealmServer.Entities.Players;

namespace Forged.RealmServer.Chat;

[CommandGroup("reset")]
class ResetCommands
{
	[Command("achievements", RBACPermissions.CommandResetAchievements, true)]
	static bool HandleResetAchievementsCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		if (player.IsConnected())
			player.GetConnectedPlayer().ResetAchievements();
		else
			PlayerAchievementMgr.DeleteFromDB(player.GetGUID());

		return true;
	}

	[Command("honor", RBACPermissions.CommandResetHonor, true)]
	static bool HandleResetHonorCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null || !player.IsConnected())
			return false;

		player.GetConnectedPlayer().ResetHonorStats();
		player.GetConnectedPlayer().UpdateCriteria(CriteriaType.HonorableKills);

		return true;
	}

	static bool HandleResetStatsOrLevelHelper(Player player)
	{
		var classEntry = CliDB.ChrClassesStorage.LookupByKey(player.Class);

		if (classEntry == null)
		{
			Log.Logger.Error("Class {0} not found in DBC (Wrong DBC files?)", player.Class);

			return false;
		}

		var powerType = classEntry.DisplayPower;

		// reset m_form if no aura
		if (!player.HasAuraType(AuraType.ModShapeshift))
			player.ShapeshiftForm = ShapeShiftForm.None;

		player.SetFactionForRace(player.Race);
		player.SetPowerType(powerType);

		// reset only if player not in some form;
		if (player.ShapeshiftForm == ShapeShiftForm.None)
			player.InitDisplayIds();

		player.ReplaceAllPvpFlags(UnitPVPStateFlags.PvP);

		player.ReplaceAllUnitFlags(UnitFlags.PlayerControlled);

		//-1 is default value
		player.SetWatchedFactionIndex(0xFFFFFFFF);

		return true;
	}

	[Command("level", RBACPermissions.CommandResetLevel, true)]
	static bool HandleResetLevelCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null || !player.IsConnected())
			return false;

		var target = player.GetConnectedPlayer();

		if (!HandleResetStatsOrLevelHelper(target))
			return false;

		var oldLevel = (byte)target.Level;

		// set starting level
		var startLevel = target.GetStartLevel(target.Race, target.Class);

		target._ApplyAllLevelScaleItemMods(false);
		target.SetLevel(startLevel);
		target.InitRunes();
		target.InitStatsForLevel(true);
		target.InitTaxiNodesForLevel();
		target.InitTalentForLevel();
		target.XP = 0;

		target._ApplyAllLevelScaleItemMods(true);

		// reset level for pet
		var pet = target.CurrentPet;

		if (pet)
			pet.SynchronizeLevelWithOwner();

		Global.ScriptMgr.ForEach<IPlayerOnLevelChanged>(target.Class, p => p.OnLevelChanged(target, oldLevel));

		return true;
	}

	[Command("spells", RBACPermissions.CommandResetSpells, true)]
	static bool HandleResetSpellsCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		if (player.IsConnected())
		{
			var target = player.GetConnectedPlayer();
			target.ResetSpells();

			target.SendSysMessage(CypherStrings.ResetSpells);

			if (handler.Session == null || handler.Session.Player != target)
				handler.SendSysMessage(CypherStrings.ResetSpellsOnline, handler.GetNameLink(target));
		}
		else
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
			stmt.AddValue(0, (ushort)AtLoginFlags.ResetSpells);
			stmt.AddValue(1, player.GetGUID().Counter);
			_characterDatabase.Execute(stmt);

			handler.SendSysMessage(CypherStrings.ResetSpellsOffline, player.GetName());
		}

		return true;
	}

	[Command("stats", RBACPermissions.CommandResetStats, true)]
	static bool HandleResetStatsCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null || !player.IsConnected())
			return false;

		var target = player.GetConnectedPlayer();

		if (!HandleResetStatsOrLevelHelper(target))
			return false;

		target.InitRunes();
		target.InitStatsForLevel(true);
		target.InitTaxiNodesForLevel();
		target.InitTalentForLevel();

		return true;
	}

	[Command("talents", RBACPermissions.CommandResetTalents, true)]
	static bool HandleResetTalentsCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		if (player.IsConnected())
		{
			var target = player.GetConnectedPlayer();
			target.ResetTalents(true);
			target.ResetTalentSpecialization();
			target.SendTalentsInfoData();
			target.SendSysMessage(CypherStrings.ResetTalents);

			if (handler.Session == null || handler.Session.Player != target)
				handler.SendSysMessage(CypherStrings.ResetTalentsOnline, handler.GetNameLink(target));

			/* TODO: 6.x remove/update pet talents
			Pet* pet = target.GetPet();
			Pet.resetTalentsForAllPetsOf(target, pet);
			if (pet)
				target.SendTalentsInfoData(true);
			*/
			return true;
		}
		else if (!player.GetGUID().IsEmpty)
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
			stmt.AddValue(0, (ushort)(AtLoginFlags.None | AtLoginFlags.ResetPetTalents));
			stmt.AddValue(1, player.GetGUID().Counter);
			_characterDatabase.Execute(stmt);

			var nameLink = handler.PlayerLink(player.GetName());
			handler.SendSysMessage(CypherStrings.ResetTalentsOffline, nameLink);

			return true;
		}

		handler.SendSysMessage(CypherStrings.NoCharSelected);

		return false;
	}

	[Command("all", RBACPermissions.CommandResetAll, true)]
	static bool HandleResetAllCommand(CommandHandler handler, string subCommand)
	{
		AtLoginFlags atLogin;

		// Command specially created as single command to prevent using short case names
		if (subCommand == "spells")
		{
			atLogin = AtLoginFlags.ResetSpells;
			_worldManager.SendWorldText(CypherStrings.ResetallSpells);

			if (handler.Session == null)
				handler.SendSysMessage(CypherStrings.ResetallSpells);
		}
		else if (subCommand == "talents")
		{
			atLogin = AtLoginFlags.ResetTalents | AtLoginFlags.ResetPetTalents;
			_worldManager.SendWorldText(CypherStrings.ResetallTalents);

			if (handler.Session == null)
				handler.SendSysMessage(CypherStrings.ResetallTalents);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.ResetallUnknownCase, subCommand);

			return false;
		}

		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ALL_AT_LOGIN_FLAGS);
		stmt.AddValue(0, (ushort)atLogin);
		_characterDatabase.Execute(stmt);

		var plist = _objectAccessor.GetPlayers();

		foreach (var player in plist)
			player.SetAtLoginFlag(atLogin);

		return true;
	}
}