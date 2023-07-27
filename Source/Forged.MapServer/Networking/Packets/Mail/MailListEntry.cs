// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Mails;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailListEntry
{
    public uint? AltSenderID;
    public List<MailAttachedItem> Attachments = new();
    public string Body = "";
    public ulong Cod;
    public float DaysLeft;
    public int Flags;
    public ulong MailID;
    public int MailTemplateID;
    public ObjectGuid? SenderCharacter;
    public byte SenderType;
    public ulong SentMoney;
    public int StationeryID;
    public string Subject = "";

    public MailListEntry(Mails.Mail mail, Player player)
    {
        MailID = mail.MessageID;
        SenderType = (byte)mail.MessageType;

        switch (mail.MessageType)
        {
            case MailMessageType.Normal:
                SenderCharacter = ObjectGuid.Create(HighGuid.Player, mail.Sender);

                break;
            case MailMessageType.Creature:
            case MailMessageType.Gameobject:
            case MailMessageType.Auction:
            case MailMessageType.Calendar:
            case MailMessageType.Blackmarket:
            case MailMessageType.CommerceAuction:
            case MailMessageType.Auction2:
            case MailMessageType.ArtisansConsortium:
                AltSenderID = (uint)mail.Sender;

                break;
        }

        Cod = mail.Cod;
        StationeryID = (int)mail.Stationery;
        SentMoney = mail.Money;
        Flags = (int)mail.CheckMask;
        DaysLeft = (float)(mail.ExpireTime - GameTime.CurrentTime) / Time.DAY;
        MailTemplateID = (int)mail.MailTemplateId;
        Subject = mail.Subject;
        Body = mail.Body;

        for (byte i = 0; i < mail.Items.Count; i++)
        {
            var item = player.GetMItem(mail.Items[i].ItemGUID);

            if (item != null)
                Attachments.Add(new MailAttachedItem(item, i));
        }
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt64(MailID);
        data.WriteUInt32(SenderType);
        data.WriteUInt64(Cod);
        data.WriteInt32(StationeryID);
        data.WriteUInt64(SentMoney);
        data.WriteInt32(Flags);
        data.WriteFloat(DaysLeft);
        data.WriteInt32(MailTemplateID);
        data.WriteInt32(Attachments.Count);

        switch ((MailMessageType)SenderType)
        {
            case MailMessageType.Normal:
                data.WritePackedGuid(SenderCharacter.Value);

                break;
            case MailMessageType.Creature:
            case MailMessageType.Gameobject:
            case MailMessageType.Auction:
            case MailMessageType.Calendar:
            case MailMessageType.Blackmarket:
            case MailMessageType.CommerceAuction:
            case MailMessageType.Auction2:
            case MailMessageType.ArtisansConsortium:
                data.WriteUInt32(AltSenderID.Value);

                break;
        }

        data.WriteBits(Subject.GetByteCount(), 8);
        data.WriteBits(Body.GetByteCount(), 13);
        data.FlushBits();

        Attachments.ForEach(p => p.Write(data));
        
        data.WriteString(Subject);
        data.WriteString(Body);
    }
}