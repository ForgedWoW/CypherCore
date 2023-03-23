// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Game.Entities;

namespace Forged.RealmServer.Chat.Commands;

[CommandGroup("titles")]
class TitleCommands
{
	[Command("current", RBACPermissions.CommandTitlesCurrent)]
	static bool HandleTitlesCurrentCommand(CommandHandler handler, uint titleId)
	{
		var target = handler.SelectedPlayer;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		var titleInfo = CliDB.CharTitlesStorage.LookupByKey(titleId);

		if (titleInfo == null)
		{
			handler.SendSysMessage(CypherStrings.InvalidTitleId, titleId);

			return false;
		}

		var tNameLink = handler.GetNameLink(target);
		var titleNameStr = string.Format(target.NativeGender == Gender.Male ? titleInfo.Name[handler.SessionDbcLocale] : titleInfo.Name1[handler.SessionDbcLocale].ConvertFormatSyntax(), target.GetName());

		target.SetTitle(titleInfo);
		target.SetChosenTitle(titleInfo.MaskID);

		handler.SendSysMessage(CypherStrings.TitleCurrentRes, titleId, titleNameStr, tNameLink);

		return true;
	}

	[Command("add", RBACPermissions.CommandTitlesAdd)]
	static bool HandleTitlesAddCommand(CommandHandler handler, uint titleId)
	{
		var target = handler.SelectedPlayer;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		var titleInfo = CliDB.CharTitlesStorage.LookupByKey(titleId);

		if (titleInfo == null)
		{
			handler.SendSysMessage(CypherStrings.InvalidTitleId, titleId);

			return false;
		}

		var tNameLink = handler.GetNameLink(target);

		var titleNameStr = string.Format((target.NativeGender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.SessionDbcLocale].ConvertFormatSyntax(), target.GetName());

		target.SetTitle(titleInfo);
		handler.SendSysMessage(CypherStrings.TitleAddRes, titleId, titleNameStr, tNameLink);

		return true;
	}

	[Command("remove", RBACPermissions.CommandTitlesRemove)]
	static bool HandleTitlesRemoveCommand(CommandHandler handler, uint titleId)
	{
		var target = handler.SelectedPlayer;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		var titleInfo = CliDB.CharTitlesStorage.LookupByKey(titleId);

		if (titleInfo == null)
		{
			handler.SendSysMessage(CypherStrings.InvalidTitleId, titleId);

			return false;
		}

		target.SetTitle(titleInfo, true);

		var tNameLink = handler.GetNameLink(target);
		var titleNameStr = string.Format((target.NativeGender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.SessionDbcLocale].ConvertFormatSyntax(), target.GetName());

		handler.SendSysMessage(CypherStrings.TitleRemoveRes, titleId, titleNameStr, tNameLink);

		if (!target.HasTitle(target.PlayerData.PlayerTitle))
		{
			target.SetChosenTitle(0);
			handler.SendSysMessage(CypherStrings.CurrentTitleReset, tNameLink);
		}

		return true;
	}

	[CommandGroup("set")]
	class TitleSetCommands
	{
		//Edit Player KnownTitles
		[Command("mask", RBACPermissions.CommandTitlesSetMask)]
		static bool HandleTitlesSetMaskCommand(CommandHandler handler, ulong mask)
		{
			var target = handler.SelectedPlayer;

			if (!target)
			{
				handler.SendSysMessage(CypherStrings.NoCharSelected);

				return false;
			}

			// check online security
			if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
				return false;

			var titles2 = mask;

			foreach (var tEntry in CliDB.CharTitlesStorage.Values)
				titles2 &= ~(1ul << tEntry.MaskID);

			mask &= ~titles2; // remove not existed titles

			target.SetKnownTitles(0, mask);
			handler.SendSysMessage(CypherStrings.Done);

			if (!target.HasTitle(target.PlayerData.PlayerTitle))
			{
				target.SetChosenTitle(0);
				handler.SendSysMessage(CypherStrings.CurrentTitleReset, handler.GetNameLink(target));
			}

			return true;
		}
	}
}