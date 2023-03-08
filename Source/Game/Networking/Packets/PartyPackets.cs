// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Spells;

namespace Game.Networking.Packets;

class PartyCommandResult : ServerPacket
{
	public string Name;
	public byte Command;
	public byte Result;
	public uint ResultData;
	public ObjectGuid ResultGUID;
	public PartyCommandResult() : base(ServerOpcodes.PartyCommandResult) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteBits(Command, 4);
		_worldPacket.WriteBits(Result, 6);

		_worldPacket.WriteUInt32(ResultData);
		_worldPacket.WritePackedGuid(ResultGUID);
		_worldPacket.WriteString(Name);
	}
}

class PartyInviteClient : ClientPacket
{
	public byte PartyIndex;
	public uint ProposedRoles;
	public string TargetName;
	public string TargetRealm;
	public ObjectGuid TargetGUID;
	public PartyInviteClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();

		var targetNameLen = _worldPacket.ReadBits<uint>(9);
		var targetRealmLen = _worldPacket.ReadBits<uint>(9);

		ProposedRoles = _worldPacket.ReadUInt32();
		TargetGUID = _worldPacket.ReadPackedGuid();

		TargetName = _worldPacket.ReadString(targetNameLen);
		TargetRealm = _worldPacket.ReadString(targetRealmLen);
	}
}

class PartyInvite : ServerPacket
{
	public bool MightCRZYou;
	public bool MustBeBNetFriend;
	public bool AllowMultipleRoles;
	public bool QuestSessionActive;
	public ushort Unk1;

	public bool CanAccept;

	// Inviter
	public VirtualRealmInfo InviterRealm;
	public ObjectGuid InviterGUID;
	public ObjectGuid InviterBNetAccountId;
	public string InviterName;

	// Realm
	public bool IsXRealm;

	// Lfg
	public uint ProposedRoles;
	public int LfgCompletedMask;
	public List<int> LfgSlots = new();
	public PartyInvite() : base(ServerOpcodes.PartyInvite) { }

	public void Initialize(Player inviter, uint proposedRoles, bool canAccept)
	{
		CanAccept = canAccept;

		InviterName = inviter.GetName();
		InviterGUID = inviter.GUID;
		InviterBNetAccountId = inviter.Session.AccountGUID;

		ProposedRoles = proposedRoles;

		var realm = Global.WorldMgr.Realm;
		InviterRealm = new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName);
	}

	public override void Write()
	{
		_worldPacket.WriteBit(CanAccept);
		_worldPacket.WriteBit(MightCRZYou);
		_worldPacket.WriteBit(IsXRealm);
		_worldPacket.WriteBit(MustBeBNetFriend);
		_worldPacket.WriteBit(AllowMultipleRoles);
		_worldPacket.WriteBit(QuestSessionActive);
		_worldPacket.WriteBits(InviterName.GetByteCount(), 6);

		InviterRealm.Write(_worldPacket);

		_worldPacket.WritePackedGuid(InviterGUID);
		_worldPacket.WritePackedGuid(InviterBNetAccountId);
		_worldPacket.WriteUInt16(Unk1);
		_worldPacket.WriteUInt32(ProposedRoles);
		_worldPacket.WriteInt32(LfgSlots.Count);
		_worldPacket.WriteInt32(LfgCompletedMask);

		_worldPacket.WriteString(InviterName);

		foreach (var LfgSlot in LfgSlots)
			_worldPacket.WriteInt32(LfgSlot);
	}
}

class PartyInviteResponse : ClientPacket
{
	public byte PartyIndex;
	public bool Accept;
	public uint? RolesDesired;
	public PartyInviteResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();

		Accept = _worldPacket.HasBit();

		var hasRolesDesired = _worldPacket.HasBit();

		if (hasRolesDesired)
			RolesDesired = _worldPacket.ReadUInt32();
	}
}

class PartyUninvite : ClientPacket
{
	public byte PartyIndex;
	public ObjectGuid TargetGUID;
	public string Reason;
	public PartyUninvite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		TargetGUID = _worldPacket.ReadPackedGuid();

		var reasonLen = _worldPacket.ReadBits<byte>(8);
		Reason = _worldPacket.ReadString(reasonLen);
	}
}

class GroupDecline : ServerPacket
{
	public string Name;

	public GroupDecline(string name) : base(ServerOpcodes.GroupDecline)
	{
		Name = name;
	}

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(Name);
	}
}

class GroupUninvite : ServerPacket
{
	public GroupUninvite() : base(ServerOpcodes.GroupUninvite) { }

	public override void Write() { }
}

class RequestPartyMemberStats : ClientPacket
{
	public byte PartyIndex;
	public ObjectGuid TargetGUID;
	public RequestPartyMemberStats(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		TargetGUID = _worldPacket.ReadPackedGuid();
	}
}

class PartyMemberFullState : ServerPacket
{
	public bool ForEnemy;
	public ObjectGuid MemberGuid;
	public PartyMemberStats MemberStats = new();
	public PartyMemberFullState() : base(ServerOpcodes.PartyMemberFullState) { }

	public override void Write()
	{
		_worldPacket.WriteBit(ForEnemy);
		_worldPacket.FlushBits();

		MemberStats.Write(_worldPacket);
		_worldPacket.WritePackedGuid(MemberGuid);
	}

	public void Initialize(Player player)
	{
		ForEnemy = false;

		MemberGuid = player.GUID;

		// Status
		MemberStats.Status = GroupMemberOnlineStatus.Online;

		if (player.IsPvP)
			MemberStats.Status |= GroupMemberOnlineStatus.PVP;

		if (!player.IsAlive)
		{
			if (player.HasPlayerFlag(PlayerFlags.Ghost))
				MemberStats.Status |= GroupMemberOnlineStatus.Ghost;
			else
				MemberStats.Status |= GroupMemberOnlineStatus.Dead;
		}

		if (player.IsFFAPvP)
			MemberStats.Status |= GroupMemberOnlineStatus.PVPFFA;

		if (player.IsAFK)
			MemberStats.Status |= GroupMemberOnlineStatus.AFK;

		if (player.IsDND)
			MemberStats.Status |= GroupMemberOnlineStatus.DND;

		if (player.Vehicle1)
			MemberStats.Status |= GroupMemberOnlineStatus.Vehicle;

		// Level
		MemberStats.Level = (ushort)player.Level;

		// Health
		MemberStats.CurrentHealth = (int)player.Health;
		MemberStats.MaxHealth = (int)player.MaxHealth;

		// Power
		MemberStats.PowerType = (byte)player.DisplayPowerType;
		MemberStats.PowerDisplayID = 0;
		MemberStats.CurrentPower = (ushort)player.GetPower(player.DisplayPowerType);
		MemberStats.MaxPower = (ushort)player.GetMaxPower(player.DisplayPowerType);

		// Position
		MemberStats.ZoneID = (ushort)player.Zone;
		MemberStats.PositionX = (short)player.Location.X;
		MemberStats.PositionY = (short)(player.Location.Y);
		MemberStats.PositionZ = (short)(player.Location.Z);

		MemberStats.SpecID = (ushort)player.GetPrimarySpecialization();
		MemberStats.PartyType[0] = (sbyte)(player.PlayerData.PartyType & 0xF);
		MemberStats.PartyType[1] = (sbyte)(player.PlayerData.PartyType >> 4);
		MemberStats.WmoGroupID = 0;
		MemberStats.WmoDoodadPlacementID = 0;

		// Vehicle
		var vehicle = player.Vehicle1;

		if (vehicle != null)
		{
			var vehicleSeat = vehicle.GetSeatForPassenger(player);

			if (vehicleSeat != null)
				MemberStats.VehicleSeat = (int)vehicleSeat.Id;
		}

		// Auras
		foreach (var aurApp in player.VisibleAuras)
		{
			PartyMemberAuraStates aura = new();
			aura.SpellID = (int)aurApp.Base.Id;
			aura.ActiveFlags = aurApp.EffectMask;
			aura.Flags = (byte)aurApp.Flags;

			if (aurApp.Flags.HasAnyFlag(AuraFlags.Scalable))
				foreach (var aurEff in aurApp.Base.AuraEffects)
					if (aurApp.HasEffect(aurEff.Value.EffIndex))
						aura.Points.Add((float)aurEff.Value.Amount);

			MemberStats.Auras.Add(aura);
		}

		// Phases
		PhasingHandler.FillPartyMemberPhase(MemberStats.Phases, player.PhaseShift);

		// Pet
		if (player.CurrentPet)
		{
			var pet = player.CurrentPet;

			MemberStats.PetStats = new PartyMemberPetStats();

			MemberStats.PetStats.GUID = pet.GUID;
			MemberStats.PetStats.Name = pet.GetName();
			MemberStats.PetStats.ModelId = (short)pet.DisplayId;

			MemberStats.PetStats.CurrentHealth = (int)pet.Health;
			MemberStats.PetStats.MaxHealth = (int)pet.MaxHealth;

			foreach (var aurApp in pet.VisibleAuras)
			{
				PartyMemberAuraStates aura = new();

				aura.SpellID = (int)aurApp.Base.Id;
				aura.ActiveFlags = aurApp.EffectMask;
				aura.Flags = (byte)aurApp.Flags;

				if (aurApp.Flags.HasAnyFlag(AuraFlags.Scalable))
					foreach (var aurEff in aurApp.Base.AuraEffects)
						if (aurApp.HasEffect(aurEff.Value.EffIndex))
							aura.Points.Add((float)aurEff.Value.Amount);

				MemberStats.PetStats.Auras.Add(aura);
			}
		}
	}
}

class SetPartyLeader : ClientPacket
{
	public sbyte PartyIndex;
	public ObjectGuid TargetGUID;
	public SetPartyLeader(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		TargetGUID = _worldPacket.ReadPackedGuid();
	}
}

class SetRole : ClientPacket
{
	public sbyte PartyIndex;
	public ObjectGuid TargetGUID;
	public int Role;
	public SetRole(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		TargetGUID = _worldPacket.ReadPackedGuid();
		Role = _worldPacket.ReadInt32();
	}
}

class RoleChangedInform : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid From;
	public ObjectGuid ChangedUnit;
	public int OldRole;
	public int NewRole;
	public RoleChangedInform() : base(ServerOpcodes.RoleChangedInform) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(From);
		_worldPacket.WritePackedGuid(ChangedUnit);
		_worldPacket.WriteInt32(OldRole);
		_worldPacket.WriteInt32(NewRole);
	}
}

class LeaveGroup : ClientPacket
{
	public sbyte PartyIndex;
	public LeaveGroup(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
	}
}

class GroupDestroyed : ServerPacket
{
	public GroupDestroyed() : base(ServerOpcodes.GroupDestroyed) { }

	public override void Write() { }
}

class SetLootMethod : ClientPacket
{
	public sbyte PartyIndex;
	public ObjectGuid LootMasterGUID;
	public LootMethod LootMethod;
	public ItemQuality LootThreshold;
	public SetLootMethod(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		LootMethod = (LootMethod)_worldPacket.ReadUInt8();
		LootMasterGUID = _worldPacket.ReadPackedGuid();
		LootThreshold = (ItemQuality)_worldPacket.ReadUInt32();
	}
}

class MinimapPingClient : ClientPacket
{
	public sbyte PartyIndex;
	public float PositionX;
	public float PositionY;
	public MinimapPingClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PositionX = _worldPacket.ReadFloat();
		PositionY = _worldPacket.ReadFloat();
		PartyIndex = _worldPacket.ReadInt8();
	}
}

class MinimapPing : ServerPacket
{
	public ObjectGuid Sender;
	public float PositionX;
	public float PositionY;
	public MinimapPing() : base(ServerOpcodes.MinimapPing) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Sender);
		_worldPacket.WriteFloat(PositionX);
		_worldPacket.WriteFloat(PositionY);
	}
}

class UpdateRaidTarget : ClientPacket
{
	public sbyte PartyIndex;
	public ObjectGuid Target;
	public sbyte Symbol;
	public UpdateRaidTarget(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		Target = _worldPacket.ReadPackedGuid();
		Symbol = _worldPacket.ReadInt8();
	}
}

class SendRaidTargetUpdateSingle : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid Target;
	public ObjectGuid ChangedBy;
	public sbyte Symbol;
	public SendRaidTargetUpdateSingle() : base(ServerOpcodes.SendRaidTargetUpdateSingle) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WriteInt8(Symbol);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(ChangedBy);
	}
}

class SendRaidTargetUpdateAll : ServerPacket
{
	public sbyte PartyIndex;
	public Dictionary<byte, ObjectGuid> TargetIcons = new();
	public SendRaidTargetUpdateAll() : base(ServerOpcodes.SendRaidTargetUpdateAll) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);

		_worldPacket.WriteInt32(TargetIcons.Count);

		foreach (var pair in TargetIcons)
		{
			_worldPacket.WritePackedGuid(pair.Value);
			_worldPacket.WriteUInt8(pair.Key);
		}
	}
}

class ConvertRaid : ClientPacket
{
	public bool Raid;
	public ConvertRaid(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Raid = _worldPacket.HasBit();
	}
}

class RequestPartyJoinUpdates : ClientPacket
{
	public sbyte PartyIndex;
	public RequestPartyJoinUpdates(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
	}
}

class SetAssistantLeader : ClientPacket
{
	public ObjectGuid Target;
	public byte PartyIndex;
	public bool Apply;
	public SetAssistantLeader(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		Target = _worldPacket.ReadPackedGuid();
		Apply = _worldPacket.HasBit();
	}
}

class SetPartyAssignment : ClientPacket
{
	public byte Assignment;
	public byte PartyIndex;
	public ObjectGuid Target;
	public bool Set;
	public SetPartyAssignment(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		Assignment = _worldPacket.ReadUInt8();
		Target = _worldPacket.ReadPackedGuid();
		Set = _worldPacket.HasBit();
	}
}

class DoReadyCheck : ClientPacket
{
	public sbyte PartyIndex;
	public DoReadyCheck(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
	}
}

class ReadyCheckStarted : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid PartyGUID;
	public ObjectGuid InitiatorGUID;
	public uint Duration;
	public ReadyCheckStarted() : base(ServerOpcodes.ReadyCheckStarted) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(PartyGUID);
		_worldPacket.WritePackedGuid(InitiatorGUID);
		_worldPacket.WriteUInt32(Duration);
	}
}

class ReadyCheckResponseClient : ClientPacket
{
	public byte PartyIndex;
	public bool IsReady;
	public ReadyCheckResponseClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		IsReady = _worldPacket.HasBit();
	}
}

class ReadyCheckResponse : ServerPacket
{
	public ObjectGuid PartyGUID;
	public ObjectGuid Player;
	public bool IsReady;
	public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PartyGUID);
		_worldPacket.WritePackedGuid(Player);

		_worldPacket.WriteBit(IsReady);
		_worldPacket.FlushBits();
	}
}

class ReadyCheckCompleted : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid PartyGUID;
	public ReadyCheckCompleted() : base(ServerOpcodes.ReadyCheckCompleted) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(PartyGUID);
	}
}

class RequestRaidInfo : ClientPacket
{
	public RequestRaidInfo(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class OptOutOfLoot : ClientPacket
{
	public bool PassOnLoot;
	public OptOutOfLoot(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PassOnLoot = _worldPacket.HasBit();
	}
}

class InitiateRolePoll : ClientPacket
{
	public sbyte PartyIndex;
	public InitiateRolePoll(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
	}
}

class RolePollInform : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid From;
	public RolePollInform() : base(ServerOpcodes.RolePollInform) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(From);
	}
}

class GroupNewLeader : ServerPacket
{
	public sbyte PartyIndex;
	public string Name;
	public GroupNewLeader() : base(ServerOpcodes.GroupNewLeader) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteString(Name);
	}
}

class PartyUpdate : ServerPacket
{
	public GroupFlags PartyFlags;
	public byte PartyIndex;
	public GroupType PartyType;

	public ObjectGuid PartyGUID;
	public ObjectGuid LeaderGUID;
	public byte LeaderFactionGroup;

	public int MyIndex;
	public int SequenceNum;

	public List<PartyPlayerInfo> PlayerList = new();

	public PartyLFGInfo? LfgInfos;
	public PartyLootSettings? LootSettings;
	public PartyDifficultySettings? DifficultySettings;
	public PartyUpdate() : base(ServerOpcodes.PartyUpdate) { }

	public override void Write()
	{
		_worldPacket.WriteUInt16((ushort)PartyFlags);
		_worldPacket.WriteUInt8(PartyIndex);
		_worldPacket.WriteUInt8((byte)PartyType);
		_worldPacket.WriteInt32(MyIndex);
		_worldPacket.WritePackedGuid(PartyGUID);
		_worldPacket.WriteInt32(SequenceNum);
		_worldPacket.WritePackedGuid(LeaderGUID);
		_worldPacket.WriteUInt8(LeaderFactionGroup);
		_worldPacket.WriteInt32(PlayerList.Count);
		_worldPacket.WriteBit(LfgInfos.HasValue);
		_worldPacket.WriteBit(LootSettings.HasValue);
		_worldPacket.WriteBit(DifficultySettings.HasValue);
		_worldPacket.FlushBits();

		foreach (var playerInfo in PlayerList)
			playerInfo.Write(_worldPacket);

		if (LootSettings.HasValue)
			LootSettings.Value.Write(_worldPacket);

		if (DifficultySettings.HasValue)
			DifficultySettings.Value.Write(_worldPacket);

		if (LfgInfos.HasValue)
			LfgInfos.Value.Write(_worldPacket);
	}
}

class SetEveryoneIsAssistant : ClientPacket
{
	public byte PartyIndex;
	public bool EveryoneIsAssistant;
	public SetEveryoneIsAssistant(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		EveryoneIsAssistant = _worldPacket.HasBit();
	}
}

class ChangeSubGroup : ClientPacket
{
	public ObjectGuid TargetGUID;
	public sbyte PartyIndex;
	public byte NewSubGroup;
	public ChangeSubGroup(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TargetGUID = _worldPacket.ReadPackedGuid();
		PartyIndex = _worldPacket.ReadInt8();
		NewSubGroup = _worldPacket.ReadUInt8();
	}
}

class SwapSubGroups : ClientPacket
{
	public ObjectGuid FirstTarget;
	public ObjectGuid SecondTarget;
	public sbyte PartyIndex;
	public SwapSubGroups(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
		FirstTarget = _worldPacket.ReadPackedGuid();
		SecondTarget = _worldPacket.ReadPackedGuid();
	}
}

class ClearRaidMarker : ClientPacket
{
	public byte MarkerId;
	public ClearRaidMarker(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MarkerId = _worldPacket.ReadUInt8();
	}
}

class RaidMarkersChanged : ServerPacket
{
	public sbyte PartyIndex;
	public uint ActiveMarkers;

	public List<RaidMarker> RaidMarkers = new();
	public RaidMarkersChanged() : base(ServerOpcodes.RaidMarkersChanged) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WriteUInt32(ActiveMarkers);

		_worldPacket.WriteBits(RaidMarkers.Count, 4);
		_worldPacket.FlushBits();

		foreach (var raidMarker in RaidMarkers)
		{
			_worldPacket.WritePackedGuid(raidMarker.TransportGUID);
			_worldPacket.WriteUInt32(raidMarker.Location.MapId);
			_worldPacket.WriteXYZ(raidMarker.Location);
		}
	}
}

class PartyKillLog : ServerPacket
{
	public ObjectGuid Player;
	public ObjectGuid Victim;
	public PartyKillLog() : base(ServerOpcodes.PartyKillLog) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WritePackedGuid(Victim);
	}
}

class BroadcastSummonCast : ServerPacket
{
	public ObjectGuid Target;

	public BroadcastSummonCast() : base(ServerOpcodes.BroadcastSummonCast) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
	}
}

class BroadcastSummonResponse : ServerPacket
{
	public ObjectGuid Target;
	public bool Accepted;

	public BroadcastSummonResponse() : base(ServerOpcodes.BroadcastSummonResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteBit(Accepted);
		_worldPacket.FlushBits();
	}
}

//Structs
public struct PartyMemberPhase
{
	public PartyMemberPhase(uint flags, uint id)
	{
		Flags = (ushort)flags;
		Id = (ushort)id;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt16(Flags);
		data.WriteUInt16(Id);
	}

	public ushort Flags;
	public ushort Id;
}

public class PartyMemberPhaseStates
{
	public int PhaseShiftFlags;
	public ObjectGuid PersonalGUID;
	public List<PartyMemberPhase> List = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PhaseShiftFlags);
		data.WriteInt32(List.Count);
		data.WritePackedGuid(PersonalGUID);

		foreach (var phase in List)
			phase.Write(data);
	}
}

class PartyMemberAuraStates
{
	public int SpellID;
	public ushort Flags;
	public uint ActiveFlags;
	public List<float> Points = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(SpellID);
		data.WriteUInt16(Flags);
		data.WriteUInt32(ActiveFlags);
		data.WriteInt32(Points.Count);

		foreach (var points in Points)
			data.WriteFloat(points);
	}
}

class PartyMemberPetStats
{
	public ObjectGuid GUID;
	public string Name;
	public short ModelId;

	public int CurrentHealth;
	public int MaxHealth;

	public List<PartyMemberAuraStates> Auras = new();

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(GUID);
		data.WriteInt32(ModelId);
		data.WriteInt32(CurrentHealth);
		data.WriteInt32(MaxHealth);
		data.WriteInt32(Auras.Count);
		Auras.ForEach(p => p.Write(data));

		data.WriteBits(Name.GetByteCount(), 8);
		data.FlushBits();
		data.WriteString(Name);
	}
}

public struct CTROptions
{
	public uint ContentTuningConditionMask;
	public int Unused901;
	public uint ExpansionLevelMask;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(ContentTuningConditionMask);
		data.WriteInt32(Unused901);
		data.WriteUInt32(ExpansionLevelMask);
	}
}

class PartyMemberStats
{
	public ushort Level;
	public GroupMemberOnlineStatus Status;

	public int CurrentHealth;
	public int MaxHealth;

	public byte PowerType;
	public ushort CurrentPower;
	public ushort MaxPower;

	public ushort ZoneID;
	public short PositionX;
	public short PositionY;
	public short PositionZ;

	public int VehicleSeat;

	public PartyMemberPhaseStates Phases = new();
	public List<PartyMemberAuraStates> Auras = new();
	public PartyMemberPetStats PetStats;

	public ushort PowerDisplayID;
	public ushort SpecID;
	public ushort WmoGroupID;
	public uint WmoDoodadPlacementID;
	public sbyte[] PartyType = new sbyte[2];
	public CTROptions ChromieTime;
	public DungeonScoreSummary DungeonScore = new();

	public void Write(WorldPacket data)
	{
		for (byte i = 0; i < 2; i++)
			data.WriteInt8(PartyType[i]);

		data.WriteInt16((short)Status);
		data.WriteUInt8(PowerType);
		data.WriteInt16((short)PowerDisplayID);
		data.WriteInt32(CurrentHealth);
		data.WriteInt32(MaxHealth);
		data.WriteUInt16(CurrentPower);
		data.WriteUInt16(MaxPower);
		data.WriteUInt16(Level);
		data.WriteUInt16(SpecID);
		data.WriteUInt16(ZoneID);
		data.WriteUInt16(WmoGroupID);
		data.WriteUInt32(WmoDoodadPlacementID);
		data.WriteInt16(PositionX);
		data.WriteInt16(PositionY);
		data.WriteInt16(PositionZ);
		data.WriteInt32(VehicleSeat);
		data.WriteInt32(Auras.Count);

		Phases.Write(data);
		ChromieTime.Write(data);

		foreach (var aura in Auras)
			aura.Write(data);

		data.WriteBit(PetStats != null);
		data.FlushBits();

		DungeonScore.Write(data);

		if (PetStats != null)
			PetStats.Write(data);
	}
}

struct PartyPlayerInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteBits(Name.GetByteCount(), 6);
		data.WriteBits(VoiceStateID.GetByteCount() + 1, 6);
		data.WriteBit(Connected);
		data.WriteBit(VoiceChatSilenced);
		data.WriteBit(FromSocialQueue);
		data.WritePackedGuid(GUID);
		data.WriteUInt8(Subgroup);
		data.WriteUInt8(Flags);
		data.WriteUInt8(RolesAssigned);
		data.WriteUInt8(Class);
		data.WriteUInt8(FactionGroup);
		data.WriteString(Name);

		if (!VoiceStateID.IsEmpty())
			data.WriteString(VoiceStateID);
	}

	public ObjectGuid GUID;
	public string Name;
	public string VoiceStateID; // same as bgs.protocol.club.v1.MemberVoiceState.id
	public byte Class;
	public byte Subgroup;
	public byte Flags;
	public byte RolesAssigned;
	public byte FactionGroup;
	public bool FromSocialQueue;
	public bool VoiceChatSilenced;
	public bool Connected;
}

struct PartyLFGInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt8(MyFlags);
		data.WriteUInt32(Slot);
		data.WriteUInt32(MyRandomSlot);
		data.WriteUInt8(MyPartialClear);
		data.WriteFloat(MyGearDiff);
		data.WriteUInt8(MyStrangerCount);
		data.WriteUInt8(MyKickVoteCount);
		data.WriteUInt8(BootCount);
		data.WriteBit(Aborted);
		data.WriteBit(MyFirstReward);
		data.FlushBits();
	}

	public byte MyFlags;
	public uint Slot;
	public byte BootCount;
	public uint MyRandomSlot;
	public bool Aborted;
	public byte MyPartialClear;
	public float MyGearDiff;
	public byte MyStrangerCount;
	public byte MyKickVoteCount;
	public bool MyFirstReward;
}

struct PartyLootSettings
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Method);
		data.WritePackedGuid(LootMaster);
		data.WriteUInt8(Threshold);
	}

	public byte Method;
	public ObjectGuid LootMaster;
	public byte Threshold;
}

struct PartyDifficultySettings
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(DungeonDifficultyID);
		data.WriteUInt32(RaidDifficultyID);
		data.WriteUInt32(LegacyRaidDifficultyID);
	}

	public uint DungeonDifficultyID;
	public uint RaidDifficultyID;
	public uint LegacyRaidDifficultyID;
}