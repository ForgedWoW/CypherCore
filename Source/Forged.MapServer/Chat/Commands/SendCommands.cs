﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Mails;

namespace Game.Chat.Commands;

[CommandGroup("send")]
class SendCommands
{
	[Command("mail", RBACPermissions.CommandSendMail, true)]
	static bool HandleSendMailCommand(CommandHandler handler, PlayerIdentifier playerIdentifier, QuotedString subject, QuotedString text)
	{
		// format: name "subject text" "mail text"
		if (playerIdentifier == null)
			playerIdentifier = PlayerIdentifier.FromTarget(handler);

		if (playerIdentifier == null)
			return false;

		if (subject.IsEmpty() || text.IsEmpty())
			return false;

		// from console show not existed sender
		MailSender sender = new(MailMessageType.Normal, handler.Session ? handler.Session.Player.GUID.Counter : 0, MailStationery.Gm);

		// @todo Fix poor design
		SQLTransaction trans = new();

		new MailDraft(subject, text)
			.SendMailTo(trans, new MailReceiver(playerIdentifier.GetGUID().Counter), sender);

		DB.Characters.CommitTransaction(trans);

		var nameLink = handler.PlayerLink(playerIdentifier.GetName());
		handler.SendSysMessage(CypherStrings.MailSent, nameLink);

		return true;
	}

	[Command("items", RBACPermissions.CommandSendItems, true)]
	static bool HandleSendItemsCommand(CommandHandler handler, PlayerIdentifier playerIdentifier, QuotedString subject, QuotedString text, string itemsStr)
	{
		// format: name "subject text" "mail text" item1[:count1] item2[:count2] ... item12[:count12]
		if (playerIdentifier == null)
			playerIdentifier = PlayerIdentifier.FromTarget(handler);

		if (playerIdentifier == null)
			return false;

		if (subject.IsEmpty() || text.IsEmpty())
			return false;

		// extract items
		List<KeyValuePair<uint, uint>> items = new();

		var tokens = new StringArray(itemsStr, ' ');

		for (var i = 0; i < tokens.Length; ++i)
		{
			// parse item str
			var itemIdAndCountStr = tokens[i].Split(':');

			if (!uint.TryParse(itemIdAndCountStr[0], out var itemId) || itemId == 0)
				return false;

			var itemProto = Global.ObjectMgr.GetItemTemplate(itemId);

			if (itemProto == null)
			{
				handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);

				return false;
			}

			if (itemIdAndCountStr[1].IsEmpty() || !uint.TryParse(itemIdAndCountStr[1], out var itemCount))
				itemCount = 1;

			if (itemCount < 1 || (itemProto.MaxCount > 0 && itemCount > itemProto.MaxCount))
			{
				handler.SendSysMessage(CypherStrings.CommandInvalidItemCount, itemCount, itemId);

				return false;
			}

			while (itemCount > itemProto.MaxStackSize)
			{
				items.Add(new KeyValuePair<uint, uint>(itemId, itemProto.MaxStackSize));
				itemCount -= itemProto.MaxStackSize;
			}

			items.Add(new KeyValuePair<uint, uint>(itemId, itemCount));

			if (items.Count > SharedConst.MaxMailItems)
			{
				handler.SendSysMessage(CypherStrings.CommandMailItemsLimit, SharedConst.MaxMailItems);

				return false;
			}
		}

		// from console show not existed sender
		MailSender sender = new(MailMessageType.Normal, handler.Session ? handler.Session.Player.GUID.Counter : 0, MailStationery.Gm);

		// fill mail
		MailDraft draft = new(subject, text);

		SQLTransaction trans = new();

		foreach (var pair in items)
		{
			var item = Item.CreateItem(pair.Key, pair.Value, ItemContext.None, handler.Session ? handler.Session.Player : null);

			if (item)
			{
				item.SaveToDB(trans); // save for prevent lost at next mail load, if send fail then item will deleted
				draft.AddItem(item);
			}
		}

		draft.SendMailTo(trans, new MailReceiver(playerIdentifier.GetGUID().Counter), sender);
		DB.Characters.CommitTransaction(trans);

		var nameLink = handler.PlayerLink(playerIdentifier.GetName());
		handler.SendSysMessage(CypherStrings.MailSent, nameLink);

		return true;
	}

	[Command("money", RBACPermissions.CommandSendMoney, true)]
	static bool HandleSendMoneyCommand(CommandHandler handler, PlayerIdentifier playerIdentifier, QuotedString subject, QuotedString text, long money)
	{
		// format: name "subject text" "mail text" money
		if (playerIdentifier == null)
			playerIdentifier = PlayerIdentifier.FromTarget(handler);

		if (playerIdentifier == null)
			return false;

		if (subject.IsEmpty() || text.IsEmpty())
			return false;

		if (money <= 0)
			return false;

		// from console show not existed sender
		MailSender sender = new(MailMessageType.Normal, handler.Session ? handler.Session.Player.GUID.Counter : 0, MailStationery.Gm);

		SQLTransaction trans = new();

		new MailDraft(subject, text)
			.AddMoney((uint)money)
			.SendMailTo(trans, new MailReceiver(playerIdentifier.GetGUID().Counter), sender);

		DB.Characters.CommitTransaction(trans);

		var nameLink = handler.PlayerLink(playerIdentifier.GetName());
		handler.SendSysMessage(CypherStrings.MailSent, nameLink);

		return true;
	}

	[Command("message", RBACPermissions.CommandSendMessage, true)]
	static bool HandleSendMessageCommand(CommandHandler handler, PlayerIdentifier playerIdentifier, QuotedString msgStr)
	{
		// - Find the player
		if (playerIdentifier == null)
			playerIdentifier = PlayerIdentifier.FromTarget(handler);

		if (playerIdentifier == null || !playerIdentifier.IsConnected())
			return false;

		if (!msgStr.IsEmpty())
			return false;

		// Check that he is not logging out.
		if (playerIdentifier.GetConnectedPlayer().Session.IsLogingOut)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		// - Send the message
		playerIdentifier.GetConnectedPlayer()
						.
						// - Send the message
						Session.SendNotification("{0}", msgStr);

		playerIdentifier.GetConnectedPlayer().Session.SendNotification("|cffff0000[Message from administrator]:|r");

		// Confirmation message
		var nameLink = handler.GetNameLink(playerIdentifier.GetConnectedPlayer());
		handler.SendSysMessage(CypherStrings.Sendmessage, nameLink, msgStr);

		return true;
	}
}