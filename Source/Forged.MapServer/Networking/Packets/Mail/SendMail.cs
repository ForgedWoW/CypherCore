// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Mail;

public class SendMail : ClientPacket
{
    public StructSendMail Info;

    public SendMail(WorldPacket packet) : base(packet)
    {
        Info = new StructSendMail();
    }

    public override void Read()
    {
        Info.Mailbox = WorldPacket.ReadPackedGuid();
        Info.StationeryID = WorldPacket.ReadInt32();
        Info.SendMoney = WorldPacket.ReadInt64();
        Info.Cod = WorldPacket.ReadInt64();

        var targetLength = WorldPacket.ReadBits<uint>(9);
        var subjectLength = WorldPacket.ReadBits<uint>(9);
        var bodyLength = WorldPacket.ReadBits<uint>(11);

        var count = WorldPacket.ReadBits<uint>(5);

        Info.Target = WorldPacket.ReadString(targetLength);
        Info.Subject = WorldPacket.ReadString(subjectLength);
        Info.Body = WorldPacket.ReadString(bodyLength);

        for (var i = 0; i < count; ++i)
        {
            var att = new StructSendMail.MailAttachment
            {
                AttachPosition = WorldPacket.ReadUInt8(),
                ItemGUID = WorldPacket.ReadPackedGuid()
            };

            Info.Attachments.Add(att);
        }
    }

    public class StructSendMail
    {
        public List<MailAttachment> Attachments = new();
        public string Body;
        public long Cod;
        public ObjectGuid Mailbox;
        public long SendMoney;
        public int StationeryID;
        public string Subject;
        public string Target;

        public struct MailAttachment
        {
            public byte AttachPosition;
            public ObjectGuid ItemGUID;
        }
    }
}