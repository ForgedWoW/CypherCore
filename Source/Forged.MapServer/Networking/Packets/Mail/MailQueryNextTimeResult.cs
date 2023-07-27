// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailQueryNextTimeResult : ServerPacket
{
    public List<MailNextTimeEntry> Next;
    public float NextMailTime;

    public MailQueryNextTimeResult() : base(ServerOpcodes.MailQueryNextTimeResult)
    {
        Next = new List<MailNextTimeEntry>();
    }

    public override void Write()
    {
        WorldPacket.WriteFloat(NextMailTime);
        WorldPacket.WriteInt32(Next.Count);

        foreach (var entry in Next)
        {
            WorldPacket.WritePackedGuid(entry.SenderGuid);
            WorldPacket.WriteFloat(entry.TimeLeft);
            WorldPacket.WriteInt32(entry.AltSenderID);
            WorldPacket.WriteInt8(entry.AltSenderType);
            WorldPacket.WriteInt32(entry.StationeryID);
        }
    }

    public class MailNextTimeEntry
    {
        public int AltSenderID;
        public sbyte AltSenderType;
        public ObjectGuid SenderGuid;
        public int StationeryID;
        public float TimeLeft;

        public MailNextTimeEntry(Mails.Mail mail)
        {
            switch (mail.MessageType)
            {
                case MailMessageType.Normal:
                    SenderGuid = ObjectGuid.Create(HighGuid.Player, mail.Sender);

                    break;
                case MailMessageType.Auction:
                case MailMessageType.Creature:
                case MailMessageType.Gameobject:
                case MailMessageType.Calendar:
                case MailMessageType.Blackmarket:
                case MailMessageType.CommerceAuction:
                case MailMessageType.Auction2:
                case MailMessageType.ArtisansConsortium:
                    AltSenderID = (int)mail.Sender;

                    break;
                default:
                    break;
            }

            TimeLeft = mail.DeliverTime - GameTime.CurrentTime;
            AltSenderType = (sbyte)mail.MessageType;
            StationeryID = (int)mail.Stationery;
        }
    }
}