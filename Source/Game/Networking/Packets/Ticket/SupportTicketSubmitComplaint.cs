// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets;

public class SupportTicketSubmitComplaint : ClientPacket
{
	public SupportTicketHeader Header;
	public SupportTicketChatLog ChatLog;
	public ObjectGuid TargetCharacterGUID;
	public int ReportType;
	public int MajorCategory;
	public int MinorCategoryFlags;
	public string Note;
	public SupportTicketHorusChatLog HorusChatLog;
	public SupportTicketMailInfo? MailInfo;
	public SupportTicketCalendarEventInfo? CalenderInfo;
	public SupportTicketPetInfo? PetInfo;
	public SupportTicketGuildInfo? GuildInfo;
	public SupportTicketLFGListSearchResult? LFGListSearchResult;
	public SupportTicketLFGListApplicant? LFGListApplicant;
	public SupportTicketCommunityMessage? CommunityMessage;
	public SupportTicketClubFinderResult? ClubFinderResult;
	public SupportTicketUnused910? Unused910;
	public SupportTicketSubmitComplaint(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Header.Read(_worldPacket);
		TargetCharacterGUID = _worldPacket.ReadPackedGuid();
		ReportType = _worldPacket.ReadInt32();
		MajorCategory = _worldPacket.ReadInt32();
		MinorCategoryFlags = _worldPacket.ReadInt32();
		ChatLog.Read(_worldPacket);

		var noteLength = _worldPacket.ReadBits<uint>(10);
		var hasMailInfo = _worldPacket.HasBit();
		var hasCalendarInfo = _worldPacket.HasBit();
		var hasPetInfo = _worldPacket.HasBit();
		var hasGuildInfo = _worldPacket.HasBit();
		var hasLFGListSearchResult = _worldPacket.HasBit();
		var hasLFGListApplicant = _worldPacket.HasBit();
		var hasClubMessage = _worldPacket.HasBit();
		var hasClubFinderResult = _worldPacket.HasBit();
		var hasUnk910 = _worldPacket.HasBit();

		_worldPacket.ResetBitPos();

		if (hasClubMessage)
		{
			SupportTicketCommunityMessage communityMessage = new();
			communityMessage.IsPlayerUsingVoice = _worldPacket.HasBit();
			CommunityMessage = communityMessage;
			_worldPacket.ResetBitPos();
		}

		HorusChatLog.Read(_worldPacket);

		Note = _worldPacket.ReadString(noteLength);

		if (hasMailInfo)
		{
			MailInfo = new SupportTicketMailInfo();
			MailInfo.Value.Read(_worldPacket);
		}

		if (hasCalendarInfo)
		{
			CalenderInfo = new SupportTicketCalendarEventInfo();
			CalenderInfo.Value.Read(_worldPacket);
		}

		if (hasPetInfo)
		{
			PetInfo = new SupportTicketPetInfo();
			PetInfo.Value.Read(_worldPacket);
		}

		if (hasGuildInfo)
		{
			GuildInfo = new SupportTicketGuildInfo();
			GuildInfo.Value.Read(_worldPacket);
		}

		if (hasLFGListSearchResult)
		{
			LFGListSearchResult = new SupportTicketLFGListSearchResult();
			LFGListSearchResult.Value.Read(_worldPacket);
		}

		if (hasLFGListApplicant)
		{
			LFGListApplicant = new SupportTicketLFGListApplicant();
			LFGListApplicant.Value.Read(_worldPacket);
		}

		if (hasClubFinderResult)
		{
			ClubFinderResult = new SupportTicketClubFinderResult();
			ClubFinderResult.Value.Read(_worldPacket);
		}

		if (hasUnk910)
		{
			Unused910 = new SupportTicketUnused910();
			Unused910.Value.Read(_worldPacket);
		}
	}

	public struct SupportTicketChatLine
	{
		public long Timestamp;
		public string Text;

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

	public struct SupportTicketHorusChatLine
	{
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
				SenderRealm senderRealm = new();
				senderRealm.VirtualRealmAddress = data.ReadUInt32();
				senderRealm.field_4 = data.ReadUInt16();
				senderRealm.field_6 = data.ReadUInt8();
				RealmAddress = senderRealm;
			}

			if (hasSlashCmd)
				SlashCmd = data.ReadInt32();

			Text = data.ReadString(textLength);
		}

		public struct SenderRealm
		{
			public uint VirtualRealmAddress;
			public ushort field_4;
			public byte field_6;
		}

		public long Timestamp;
		public ObjectGuid AuthorGUID;
		public ulong? ClubID;
		public ObjectGuid? ChannelGUID;
		public SenderRealm? RealmAddress;
		public int? SlashCmd;
		public string Text;
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

	public struct SupportTicketMailInfo
	{
		public void Read(WorldPacket data)
		{
			MailID = data.ReadUInt64();
			var bodyLength = data.ReadBits<uint>(13);
			var subjectLength = data.ReadBits<uint>(9);

			MailBody = data.ReadString(bodyLength);
			MailSubject = data.ReadString(subjectLength);
		}

		public ulong MailID;
		public string MailSubject;
		public string MailBody;
	}

	public struct SupportTicketCalendarEventInfo
	{
		public void Read(WorldPacket data)
		{
			EventID = data.ReadUInt64();
			InviteID = data.ReadUInt64();

			EventTitle = data.ReadString(data.ReadBits<byte>(8));
		}

		public ulong EventID;
		public ulong InviteID;
		public string EventTitle;
	}

	public struct SupportTicketPetInfo
	{
		public void Read(WorldPacket data)
		{
			PetID = data.ReadPackedGuid();

			PetName = data.ReadString(data.ReadBits<byte>(8));
		}

		public ObjectGuid PetID;
		public string PetName;
	}

	public struct SupportTicketGuildInfo
	{
		public void Read(WorldPacket data)
		{
			var nameLength = data.ReadBits<byte>(8);
			GuildID = data.ReadPackedGuid();

			GuildName = data.ReadString(nameLength);
		}

		public ObjectGuid GuildID;
		public string GuildName;
	}

	public struct SupportTicketLFGListSearchResult
	{
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

		public RideTicket RideTicket;
		public uint GroupFinderActivityID;
		public ObjectGuid LastTitleAuthorGuid;
		public ObjectGuid LastDescriptionAuthorGuid;
		public ObjectGuid LastVoiceChatAuthorGuid;
		public ObjectGuid ListingCreatorGuid;
		public ObjectGuid Unknown735;
		public string Title;
		public string Description;
		public string VoiceChat;
	}

	public struct SupportTicketLFGListApplicant
	{
		public void Read(WorldPacket data)
		{
			RideTicket = new RideTicket();
			RideTicket.Read(data);

			Comment = data.ReadString(data.ReadBits<uint>(9));
		}

		public RideTicket RideTicket;
		public string Comment;
	}

	public struct SupportTicketCommunityMessage
	{
		public bool IsPlayerUsingVoice;
	}

	public struct SupportTicketClubFinderResult
	{
		public ulong ClubFinderPostingID;
		public ulong ClubID;
		public ObjectGuid ClubFinderGUID;
		public string ClubName;

		public void Read(WorldPacket data)
		{
			ClubFinderPostingID = data.ReadUInt64();
			ClubID = data.ReadUInt64();
			ClubFinderGUID = data.ReadPackedGuid();
			ClubName = data.ReadString(data.ReadBits<uint>(12));
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
}