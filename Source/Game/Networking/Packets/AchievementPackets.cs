// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class AllAchievementData : ServerPacket
{
	public AllAchievements Data = new();
	public AllAchievementData() : base(ServerOpcodes.AllAchievementData, ConnectionType.Instance) { }

	public override void Write()
	{
		Data.Write(_worldPacket);
	}
}

class AllAccountCriteria : ServerPacket
{
	public List<CriteriaProgressPkt> Progress = new();
	public AllAccountCriteria() : base(ServerOpcodes.AllAccountCriteria, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Progress.Count);

		foreach (var progress in Progress)
			progress.Write(_worldPacket);
	}
}

public class RespondInspectAchievements : ServerPacket
{
	public ObjectGuid Player;
	public AllAchievements Data = new();
	public RespondInspectAchievements() : base(ServerOpcodes.RespondInspectAchievements, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Player);
		Data.Write(_worldPacket);
	}
}

public class CriteriaUpdate : ServerPacket
{
	public uint CriteriaID;
	public ulong Quantity;
	public ObjectGuid PlayerGUID;
	public uint Flags;
	public long CurrentTime;
	public long ElapsedTime;
	public long CreationTime;
	public ulong? RafAcceptanceID;
	public CriteriaUpdate() : base(ServerOpcodes.CriteriaUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(CriteriaID);
		_worldPacket.WriteUInt64(Quantity);
		_worldPacket.WritePackedGuid(PlayerGUID);
		_worldPacket.WriteUInt32(Flags);
		_worldPacket.WritePackedTime(CurrentTime);
		_worldPacket.WriteInt64(ElapsedTime);
		_worldPacket.WriteInt64(CreationTime);
		_worldPacket.WriteBit(RafAcceptanceID.HasValue);
		_worldPacket.FlushBits();

		if (RafAcceptanceID.HasValue)
			_worldPacket.WriteUInt64(RafAcceptanceID.Value);
	}
}

class AccountCriteriaUpdate : ServerPacket
{
	public CriteriaProgressPkt Progress;
	public AccountCriteriaUpdate() : base(ServerOpcodes.AccountCriteriaUpdate) { }

	public override void Write()
	{
		Progress.Write(_worldPacket);
	}
}

public class CriteriaDeleted : ServerPacket
{
	public uint CriteriaID;
	public CriteriaDeleted() : base(ServerOpcodes.CriteriaDeleted, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(CriteriaID);
	}
}

public class AchievementDeleted : ServerPacket
{
	public uint AchievementID;
	public uint Immunities; // this is just garbage, not used by client
	public AchievementDeleted() : base(ServerOpcodes.AchievementDeleted, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WriteUInt32(Immunities);
	}
}

public class AchievementEarned : ServerPacket
{
	public ObjectGuid Earner;
	public uint EarnerNativeRealm;
	public uint EarnerVirtualRealm;
	public uint AchievementID;
	public long Time;
	public bool Initial;
	public ObjectGuid Sender;
	public AchievementEarned() : base(ServerOpcodes.AchievementEarned, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Sender);
		_worldPacket.WritePackedGuid(Earner);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WritePackedTime(Time);
		_worldPacket.WriteUInt32(EarnerNativeRealm);
		_worldPacket.WriteUInt32(EarnerVirtualRealm);
		_worldPacket.WriteBit(Initial);
		_worldPacket.FlushBits();
	}
}

public class BroadcastAchievement : ServerPacket
{
	public ObjectGuid PlayerGUID;
	public string Name = "";
	public uint AchievementID;
	public bool GuildAchievement;
	public BroadcastAchievement() : base(ServerOpcodes.BroadcastAchievement) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 7);
		_worldPacket.WriteBit(GuildAchievement);
		_worldPacket.WritePackedGuid(PlayerGUID);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WriteString(Name);
	}
}

public class GuildCriteriaUpdate : ServerPacket
{
	public List<GuildCriteriaProgress> Progress = new();
	public GuildCriteriaUpdate() : base(ServerOpcodes.GuildCriteriaUpdate) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Progress.Count);

		foreach (var progress in Progress)
		{
			_worldPacket.WriteUInt32(progress.CriteriaID);
			_worldPacket.WriteInt64(progress.DateCreated);
			_worldPacket.WriteInt64(progress.DateStarted);
			_worldPacket.WritePackedTime(progress.DateUpdated);
			_worldPacket.WriteUInt32(0); // this is a hack. this is a packed time written as int64 (progress.DateUpdated)
			_worldPacket.WriteUInt64(progress.Quantity);
			_worldPacket.WritePackedGuid(progress.PlayerGUID);
			_worldPacket.WriteInt32(progress.Flags);
		}
	}
}

public class GuildCriteriaDeleted : ServerPacket
{
	public ObjectGuid GuildGUID;
	public uint CriteriaID;
	public GuildCriteriaDeleted() : base(ServerOpcodes.GuildCriteriaDeleted) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(CriteriaID);
	}
}

public class GuildSetFocusedAchievement : ClientPacket
{
	public uint AchievementID;
	public GuildSetFocusedAchievement(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AchievementID = _worldPacket.ReadUInt32();
	}
}

public class GuildAchievementDeleted : ServerPacket
{
	public ObjectGuid GuildGUID;
	public uint AchievementID;
	public long TimeDeleted;
	public GuildAchievementDeleted() : base(ServerOpcodes.GuildAchievementDeleted) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WritePackedTime(TimeDeleted);
	}
}

public class GuildAchievementEarned : ServerPacket
{
	public uint AchievementID;
	public ObjectGuid GuildGUID;
	public long TimeEarned;
	public GuildAchievementEarned() : base(ServerOpcodes.GuildAchievementEarned) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WritePackedTime(TimeEarned);
	}
}

public class AllGuildAchievements : ServerPacket
{
	public List<EarnedAchievement> Earned = new();
	public AllGuildAchievements() : base(ServerOpcodes.AllGuildAchievements) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Earned.Count);

		foreach (var earned in Earned)
			earned.Write(_worldPacket);
	}
}

class GuildGetAchievementMembers : ClientPacket
{
	public ObjectGuid PlayerGUID;
	public ObjectGuid GuildGUID;
	public uint AchievementID;
	public GuildGetAchievementMembers(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PlayerGUID = _worldPacket.ReadPackedGuid();
		GuildGUID = _worldPacket.ReadPackedGuid();
		AchievementID = _worldPacket.ReadUInt32();
	}
}

class GuildAchievementMembers : ServerPacket
{
	public ObjectGuid GuildGUID;
	public uint AchievementID;
	public List<ObjectGuid> Member = new();
	public GuildAchievementMembers() : base(ServerOpcodes.GuildAchievementMembers) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WriteInt32(Member.Count);

		foreach (var guid in Member)
			_worldPacket.WritePackedGuid(guid);
	}
}

//Structs
public struct EarnedAchievement
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Id);
		data.WritePackedTime(Date);
		data.WritePackedGuid(Owner);
		data.WriteUInt32(VirtualRealmAddress);
		data.WriteUInt32(NativeRealmAddress);
	}

	public uint Id;
	public long Date;
	public ObjectGuid Owner;
	public uint VirtualRealmAddress;
	public uint NativeRealmAddress;
}

public struct CriteriaProgressPkt
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Id);
		data.WriteUInt64(Quantity);
		data.WritePackedGuid(Player);
		data.WritePackedTime(Date);
		data.WriteInt64(TimeFromStart);
		data.WriteInt64(TimeFromCreate);
		data.WriteBits(Flags, 4);
		data.WriteBit(RafAcceptanceID.HasValue);
		data.FlushBits();

		if (RafAcceptanceID.HasValue)
			data.WriteUInt64(RafAcceptanceID.Value);
	}

	public uint Id;
	public ulong Quantity;
	public ObjectGuid Player;
	public uint Flags;
	public long Date;
	public long TimeFromStart;
	public long TimeFromCreate;
	public ulong? RafAcceptanceID;
}

public struct GuildCriteriaProgress
{
	public uint CriteriaID;
	public long DateCreated;
	public long DateStarted;
	public long DateUpdated;
	public ulong Quantity;
	public ObjectGuid PlayerGUID;
	public int Flags;
}

public class AllAchievements
{
	public List<EarnedAchievement> Earned = new();
	public List<CriteriaProgressPkt> Progress = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Earned.Count);
		data.WriteInt32(Progress.Count);

		foreach (var earned in Earned)
			earned.Write(data);

		foreach (var progress in Progress)
			progress.Write(data);
	}
}