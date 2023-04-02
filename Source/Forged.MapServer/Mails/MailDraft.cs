// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.LootManagement;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Mails;

public class MailDraft
{
    private readonly string m_body;
    private readonly Dictionary<ulong, Item> m_items = new();
    private readonly uint m_mailTemplateId;
    private readonly string m_subject;
    private ulong m_COD;
    private bool m_mailTemplateItemsNeed;

    private ulong m_money;
    public MailDraft(uint mailTemplateId, bool need_items = true)
    {
        m_mailTemplateId = mailTemplateId;
        m_mailTemplateItemsNeed = need_items;
        m_money = 0;
        m_COD = 0;
    }

    public MailDraft(string subject, string body)
    {
        m_mailTemplateId = 0;
        m_mailTemplateItemsNeed = false;
        m_subject = subject;
        m_body = body;
        m_money = 0;
        m_COD = 0;
    }

    public MailDraft AddCOD(uint COD)
    {
        m_COD = COD;

        return this;
    }

    public MailDraft AddItem(Item item)
    {
        m_items[item.GUID.Counter] = item;

        return this;
    }

    public MailDraft AddMoney(ulong money)
    {
        m_money = money;

        return this;
    }

    public void SendMailTo(SQLTransaction trans, Player receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliver_delay = 0)
    {
        SendMailTo(trans, new MailReceiver(receiver), sender, checkMask, deliver_delay);
    }

    public void SendMailTo(SQLTransaction trans, MailReceiver receiver, MailSender sender, MailCheckMask checkMask = MailCheckMask.None, uint deliver_delay = 0)
    {
        var pReceiver = receiver.GetPlayer(); // can be NULL
        var pSender = sender.GetMailMessageType() == MailMessageType.Normal ? Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, sender.GetSenderId())) : null;

        if (pReceiver != null)
            PrepareItems(pReceiver, trans); // generate mail template items

        var mailId = Global.ObjectMgr.GenerateMailID();

        var deliver_time = GameTime.CurrentTime + deliver_delay;

        //expire time if COD 3 days, if no COD 30 days, if auction sale pending 1 hour
        uint expire_delay;

        // auction mail without any items and money
        if (sender.GetMailMessageType() == MailMessageType.Auction && m_items.Empty() && m_money == 0)
            expire_delay = GetDefaultValue("MailDeliveryDelay", Time.HOUR);
        // default case: expire time if COD 3 days, if no COD 30 days (or 90 days if sender is a game master)
        else if (m_COD != 0)
            expire_delay = 3 * Time.DAY;
        else
            expire_delay = (uint)(pSender != null && pSender.IsGameMaster ? 90 * Time.DAY : 30 * Time.DAY);

        var expire_time = deliver_time + expire_delay;

        // Add to DB
        byte index = 0;
        var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_MAIL);
        stmt.AddValue(index, mailId);
        stmt.AddValue(++index, (byte)sender.GetMailMessageType());
        stmt.AddValue(++index, (sbyte)sender.GetStationery());
        stmt.AddValue(++index, GetMailTemplateId());
        stmt.AddValue(++index, sender.GetSenderId());
        stmt.AddValue(++index, receiver.GetPlayerGUIDLow());
        stmt.AddValue(++index, GetSubject());
        stmt.AddValue(++index, GetBody());
        stmt.AddValue(++index, !m_items.Empty());
        stmt.AddValue(++index, expire_time);
        stmt.AddValue(++index, deliver_time);
        stmt.AddValue(++index, m_money);
        stmt.AddValue(++index, m_COD);
        stmt.AddValue(++index, (byte)checkMask);
        trans.Append(stmt);

        foreach (var item in m_items.Values)
        {
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_MAIL_ITEM);
            stmt.AddValue(0, mailId);
            stmt.AddValue(1, item.GUID.Counter);
            stmt.AddValue(2, receiver.GetPlayerGUIDLow());
            trans.Append(stmt);
        }

        // For online receiver update in game mail status and data
        if (pReceiver != null)
        {
            pReceiver.AddNewMailDeliverTime(deliver_time);


            Mail m = new()
            {
                messageID = mailId,
                mailTemplateId = GetMailTemplateId(),
                subject = GetSubject(),
                body = GetBody(),
                money = GetMoney(),
                COD = GetCOD()
            };

            foreach (var item in m_items.Values)
                m.AddItem(item.GUID.Counter, item.Entry);

            m.messageType = sender.GetMailMessageType();
            m.stationery = sender.GetStationery();
            m.sender = sender.GetSenderId();
            m.receiver = receiver.GetPlayerGUIDLow();
            m.expire_time = expire_time;
            m.deliver_time = deliver_time;
            m.checkMask = checkMask;
            m.state = MailState.Unchanged;

            pReceiver.AddMail(m); // to insert new mail to beginning of maillist

            if (!m_items.Empty())
                foreach (var item in m_items.Values)
                    pReceiver.AddMItem(item);
        }
        else if (!m_items.Empty())
        {
            DeleteIncludedItems(null);
        }
    }

    public void SendReturnToSender(uint senderAcc, ulong senderGuid, ulong receiver_guid, SQLTransaction trans)
    {
        var receiverGuid = ObjectGuid.Create(HighGuid.Player, receiver_guid);
        var receiver = Global.ObjAccessor.FindPlayer(receiverGuid);

        uint rc_account = 0;

        if (receiver == null)
            rc_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(receiverGuid);

        if (receiver == null && rc_account == 0) // sender not exist
        {
            DeleteIncludedItems(trans, true);

            return;
        }

        // prepare mail and send in other case
        var needItemDelay = false;

        if (!m_items.Empty())
        {
            // if item send to character at another account, then apply item delivery delay
            needItemDelay = senderAcc != rc_account;

            // set owner to new receiver (to prevent delete item with sender char deleting)
            foreach (var item in m_items.Values)
            {
                item.SaveToDB(trans); // item not in inventory and can be save standalone
                // owner in data will set at mail receive and item extracting
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                stmt.AddValue(0, receiver_guid);
                stmt.AddValue(1, item.GUID.Counter);
                trans.Append(stmt);
            }
        }

        // If theres is an item, there is a one hour delivery delay.
        var deliver_delay = needItemDelay ? GetDefaultValue("MailDeliveryDelay", Time.HOUR) : 0;

        // will delete item or place to receiver mail list
        SendMailTo(trans, new MailReceiver(receiver, receiver_guid), new MailSender(MailMessageType.Normal, senderGuid), MailCheckMask.Returned, deliver_delay);
    }
    private void DeleteIncludedItems(SQLTransaction trans, bool inDB = false)
    {
        foreach (var item in m_items.Values)
            if (inDB)
                item.DeleteFromDB(trans);

        m_items.Clear();
    }

    private string GetBody()
    {
        return m_body;
    }

    private ulong GetCOD()
    {
        return m_COD;
    }

    private uint GetMailTemplateId()
    {
        return m_mailTemplateId;
    }

    private ulong GetMoney()
    {
        return m_money;
    }

    private string GetSubject()
    {
        return m_subject;
    }

    private void PrepareItems(Player receiver, SQLTransaction trans)
    {
        if (m_mailTemplateId == 0 || !m_mailTemplateItemsNeed)
            return;

        m_mailTemplateItemsNeed = false;

        // The mail sent after turning in the quest The Good News and The Bad News contains 100g
        if (m_mailTemplateId == 123)
            m_money = 1000000;

        Loot mailLoot = new(null, ObjectGuid.Empty, LootType.None, null);

        // can be empty
        mailLoot.FillLoot(m_mailTemplateId, LootStoreBox.Mail, receiver, true, true, LootModes.Default, ItemContext.None);

        for (uint i = 0; m_items.Count < SharedConst.MaxMailItems && i < mailLoot.Items.Count; ++i)
        {
            var lootitem = mailLoot.LootItemInSlot(i, receiver);

            if (lootitem != null)
            {
                var item = Item.CreateItem(lootitem.Itemid, lootitem.Count, lootitem.Context, receiver);

                if (item != null)
                {
                    item.SaveToDB(trans); // save for prevent lost at next mail load, if send fail then item will deleted
                    AddItem(item);
                }
            }
        }
    }
}