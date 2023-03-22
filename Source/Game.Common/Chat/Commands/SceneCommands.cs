﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Chat;

[CommandGroup("scene")]
class SceneCommands
{
	[Command("cancel", RBACPermissions.CommandSceneCancel)]
	static bool HandleCancelSceneCommand(CommandHandler handler, uint sceneScriptPackageId)
	{
		var target = handler.SelectedPlayerOrSelf;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		if (!CliDB.SceneScriptPackageStorage.HasRecord(sceneScriptPackageId))
			return false;

		target.SceneMgr.CancelSceneByPackageId(sceneScriptPackageId);

		return true;
	}

	[Command("debug", RBACPermissions.CommandSceneDebug)]
	static bool HandleDebugSceneCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;

		if (player)
		{
			player.SceneMgr.ToggleDebugSceneMode();
			handler.SendSysMessage(player.SceneMgr.IsInDebugSceneMode() ? CypherStrings.CommandSceneDebugOn : CypherStrings.CommandSceneDebugOff);
		}

		return true;
	}

	[Command("play", RBACPermissions.CommandScenePlay)]
	static bool HandlePlaySceneCommand(CommandHandler handler, uint sceneId)
	{
		var target = handler.SelectedPlayerOrSelf;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		if (Global.ObjectMgr.GetSceneTemplate(sceneId) == null)
			return false;

		target.SceneMgr.PlayScene(sceneId);

		return true;
	}

	[Command("playpackage", RBACPermissions.CommandScenePlayPackage)]
	static bool HandlePlayScenePackageCommand(CommandHandler handler, uint sceneScriptPackageId, SceneFlags? flags)
	{
		var target = handler.SelectedPlayerOrSelf;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		if (!CliDB.SceneScriptPackageStorage.HasRecord(sceneScriptPackageId))
			return false;

		target.SceneMgr.PlaySceneByPackageId(sceneScriptPackageId, flags.GetValueOrDefault(0));

		return true;
	}
}