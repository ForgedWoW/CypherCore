// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Mails;

public class Mail
{
    public string body;
    public MailCheckMask checkMask;
    public ulong COD;
    public long deliver_time;
    public long expire_time;
    public List<MailItemInfo> items = new();
    public uint mailTemplateId;
    public ulong messageID;
    public MailMessageType messageType;
    public ulong money;
    public ulong receiver;
    public List<ulong> removedItems = new();
    public ulong sender;
    public MailState state;
    public MailStationery stationery;
    public string subject;

    public void AddItem(ulong itemGuidLow, uint item_template)
    {
        MailItemInfo mii = new()
        {
            item_guid = itemGuidLow,
            item_template = item_template
        };

        items.Add(mii);
    }

    public bool HasItems()
    {
        return !items.Empty();
    }

    public bool RemoveItem(ulong itemGuid)
    {
        foreach (var item in items)
            if (item.item_guid == itemGuid)
            {
                items.Remove(item);

                return true;
            }

        return false;
    }
}