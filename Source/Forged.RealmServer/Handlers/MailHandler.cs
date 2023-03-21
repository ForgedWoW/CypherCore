// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Mails;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendShowMailBox(ObjectGuid guid)
	{
		NPCInteractionOpenResult npcInteraction = new();
		npcInteraction.Npc = guid;
		npcInteraction.InteractionType = PlayerInteractionType.MailInfo;
		npcInteraction.Success = true;
		SendPacket(npcInteraction);
	}

	bool CanOpenMailBox(ObjectGuid guid)
	{
		if (guid == Player.GUID)
		{
			if (!HasPermission(RBACPermissions.CommandMailbox))
			{
				Log.outWarn(LogFilter.ChatSystem, "{0} attempt open mailbox in cheating way.", Player.GetName());

				return false;
			}
		}
		else if (guid.IsGameObject)
		{
			if (!Player.GetGameObjectIfCanInteractWith(guid, GameObjectTypes.Mailbox))
				return false;
		}
		else if (guid.IsAnyTypeCreature)
		{
			if (!Player.GetNPCIfCanInteractWith(guid, NPCFlags.Mailbox, NPCFlags2.None))
				return false;
		}
		else
		{
			return false;
		}

		return true;
	}

	[WorldPacketHandler(ClientOpcodes.SendMail)]
	void HandleSendMail(SendMail sendMail)
	{
		if (sendMail.Info.Attachments.Count > SharedConst.MaxClientMailItems) // client limit
		{
			Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.TooManyAttachments);

			return;
		}

		if (!CanOpenMailBox(sendMail.Info.Mailbox))
			return;

		if (string.IsNullOrEmpty(sendMail.Info.Target))
			return;

		if (!ValidateHyperlinksAndMaybeKick(sendMail.Info.Subject) || !ValidateHyperlinksAndMaybeKick(sendMail.Info.Body))
			return;

		var player = Player;

		if (player.Level < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
		{
			SendNotification(CypherStrings.MailSenderReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));

			return;
		}

		var receiverGuid = ObjectGuid.Empty;

		if (ObjectManager.NormalizePlayerName(ref sendMail.Info.Target))
			receiverGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(sendMail.Info.Target);

		if (receiverGuid.IsEmpty)
		{
			Log.outInfo(LogFilter.Network,
						"Player {0} is sending mail to {1} (GUID: not existed!) with subject {2}" +
						"and body {3} includes {4} items, {5} copper and {6} COD copper with StationeryID = {7}",
						GetPlayerInfo(),
						sendMail.Info.Target,
						sendMail.Info.Subject,
						sendMail.Info.Body,
						sendMail.Info.Attachments.Count,
						sendMail.Info.SendMoney,
						sendMail.Info.Cod,
						sendMail.Info.StationeryID);

			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientNotFound);

			return;
		}

		if (sendMail.Info.SendMoney < 0)
		{
			Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

			Log.outWarn(LogFilter.Server,
						"Player {0} attempted to send mail to {1} ({2}) with negative money value (SendMoney: {3})",
						GetPlayerInfo(),
						sendMail.Info.Target,
						receiverGuid.ToString(),
						sendMail.Info.SendMoney);

			return;
		}

		if (sendMail.Info.Cod < 0)
		{
			Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

			Log.outWarn(LogFilter.Server,
						"Player {0} attempted to send mail to {1} ({2}) with negative COD value (Cod: {3})",
						GetPlayerInfo(),
						sendMail.Info.Target,
						receiverGuid.ToString(),
						sendMail.Info.Cod);

			return;
		}

		Log.outInfo(LogFilter.Network,
					"Player {0} is sending mail to {1} ({2}) with subject {3} and body {4}" +
					"includes {5} items, {6} copper and {7} COD copper with StationeryID = {8}",
					GetPlayerInfo(),
					sendMail.Info.Target,
					receiverGuid.ToString(),
					sendMail.Info.Subject,
					sendMail.Info.Body,
					sendMail.Info.Attachments.Count,
					sendMail.Info.SendMoney,
					sendMail.Info.Cod,
					sendMail.Info.StationeryID);

		if (player.GUID == receiverGuid)
		{
			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CannotSendToSelf);

			return;
		}

		var cost = (uint)(!sendMail.Info.Attachments.Empty() ? 30 * sendMail.Info.Attachments.Count : 30); // price hardcoded in client

		var reqmoney = cost + sendMail.Info.SendMoney;

		// Check for overflow
		if (reqmoney < sendMail.Info.SendMoney)
		{
			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

			return;
		}

		if (!player.HasEnoughMoney(reqmoney) && !player.IsGameMaster)
		{
			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

			return;
		}

		void mailCountCheckContinuation(TeamFaction receiverTeam, ulong mailsCount, uint receiverLevel, uint receiverAccountId, uint receiverBnetAccountId)
		{
			if (_player != player)
				return;

			// do not allow to have more than 100 mails in mailbox.. mails count is in opcode uint8!!! - so max can be 255..
			if (mailsCount > 100)
			{
				player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientCapReached);

				return;
			}

			// test the receiver's Faction... or all items are account bound
			var accountBound = !sendMail.Info.Attachments.Empty();

			foreach (var att in sendMail.Info.Attachments)
			{
				var item = player.GetItemByGuid(att.ItemGUID);

				if (item != null)
				{
					var itemProto = item.Template;

					if (itemProto == null || !itemProto.HasFlag(ItemFlags.IsBoundToAccount))
					{
						accountBound = false;

						break;
					}
				}
			}

			if (!accountBound && player.Team != receiverTeam && !HasPermission(RBACPermissions.TwoSideInteractionMail))
			{
				player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotYourTeam);

				return;
			}

			if (receiverLevel < WorldConfig.GetIntValue(WorldCfg.MailLevelReq))
			{
				SendNotification(CypherStrings.MailReceiverReq, WorldConfig.GetIntValue(WorldCfg.MailLevelReq));

				return;
			}

			List<Item> items = new();

			foreach (var att in sendMail.Info.Attachments)
			{
				if (att.ItemGUID.IsEmpty)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

					return;
				}

				var item = player.GetItemByGuid(att.ItemGUID);

				// prevent sending bag with items (cheat: can be placed in bag after adding equipped empty bag to mail)
				if (item == null)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

					return;
				}

				if (!item.CanBeTraded(true))
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

					return;
				}

				if (item.IsBoundAccountWide && item.IsSoulBound && player.Session.AccountId != receiverAccountId)
					if (!item.IsBattlenetAccountBound || player.Session.BattlenetAccountId == 0 || player.Session.BattlenetAccountId != receiverBnetAccountId)
					{
						player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.NotSameAccount);

						return;
					}

				if (item.Template.HasFlag(ItemFlags.Conjured) || item.ItemData.Expiration != 0)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

					return;
				}

				if (sendMail.Info.Cod != 0 && item.IsWrapped)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CantSendWrappedCod);

					return;
				}

				if (item.IsNotEmptyBag)
				{
					player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.DestroyNonemptyBag);

					return;
				}

				items.Add(item);
			}

			player.SendMailResult(0, MailResponseType.Send, MailResponseResult.Ok);

			player.ModifyMoney(-reqmoney);
			player.UpdateCriteria(CriteriaType.MoneySpentOnPostage, cost);

			var needItemDelay = false;

			MailDraft draft = new(sendMail.Info.Subject, sendMail.Info.Body);

			var trans = new SQLTransaction();

			if (!sendMail.Info.Attachments.Empty() || sendMail.Info.SendMoney > 0)
			{
				var log = HasPermission(RBACPermissions.LogGmTrade);

				if (!sendMail.Info.Attachments.Empty())
				{
					foreach (var item in items)
					{
						if (log)
							Log.outCommand(AccountId,
											$"GM {PlayerName} ({_player.GUID}) (Account: {AccountId}) mail item: {item.Template.GetName()} " +
											$"(Entry: {item.Entry} Count: {item.Count}) to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");

						item.SetNotRefundable(Player); // makes the item no longer refundable
						player.MoveItemFromInventory(item.BagSlot, item.Slot, true);

						item.DeleteFromInventoryDB(trans); // deletes item from character's inventory
						item.SetOwnerGUID(receiverGuid);
						item.SetState(ItemUpdateState.Changed);
						item.SaveToDB(trans); // recursive and not have transaction guard into self, item not in inventory and can be save standalone

						draft.AddItem(item);
					}

					// if item send to character at another account, then apply item delivery delay
					needItemDelay = player.Session.AccountId != receiverAccountId;
				}

				if (log && sendMail.Info.SendMoney > 0)
					Log.outCommand(AccountId, $"GM {PlayerName} ({_player.GUID}) (Account: {AccountId}) mail money: {sendMail.Info.SendMoney} to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");
			}

			// If theres is an item, there is a one hour delivery delay if sent to another account's character.
			var deliver_delay = needItemDelay ? WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay) : 0;

			// Mail sent between guild members arrives instantly
			var guild = Global.GuildMgr.GetGuildById(player.GuildId);

			if (guild != null)
				if (guild.IsMember(receiverGuid))
					deliver_delay = 0;

			// don't ask for COD if there are no items
			if (sendMail.Info.Attachments.Empty())
				sendMail.Info.Cod = 0;

			// will delete item or place to receiver mail list
			draft.AddMoney((ulong)sendMail.Info.SendMoney)
				.AddCOD((uint)sendMail.Info.Cod)
				.SendMailTo(trans, new MailReceiver(Global.ObjAccessor.FindConnectedPlayer(receiverGuid), receiverGuid.Counter), new MailSender(player), sendMail.Info.Body.IsEmpty() ? MailCheckMask.Copied : MailCheckMask.HasBody, deliver_delay);

			player.SaveInventoryAndGoldToDB(trans);
			DB.Characters.CommitTransaction(trans);
		}

		var receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

		if (receiver != null)
		{
			mailCountCheckContinuation(receiver.Team, receiver.MailSize, receiver.Level, receiver.Session.AccountId, receiver.Session.BattlenetAccountId);
		}
		else
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT);
			stmt.AddValue(0, receiverGuid.Counter);

			QueryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt)
										.WithChainingCallback((queryCallback, mailCountResult) =>
										{
											var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(receiverGuid);

											if (characterInfo != null)
												queryCallback.WithCallback(bnetAccountResult =>
															{
																mailCountCheckContinuation(Player.TeamForRace(characterInfo.RaceId),
																							!mailCountResult.IsEmpty() ? mailCountResult.Read<ulong>(0) : 0,
																							characterInfo.Level,
																							characterInfo.AccountId,
																							!bnetAccountResult.IsEmpty() ? bnetAccountResult.Read<uint>(0) : 0);
															})
															.SetNextQuery(Global.BNetAccountMgr.GetIdByGameAccountAsync(characterInfo.AccountId));
										}));
		}
	}

	[WorldPacketHandler(ClientOpcodes.MailReturnToSender)]
	void HandleMailReturnToSender(MailReturnToSender returnToSender)
	{
		if (!CanOpenMailBox(_player.PlayerTalkClass.GetInteractionData().SourceGuid))
			return;

		var player = Player;
		var m = player.GetMail(returnToSender.MailID);

		if (m == null || m.state == MailState.Deleted || m.deliver_time > GameTime.GetGameTime() || m.sender != returnToSender.SenderGUID.Counter)
		{
			player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.InternalError);

			return;
		}

		//we can return mail now, so firstly delete the old one
		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
		stmt.AddValue(0, returnToSender.MailID);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
		stmt.AddValue(0, returnToSender.MailID);
		trans.Append(stmt);

		player.RemoveMail(returnToSender.MailID);

		// only return mail if the player exists (and delete if not existing)
		if (m.messageType == MailMessageType.Normal && m.sender != 0)
		{
			MailDraft draft = new(m.subject, m.body);

			if (m.mailTemplateId != 0)
				draft = new MailDraft(m.mailTemplateId, false); // items already included

			if (m.HasItems())
				foreach (var itemInfo in m.items)
				{
					var item = player.GetMItem(itemInfo.item_guid);

					if (item)
						draft.AddItem(item);

					player.RemoveMItem(itemInfo.item_guid);
				}

			draft.AddMoney(m.money).SendReturnToSender(AccountId, m.receiver, m.sender, trans);
		}

		DB.Characters.CommitTransaction(trans);

		player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.Ok);
	}

	//used when player copies mail body to his inventory
	[WorldPacketHandler(ClientOpcodes.MailCreateTextItem)]
	void HandleMailCreateTextItem(MailCreateTextItem createTextItem)
	{
		if (!CanOpenMailBox(createTextItem.Mailbox))
			return;

		var player = Player;

		var m = player.GetMail(createTextItem.MailID);

		if (m == null || (string.IsNullOrEmpty(m.body) && m.mailTemplateId == 0) || m.state == MailState.Deleted || m.deliver_time > GameTime.GetGameTime())
		{
			player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

			return;
		}

		Item bodyItem = new(); // This is not bag and then can be used new Item.

		if (!bodyItem.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), 8383, ItemContext.None, player))
			return;

		// in mail template case we need create new item text
		if (m.mailTemplateId != 0)
		{
			var mailTemplateEntry = CliDB.MailTemplateStorage.LookupByKey(m.mailTemplateId);

			if (mailTemplateEntry == null)
			{
				player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

				return;
			}

			bodyItem.SetText(mailTemplateEntry.Body[SessionDbcLocale]);
		}
		else
		{
			bodyItem.SetText(m.body);
		}

		if (m.messageType == MailMessageType.Normal)
			bodyItem.SetCreator(ObjectGuid.Create(HighGuid.Player, m.sender));

		bodyItem.SetItemFlag(ItemFieldFlags.Readable);

		Log.outInfo(LogFilter.Network, "HandleMailCreateTextItem mailid={0}", createTextItem.MailID);

		List<ItemPosCount> dest = new();
		var msg = Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, bodyItem, false);

		if (msg == InventoryResult.Ok)
		{
			m.checkMask = m.checkMask | MailCheckMask.Copied;
			m.state = MailState.Changed;
			player.MailsUpdated = true;

			player.StoreItem(dest, bodyItem, true);
			player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.Ok);
		}
		else
		{
			player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.EquipError, msg);
		}
	}
}