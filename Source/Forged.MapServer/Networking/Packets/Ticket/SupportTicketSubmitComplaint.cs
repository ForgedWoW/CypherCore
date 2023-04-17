// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.LFG;

namespace Forged.MapServer.Networking.Packets.Ticket;

public class SupportTicketSubmitComplaint : ClientPacket
{
    public SupportTicketCalendarEventInfo? CalenderInfo;
    public SupportTicketChatLog ChatLog;
    public SupportTicketClubFinderResult? ClubFinderResult;
    public SupportTicketCommunityMessage? CommunityMessage;
    public SupportTicketGuildInfo? GuildInfo;
    public SupportTicketHeader Header;
    public SupportTicketHorusChatLog HorusChatLog;
    public SupportTicketLFGListApplicant? LFGListApplicant;
    public SupportTicketLFGListSearchResult? LFGListSearchResult;
    public SupportTicketMailInfo? MailInfo;
    public int MajorCategory;
    public int MinorCategoryFlags;
    public string Note;
    public SupportTicketPetInfo? PetInfo;
    public int ReportType;
    public ObjectGuid TargetCharacterGUID;
    public SupportTicketUnused910? Unused910;
    public SupportTicketSubmitComplaint(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Header.Read(WorldPacket);
        TargetCharacterGUID = WorldPacket.ReadPackedGuid();
        ReportType = WorldPacket.ReadInt32();
        MajorCategory = WorldPacket.ReadInt32();
        MinorCategoryFlags = WorldPacket.ReadInt32();
        ChatLog.Read(WorldPacket);

        var noteLength = WorldPacket.ReadBits<uint>(10);
        var hasMailInfo = WorldPacket.HasBit();
        var hasCalendarInfo = WorldPacket.HasBit();
        var hasPetInfo = WorldPacket.HasBit();
        var hasGuildInfo = WorldPacket.HasBit();
        var hasLFGListSearchResult = WorldPacket.HasBit();
        var hasLFGListApplicant = WorldPacket.HasBit();
        var hasClubMessage = WorldPacket.HasBit();
        var hasClubFinderResult = WorldPacket.HasBit();
        var hasUnk910 = WorldPacket.HasBit();

        WorldPacket.ResetBitPos();

        if (hasClubMessage)
        {
            SupportTicketCommunityMessage communityMessage = new()
            {
                IsPlayerUsingVoice = WorldPacket.HasBit()
            };

            CommunityMessage = communityMessage;
            WorldPacket.ResetBitPos();
        }

        HorusChatLog.Read(WorldPacket);

        Note = WorldPacket.ReadString(noteLength);

        if (hasMailInfo)
        {
            MailInfo = new SupportTicketMailInfo();
            MailInfo.Value.Read(WorldPacket);
        }

        if (hasCalendarInfo)
        {
            CalenderInfo = new SupportTicketCalendarEventInfo();
            CalenderInfo.Value.Read(WorldPacket);
        }

        if (hasPetInfo)
        {
            PetInfo = new SupportTicketPetInfo();
            PetInfo.Value.Read(WorldPacket);
        }

        if (hasGuildInfo)
        {
            GuildInfo = new SupportTicketGuildInfo();
            GuildInfo.Value.Read(WorldPacket);
        }

        if (hasLFGListSearchResult)
        {
            LFGListSearchResult = new SupportTicketLFGListSearchResult();
            LFGListSearchResult.Value.Read(WorldPacket);
        }

        if (hasLFGListApplicant)
        {
            LFGListApplicant = new SupportTicketLFGListApplicant();
            LFGListApplicant.Value.Read(WorldPacket);
        }

        if (hasClubFinderResult)
        {
            ClubFinderResult = new SupportTicketClubFinderResult();
            ClubFinderResult.Value.Read(WorldPacket);
        }

        if (hasUnk910)
        {
            Unused910 = new SupportTicketUnused910();
            Unused910.Value.Read(WorldPacket);
        }
    }

    public struct SupportTicketCalendarEventInfo
    {
        public ulong EventID;

        public string EventTitle;

        public ulong InviteID;

        public void Read(WorldPacket data)
        {
            EventID = data.ReadUInt64();
            InviteID = data.ReadUInt64();

            EventTitle = data.ReadString(data.ReadBits<byte>(8));
        }
    }

    public struct SupportTicketChatLine
    {
        public string Text;
        public long Timestamp;

        public SupportTicketChatLine(WorldPacket data)
        {
            Timestamp = data.ReadInt64();
            Text = data.ReadString(data.ReadBits<uint>(12));
        }

        public SupportTicketChatLine(long timestamp, string text)
        {
            Timestamp = timestamp;
            Text = text;
        }

        public void Read(WorldPacket data)
        {
            Timestamp = data.ReadUInt32();
            Text = data.ReadString(data.ReadBits<uint>(12));
        }
    }

    public struct SupportTicketClubFinderResult
    {
        public ObjectGuid ClubFinderGUID;
        public ulong ClubFinderPostingID;
        public ulong ClubID;
        public string ClubName;

        public void Read(WorldPacket data)
        {
            ClubFinderPostingID = data.ReadUInt64();
            ClubID = data.ReadUInt64();
            ClubFinderGUID = data.ReadPackedGuid();
            ClubName = data.ReadString(data.ReadBits<uint>(12));
        }
    }

    public struct SupportTicketCommunityMessage
    {
        public bool IsPlayerUsingVoice;
    }

    public struct SupportTicketGuildInfo
    {
        public ObjectGuid GuildID;

        public string GuildName;

        public void Read(WorldPacket data)
        {
            var nameLength = data.ReadBits<byte>(8);
            GuildID = data.ReadPackedGuid();

            GuildName = data.ReadString(nameLength);
        }
    }

    public struct SupportTicketHorusChatLine
    {
        public ObjectGuid AuthorGUID;

        public ObjectGuid? ChannelGUID;

        public ulong? ClubID;

        public SenderRealm? RealmAddress;

        public int? SlashCmd;

        public string Text;

        public long Timestamp;

        public void Read(WorldPacket data)
        {
            Timestamp = data.ReadInt64();
            AuthorGUID = data.ReadPackedGuid();

            var hasClubID = data.HasBit();
            var hasChannelGUID = data.HasBit();
            var hasRealmAddress = data.HasBit();
            var hasSlashCmd = data.HasBit();
            var textLength = data.ReadBits<uint>(12);

            if (hasClubID)
                ClubID = data.ReadUInt64();

            if (hasChannelGUID)
                ChannelGUID = data.ReadPackedGuid();

            if (hasRealmAddress)
            {
                SenderRealm senderRealm = new()
                {
                    VirtualRealmAddress = data.ReadUInt32(),
                    field_4 = data.ReadUInt16(),
                    field_6 = data.ReadUInt8()
                };

                RealmAddress = senderRealm;
            }

            if (hasSlashCmd)
                SlashCmd = data.ReadInt32();

            Text = data.ReadString(textLength);
        }

        public struct SenderRealm
        {
            public ushort field_4;
            public byte field_6;
            public uint VirtualRealmAddress;
        }
    }

    public struct SupportTicketLFGListApplicant
    {
        public string Comment;

        public RideTicket RideTicket;

        public void Read(WorldPacket data)
        {
            RideTicket = new RideTicket();
            RideTicket.Read(data);

            Comment = data.ReadString(data.ReadBits<uint>(9));
        }
    }

    public struct SupportTicketLFGListSearchResult
    {
        public string Description;

        public uint GroupFinderActivityID;

        public ObjectGuid LastDescriptionAuthorGuid;

        public ObjectGuid LastTitleAuthorGuid;

        public ObjectGuid LastVoiceChatAuthorGuid;

        public ObjectGuid ListingCreatorGuid;

        public RideTicket RideTicket;

        public string Title;

        public ObjectGuid Unknown735;

        public string VoiceChat;

        public void Read(WorldPacket data)
        {
            RideTicket = new RideTicket();
            RideTicket.Read(data);

            GroupFinderActivityID = data.ReadUInt32();
            LastTitleAuthorGuid = data.ReadPackedGuid();
            LastDescriptionAuthorGuid = data.ReadPackedGuid();
            LastVoiceChatAuthorGuid = data.ReadPackedGuid();
            ListingCreatorGuid = data.ReadPackedGuid();
            Unknown735 = data.ReadPackedGuid();

            var titleLength = data.ReadBits<byte>(10);
            var descriptionLength = data.ReadBits<byte>(11);
            var voiceChatLength = data.ReadBits<byte>(8);

            Title = data.ReadString(titleLength);
            Description = data.ReadString(descriptionLength);
            VoiceChat = data.ReadString(voiceChatLength);
        }
    }

    public struct SupportTicketMailInfo
    {
        public string MailBody;

        public ulong MailID;

        public string MailSubject;

        public void Read(WorldPacket data)
        {
            MailID = data.ReadUInt64();
            var bodyLength = data.ReadBits<uint>(13);
            var subjectLength = data.ReadBits<uint>(9);

            MailBody = data.ReadString(bodyLength);
            MailSubject = data.ReadString(subjectLength);
        }
    }

    public struct SupportTicketPetInfo
    {
        public ObjectGuid PetID;

        public string PetName;

        public void Read(WorldPacket data)
        {
            PetID = data.ReadPackedGuid();

            PetName = data.ReadString(data.ReadBits<byte>(8));
        }
    }

    public struct SupportTicketUnused910
    {
        public string field_0;
        public ObjectGuid field_104;

        public void Read(WorldPacket data)
        {
            var field_0Length = data.ReadBits<uint>(7);
            field_104 = data.ReadPackedGuid();
            field_0 = data.ReadString(field_0Length);
        }
    }

    public class SupportTicketChatLog
    {
        public List<SupportTicketChatLine> Lines = new();
        public uint? ReportLineIndex;

        public void Read(WorldPacket data)
        {
            var linesCount = data.ReadUInt32();
            var hasReportLineIndex = data.HasBit();

            data.ResetBitPos();

            for (uint i = 0; i < linesCount; i++)
                Lines.Add(new SupportTicketChatLine(data));

            if (hasReportLineIndex)
                ReportLineIndex = data.ReadUInt32();
        }
    }

    public class SupportTicketHorusChatLog
    {
        public List<SupportTicketHorusChatLine> Lines = new();

        public void Read(WorldPacket data)
        {
            var linesCount = data.ReadUInt32();
            data.ResetBitPos();

            for (uint i = 0; i < linesCount; i++)
            {
                var chatLine = new SupportTicketHorusChatLine();
                chatLine.Read(data);
                Lines.Add(chatLine);
            }
        }
    }
}