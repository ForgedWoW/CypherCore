// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Accounts;
using Forged.MapServer.Cache;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Mails;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Mail;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class MailHandler : IWorldSessionHandler
{
    private readonly BNetAccountManager _bNetAccountManager;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly GuildManager _guildManager;
    private readonly DB6Storage<MailTemplateRecord> _mailTemplateRecords;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly WorldSession _session;

    public MailHandler(WorldSession session, ClassFactory classFactory, GameObjectManager objectManager, DB6Storage<MailTemplateRecord> mailTemplateRecords,
                       CharacterDatabase characterDatabase, ObjectAccessor objectAccessor, CharacterCache characterCache, IConfiguration configuration, GuildManager guildManager,
                       CliDB cliDB, BNetAccountManager bNetAccountManager)
    {
        _session = session;
        _classFactory = classFactory;
        _objectManager = objectManager;
        _mailTemplateRecords = mailTemplateRecords;
        _characterDatabase = characterDatabase;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _configuration = configuration;
        _guildManager = guildManager;
        _cliDB = cliDB;
        _bNetAccountManager = bNetAccountManager;
    }

    public void SendShowMailBox(ObjectGuid guid)
    {
        _session.SendPacket(new NPCInteractionOpenResult()
        {
            Npc = guid,
            InteractionType = PlayerInteractionType.MailInfo,
            Success = true
        });
    }

    private bool CanOpenMailBox(ObjectGuid guid)
    {
        if (guid == _session.Player.GUID)
        {
            if (_session.HasPermission(RBACPermissions.CommandMailbox))
                return true;

            Log.Logger.Warning("{0} attempt open mailbox in cheating way.", _session.Player.GetName());

            return false;
        }

        if (guid.IsGameObject)
        {
            if (_session.Player.GetGameObjectIfCanInteractWith(guid, GameObjectTypes.Mailbox) == null)
                return false;
        }
        else if (guid.IsAnyTypeCreature)
        {
            if (_session.Player.GetNPCIfCanInteractWith(guid, NPCFlags.Mailbox, NPCFlags2.None) == null)
                return false;
        }
        else
            return false;

        return true;
    }

    //called when _session.Player lists his received mails
    [WorldPacketHandler(ClientOpcodes.MailGetList)]
    private void HandleGetMailList(MailGetList getList)
    {
        if (!CanOpenMailBox(getList.Mailbox))
            return;

        var mails = _session.Player.Mails;

        MailListResult response = new();
        var curTime = GameTime.CurrentTime;

        foreach (var m in mails)
        {
            // skip deleted or not delivered (deliver delay not expired) mails
            if (m.State == MailState.Deleted || curTime < m.DeliverTime)
                continue;

            // max. 100 mails can be sent
            if (response.Mails.Count < 100)
                response.Mails.Add(new MailListEntry(m, _session.Player));
        }

        _session.Player.PlayerTalkClass.InteractionData.Reset();
        _session.Player.PlayerTalkClass.InteractionData.SourceGuid = getList.Mailbox;
        _session.SendPacket(response);

        // recalculate m_nextMailDelivereTime and unReadMails
        _session.Player.UpdateNextMailTimeAndUnreads();
    }

    //used when _session.Player copies mail body to his inventory
    [WorldPacketHandler(ClientOpcodes.MailCreateTextItem)]
    private void HandleMailCreateTextItem(MailCreateTextItem createTextItem)
    {
        if (!CanOpenMailBox(createTextItem.Mailbox))
            return;

        var m = _session.Player.GetMail(createTextItem.MailID);

        if (m == null || (string.IsNullOrEmpty(m.Body) && m.MailTemplateId == 0) || m.State == MailState.Deleted || m.DeliverTime > GameTime.CurrentTime)
        {
            _session.Player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

            return;
        }

        var bodyItem = _classFactory.Resolve<Item>(); // This is not bag and then can be used new Item.

        if (!bodyItem.Create(_objectManager.GetGenerator(HighGuid.Item).Generate(), 8383, ItemContext.None, _session.Player))
            return;

        // in mail template case we need create new item text
        if (m.MailTemplateId != 0)
        {
            if (!_mailTemplateRecords.TryGetValue(m.MailTemplateId, out var mailTemplateEntry))
            {
                _session.Player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.InternalError);

                return;
            }

            bodyItem.SetText(mailTemplateEntry.Body[_session.SessionDbcLocale]);
        }
        else
            bodyItem.SetText(m.Body);

        if (m.MessageType == MailMessageType.Normal)
            bodyItem.SetCreator(ObjectGuid.Create(HighGuid.Player, m.Sender));

        bodyItem.SetItemFlag(ItemFieldFlags.Readable);

        Log.Logger.Information("HandleMailCreateTextItem mailid={0}", createTextItem.MailID);

        List<ItemPosCount> dest = new();
        var msg = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, bodyItem);

        if (msg == InventoryResult.Ok)
        {
            m.CheckMask |= MailCheckMask.Copied;
            m.State = MailState.Changed;
            _session.Player.MailsUpdated = true;

            _session.Player.StoreItem(dest, bodyItem, true);
            _session.Player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.Ok);
        }
        else
            _session.Player.SendMailResult(createTextItem.MailID, MailResponseType.MadePermanent, MailResponseResult.EquipError, msg);
    }

    //called when client deletes mail
    [WorldPacketHandler(ClientOpcodes.MailDelete)]
    private void HandleMailDelete(MailDelete mailDelete)
    {
        var m = _session.Player.GetMail(mailDelete.MailID);
        _session.Player.MailsUpdated = true;

        if (m != null)
        {
            // delete shouldn't show up for COD mails
            if (m.Cod != 0)
            {
                _session.Player.SendMailResult(mailDelete.MailID, MailResponseType.Deleted, MailResponseResult.InternalError);

                return;
            }

            m.State = MailState.Deleted;
        }

        _session.Player.SendMailResult(mailDelete.MailID, MailResponseType.Deleted, MailResponseResult.Ok);
    }

    //called when mail is read
    [WorldPacketHandler(ClientOpcodes.MailMarkAsRead)]
    private void HandleMailMarkAsRead(MailMarkAsRead markAsRead)
    {
        if (!CanOpenMailBox(markAsRead.Mailbox))
            return;

        var m = _session.Player.GetMail(markAsRead.MailID);

        if (m == null || m.State == MailState.Deleted)
            return;

        if (_session.Player.UnReadMails != 0)
            --_session.Player.UnReadMails;

        m.CheckMask |= MailCheckMask.Read;
        _session.Player.MailsUpdated = true;
        m.State = MailState.Changed;
    }

    [WorldPacketHandler(ClientOpcodes.MailReturnToSender)]
    private void HandleMailReturnToSender(MailReturnToSender returnToSender)
    {
        if (!CanOpenMailBox(_session.Player.PlayerTalkClass.InteractionData.SourceGuid))
            return;

        var m = _session.Player.GetMail(returnToSender.MailID);

        if (m == null || m.State == MailState.Deleted || m.DeliverTime > GameTime.CurrentTime || m.Sender != returnToSender.SenderGUID.Counter)
        {
            _session.Player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.InternalError);

            return;
        }

        //we can return mail now, so firstly delete the old one
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
        stmt.AddValue(0, returnToSender.MailID);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
        stmt.AddValue(0, returnToSender.MailID);
        trans.Append(stmt);

        _session.Player.RemoveMail(returnToSender.MailID);

        // only return mail if the _session.Player exists (and delete if not existing)
        if (m.MessageType == MailMessageType.Normal && m.Sender != 0)
        {
            var draft = _classFactory.ResolveWithPositionalParameters<MailDraft>(m.Subject, m.Body);

            if (m.MailTemplateId != 0)
                draft = _classFactory.ResolveWithPositionalParameters<MailDraft>(m.MailTemplateId, false); // items already included

            if (m.HasItems())
                foreach (var itemInfo in m.Items)
                {
                    var item = _session.Player.GetMItem(itemInfo.ItemGUID);

                    if (item != null)
                        draft.AddItem(item);

                    _session.Player.RemoveMItem(itemInfo.ItemGUID);
                }

            draft.AddMoney(m.Money).SendReturnToSender(_session.AccountId, m.Receiver, m.Sender, trans);
        }

        _characterDatabase.CommitTransaction(trans);

        _session.Player.SendMailResult(returnToSender.MailID, MailResponseType.ReturnedToSender, MailResponseResult.Ok);
    }

    //called when _session.Player takes item attached in mail
    [WorldPacketHandler(ClientOpcodes.MailTakeItem)]
    private void HandleMailTakeItem(MailTakeItem takeItem)
    {
        var attachID = takeItem.AttachID;

        if (!CanOpenMailBox(takeItem.Mailbox))
            return;

        var m = _session.Player.GetMail(takeItem.MailID);

        if (m == null || m.State == MailState.Deleted || m.DeliverTime > GameTime.CurrentTime)
        {
            _session.Player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);

            return;
        }

        // verify that the mail has the item to avoid cheaters taking COD items without paying
        if (m.Items.All(p => p.ItemGUID != attachID))
        {
            _session.Player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.InternalError);

            return;
        }

        // prevent cheating with skip client money check
        if (!_session.Player.HasEnoughMoney(m.Cod))
        {
            _session.Player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.NotEnoughMoney);

            return;
        }

        var it = _session.Player.GetMItem(takeItem.AttachID);

        List<ItemPosCount> dest = new();
        var msg = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, it);

        if (msg == InventoryResult.Ok)
        {
            SQLTransaction trans = new();
            m.RemoveItem(takeItem.AttachID);
            m.RemovedItems.Add(takeItem.AttachID);

            if (m.Cod > 0) //if there is COD, take COD money from _session.Player and send them to sender by mail
            {
                var senderGUID = ObjectGuid.Create(HighGuid.Player, m.Sender);
                var receiver = _objectAccessor.FindPlayer(senderGUID);

                uint senderAccId = 0;

                if (_session.HasPermission(RBACPermissions.LogGmTrade))
                {
                    string senderName;

                    if (receiver != null)
                    {
                        senderAccId = receiver.Session.AccountId;
                        senderName = receiver.GetName();
                    }
                    else
                    {
                        // can be calculated early
                        senderAccId = _characterCache.GetCharacterAccountIdByGuid(senderGUID);

                        if (!_characterCache.GetCharacterNameByGuid(senderGUID, out senderName))
                            senderName = _objectManager.GetCypherString(CypherStrings.Unknown);
                    }

                    Log.Logger.ForContext<GMCommands>().Information(
                                   "GM {0} (Account: {1}) receiver mail item: {2} (Entry: {3} Count: {4}) and send COD money: {5} to _session.Player: {6} (Account: {7})",
                                   _session.PlayerName,
                                   _session.AccountId,
                                   it.Template.GetName(),
                                   it.Entry,
                                   it.Count,
                                   m.Cod,
                                   senderName,
                                   senderAccId);
                }
                else if (receiver == null)
                    senderAccId = _characterCache.GetCharacterAccountIdByGuid(senderGUID);

                // check _session.Player existence
                if (receiver == null || senderAccId != 0)
                    _classFactory.ResolveWithPositionalParameters<MailDraft>(m.Subject, "")
                                 .AddMoney(m.Cod)
                                 .SendMailTo(trans, new MailReceiver(receiver, m.Sender), new MailSender(MailMessageType.Normal, m.Receiver), MailCheckMask.CodPayment);

                _session.Player.ModifyMoney(-(long)m.Cod);
            }

            m.Cod = 0;
            m.State = MailState.Changed;
            _session.Player.MailsUpdated = true;
            _session.Player.RemoveMItem(it.GUID.Counter);

            var count = it.Count;                   // save counts before store and possible merge with deleting
            it.SetState(ItemUpdateState.Unchanged); // need to set this state, otherwise item cannot be removed later, if neccessary
            _session.Player.MoveItemToInventory(dest, it, true);

            _session.Player.SaveInventoryAndGoldToDB(trans);
            _session.Player._SaveMail(trans);
            _characterDatabase.CommitTransaction(trans);

            _session.Player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.Ok, 0, takeItem.AttachID, count);
        }
        else
            _session.Player.SendMailResult(takeItem.MailID, MailResponseType.ItemTaken, MailResponseResult.EquipError, msg);
    }

    [WorldPacketHandler(ClientOpcodes.MailTakeMoney)]
    private void HandleMailTakeMoney(MailTakeMoney takeMoney)
    {
        if (!CanOpenMailBox(takeMoney.Mailbox))
            return;

        var m = _session.Player.GetMail(takeMoney.MailID);

        if (m == null ||
            m.State == MailState.Deleted ||
            m.DeliverTime > GameTime.CurrentTime ||
            (takeMoney.Money > 0 && m.Money != takeMoney.Money))
        {
            _session.Player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.InternalError);

            return;
        }

        if (!_session.Player.ModifyMoney(m.Money, false))
        {
            _session.Player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.EquipError, InventoryResult.TooMuchGold);

            return;
        }

        m.Money = 0;
        m.State = MailState.Changed;
        _session.Player.MailsUpdated = true;

        _session.Player.SendMailResult(takeMoney.MailID, MailResponseType.MoneyTaken, MailResponseResult.Ok);

        // save money and mail to prevent cheating
        SQLTransaction trans = new();
        _session.Player.SaveGoldToDB(trans);
        _session.Player._SaveMail(trans);
        _characterDatabase.CommitTransaction(trans);
    }

    [WorldPacketHandler(ClientOpcodes.QueryNextMailTime)]
    private void HandleQueryNextMailTime(MailQueryNextMailTime queryNextMailTime)
    {
        if (queryNextMailTime == null)
            return;

        MailQueryNextTimeResult result = new();

        if (_session.Player.UnReadMails > 0)
        {
            result.NextMailTime = 0.0f;

            var now = GameTime.CurrentTime;
            List<ulong> sentSenders = new();

            foreach (var mail in _session.Player.Mails)
            {
                if (mail.CheckMask.HasAnyFlag(MailCheckMask.Read))
                    continue;

                // and already delivered
                if (now < mail.DeliverTime)
                    continue;

                // only send each mail sender once
                if (sentSenders.Any(p => p == mail.Sender))
                    continue;

                result.Next.Add(new MailQueryNextTimeResult.MailNextTimeEntry(mail));

                sentSenders.Add(mail.Sender);

                // do not send more than 2 mails
                if (sentSenders.Count > 2)
                    break;
            }
        }
        else
            result.NextMailTime = -Time.DAY;

        _session.SendPacket(result);
    }

    [WorldPacketHandler(ClientOpcodes.SendMail)]
    private void HandleSendMail(SendMail sendMail)
    {
        if (sendMail.Info.Attachments.Count > SharedConst.MaxClientMailItems) // client limit
        {
            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.TooManyAttachments);

            return;
        }

        if (!CanOpenMailBox(sendMail.Info.Mailbox))
            return;

        if (string.IsNullOrEmpty(sendMail.Info.Target))
            return;

        if (!_session.ValidateHyperlinksAndMaybeKick(sendMail.Info.Subject) || !_session.ValidateHyperlinksAndMaybeKick(sendMail.Info.Body))
            return;

        if (_session.Player.Level < _configuration.GetDefaultValue("LevelReq:Mail", 1))
        {
            _session.SendNotification(CypherStrings.MailSenderReq, _configuration.GetDefaultValue("LevelReq:Mail", 1));

            return;
        }

        var receiverGuid = ObjectGuid.Empty;

        if (_objectManager.NormalizePlayerName(ref sendMail.Info.Target))
            receiverGuid = _characterCache.GetCharacterGuidByName(sendMail.Info.Target);

        if (receiverGuid.IsEmpty)
        {
            Log.Logger.Information("_session.Player {0} is sending mail to {1} (GUID: not existed!) with subject {2}" +
                                   "and body {3} includes {4} items, {5} copper and {6} COD copper with StationeryID = {7}",
                                   _session.GetPlayerInfo(),
                                   sendMail.Info.Target,
                                   sendMail.Info.Subject,
                                   sendMail.Info.Body,
                                   sendMail.Info.Attachments.Count,
                                   sendMail.Info.SendMoney,
                                   sendMail.Info.Cod,
                                   sendMail.Info.StationeryID);

            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientNotFound);

            return;
        }

        if (sendMail.Info.SendMoney < 0)
        {
            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

            Log.Logger.Warning("_session.Player {0} attempted to send mail to {1} ({2}) with negative money value (SendMoney: {3})",
                               _session.GetPlayerInfo(),
                               sendMail.Info.Target,
                               receiverGuid.ToString(),
                               sendMail.Info.SendMoney);

            return;
        }

        if (sendMail.Info.Cod < 0)
        {
            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.InternalError);

            Log.Logger.Warning("_session.Player {0} attempted to send mail to {1} ({2}) with negative COD value (Cod: {3})",
                               _session.GetPlayerInfo(),
                               sendMail.Info.Target,
                               receiverGuid.ToString(),
                               sendMail.Info.Cod);

            return;
        }

        Log.Logger.Information("_session.Player {0} is sending mail to {1} ({2}) with subject {3} and body {4}" +
                               "includes {5} items, {6} copper and {7} COD copper with StationeryID = {8}",
                               _session.GetPlayerInfo(),
                               sendMail.Info.Target,
                               receiverGuid.ToString(),
                               sendMail.Info.Subject,
                               sendMail.Info.Body,
                               sendMail.Info.Attachments.Count,
                               sendMail.Info.SendMoney,
                               sendMail.Info.Cod,
                               sendMail.Info.StationeryID);

        if (_session.Player.GUID == receiverGuid)
        {
            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CannotSendToSelf);

            return;
        }

        var cost = (uint)(!sendMail.Info.Attachments.Empty() ? 30 * sendMail.Info.Attachments.Count : 30); // price hardcoded in client

        var reqmoney = cost + sendMail.Info.SendMoney;

        // Check for overflow
        if (reqmoney < sendMail.Info.SendMoney)
        {
            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

            return;
        }

        if (!_session.Player.HasEnoughMoney(reqmoney) && !_session.Player.IsGameMaster)
        {
            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotEnoughMoney);

            return;
        }

        void MailCountCheckContinue(TeamFaction receiverTeam, ulong mailsCount, uint receiverLevel, uint receiverAccountId, uint receiverBnetAccountId)
        {
            // do not allow to have more than 100 mails in mailbox.. mails count is in opcode uint8!!! - so max can be 255..
            if (mailsCount > 100)
            {
                _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.RecipientCapReached);

                return;
            }

            // test the receiver's Faction... or all items are account bound
            var accountBound = !sendMail.Info.Attachments.Empty();

            foreach (var att in sendMail.Info.Attachments)
            {
                var item = _session.Player.GetItemByGuid(att.ItemGUID);

                if (item == null)
                    continue;

                var itemProto = item.Template;

                if (itemProto != null && itemProto.HasFlag(ItemFlags.IsBoundToAccount))
                    continue;

                accountBound = false;

                break;
            }

            if (!accountBound && _session.Player.Team != receiverTeam && !_session.HasPermission(RBACPermissions.TwoSideInteractionMail))
            {
                _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.NotYourTeam);

                return;
            }

            if (receiverLevel < _configuration.GetDefaultValue("LevelReq:Mail", 1))
            {
                _session.SendNotification(CypherStrings.MailReceiverReq, _configuration.GetDefaultValue("LevelReq:Mail", 1));

                return;
            }

            List<Item> items = new();

            foreach (var att in sendMail.Info.Attachments)
            {
                if (att.ItemGUID.IsEmpty)
                {
                    _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

                    return;
                }

                var item = _session.Player.GetItemByGuid(att.ItemGUID);

                // prevent sending bag with items (cheat: can be placed in bag after adding equipped empty bag to mail)
                if (item == null)
                {
                    _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.MailAttachmentInvalid);

                    return;
                }

                if (!item.CanBeTraded(true))
                {
                    _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

                    return;
                }

                if (item.IsBoundAccountWide && item.IsSoulBound && _session.Player.Session.AccountId != receiverAccountId)
                    if (!item.IsBattlenetAccountBound || _session.Player.Session.BattlenetAccountId == 0 || _session.Player.Session.BattlenetAccountId != receiverBnetAccountId)
                    {
                        _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.NotSameAccount);

                        return;
                    }

                if (item.Template.HasFlag(ItemFlags.Conjured) || item.ItemData.Expiration != 0)
                {
                    _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.MailBoundItem);

                    return;
                }

                if (sendMail.Info.Cod != 0 && item.IsWrapped)
                {
                    _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.CantSendWrappedCod);

                    return;
                }

                if (item.IsNotEmptyBag)
                {
                    _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.EquipError, InventoryResult.DestroyNonemptyBag);

                    return;
                }

                items.Add(item);
            }

            _session.Player.SendMailResult(0, MailResponseType.Send, MailResponseResult.Ok);

            _session.Player.ModifyMoney(-reqmoney);
            _session.Player.UpdateCriteria(CriteriaType.MoneySpentOnPostage, cost);

            var needItemDelay = false;

            var draft = _classFactory.ResolveWithPositionalParameters<MailDraft>(sendMail.Info.Subject, sendMail.Info.Body);

            var trans = new SQLTransaction();

            if (!sendMail.Info.Attachments.Empty() || sendMail.Info.SendMoney > 0)
            {
                var log = _session.HasPermission(RBACPermissions.LogGmTrade);

                if (!sendMail.Info.Attachments.Empty())
                {
                    foreach (var item in items)
                    {
                        if (log)
                            Log.Logger.ForContext<GMCommands>().Information($"GM {_session.PlayerName} ({_session.Player.GUID}) (Account: {_session.AccountId}) mail item: {item.Template.GetName()} " +
                                           $"(Entry: {item.Entry} Count: {item.Count}) to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");

                        item.SetNotRefundable(_session.Player); // makes the item no longer refundable
                        _session.Player.MoveItemFromInventory(item.BagSlot, item.Slot, true);

                        item.DeleteFromInventoryDB(trans); // deletes item from character's inventory
                        item.SetOwnerGUID(receiverGuid);
                        item.SetState(ItemUpdateState.Changed);
                        item.SaveToDB(trans); // recursive and not have transaction guard into self, item not in inventory and can be save standalone

                        draft.AddItem(item);
                    }

                    // if item send to character at another account, then apply item delivery delay
                    needItemDelay = _session.Player.Session.AccountId != receiverAccountId;
                }

                if (log && sendMail.Info.SendMoney > 0)
                    Log.Logger.ForContext<GMCommands>().Information($"GM {_session.PlayerName} ({_session.Player.GUID}) (Account: {_session.AccountId}) mail money: {sendMail.Info.SendMoney} to: {sendMail.Info.Target} ({receiverGuid}) (Account: {receiverAccountId})");
            }

            // If theres is an item, there is a one hour delivery delay if sent to another account's character.
            var deliverDelay = needItemDelay ? _configuration.GetDefaultValue("MailDeliveryDelay", Time.HOUR) : 0;

            // Mail sent between guild members arrives instantly
            var guild = _guildManager.GetGuildById(_session.Player.GuildId);

            if (guild != null)
                if (guild.IsMember(receiverGuid))
                    deliverDelay = 0;

            // don't ask for COD if there are no items
            if (sendMail.Info.Attachments.Empty())
                sendMail.Info.Cod = 0;

            // will delete item or place to receiver mail list
            draft.AddMoney((ulong)sendMail.Info.SendMoney)
                 .AddCod((uint)sendMail.Info.Cod)
                 .SendMailTo(trans, _objectAccessor.FindConnectedPlayer(receiverGuid), new MailSender(_session.Player), sendMail.Info.Body.IsEmpty() ? MailCheckMask.Copied : MailCheckMask.HasBody, (uint)deliverDelay);

            _session.Player.SaveInventoryAndGoldToDB(trans);
            _characterDatabase.CommitTransaction(trans);
        }

        var receiver = _objectAccessor.FindPlayer(receiverGuid);

        if (receiver != null)
            MailCountCheckContinue(receiver.Team, receiver.MailSize, receiver.Level, receiver.Session.AccountId, receiver.Session.BattlenetAccountId);
        else
        {
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT);
            stmt.AddValue(0, receiverGuid.Counter);

            _session.QueryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt)
                                         .WithChainingCallback((queryCallback, mailCountResult) =>
                                         {
                                             var characterInfo = _characterCache.GetCharacterCacheByGuid(receiverGuid);

                                             if (characterInfo != null)
                                                 queryCallback.WithCallback(bnetAccountResult =>
                                                              {
                                                                  MailCountCheckContinue(Player.TeamForRace(characterInfo.RaceId, _cliDB),
                                                                                             !mailCountResult.IsEmpty() ? mailCountResult.Read<ulong>(0) : 0,
                                                                                             characterInfo.Level,
                                                                                             characterInfo.AccountId,
                                                                                             !bnetAccountResult.IsEmpty() ? bnetAccountResult.Read<uint>(0) : 0);
                                                              })
                                                              .SetNextQuery(_bNetAccountManager.GetIdByGameAccountAsync(characterInfo.AccountId));
                                         }));
        }
    }
}