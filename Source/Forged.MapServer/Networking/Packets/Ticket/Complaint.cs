// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Ticket;

internal class Complaint : ClientPacket
{
    public ComplaintChat Chat;
    public SupportSpamType ComplaintType;
    public ulong EventGuid;
    public ulong InviteGuid;
    public ulong MailID;
    public ComplaintOffender Offender;
    public Complaint(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ComplaintType = (SupportSpamType)_worldPacket.ReadUInt8();
        Offender.Read(_worldPacket);

        switch (ComplaintType)
        {
            case SupportSpamType.Mail:
                MailID = _worldPacket.ReadUInt64();

                break;
            case SupportSpamType.Chat:
                Chat.Read(_worldPacket);

                break;
            case SupportSpamType.Calendar:
                EventGuid = _worldPacket.ReadUInt64();
                InviteGuid = _worldPacket.ReadUInt64();

                break;
        }
    }

    public struct ComplaintChat
    {
        public uint ChannelID;

        public uint Command;

        public string MessageLog;

        public void Read(WorldPacket data)
        {
            Command = data.ReadUInt32();
            ChannelID = data.ReadUInt32();
            MessageLog = data.ReadString(data.ReadBits<uint>(12));
        }
    }

    public struct ComplaintOffender
    {
        public ObjectGuid PlayerGuid;

        public uint RealmAddress;

        public uint TimeSinceOffence;

        public void Read(WorldPacket data)
        {
            PlayerGuid = data.ReadPackedGuid();
            RealmAddress = data.ReadUInt32();
            TimeSinceOffence = data.ReadUInt32();
        }
    }
}