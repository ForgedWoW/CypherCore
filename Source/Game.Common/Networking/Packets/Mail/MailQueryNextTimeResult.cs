// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Mails;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Mail;

namespace Game.Common.Networking.Packets.Mail;

public class MailQueryNextTimeResult : ServerPacket
{
	public float NextMailTime;
	public List<MailNextTimeEntry> Next;

	public MailQueryNextTimeResult() : base(ServerOpcodes.MailQueryNextTimeResult)
	{
		Next = new List<MailNextTimeEntry>();
	}

	public override void Write()
	{
		_worldPacket.WriteFloat(NextMailTime);
		_worldPacket.WriteInt32(Next.Count);

		foreach (var entry in Next)
		{
			_worldPacket.WritePackedGuid(entry.SenderGuid);
			_worldPacket.WriteFloat(entry.TimeLeft);
			_worldPacket.WriteInt32(entry.AltSenderID);
			_worldPacket.WriteInt8(entry.AltSenderType);
			_worldPacket.WriteInt32(entry.StationeryID);
		}
	}

	public class MailNextTimeEntry
	{
		public ObjectGuid SenderGuid;
		public float TimeLeft;
		public int AltSenderID;
		public sbyte AltSenderType;
		public int StationeryID;

		public MailNextTimeEntry(Mail mail)
		{
			switch (mail.messageType)
			{
				case MailMessageType.Normal:
					SenderGuid = ObjectGuid.Create(HighGuid.Player, mail.sender);

					break;
				case MailMessageType.Auction:
				case MailMessageType.Creature:
				case MailMessageType.Gameobject:
				case MailMessageType.Calendar:
					AltSenderID = (int)mail.sender;

					break;
			}

			TimeLeft = mail.deliver_time - GameTime.GetGameTime();
			AltSenderType = (sbyte)mail.messageType;
			StationeryID = (int)mail.stationery;
		}
	}
}
