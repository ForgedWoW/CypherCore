// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Mails;

namespace Forged.RealmServer.Networking.Packets;

public class MailListEntry
{
	public ulong MailID;
	public byte SenderType;
	public ObjectGuid? SenderCharacter;
	public uint? AltSenderID;
	public ulong Cod;
	public int StationeryID;
	public ulong SentMoney;
	public int Flags;
	public float DaysLeft;
	public int MailTemplateID;
	public string Subject = "";
	public string Body = "";
	public List<MailAttachedItem> Attachments = new();

	public MailListEntry(Mail mail, Player player)
	{
		MailID = mail.messageID;
		SenderType = (byte)mail.messageType;

		switch (mail.messageType)
		{
			case MailMessageType.Normal:
				SenderCharacter = ObjectGuid.Create(HighGuid.Player, mail.sender);

				break;
			case MailMessageType.Creature:
			case MailMessageType.Gameobject:
			case MailMessageType.Auction:
			case MailMessageType.Calendar:
				AltSenderID = (uint)mail.sender;

				break;
		}

		Cod = mail.COD;
		StationeryID = (int)mail.stationery;
		SentMoney = mail.money;
		Flags = (int)mail.checkMask;
		DaysLeft = (float)(mail.expire_time - _gameTime.GetGameTime) / Time.Day;
		MailTemplateID = (int)mail.mailTemplateId;
		Subject = mail.subject;
		Body = mail.body;

		for (byte i = 0; i < mail.items.Count; i++)
		{
			var item = player.GetMItem(mail.items[i].item_guid);

			if (item)
				Attachments.Add(new MailAttachedItem(item, i));
		}
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt64(MailID);
		data.WriteUInt8(SenderType);
		data.WriteUInt64(Cod);
		data.WriteInt32(StationeryID);
		data.WriteUInt64(SentMoney);
		data.WriteInt32(Flags);
		data.WriteFloat(DaysLeft);
		data.WriteInt32(MailTemplateID);
		data.WriteInt32(Attachments.Count);

		data.WriteBit(SenderCharacter.HasValue);
		data.WriteBit(AltSenderID.HasValue);
		data.WriteBits(Subject.GetByteCount(), 8);
		data.WriteBits(Body.GetByteCount(), 13);
		data.FlushBits();

		Attachments.ForEach(p => p.Write(data));

		if (SenderCharacter.HasValue)
			data.WritePackedGuid(SenderCharacter.Value);

		if (AltSenderID.HasValue)
			data.WriteUInt32(AltSenderID.Value);

		data.WriteString(Subject);
		data.WriteString(Body);
	}
}