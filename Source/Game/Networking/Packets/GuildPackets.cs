﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class QueryGuildInfo : ClientPacket
{
	public ObjectGuid GuildGuid;
	public ObjectGuid PlayerGuid;
	public QueryGuildInfo(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGuid = _worldPacket.ReadPackedGuid();
		PlayerGuid = _worldPacket.ReadPackedGuid();
	}
}

public class QueryGuildInfoResponse : ServerPacket
{
	public ObjectGuid GuildGUID;
	public GuildInfo Info = new();
	public bool HasGuildInfo;
	public QueryGuildInfoResponse() : base(ServerOpcodes.QueryGuildInfoResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteBit(HasGuildInfo);
		_worldPacket.FlushBits();

		if (HasGuildInfo)
		{
			_worldPacket.WritePackedGuid(Info.GuildGuid);
			_worldPacket.WriteUInt32(Info.VirtualRealmAddress);
			_worldPacket.WriteInt32(Info.Ranks.Count);
			_worldPacket.WriteUInt32(Info.EmblemStyle);
			_worldPacket.WriteUInt32(Info.EmblemColor);
			_worldPacket.WriteUInt32(Info.BorderStyle);
			_worldPacket.WriteUInt32(Info.BorderColor);
			_worldPacket.WriteUInt32(Info.BackgroundColor);
			_worldPacket.WriteBits(Info.GuildName.GetByteCount(), 7);
			_worldPacket.FlushBits();

			foreach (var rank in Info.Ranks)
			{
				_worldPacket.WriteUInt32(rank.RankID);
				_worldPacket.WriteUInt32(rank.RankOrder);

				_worldPacket.WriteBits(rank.RankName.GetByteCount(), 7);
				_worldPacket.WriteString(rank.RankName);
			}

			_worldPacket.WriteString(Info.GuildName);
		}
	}

	public class GuildInfo
	{
		public ObjectGuid GuildGuid;

		public uint VirtualRealmAddress; // a special identifier made from the Index, BattleGroup and Region.

		public uint EmblemStyle;
		public uint EmblemColor;
		public uint BorderStyle;
		public uint BorderColor;
		public uint BackgroundColor;
		public List<RankInfo> Ranks = new();
		public string GuildName = "";

		public struct RankInfo
		{
			public RankInfo(uint id, uint order, string name)
			{
				RankID = id;
				RankOrder = order;
				RankName = name;
			}

			public uint RankID;
			public uint RankOrder;
			public string RankName;
		}
	}
}

public class GuildGetRoster : ClientPacket
{
	public GuildGetRoster(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildRoster : ServerPacket
{
	public List<GuildRosterMemberData> MemberData;
	public string WelcomeText;
	public string InfoText;
	public uint CreateDate;
	public int NumAccounts;
	public int GuildFlags;

	public GuildRoster() : base(ServerOpcodes.GuildRoster)
	{
		MemberData = new List<GuildRosterMemberData>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(NumAccounts);
		_worldPacket.WritePackedTime(CreateDate);
		_worldPacket.WriteInt32(GuildFlags);
		_worldPacket.WriteInt32(MemberData.Count);
		_worldPacket.WriteBits(WelcomeText.GetByteCount(), 11);
		_worldPacket.WriteBits(InfoText.GetByteCount(), 10);
		_worldPacket.FlushBits();

		MemberData.ForEach(p => p.Write(_worldPacket));

		_worldPacket.WriteString(WelcomeText);
		_worldPacket.WriteString(InfoText);
	}
}

public class GuildRosterUpdate : ServerPacket
{
	public List<GuildRosterMemberData> MemberData;

	public GuildRosterUpdate() : base(ServerOpcodes.GuildRosterUpdate)
	{
		MemberData = new List<GuildRosterMemberData>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(MemberData.Count);

		MemberData.ForEach(p => p.Write(_worldPacket));
	}
}

public class GuildUpdateMotdText : ClientPacket
{
	public string MotdText;
	public GuildUpdateMotdText(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var textLen = _worldPacket.ReadBits<uint>(11);
		MotdText = _worldPacket.ReadString(textLen);
	}
}

public class GuildCommandResult : ServerPacket
{
	public string Name;
	public GuildCommandError Result;
	public GuildCommandType Command;
	public GuildCommandResult() : base(ServerOpcodes.GuildCommandResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WriteUInt32((uint)Command);

		_worldPacket.WriteBits(Name.GetByteCount(), 8);
		_worldPacket.WriteString(Name);
	}
}

public class AcceptGuildInvite : ClientPacket
{
	public AcceptGuildInvite(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildDeclineInvitation : ClientPacket
{
	public GuildDeclineInvitation(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class DeclineGuildInvites : ClientPacket
{
	public bool Allow;
	public DeclineGuildInvites(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Allow = _worldPacket.HasBit();
	}
}

public class GuildInviteByName : ClientPacket
{
	public string Name;
	public int? Unused910;
	public GuildInviteByName(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(9);
		var hasUnused910 = _worldPacket.HasBit();

		Name = _worldPacket.ReadString(nameLen);

		if (hasUnused910)
			Unused910 = _worldPacket.ReadInt32();
	}
}

public class GuildInvite : ServerPacket
{
	public ObjectGuid GuildGUID;
	public ObjectGuid OldGuildGUID;
	public int AchievementPoints;
	public uint EmblemColor;
	public uint EmblemStyle;
	public uint BorderStyle;
	public uint BorderColor;
	public uint Background;
	public uint GuildVirtualRealmAddress;
	public uint OldGuildVirtualRealmAddress;
	public uint InviterVirtualRealmAddress;
	public string InviterName;
	public string GuildName;
	public string OldGuildName;
	public GuildInvite() : base(ServerOpcodes.GuildInvite) { }

	public override void Write()
	{
		_worldPacket.WriteBits(InviterName.GetByteCount(), 6);
		_worldPacket.WriteBits(GuildName.GetByteCount(), 7);
		_worldPacket.WriteBits(OldGuildName.GetByteCount(), 7);

		_worldPacket.WriteUInt32(InviterVirtualRealmAddress);
		_worldPacket.WriteUInt32(GuildVirtualRealmAddress);
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(OldGuildVirtualRealmAddress);
		_worldPacket.WritePackedGuid(OldGuildGUID);
		_worldPacket.WriteUInt32(EmblemStyle);
		_worldPacket.WriteUInt32(EmblemColor);
		_worldPacket.WriteUInt32(BorderStyle);
		_worldPacket.WriteUInt32(BorderColor);
		_worldPacket.WriteUInt32(Background);
		_worldPacket.WriteInt32(AchievementPoints);

		_worldPacket.WriteString(InviterName);
		_worldPacket.WriteString(GuildName);
		_worldPacket.WriteString(OldGuildName);
	}
}

public class GuildEventStatusChange : ServerPacket
{
	public ObjectGuid Guid;
	public bool AFK;
	public bool DND;
	public GuildEventStatusChange() : base(ServerOpcodes.GuildEventStatusChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(AFK);
		_worldPacket.WriteBit(DND);
		_worldPacket.FlushBits();
	}
}

public class GuildEventPresenceChange : ServerPacket
{
	public ObjectGuid Guid;
	public uint VirtualRealmAddress;
	public string Name;
	public bool Mobile;
	public bool LoggedOn;
	public GuildEventPresenceChange() : base(ServerOpcodes.GuildEventPresenceChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(VirtualRealmAddress);

		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.WriteBit(LoggedOn);
		_worldPacket.WriteBit(Mobile);

		_worldPacket.WriteString(Name);
	}
}

public class GuildEventMotd : ServerPacket
{
	public string MotdText;
	public GuildEventMotd() : base(ServerOpcodes.GuildEventMotd) { }

	public override void Write()
	{
		_worldPacket.WriteBits(MotdText.GetByteCount(), 11);
		_worldPacket.WriteString(MotdText);
	}
}

public class GuildEventPlayerJoined : ServerPacket
{
	public ObjectGuid Guid;
	public string Name;
	public uint VirtualRealmAddress;
	public GuildEventPlayerJoined() : base(ServerOpcodes.GuildEventPlayerJoined) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(VirtualRealmAddress);

		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.WriteString(Name);
	}
}

public class GuildEventRankChanged : ServerPacket
{
	public uint RankID;
	public GuildEventRankChanged() : base(ServerOpcodes.GuildEventRankChanged) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(RankID);
	}
}

public class GuildEventRanksUpdated : ServerPacket
{
	public GuildEventRanksUpdated() : base(ServerOpcodes.GuildEventRanksUpdated) { }

	public override void Write() { }
}

public class GuildEventBankMoneyChanged : ServerPacket
{
	public ulong Money;
	public GuildEventBankMoneyChanged() : base(ServerOpcodes.GuildEventBankMoneyChanged) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(Money);
	}
}

public class GuildEventDisbanded : ServerPacket
{
	public GuildEventDisbanded() : base(ServerOpcodes.GuildEventDisbanded) { }

	public override void Write() { }
}

public class GuildEventLogQuery : ClientPacket
{
	public GuildEventLogQuery(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildEventLogQueryResults : ServerPacket
{
	public List<GuildEventEntry> Entry;

	public GuildEventLogQueryResults() : base(ServerOpcodes.GuildEventLogQueryResults)
	{
		Entry = new List<GuildEventEntry>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(Entry.Count);

		foreach (var entry in Entry)
		{
			_worldPacket.WritePackedGuid(entry.PlayerGUID);
			_worldPacket.WritePackedGuid(entry.OtherGUID);
			_worldPacket.WriteUInt8(entry.TransactionType);
			_worldPacket.WriteUInt8(entry.RankID);
			_worldPacket.WriteUInt32(entry.TransactionDate);
		}
	}
}

public class GuildEventPlayerLeft : ServerPacket
{
	public ObjectGuid LeaverGUID;
	public string LeaverName;
	public uint LeaverVirtualRealmAddress;
	public ObjectGuid RemoverGUID;
	public string RemoverName;
	public uint RemoverVirtualRealmAddress;
	public bool Removed;
	public GuildEventPlayerLeft() : base(ServerOpcodes.GuildEventPlayerLeft) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Removed);
		_worldPacket.WriteBits(LeaverName.GetByteCount(), 6);

		if (Removed)
		{
			_worldPacket.WriteBits(RemoverName.GetByteCount(), 6);
			_worldPacket.WritePackedGuid(RemoverGUID);
			_worldPacket.WriteUInt32(RemoverVirtualRealmAddress);
			_worldPacket.WriteString(RemoverName);
		}

		_worldPacket.WritePackedGuid(LeaverGUID);
		_worldPacket.WriteUInt32(LeaverVirtualRealmAddress);
		_worldPacket.WriteString(LeaverName);
	}
}

public class GuildEventNewLeader : ServerPacket
{
	public ObjectGuid NewLeaderGUID;
	public string NewLeaderName;
	public uint NewLeaderVirtualRealmAddress;
	public ObjectGuid OldLeaderGUID;
	public string OldLeaderName = "";
	public uint OldLeaderVirtualRealmAddress;
	public bool SelfPromoted;
	public GuildEventNewLeader() : base(ServerOpcodes.GuildEventNewLeader) { }

	public override void Write()
	{
		_worldPacket.WriteBit(SelfPromoted);
		_worldPacket.WriteBits(OldLeaderName.GetByteCount(), 6);
		_worldPacket.WriteBits(NewLeaderName.GetByteCount(), 6);

		_worldPacket.WritePackedGuid(OldLeaderGUID);
		_worldPacket.WriteUInt32(OldLeaderVirtualRealmAddress);
		_worldPacket.WritePackedGuid(NewLeaderGUID);
		_worldPacket.WriteUInt32(NewLeaderVirtualRealmAddress);

		_worldPacket.WriteString(OldLeaderName);
		_worldPacket.WriteString(NewLeaderName);
	}
}

public class GuildEventTabAdded : ServerPacket
{
	public GuildEventTabAdded() : base(ServerOpcodes.GuildEventTabAdded) { }

	public override void Write() { }
}

public class GuildEventTabModified : ServerPacket
{
	public string Icon;
	public string Name;
	public int Tab;
	public GuildEventTabModified() : base(ServerOpcodes.GuildEventTabModified) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);

		_worldPacket.WriteBits(Name.GetByteCount(), 7);
		_worldPacket.WriteBits(Icon.GetByteCount(), 9);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(Name);
		_worldPacket.WriteString(Icon);
	}
}

public class GuildEventTabTextChanged : ServerPacket
{
	public int Tab;
	public GuildEventTabTextChanged() : base(ServerOpcodes.GuildEventTabTextChanged) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);
	}
}

public class GuildEventBankContentsChanged : ServerPacket
{
	public GuildEventBankContentsChanged() : base(ServerOpcodes.GuildEventBankContentsChanged) { }

	public override void Write() { }
}

public class GuildPermissionsQuery : ClientPacket
{
	public GuildPermissionsQuery(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildPermissionsQueryResults : ServerPacket
{
	public int NumTabs;
	public int WithdrawGoldLimit;
	public int Flags;
	public uint RankID;
	public List<GuildRankTabPermissions> Tab;

	public GuildPermissionsQueryResults() : base(ServerOpcodes.GuildPermissionsQueryResults)
	{
		Tab = new List<GuildRankTabPermissions>();
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(RankID);
		_worldPacket.WriteInt32(WithdrawGoldLimit);
		_worldPacket.WriteInt32(Flags);
		_worldPacket.WriteInt32(NumTabs);
		_worldPacket.WriteInt32(Tab.Count);

		foreach (var tab in Tab)
		{
			_worldPacket.WriteInt32(tab.Flags);
			_worldPacket.WriteInt32(tab.WithdrawItemLimit);
		}
	}

	public struct GuildRankTabPermissions
	{
		public int Flags;
		public int WithdrawItemLimit;
	}
}

public class GuildSetRankPermissions : ClientPacket
{
	public byte RankID;
	public int RankOrder;
	public uint WithdrawGoldLimit;
	public uint Flags;
	public uint OldFlags;
	public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
	public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];
	public string RankName;
	public GuildSetRankPermissions(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RankID = _worldPacket.ReadUInt8();
		RankOrder = _worldPacket.ReadInt32();
		Flags = _worldPacket.ReadUInt32();
		WithdrawGoldLimit = _worldPacket.ReadUInt32();

		for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
		{
			TabFlags[i] = _worldPacket.ReadUInt32();
			TabWithdrawItemLimit[i] = _worldPacket.ReadUInt32();
		}

		_worldPacket.ResetBitPos();
		var rankNameLen = _worldPacket.ReadBits<uint>(7);

		RankName = _worldPacket.ReadString(rankNameLen);

		OldFlags = _worldPacket.ReadUInt32();
	}
}

public class GuildAddRank : ClientPacket
{
	public string Name;
	public int RankOrder;
	public GuildAddRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(7);
		_worldPacket.ResetBitPos();

		RankOrder = _worldPacket.ReadInt32();
		Name = _worldPacket.ReadString(nameLen);
	}
}

public class GuildAssignMemberRank : ClientPacket
{
	public ObjectGuid Member;
	public int RankOrder;
	public GuildAssignMemberRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Member = _worldPacket.ReadPackedGuid();
		RankOrder = _worldPacket.ReadInt32();
	}
}

public class GuildDeleteRank : ClientPacket
{
	public int RankOrder;
	public GuildDeleteRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RankOrder = _worldPacket.ReadInt32();
	}
}

public class GuildGetRanks : ClientPacket
{
	public ObjectGuid GuildGUID;
	public GuildGetRanks(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGUID = _worldPacket.ReadPackedGuid();
	}
}

public class GuildRanks : ServerPacket
{
	public List<GuildRankData> Ranks;

	public GuildRanks() : base(ServerOpcodes.GuildRanks)
	{
		Ranks = new List<GuildRankData>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(Ranks.Count);

		Ranks.ForEach(p => p.Write(_worldPacket));
	}
}

public class GuildSendRankChange : ServerPacket
{
	public ObjectGuid Other;
	public ObjectGuid Officer;
	public bool Promote;
	public uint RankID;
	public GuildSendRankChange() : base(ServerOpcodes.GuildSendRankChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Officer);
		_worldPacket.WritePackedGuid(Other);
		_worldPacket.WriteUInt32(RankID);

		_worldPacket.WriteBit(Promote);
		_worldPacket.FlushBits();
	}
}

public class GuildShiftRank : ClientPacket
{
	public bool ShiftUp;
	public int RankOrder;
	public GuildShiftRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RankOrder = _worldPacket.ReadInt32();
		ShiftUp = _worldPacket.HasBit();
	}
}

public class GuildUpdateInfoText : ClientPacket
{
	public string InfoText;
	public GuildUpdateInfoText(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var textLen = _worldPacket.ReadBits<uint>(11);
		InfoText = _worldPacket.ReadString(textLen);
	}
}

public class GuildSetMemberNote : ClientPacket
{
	public ObjectGuid NoteeGUID;
	public bool IsPublic; // 0 == Officer, 1 == Public
	public string Note;
	public GuildSetMemberNote(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		NoteeGUID = _worldPacket.ReadPackedGuid();

		var noteLen = _worldPacket.ReadBits<uint>(8);
		IsPublic = _worldPacket.HasBit();

		Note = _worldPacket.ReadString(noteLen);
	}
}

public class GuildMemberUpdateNote : ServerPacket
{
	public ObjectGuid Member;
	public bool IsPublic; // 0 == Officer, 1 == Public
	public string Note;
	public GuildMemberUpdateNote() : base(ServerOpcodes.GuildMemberUpdateNote) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Member);

		_worldPacket.WriteBits(Note.GetByteCount(), 8);
		_worldPacket.WriteBit(IsPublic);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(Note);
	}
}

public class GuildMemberDailyReset : ServerPacket
{
	public GuildMemberDailyReset() : base(ServerOpcodes.GuildMemberDailyReset) { }

	public override void Write() { }
}

public class GuildDelete : ClientPacket
{
	public GuildDelete(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildDemoteMember : ClientPacket
{
	public ObjectGuid Demotee;
	public GuildDemoteMember(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Demotee = _worldPacket.ReadPackedGuid();
	}
}

public class GuildPromoteMember : ClientPacket
{
	public ObjectGuid Promotee;
	public GuildPromoteMember(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Promotee = _worldPacket.ReadPackedGuid();
	}
}

public class GuildOfficerRemoveMember : ClientPacket
{
	public ObjectGuid Removee;
	public GuildOfficerRemoveMember(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Removee = _worldPacket.ReadPackedGuid();
	}
}

public class GuildLeave : ClientPacket
{
	public GuildLeave(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildChangeNameRequest : ClientPacket
{
	public string NewName;
	public GuildChangeNameRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(7);
		NewName = _worldPacket.ReadString(nameLen);
	}
}

public class GuildFlaggedForRename : ServerPacket
{
	public bool FlagSet;
	public GuildFlaggedForRename() : base(ServerOpcodes.GuildFlaggedForRename) { }

	public override void Write()
	{
		_worldPacket.WriteBit(FlagSet);
	}
}

public class RequestGuildPartyState : ClientPacket
{
	public ObjectGuid GuildGUID;
	public RequestGuildPartyState(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGUID = _worldPacket.ReadPackedGuid();
	}
}

public class GuildPartyState : ServerPacket
{
	public float GuildXPEarnedMult = 0.0f;
	public int NumMembers;
	public int NumRequired;
	public bool InGuildParty;
	public GuildPartyState() : base(ServerOpcodes.GuildPartyState, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(InGuildParty);
		_worldPacket.FlushBits();

		_worldPacket.WriteInt32(NumMembers);
		_worldPacket.WriteInt32(NumRequired);
		_worldPacket.WriteFloat(GuildXPEarnedMult);
	}
}

public class RequestGuildRewardsList : ClientPacket
{
	public long CurrentVersion;
	public RequestGuildRewardsList(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CurrentVersion = _worldPacket.ReadInt64();
	}
}

public class GuildRewardList : ServerPacket
{
	public List<GuildRewardItem> RewardItems;
	public long Version;

	public GuildRewardList() : base(ServerOpcodes.GuildRewardList)
	{
		RewardItems = new List<GuildRewardItem>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt64(Version);
		_worldPacket.WriteInt32(RewardItems.Count);

		foreach (var item in RewardItems)
			item.Write(_worldPacket);
	}
}

public class GuildBankActivate : ClientPacket
{
	public ObjectGuid Banker;
	public bool FullUpdate;
	public GuildBankActivate(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		FullUpdate = _worldPacket.HasBit();
	}
}

public class GuildBankBuyTab : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public GuildBankBuyTab(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
	}
}

public class GuildBankUpdateTab : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public string Name;
	public string Icon;
	public GuildBankUpdateTab(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();

		_worldPacket.ResetBitPos();
		var nameLen = _worldPacket.ReadBits<uint>(7);
		var iconLen = _worldPacket.ReadBits<uint>(9);

		Name = _worldPacket.ReadString(nameLen);
		Icon = _worldPacket.ReadString(iconLen);
	}
}

public class GuildBankDepositMoney : ClientPacket
{
	public ObjectGuid Banker;
	public ulong Money;
	public GuildBankDepositMoney(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		Money = _worldPacket.ReadUInt64();
	}
}

public class GuildBankQueryTab : ClientPacket
{
	public ObjectGuid Banker;
	public byte Tab;
	public bool FullUpdate;
	public GuildBankQueryTab(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		Tab = _worldPacket.ReadUInt8();

		FullUpdate = _worldPacket.HasBit();
	}
}

public class GuildBankRemainingWithdrawMoneyQuery : ClientPacket
{
	public GuildBankRemainingWithdrawMoneyQuery(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildBankRemainingWithdrawMoney : ServerPacket
{
	public long RemainingWithdrawMoney;
	public GuildBankRemainingWithdrawMoney() : base(ServerOpcodes.GuildBankRemainingWithdrawMoney) { }

	public override void Write()
	{
		_worldPacket.WriteInt64(RemainingWithdrawMoney);
	}
}

public class GuildBankWithdrawMoney : ClientPacket
{
	public ObjectGuid Banker;
	public ulong Money;
	public GuildBankWithdrawMoney(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		Money = _worldPacket.ReadUInt64();
	}
}

public class GuildBankQueryResults : ServerPacket
{
	public List<GuildBankItemInfo> ItemInfo;
	public List<GuildBankTabInfo> TabInfo;
	public int WithdrawalsRemaining;
	public int Tab;
	public ulong Money;
	public bool FullUpdate;

	public GuildBankQueryResults() : base(ServerOpcodes.GuildBankQueryResults)
	{
		ItemInfo = new List<GuildBankItemInfo>();
		TabInfo = new List<GuildBankTabInfo>();
	}

	public override void Write()
	{
		_worldPacket.WriteUInt64(Money);
		_worldPacket.WriteInt32(Tab);
		_worldPacket.WriteInt32(WithdrawalsRemaining);
		_worldPacket.WriteInt32(TabInfo.Count);
		_worldPacket.WriteInt32(ItemInfo.Count);
		_worldPacket.WriteBit(FullUpdate);
		_worldPacket.FlushBits();

		foreach (var tab in TabInfo)
		{
			_worldPacket.WriteInt32(tab.TabIndex);
			_worldPacket.WriteBits(tab.Name.GetByteCount(), 7);
			_worldPacket.WriteBits(tab.Icon.GetByteCount(), 9);

			_worldPacket.WriteString(tab.Name);
			_worldPacket.WriteString(tab.Icon);
		}

		foreach (var item in ItemInfo)
		{
			_worldPacket.WriteInt32(item.Slot);
			_worldPacket.WriteInt32(item.Count);
			_worldPacket.WriteInt32(item.EnchantmentID);
			_worldPacket.WriteInt32(item.Charges);
			_worldPacket.WriteInt32(item.OnUseEnchantmentID);
			_worldPacket.WriteUInt32(item.Flags);

			item.Item.Write(_worldPacket);

			_worldPacket.WriteBits(item.SocketEnchant.Count, 2);
			_worldPacket.WriteBit(item.Locked);
			_worldPacket.FlushBits();

			foreach (var socketEnchant in item.SocketEnchant)
				socketEnchant.Write(_worldPacket);
		}
	}
}

class AutoGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public AutoGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class StoreGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public StoreGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class SwapItemWithGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public SwapItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class SwapGuildBankItemWithGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte[] BankTab = new byte[2];
	public byte[] BankSlot = new byte[2];
	public SwapGuildBankItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab[0] = _worldPacket.ReadUInt8();
		BankSlot[0] = _worldPacket.ReadUInt8();
		BankTab[1] = _worldPacket.ReadUInt8();
		BankSlot[1] = _worldPacket.ReadUInt8();
	}
}

class MoveGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte BankTab1;
	public byte BankSlot1;
	public MoveGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		BankTab1 = _worldPacket.ReadUInt8();
		BankSlot1 = _worldPacket.ReadUInt8();
	}
}

class MergeItemWithGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public uint StackCount;
	public MergeItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class SplitItemToGuildBank : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public uint StackCount;
	public SplitItemToGuildBank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class MergeGuildBankItemWithItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public uint StackCount;
	public MergeGuildBankItemWithItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class SplitGuildBankItemToInventory : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public uint StackCount;
	public SplitGuildBankItemToInventory(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}

class AutoStoreGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public AutoStoreGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
	}
}

class MergeGuildBankItemWithGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte BankTab1;
	public byte BankSlot1;
	public uint StackCount;
	public MergeGuildBankItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		BankTab1 = _worldPacket.ReadUInt8();
		BankSlot1 = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();
	}
}

class SplitGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte BankTab1;
	public byte BankSlot1;
	public uint StackCount;
	public SplitGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		BankTab1 = _worldPacket.ReadUInt8();
		BankSlot1 = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();
	}
}

public class GuildBankLogQuery : ClientPacket
{
	public int Tab;
	public GuildBankLogQuery(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tab = _worldPacket.ReadInt32();
	}
}

public class GuildBankLogQueryResults : ServerPacket
{
	public int Tab;
	public List<GuildBankLogEntry> Entry;
	public ulong? WeeklyBonusMoney;

	public GuildBankLogQueryResults() : base(ServerOpcodes.GuildBankLogQueryResults)
	{
		Entry = new List<GuildBankLogEntry>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);
		_worldPacket.WriteInt32(Entry.Count);
		_worldPacket.WriteBit(WeeklyBonusMoney.HasValue);
		_worldPacket.FlushBits();

		foreach (var logEntry in Entry)
		{
			_worldPacket.WritePackedGuid(logEntry.PlayerGUID);
			_worldPacket.WriteUInt32(logEntry.TimeOffset);
			_worldPacket.WriteInt8(logEntry.EntryType);

			_worldPacket.WriteBit(logEntry.Money.HasValue);
			_worldPacket.WriteBit(logEntry.ItemID.HasValue);
			_worldPacket.WriteBit(logEntry.Count.HasValue);
			_worldPacket.WriteBit(logEntry.OtherTab.HasValue);
			_worldPacket.FlushBits();

			if (logEntry.Money.HasValue)
				_worldPacket.WriteUInt64(logEntry.Money.Value);

			if (logEntry.ItemID.HasValue)
				_worldPacket.WriteInt32(logEntry.ItemID.Value);

			if (logEntry.Count.HasValue)
				_worldPacket.WriteInt32(logEntry.Count.Value);

			if (logEntry.OtherTab.HasValue)
				_worldPacket.WriteInt8(logEntry.OtherTab.Value);
		}

		if (WeeklyBonusMoney.HasValue)
			_worldPacket.WriteUInt64(WeeklyBonusMoney.Value);
	}
}

public class GuildBankTextQuery : ClientPacket
{
	public int Tab;
	public GuildBankTextQuery(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tab = _worldPacket.ReadInt32();
	}
}

public class GuildBankTextQueryResult : ServerPacket
{
	public int Tab;
	public string Text;
	public GuildBankTextQueryResult() : base(ServerOpcodes.GuildBankTextQueryResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Tab);

		_worldPacket.WriteBits(Text.GetByteCount(), 14);
		_worldPacket.WriteString(Text);
	}
}

public class GuildBankSetTabText : ClientPacket
{
	public int Tab;
	public string TabText;
	public GuildBankSetTabText(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tab = _worldPacket.ReadInt32();
		TabText = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(14));
	}
}

public class GuildQueryNews : ClientPacket
{
	public ObjectGuid GuildGUID;
	public GuildQueryNews(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGUID = _worldPacket.ReadPackedGuid();
	}
}

public class GuildNewsPkt : ServerPacket
{
	public List<GuildNewsEvent> NewsEvents;

	public GuildNewsPkt() : base(ServerOpcodes.GuildNews)
	{
		NewsEvents = new List<GuildNewsEvent>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(NewsEvents.Count);

		foreach (var newsEvent in NewsEvents)
			newsEvent.Write(_worldPacket);
	}
}

public class GuildNewsUpdateSticky : ClientPacket
{
	public int NewsID;
	public ObjectGuid GuildGUID;
	public bool Sticky;
	public GuildNewsUpdateSticky(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGUID = _worldPacket.ReadPackedGuid();
		NewsID = _worldPacket.ReadInt32();

		Sticky = _worldPacket.HasBit();
	}
}

class GuildReplaceGuildMaster : ClientPacket
{
	public GuildReplaceGuildMaster(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildSetGuildMaster : ClientPacket
{
	public string NewMasterName;
	public GuildSetGuildMaster(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(9);
		NewMasterName = _worldPacket.ReadString(nameLen);
	}
}

public class GuildChallengeUpdateRequest : ClientPacket
{
	public GuildChallengeUpdateRequest(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class GuildChallengeUpdate : ServerPacket
{
	public int[] CurrentCount = new int[GuildConst.ChallengesTypes];
	public int[] MaxCount = new int[GuildConst.ChallengesTypes];
	public int[] Gold = new int[GuildConst.ChallengesTypes];
	public int[] MaxLevelGold = new int[GuildConst.ChallengesTypes];
	public GuildChallengeUpdate() : base(ServerOpcodes.GuildChallengeUpdate) { }

	public override void Write()
	{
		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(CurrentCount[i]);

		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(MaxCount[i]);

		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(MaxLevelGold[i]);

		for (var i = 0; i < GuildConst.ChallengesTypes; ++i)
			_worldPacket.WriteInt32(Gold[i]);
	}
}

public class SaveGuildEmblem : ClientPacket
{
	public ObjectGuid Vendor;
	public uint BStyle;
	public uint EStyle;
	public uint BColor;
	public uint EColor;
	public uint Bg;
	public SaveGuildEmblem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Vendor = _worldPacket.ReadPackedGuid();
		EStyle = _worldPacket.ReadUInt32();
		EColor = _worldPacket.ReadUInt32();
		BStyle = _worldPacket.ReadUInt32();
		BColor = _worldPacket.ReadUInt32();
		Bg = _worldPacket.ReadUInt32();
	}
}

public class PlayerSaveGuildEmblem : ServerPacket
{
	public GuildEmblemError Error;
	public PlayerSaveGuildEmblem() : base(ServerOpcodes.PlayerSaveGuildEmblem) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Error);
	}
}

class GuildSetAchievementTracking : ClientPacket
{
	public List<uint> AchievementIDs = new();
	public GuildSetAchievementTracking(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadUInt32();

		for (uint i = 0; i < count; ++i)
			AchievementIDs.Add(_worldPacket.ReadUInt32());
	}
}

class GuildNameChanged : ServerPacket
{
	public ObjectGuid GuildGUID;
	public string GuildName;
	public GuildNameChanged() : base(ServerOpcodes.GuildNameChanged) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteBits(GuildName.GetByteCount(), 7);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(GuildName);
	}
}

//Structs
public struct GuildRosterProfessionData
{
	public void Write(WorldPacket data)
	{
		data.WriteInt32(DbID);
		data.WriteInt32(Rank);
		data.WriteInt32(Step);
	}

	public int DbID;
	public int Rank;
	public int Step;
}

public class GuildRosterMemberData
{
	public ObjectGuid Guid;
	public long WeeklyXP;
	public long TotalXP;
	public int RankID;
	public int AreaID;
	public int PersonalAchievementPoints;
	public int GuildReputation;
	public int GuildRepToCap;
	public float LastSave;
	public string Name;
	public uint VirtualRealmAddress;
	public string Note;
	public string OfficerNote;
	public byte Status;
	public byte Level;
	public byte ClassID;
	public byte Gender;
	public ulong GuildClubMemberID;
	public byte RaceID;
	public bool Authenticated;
	public bool SorEligible;
	public GuildRosterProfessionData[] Profession = new GuildRosterProfessionData[2];
	public DungeonScoreSummary DungeonScore = new();

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WriteInt32(RankID);
		data.WriteInt32(AreaID);
		data.WriteInt32(PersonalAchievementPoints);
		data.WriteInt32(GuildReputation);
		data.WriteFloat(LastSave);

		for (byte i = 0; i < 2; i++)
			Profession[i].Write(data);

		data.WriteUInt32(VirtualRealmAddress);
		data.WriteUInt8(Status);
		data.WriteUInt8(Level);
		data.WriteUInt8(ClassID);
		data.WriteUInt8(Gender);
		data.WriteUInt64(GuildClubMemberID);
		data.WriteUInt8(RaceID);

		data.WriteBits(Name.GetByteCount(), 6);
		data.WriteBits(Note.GetByteCount(), 8);
		data.WriteBits(OfficerNote.GetByteCount(), 8);
		data.WriteBit(Authenticated);
		data.WriteBit(SorEligible);
		data.FlushBits();

		DungeonScore.Write(data);

		data.WriteString(Name);
		data.WriteString(Note);
		data.WriteString(OfficerNote);
	}
}

public class GuildEventEntry
{
	public ObjectGuid PlayerGUID;
	public ObjectGuid OtherGUID;
	public byte TransactionType;
	public byte RankID;
	public uint TransactionDate;
}

public class GuildRankData
{
	public byte RankID;
	public uint RankOrder;
	public uint Flags;
	public uint WithdrawGoldLimit;
	public string RankName;
	public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
	public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];

	public void Write(WorldPacket data)
	{
		data.WriteUInt8(RankID);
		data.WriteUInt32(RankOrder);
		data.WriteUInt32(Flags);
		data.WriteUInt32(WithdrawGoldLimit);

		for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
		{
			data.WriteUInt32(TabFlags[i]);
			data.WriteUInt32(TabWithdrawItemLimit[i]);
		}

		data.WriteBits(RankName.GetByteCount(), 7);
		data.WriteString(RankName);
	}
}

public class GuildRewardItem
{
	public uint ItemID;
	public uint Unk4;
	public List<uint> AchievementsRequired = new();
	public ulong RaceMask;
	public int MinGuildLevel;
	public int MinGuildRep;
	public ulong Cost;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(ItemID);
		data.WriteUInt32(Unk4);
		data.WriteInt32(AchievementsRequired.Count);
		data.WriteUInt64(RaceMask);
		data.WriteInt32(MinGuildLevel);
		data.WriteInt32(MinGuildRep);
		data.WriteUInt64(Cost);

		foreach (var achievementId in AchievementsRequired)
			data.WriteUInt32(achievementId);
	}
}

public class GuildBankItemInfo
{
	public ItemInstance Item = new();
	public int Slot;
	public int Count;
	public int EnchantmentID;
	public int Charges;
	public int OnUseEnchantmentID;
	public uint Flags;
	public bool Locked;
	public List<ItemGemData> SocketEnchant = new();
}

public struct GuildBankTabInfo
{
	public int TabIndex;
	public string Name;
	public string Icon;
}

public class GuildBankLogEntry
{
	public ObjectGuid PlayerGUID;
	public uint TimeOffset;
	public sbyte EntryType;
	public ulong? Money;
	public int? ItemID;
	public int? Count;
	public sbyte? OtherTab;
}

public class GuildNewsEvent
{
	public int Id;
	public uint CompletedDate;
	public int Type;
	public int Flags;
	public int[] Data = new int[2];
	public ObjectGuid MemberGuid;
	public List<ObjectGuid> MemberList = new();
	public ItemInstance Item;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Id);
		data.WritePackedTime(CompletedDate);
		data.WriteInt32(Type);
		data.WriteInt32(Flags);

		for (byte i = 0; i < 2; i++)
			data.WriteInt32(Data[i]);

		data.WritePackedGuid(MemberGuid);
		data.WriteInt32(MemberList.Count);

		foreach (var memberGuid in MemberList)
			data.WritePackedGuid(memberGuid);

		data.WriteBit(Item != null);
		data.FlushBits();

		if (Item != null)
			Item.Write(data);
	}
}