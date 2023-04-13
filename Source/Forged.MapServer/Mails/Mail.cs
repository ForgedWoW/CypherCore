// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;

namespace Forged.MapServer.Mails;

public class Mail
{
    public string Body { get; set; }
    public MailCheckMask CheckMask { get; set; }
    public ulong Cod { get; set; }
    public long DeliverTime { get; set; }
    public long ExpireTime { get; set; }
    public List<MailItemInfo> Items { get; set; } = new();
    public uint MailTemplateId { get; set; }
    public ulong MessageID { get; set; }
    public MailMessageType MessageType { get; set; }
    public ulong Money { get; set; }
    public ulong Receiver { get; set; }
    public List<ulong> RemovedItems { get; set; } = new();
    public ulong Sender { get; set; }
    public MailState State { get; set; }
    public MailStationery Stationery { get; set; }
    public string Subject { get; set; }

    public void AddItem(ulong itemGuidLow, uint itemTemplate)
    {
        MailItemInfo mii = new()
        {
            ItemGUID = itemGuidLow,
            ItemTemplate = itemTemplate
        };

        Items.Add(mii);
    }

    public bool HasItems()
    {
        return !Items.Empty();
    }

    public bool RemoveItem(ulong itemGuid)
    {
        foreach (var item in Items.Where(item => item.ItemGUID == itemGuid))
        {
            Items.Remove(item);

            return true;
        }

        return false;
    }
}