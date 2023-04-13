// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.LootManagement;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Mails;

public class MailDraft
{
    private readonly Dictionary<ulong, Item> _items = new();
    private bool _mailTemplateItemsNeed;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly IConfiguration _configuration;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CharacterCache _characterCache;
    private readonly LootFactory _lootFactory;

    public MailDraft(uint mailTemplateId, bool needItems, ObjectAccessor objectAccessor, GameObjectManager objectManager, IConfiguration configuration, CharacterDatabase characterDatabase,
                     CharacterCache characterCache, LootFactory lootFactory)
    {
        MailTemplateId = mailTemplateId;
        _mailTemplateItemsNeed = needItems;
        _objectAccessor = objectAccessor;
        _objectManager = objectManager;
        _configuration = configuration;
        _characterDatabase = characterDatabase;
        _characterCache = characterCache;
        _lootFactory = lootFactory;
    }

    public MailDraft(string subject, string body, ObjectAccessor objectAccessor, GameObjectManager objectManager, IConfiguration configuration, CharacterDatabase characterDatabase
                     , CharacterCache characterCache, LootFactory lootFactory)
    {
        _mailTemplateItemsNeed = false;
        Subject = subject;
        Body = body;
        _objectAccessor = objectAccessor;
        _objectManager = objectManager;
        _configuration = configuration;
        _characterDatabase = characterDatabase;
        _characterCache = characterCache;
        _lootFactory = lootFactory;
    }

    public MailDraft AddCod(uint cod)
    {
        Cod = cod;

        return this;
    }

    public MailDraft AddItem(Item item)
    {
        _items[item.GUID.Counter] = item;

        return this;
    }

    public MailDraft AddMoney(ulong money)
    {
        Money = money;

        return this;
    }

    public void SendMailTo(SQLTransaction trans, Player receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliverDelay = 0)
    {
        SendMailTo(trans, new MailReceiver(receiver), sender, checkMask, deliverDelay);
    }

    public void SendMailTo(SQLTransaction trans, MailReceiver receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliverDelay = 0)
    {
        var pReceiver = receiver.GetPlayer(); // can be NULL
        var pSender = sender.MailMessageType == MailMessageType.Normal ? _objectAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, sender.SenderId)) : null;

        if (pReceiver != null)
            PrepareItems(pReceiver, trans); // generate mail template items

        var mailId = _objectManager.GenerateMailID();

        var deliverTime = GameTime.CurrentTime + deliverDelay;

        //expire time if COD 3 days, if no COD 30 days, if auction sale pending 1 hour
        int expireDelay;

        // auction mail without any items and money
        if (sender.MailMessageType == MailMessageType.Auction && _items.Empty() && Money == 0)
            expireDelay = _configuration.GetDefaultValue("MailDeliveryDelay", Time.HOUR);
        // default case: expire time if COD 3 days, if no COD 30 days (or 90 days if sender is a GameInfo master)
        else if (Cod != 0)
            expireDelay = 3 * Time.DAY;
        else
            expireDelay = pSender is { IsGameMaster: true } ? 90 * Time.DAY : 30 * Time.DAY;

        var expireTime = deliverTime + expireDelay;

        // Add to DB
        byte index = 0;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_MAIL);
        stmt.AddValue(index, mailId);
        stmt.AddValue(++index, (byte)sender.MailMessageType);
        stmt.AddValue(++index, (sbyte)sender.Stationery);
        stmt.AddValue(++index, MailTemplateId);
        stmt.AddValue(++index, sender.SenderId);
        stmt.AddValue(++index, receiver.GetPlayerGUIDLow());
        stmt.AddValue(++index, Subject);
        stmt.AddValue(++index, Body);
        stmt.AddValue(++index, !_items.Empty());
        stmt.AddValue(++index, expireTime);
        stmt.AddValue(++index, deliverTime);
        stmt.AddValue(++index, Money);
        stmt.AddValue(++index, Cod);
        stmt.AddValue(++index, (byte)checkMask);
        trans.Append(stmt);

        foreach (var item in _items.Values)
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_MAIL_ITEM);
            stmt.AddValue(0, mailId);
            stmt.AddValue(1, item.GUID.Counter);
            stmt.AddValue(2, receiver.GetPlayerGUIDLow());
            trans.Append(stmt);
        }

        // For online receiver update in GameInfo mail status and data
        if (pReceiver != null)
        {
            pReceiver.AddNewMailDeliverTime(deliverTime);


            Mail m = new()
            {
                MessageID = mailId,
                MailTemplateId = MailTemplateId,
                Subject = Subject,
                Body = Body,
                Money = Money,
                Cod = Cod
            };

            foreach (var item in _items.Values)
                m.AddItem(item.GUID.Counter, item.Entry);

            m.MessageType = sender.MailMessageType;
            m.Stationery = sender.Stationery;
            m.Sender = sender.SenderId;
            m.Receiver = receiver.GetPlayerGUIDLow();
            m.ExpireTime = expireTime;
            m.DeliverTime = deliverTime;
            m.CheckMask = checkMask;
            m.State = MailState.Unchanged;

            pReceiver.AddMail(m); // to insert new mail to beginning of maillist

            if (!_items.Empty())
                foreach (var item in _items.Values)
                    pReceiver.AddMItem(item);
        }
        else if (!_items.Empty())
        {
            DeleteIncludedItems(null);
        }
    }

    public void SendReturnToSender(uint senderAcc, ulong senderGuid, ulong receiverGUID, SQLTransaction trans)
    {
        var receiverGuid = ObjectGuid.Create(HighGuid.Player, receiverGUID);
        var receiver = _objectAccessor.FindPlayer(receiverGuid);

        uint rcAccount = 0;

        if (receiver == null)
            rcAccount = _characterCache.GetCharacterAccountIdByGuid(receiverGuid);

        if (receiver == null && rcAccount == 0) // sender not exist
        {
            DeleteIncludedItems(trans, true);

            return;
        }

        // prepare mail and send in other case
        var needItemDelay = false;

        if (!_items.Empty())
        {
            // if item send to character at another account, then apply item delivery delay
            needItemDelay = senderAcc != rcAccount;

            // set owner to new receiver (to prevent delete item with sender char deleting)
            foreach (var item in _items.Values)
            {
                item.SaveToDB(trans); // item not in inventory and can be save standalone
                // owner in data will set at mail receive and item extracting
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                stmt.AddValue(0, receiverGUID);
                stmt.AddValue(1, item.GUID.Counter);
                trans.Append(stmt);
            }
        }

        // If theres is an item, there is a one hour delivery delay.
        var deliverDelay = needItemDelay ? _configuration.GetDefaultValue("MailDeliveryDelay", Time.UHOUR) : 0;

        // will delete item or place to receiver mail list
        SendMailTo(trans, new MailReceiver(receiver, receiverGUID), new MailSender(MailMessageType.Normal, senderGuid), MailCheckMask.Returned, deliverDelay);
    }

    private void DeleteIncludedItems(SQLTransaction trans, bool inDB = false)
    {
        if (inDB)
            foreach (var item in _items.Values)
                item.DeleteFromDB(trans);

        _items.Clear();
    }

    public string Body { get; }

    public ulong Cod { get; set; }

    public uint MailTemplateId { get; }

    public ulong Money { get; set; }

    public string Subject { get; }

    private void PrepareItems(Player receiver, SQLTransaction trans)
    {
        if (MailTemplateId == 0 || !_mailTemplateItemsNeed)
            return;

        _mailTemplateItemsNeed = false;

        // The mail sent after turning in the quest The Good News and The Bad News contains 100g
        if (MailTemplateId == 123)
            Money = 1000000;

        var mailLoot = _lootFactory.GenerateLoot(null, ObjectGuid.Empty, LootType.None, null, MailTemplateId, LootStorageType.Mail, receiver, true, true);

        for (uint i = 0; _items.Count < SharedConst.MaxMailItems && i < mailLoot.Items.Count; ++i)
        {
            var lootitem = mailLoot.LootItemInSlot(i, receiver);

            if (lootitem == null)
                continue;

            var item = Item.CreateItem(lootitem.Itemid, lootitem.Count, lootitem.Context, receiver);

            if (item == null)
                continue;

            item.SaveToDB(trans); // save for prevent lost at next mail load, if send fail then item will deleted
            AddItem(item);
        }
    }
}