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
        Info.Mailbox = _worldPacket.ReadPackedGuid();
        Info.StationeryID = _worldPacket.ReadInt32();
        Info.SendMoney = _worldPacket.ReadInt64();
        Info.Cod = _worldPacket.ReadInt64();

        var targetLength = _worldPacket.ReadBits<uint>(9);
        var subjectLength = _worldPacket.ReadBits<uint>(9);
        var bodyLength = _worldPacket.ReadBits<uint>(11);

        var count = _worldPacket.ReadBits<uint>(5);

        Info.Target = _worldPacket.ReadString(targetLength);
        Info.Subject = _worldPacket.ReadString(subjectLength);
        Info.Body = _worldPacket.ReadString(bodyLength);

        for (var i = 0; i < count; ++i)
        {
            var att = new StructSendMail.MailAttachment()
            {
                AttachPosition = _worldPacket.ReadUInt8(),
                ItemGUID = _worldPacket.ReadPackedGuid()
            };

            Info.Attachments.Add(att);
        }
    }

    public class StructSendMail
    {
        public ObjectGuid Mailbox;
        public int StationeryID;
        public long SendMoney;
        public long Cod;
        public string Target;
        public string Subject;
        public string Body;
        public List<MailAttachment> Attachments = new();

        public struct MailAttachment
        {
            public byte AttachPosition;
            public ObjectGuid ItemGUID;
        }
    }
}