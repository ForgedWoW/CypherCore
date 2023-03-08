﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class BindPointUpdate : ServerPacket
{
	public uint BindMapID = 0xFFFFFFFF;
	public Vector3 BindPosition;
	public uint BindAreaID;
	public BindPointUpdate() : base(ServerOpcodes.BindPointUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteVector3(BindPosition);
		_worldPacket.WriteUInt32(BindMapID);
		_worldPacket.WriteUInt32(BindAreaID);
	}
}

public class PlayerBound : ServerPacket
{
	readonly uint AreaID;

	readonly ObjectGuid BinderID;

	public PlayerBound(ObjectGuid binderId, uint areaId) : base(ServerOpcodes.PlayerBound)
	{
		BinderID = binderId;
		AreaID = areaId;
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(BinderID);
		_worldPacket.WriteUInt32(AreaID);
	}
}

public class InvalidatePlayer : ServerPacket
{
	public ObjectGuid Guid;
	public InvalidatePlayer() : base(ServerOpcodes.InvalidatePlayer) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
	}
}

public class LoginSetTimeSpeed : ServerPacket
{
	public float NewSpeed;
	public int ServerTimeHolidayOffset;
	public uint GameTime;
	public uint ServerTime;
	public int GameTimeHolidayOffset;
	public LoginSetTimeSpeed() : base(ServerOpcodes.LoginSetTimeSpeed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedTime(ServerTime);
		_worldPacket.WritePackedTime(GameTime);
		_worldPacket.WriteFloat(NewSpeed);
		_worldPacket.WriteInt32(ServerTimeHolidayOffset);
		_worldPacket.WriteInt32(GameTimeHolidayOffset);
	}
}

public class ResetWeeklyCurrency : ServerPacket
{
	public ResetWeeklyCurrency() : base(ServerOpcodes.ResetWeeklyCurrency, ConnectionType.Instance) { }

	public override void Write() { }
}

public class SetCurrency : ServerPacket
{
	public uint Type;
	public int Quantity;
	public CurrencyGainFlags Flags;
	public List<UiEventToast> Toasts = new();
	public int? WeeklyQuantity;
	public int? TrackedQuantity;
	public int? MaxQuantity;
	public int? TotalEarned;
	public int? QuantityChange;
	public CurrencyGainSource? QuantityGainSource;
	public CurrencyDestroyReason? QuantityLostSource;
	public uint? FirstCraftOperationID;
	public long? LastSpendTime;
	public bool SuppressChatLog;
	public SetCurrency() : base(ServerOpcodes.SetCurrency, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Type);
		_worldPacket.WriteInt32(Quantity);
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WriteInt32(Toasts.Count);

		foreach (var toast in Toasts)
			toast.Write(_worldPacket);

		_worldPacket.WriteBit(WeeklyQuantity.HasValue);
		_worldPacket.WriteBit(TrackedQuantity.HasValue);
		_worldPacket.WriteBit(MaxQuantity.HasValue);
		_worldPacket.WriteBit(TotalEarned.HasValue);
		_worldPacket.WriteBit(SuppressChatLog);
		_worldPacket.WriteBit(QuantityChange.HasValue);
		_worldPacket.WriteBit(QuantityGainSource.HasValue);
		_worldPacket.WriteBit(QuantityLostSource.HasValue);
		_worldPacket.WriteBit(FirstCraftOperationID.HasValue);
		_worldPacket.WriteBit(LastSpendTime.HasValue);
		_worldPacket.FlushBits();

		if (WeeklyQuantity.HasValue)
			_worldPacket.WriteInt32(WeeklyQuantity.Value);

		if (TrackedQuantity.HasValue)
			_worldPacket.WriteInt32(TrackedQuantity.Value);

		if (MaxQuantity.HasValue)
			_worldPacket.WriteInt32(MaxQuantity.Value);

		if (TotalEarned.HasValue)
			_worldPacket.WriteInt32(TotalEarned.Value);

		if (QuantityChange.HasValue)
			_worldPacket.WriteInt32(QuantityChange.Value);

		if (QuantityGainSource.HasValue)
			_worldPacket.WriteInt32((int)QuantityGainSource.Value);

		if (QuantityLostSource.HasValue)
			_worldPacket.WriteInt32((int)QuantityLostSource.Value);

		if (FirstCraftOperationID.HasValue)
			_worldPacket.WriteUInt32(FirstCraftOperationID.Value);

		if (LastSpendTime.HasValue)
			_worldPacket.WriteInt64(LastSpendTime.Value);
	}
}

public class SetMaxWeeklyQuantity : ServerPacket
{
	public uint MaxWeeklyQuantity;
	public uint Type;
	public SetMaxWeeklyQuantity() : base(ServerOpcodes.SetMaxWeeklyQuantity, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Type);
		_worldPacket.WriteUInt32(MaxWeeklyQuantity);
	}
}

public class SetSelection : ClientPacket
{
	public ObjectGuid Selection; // Target
	public SetSelection(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Selection = _worldPacket.ReadPackedGuid();
	}
}

public class SetupCurrency : ServerPacket
{
	public List<Record> Data = new();
	public SetupCurrency() : base(ServerOpcodes.SetupCurrency, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Data.Count);

		foreach (var data in Data)
		{
			_worldPacket.WriteUInt32(data.Type);
			_worldPacket.WriteUInt32(data.Quantity);

			_worldPacket.WriteBit(data.WeeklyQuantity.HasValue);
			_worldPacket.WriteBit(data.MaxWeeklyQuantity.HasValue);
			_worldPacket.WriteBit(data.TrackedQuantity.HasValue);
			_worldPacket.WriteBit(data.MaxQuantity.HasValue);
			_worldPacket.WriteBit(data.TotalEarned.HasValue);
			_worldPacket.WriteBit(data.LastSpendTime.HasValue);
			_worldPacket.WriteBits(data.Flags, 5);
			_worldPacket.FlushBits();

			if (data.WeeklyQuantity.HasValue)
				_worldPacket.WriteUInt32(data.WeeklyQuantity.Value);

			if (data.MaxWeeklyQuantity.HasValue)
				_worldPacket.WriteUInt32(data.MaxWeeklyQuantity.Value);

			if (data.TrackedQuantity.HasValue)
				_worldPacket.WriteUInt32(data.TrackedQuantity.Value);

			if (data.MaxQuantity.HasValue)
				_worldPacket.WriteInt32(data.MaxQuantity.Value);

			if (data.TotalEarned.HasValue)
				_worldPacket.WriteInt32(data.TotalEarned.Value);

			if (data.LastSpendTime.HasValue)
				_worldPacket.WriteInt64(data.LastSpendTime.Value);
		}
	}

	public struct Record
	{
		public uint Type;
		public uint Quantity;
		public uint? WeeklyQuantity;    // Currency count obtained this Week.  
		public uint? MaxWeeklyQuantity; // Weekly Currency cap.
		public uint? TrackedQuantity;
		public int? MaxQuantity;
		public int? TotalEarned;
		public long? LastSpendTime;
		public byte Flags;
	}
}

public class ViolenceLevel : ClientPacket
{
	public sbyte violenceLevel; // 0 - no combat effects, 1 - display some combat effects, 2 - blood, 3 - bloody, 4 - bloodier, 5 - bloodiest
	public ViolenceLevel(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		violenceLevel = _worldPacket.ReadInt8();
	}
}

public class TimeSyncRequest : ServerPacket
{
	public uint SequenceIndex;
	public TimeSyncRequest() : base(ServerOpcodes.TimeSyncRequest, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SequenceIndex);
	}
}

public class TimeSyncResponse : ClientPacket
{
	public uint ClientTime;    // Client ticks in ms
	public uint SequenceIndex; // Same index as in request
	public TimeSyncResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SequenceIndex = _worldPacket.ReadUInt32();
		ClientTime = _worldPacket.ReadUInt32();
	}

	public DateTime GetReceivedTime()
	{
		return _worldPacket.GetReceivedTime();
	}
}

public class TriggerCinematic : ServerPacket
{
	public uint CinematicID;
	public ObjectGuid ConversationGuid;
	public TriggerCinematic() : base(ServerOpcodes.TriggerCinematic) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(CinematicID);
		_worldPacket.WritePackedGuid(ConversationGuid);
	}
}

public class TriggerMovie : ServerPacket
{
	public uint MovieID;
	public TriggerMovie() : base(ServerOpcodes.TriggerMovie) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MovieID);
	}
}

public class ServerTimeOffsetRequest : ClientPacket
{
	public ServerTimeOffsetRequest(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class ServerTimeOffset : ServerPacket
{
	public long Time;
	public ServerTimeOffset() : base(ServerOpcodes.ServerTimeOffset) { }

	public override void Write()
	{
		_worldPacket.WriteInt64(Time);
	}
}

public class TutorialFlags : ServerPacket
{
	public uint[] TutorialData = new uint[SharedConst.MaxAccountTutorialValues];
	public TutorialFlags() : base(ServerOpcodes.TutorialFlags) { }

	public override void Write()
	{
		for (byte i = 0; i < (int)Tutorials.Max; ++i)
			_worldPacket.WriteUInt32(TutorialData[i]);
	}
}

public class TutorialSetFlag : ClientPacket
{
	public TutorialAction Action;
	public uint TutorialBit;
	public TutorialSetFlag(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Action = (TutorialAction)_worldPacket.ReadBits<byte>(2);

		if (Action == TutorialAction.Update)
			TutorialBit = _worldPacket.ReadUInt32();
	}
}

public class WorldServerInfo : ServerPacket
{
	public uint DifficultyID;
	public bool IsTournamentRealm;
	public bool XRealmPvpAlert;

	public bool BlockExitingLoadingScreen; // when set to true, sending SMSG_UPDATE_OBJECT with CreateObject Self bit = true will not hide loading screen

	// instead it will be done after this packet is sent again with false in this bit and SMSG_UPDATE_OBJECT Values for player
	public uint? RestrictedAccountMaxLevel;
	public ulong? RestrictedAccountMaxMoney;
	public uint? InstanceGroupSize;

	public WorldServerInfo() : base(ServerOpcodes.WorldServerInfo, ConnectionType.Instance)
	{
		InstanceGroupSize = new uint?();

		RestrictedAccountMaxLevel = new uint?();
		RestrictedAccountMaxMoney = new ulong?();
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(DifficultyID);
		_worldPacket.WriteBit(IsTournamentRealm);
		_worldPacket.WriteBit(XRealmPvpAlert);
		_worldPacket.WriteBit(BlockExitingLoadingScreen);
		_worldPacket.WriteBit(RestrictedAccountMaxLevel.HasValue);
		_worldPacket.WriteBit(RestrictedAccountMaxMoney.HasValue);
		_worldPacket.WriteBit(InstanceGroupSize.HasValue);
		_worldPacket.FlushBits();

		if (RestrictedAccountMaxLevel.HasValue)
			_worldPacket.WriteUInt32(RestrictedAccountMaxLevel.Value);

		if (RestrictedAccountMaxMoney.HasValue)
			_worldPacket.WriteUInt64(RestrictedAccountMaxMoney.Value);

		if (InstanceGroupSize.HasValue)
			_worldPacket.WriteUInt32(InstanceGroupSize.Value);
	}
}

public class SetDungeonDifficulty : ClientPacket
{
	public uint DifficultyID;
	public SetDungeonDifficulty(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		DifficultyID = _worldPacket.ReadUInt32();
	}
}

public class SetRaidDifficulty : ClientPacket
{
	public int DifficultyID;
	public byte Legacy;
	public SetRaidDifficulty(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		DifficultyID = _worldPacket.ReadInt32();
		Legacy = _worldPacket.ReadUInt8();
	}
}

public class DungeonDifficultySet : ServerPacket
{
	public int DifficultyID;
	public DungeonDifficultySet() : base(ServerOpcodes.SetDungeonDifficulty) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(DifficultyID);
	}
}

public class RaidDifficultySet : ServerPacket
{
	public int DifficultyID;
	public bool Legacy;
	public RaidDifficultySet() : base(ServerOpcodes.RaidDifficultySet) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(DifficultyID);
		_worldPacket.WriteUInt8((byte)(Legacy ? 1 : 0));
	}
}

public class CorpseReclaimDelay : ServerPacket
{
	public uint Remaining;
	public CorpseReclaimDelay() : base(ServerOpcodes.CorpseReclaimDelay, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Remaining);
	}
}

public class DeathReleaseLoc : ServerPacket
{
	public int MapID;
	public WorldLocation Loc;
	public DeathReleaseLoc() : base(ServerOpcodes.DeathReleaseLoc) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteXYZ(Loc);
	}
}

public class PortGraveyard : ClientPacket
{
	public PortGraveyard(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class PreRessurect : ServerPacket
{
	public ObjectGuid PlayerGUID;
	public PreRessurect() : base(ServerOpcodes.PreRessurect) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(PlayerGUID);
	}
}

public class ReclaimCorpse : ClientPacket
{
	public ObjectGuid CorpseGUID;
	public ReclaimCorpse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CorpseGUID = _worldPacket.ReadPackedGuid();
	}
}

public class RepopRequest : ClientPacket
{
	public bool CheckInstance;
	public RepopRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CheckInstance = _worldPacket.HasBit();
	}
}

public class RequestCemeteryList : ClientPacket
{
	public RequestCemeteryList(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

public class RequestCemeteryListResponse : ServerPacket
{
	public bool IsGossipTriggered;
	public List<uint> CemeteryID = new();
	public RequestCemeteryListResponse() : base(ServerOpcodes.RequestCemeteryListResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsGossipTriggered);
		_worldPacket.FlushBits();

		_worldPacket.WriteInt32(CemeteryID.Count);

		foreach (var cemetery in CemeteryID)
			_worldPacket.WriteUInt32(cemetery);
	}
}

public class ResurrectResponse : ClientPacket
{
	public ObjectGuid Resurrecter;
	public uint Response;
	public ResurrectResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Resurrecter = _worldPacket.ReadPackedGuid();
		Response = _worldPacket.ReadUInt32();
	}
}

public class WeatherPkt : ServerPacket
{
	readonly bool Abrupt;
	readonly float Intensity;
	readonly WeatherState WeatherID;

	public WeatherPkt(WeatherState weatherID = 0, float intensity = 0.0f, bool abrupt = false) : base(ServerOpcodes.Weather, ConnectionType.Instance)
	{
		WeatherID = weatherID;
		Intensity = intensity;
		Abrupt = abrupt;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)WeatherID);
		_worldPacket.WriteFloat(Intensity);
		_worldPacket.WriteBit(Abrupt);

		_worldPacket.FlushBits();
	}
}

public class StandStateChange : ClientPacket
{
	public UnitStandStateType StandState;
	public StandStateChange(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		StandState = (UnitStandStateType)_worldPacket.ReadUInt32();
	}
}

public class StandStateUpdate : ServerPacket
{
	readonly uint AnimKitID;
	readonly UnitStandStateType State;

	public StandStateUpdate(UnitStandStateType state, uint animKitId) : base(ServerOpcodes.StandStateUpdate)
	{
		State = state;
		AnimKitID = animKitId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(AnimKitID);
		_worldPacket.WriteUInt8((byte)State);
	}
}

public class SetAnimTier : ServerPacket
{
	public ObjectGuid Unit;
	public int Tier;
	public SetAnimTier() : base(ServerOpcodes.SetAnimTier, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteBits(Tier, 3);
		_worldPacket.FlushBits();
	}
}

public class StartMirrorTimer : ServerPacket
{
	public int Scale;
	public int MaxValue;
	public MirrorTimerType Timer;
	public int SpellID;
	public int Value;
	public bool Paused;

	public StartMirrorTimer(MirrorTimerType timer, int value, int maxValue, int scale, int spellID, bool paused) : base(ServerOpcodes.StartMirrorTimer)
	{
		Timer = timer;
		Value = value;
		MaxValue = maxValue;
		Scale = scale;
		SpellID = spellID;
		Paused = paused;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Timer);
		_worldPacket.WriteInt32(Value);
		_worldPacket.WriteInt32(MaxValue);
		_worldPacket.WriteInt32(Scale);
		_worldPacket.WriteInt32(SpellID);
		_worldPacket.WriteBit(Paused);
		_worldPacket.FlushBits();
	}
}

public class PauseMirrorTimer : ServerPacket
{
	public bool Paused = true;
	public MirrorTimerType Timer;

	public PauseMirrorTimer(MirrorTimerType timer, bool paused) : base(ServerOpcodes.PauseMirrorTimer)
	{
		Timer = timer;
		Paused = paused;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Timer);
		_worldPacket.WriteBit(Paused);
		_worldPacket.FlushBits();
	}
}

public class StopMirrorTimer : ServerPacket
{
	public MirrorTimerType Timer;

	public StopMirrorTimer(MirrorTimerType timer) : base(ServerOpcodes.StopMirrorTimer)
	{
		Timer = timer;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Timer);
	}
}

public class ExplorationExperience : ServerPacket
{
	public uint Experience;
	public uint AreaID;

	public ExplorationExperience(uint experience, uint areaID) : base(ServerOpcodes.ExplorationExperience)
	{
		Experience = experience;
		AreaID = areaID;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(AreaID);
		_worldPacket.WriteUInt32(Experience);
	}
}

public class LevelUpInfo : ServerPacket
{
	public uint Level = 0;
	public uint HealthDelta = 0;
	public int[] PowerDelta = new int[(int)PowerType.MaxPerClass];
	public int[] StatDelta = new int[(int)Stats.Max];
	public int NumNewTalents;
	public int NumNewPvpTalentSlots;
	public LevelUpInfo() : base(ServerOpcodes.LevelUpInfo) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Level);
		_worldPacket.WriteUInt32(HealthDelta);

		foreach (var power in PowerDelta)
			_worldPacket.WriteInt32(power);

		foreach (var stat in StatDelta)
			_worldPacket.WriteInt32(stat);

		_worldPacket.WriteInt32(NumNewTalents);
		_worldPacket.WriteInt32(NumNewPvpTalentSlots);
	}
}

public class PlayMusic : ServerPacket
{
	readonly uint SoundKitID;

	public PlayMusic(uint soundKitID) : base(ServerOpcodes.PlayMusic)
	{
		SoundKitID = soundKitID;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SoundKitID);
	}
}

public class RandomRollClient : ClientPacket
{
	public uint Min;
	public uint Max;
	public byte PartyIndex;
	public RandomRollClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Min = _worldPacket.ReadUInt32();
		Max = _worldPacket.ReadUInt32();
		PartyIndex = _worldPacket.ReadUInt8();
	}
}

public class RandomRoll : ServerPacket
{
	public ObjectGuid Roller;
	public ObjectGuid RollerWowAccount;
	public int Min;
	public int Max;
	public int Result;

	public RandomRoll() : base(ServerOpcodes.RandomRoll) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Roller);
		_worldPacket.WritePackedGuid(RollerWowAccount);
		_worldPacket.WriteInt32(Min);
		_worldPacket.WriteInt32(Max);
		_worldPacket.WriteInt32(Result);
	}
}

public class EnableBarberShop : ServerPacket
{
	public EnableBarberShop() : base(ServerOpcodes.EnableBarberShop) { }

	public override void Write() { }
}

class PhaseShiftChange : ServerPacket
{
	public ObjectGuid Client;
	public PhaseShiftData Phaseshift = new();
	public List<ushort> PreloadMapIDs = new();
	public List<ushort> UiMapPhaseIDs = new();
	public List<ushort> VisibleMapIDs = new();
	public PhaseShiftChange() : base(ServerOpcodes.PhaseShiftChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Client);
		Phaseshift.Write(_worldPacket);
		_worldPacket.WriteInt32(VisibleMapIDs.Count * 2); // size in bytes

		foreach (var visibleMapId in VisibleMapIDs)
			_worldPacket.WriteUInt16(visibleMapId); // Active terrain swap map id

		_worldPacket.WriteInt32(PreloadMapIDs.Count * 2); // size in bytes

		foreach (var preloadMapId in PreloadMapIDs)
			_worldPacket.WriteUInt16(preloadMapId); // Inactive terrain swap map id

		_worldPacket.WriteInt32(UiMapPhaseIDs.Count * 2); // size in bytes

		foreach (var uiMapPhaseId in UiMapPhaseIDs)
			_worldPacket.WriteUInt16(uiMapPhaseId); // UI map id, WorldMapArea.db2, controls map display
	}
}

public class ZoneUnderAttack : ServerPacket
{
	public int AreaID;
	public ZoneUnderAttack() : base(ServerOpcodes.ZoneUnderAttack, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(AreaID);
	}
}

class DurabilityDamageDeath : ServerPacket
{
	public uint Percent;
	public DurabilityDamageDeath() : base(ServerOpcodes.DurabilityDamageDeath) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Percent);
	}
}

class ObjectUpdateFailed : ClientPacket
{
	public ObjectGuid ObjectGUID;
	public ObjectUpdateFailed(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ObjectGUID = _worldPacket.ReadPackedGuid();
	}
}

class ObjectUpdateRescued : ClientPacket
{
	public ObjectGuid ObjectGUID;
	public ObjectUpdateRescued(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ObjectGUID = _worldPacket.ReadPackedGuid();
	}
}

class PlayObjectSound : ServerPacket
{
	public ObjectGuid TargetObjectGUID;
	public ObjectGuid SourceObjectGUID;
	public uint SoundKitID;
	public Vector3 Position;
	public int BroadcastTextID;
	public PlayObjectSound() : base(ServerOpcodes.PlayObjectSound) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SoundKitID);
		_worldPacket.WritePackedGuid(SourceObjectGUID);
		_worldPacket.WritePackedGuid(TargetObjectGUID);
		_worldPacket.WriteVector3(Position);
		_worldPacket.WriteInt32(BroadcastTextID);
	}
}

class PlaySound : ServerPacket
{
	public ObjectGuid SourceObjectGuid;
	public uint SoundKitID;
	public uint BroadcastTextID;

	public PlaySound(ObjectGuid sourceObjectGuid, uint soundKitID, uint broadcastTextId) : base(ServerOpcodes.PlaySound)
	{
		SourceObjectGuid = sourceObjectGuid;
		SoundKitID = soundKitID;
		BroadcastTextID = broadcastTextId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SoundKitID);
		_worldPacket.WritePackedGuid(SourceObjectGuid);
		_worldPacket.WriteUInt32(BroadcastTextID);
	}
}

class PlaySpeakerBoxSound : ServerPacket
{
	public ObjectGuid SourceObjectGUID;
	public uint SoundKitID;

	public PlaySpeakerBoxSound(ObjectGuid sourceObjectGuid, uint soundKitID) : base(ServerOpcodes.PlaySpeakerbotSound)
	{
		SourceObjectGUID = sourceObjectGuid;
		SoundKitID = soundKitID;
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SourceObjectGUID);
		_worldPacket.WriteUInt32(SoundKitID);
	}
}

class OpeningCinematic : ClientPacket
{
	public OpeningCinematic(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class CompleteCinematic : ClientPacket
{
	public CompleteCinematic(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class NextCinematicCamera : ClientPacket
{
	public NextCinematicCamera(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class CompleteMovie : ClientPacket
{
	public CompleteMovie(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class FarSight : ClientPacket
{
	public bool Enable;
	public FarSight(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Enable = _worldPacket.HasBit();
	}
}

class SaveCUFProfiles : ClientPacket
{
	public List<CufProfile> CUFProfiles = new();
	public SaveCUFProfiles(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadUInt32();

		for (byte i = 0; i < count && i < PlayerConst.MaxCUFProfiles; i++)
		{
			CufProfile cufProfile = new();

			var strLen = _worldPacket.ReadBits<byte>(7);

			// Bool Options
			for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
				cufProfile.BoolOptions.Set(option, _worldPacket.HasBit());

			// Other Options
			cufProfile.FrameHeight = _worldPacket.ReadUInt16();
			cufProfile.FrameWidth = _worldPacket.ReadUInt16();

			cufProfile.SortBy = _worldPacket.ReadUInt8();
			cufProfile.HealthText = _worldPacket.ReadUInt8();

			cufProfile.TopPoint = _worldPacket.ReadUInt8();
			cufProfile.BottomPoint = _worldPacket.ReadUInt8();
			cufProfile.LeftPoint = _worldPacket.ReadUInt8();

			cufProfile.TopOffset = _worldPacket.ReadUInt16();
			cufProfile.BottomOffset = _worldPacket.ReadUInt16();
			cufProfile.LeftOffset = _worldPacket.ReadUInt16();

			cufProfile.ProfileName = _worldPacket.ReadString(strLen);

			CUFProfiles.Add(cufProfile);
		}
	}
}

class LoadCUFProfiles : ServerPacket
{
	public List<CufProfile> CUFProfiles = new();
	public LoadCUFProfiles() : base(ServerOpcodes.LoadCufProfiles, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(CUFProfiles.Count);

		foreach (var cufProfile in CUFProfiles)
		{
			_worldPacket.WriteBits(cufProfile.ProfileName.GetByteCount(), 7);

			// Bool Options
			for (byte option = 0; option < (int)CUFBoolOptions.BoolOptionsCount; option++)
				_worldPacket.WriteBit(cufProfile.BoolOptions[option]);

			// Other Options
			_worldPacket.WriteUInt16(cufProfile.FrameHeight);
			_worldPacket.WriteUInt16(cufProfile.FrameWidth);

			_worldPacket.WriteUInt8(cufProfile.SortBy);
			_worldPacket.WriteUInt8(cufProfile.HealthText);

			_worldPacket.WriteUInt8(cufProfile.TopPoint);
			_worldPacket.WriteUInt8(cufProfile.BottomPoint);
			_worldPacket.WriteUInt8(cufProfile.LeftPoint);

			_worldPacket.WriteUInt16(cufProfile.TopOffset);
			_worldPacket.WriteUInt16(cufProfile.BottomOffset);
			_worldPacket.WriteUInt16(cufProfile.LeftOffset);

			_worldPacket.WriteString(cufProfile.ProfileName);
		}
	}
}

class PlayOneShotAnimKit : ServerPacket
{
	public ObjectGuid Unit;
	public ushort AnimKitID;
	public PlayOneShotAnimKit() : base(ServerOpcodes.PlayOneShotAnimKit) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt16(AnimKitID);
	}
}

class SetAIAnimKit : ServerPacket
{
	public ObjectGuid Unit;
	public ushort AnimKitID;
	public SetAIAnimKit() : base(ServerOpcodes.SetAiAnimKit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt16(AnimKitID);
	}
}

class SetMeleeAnimKit : ServerPacket
{
	public ObjectGuid Unit;
	public ushort AnimKitID;
	public SetMeleeAnimKit() : base(ServerOpcodes.SetMeleeAnimKit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt16(AnimKitID);
	}
}

class SetMovementAnimKit : ServerPacket
{
	public ObjectGuid Unit;
	public ushort AnimKitID;
	public SetMovementAnimKit() : base(ServerOpcodes.SetMovementAnimKit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt16(AnimKitID);
	}
}

class SetPlayHoverAnim : ServerPacket
{
	public ObjectGuid UnitGUID;
	public bool PlayHoverAnim;
	public SetPlayHoverAnim() : base(ServerOpcodes.SetPlayHoverAnim, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteBit(PlayHoverAnim);
		_worldPacket.FlushBits();
	}
}

class TogglePvP : ClientPacket
{
	public TogglePvP(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class SetPvP : ClientPacket
{
	public bool EnablePVP;
	public SetPvP(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		EnablePVP = _worldPacket.HasBit();
	}
}

class SetWarMode : ClientPacket
{
	public bool Enable;
	public SetWarMode(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Enable = _worldPacket.HasBit();
	}
}

class AccountHeirloomUpdate : ServerPacket
{
	public bool IsFullUpdate;
	public Dictionary<uint, HeirloomData> Heirlooms = new();
	public int Unk;
	public AccountHeirloomUpdate() : base(ServerOpcodes.AccountHeirloomUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsFullUpdate);
		_worldPacket.FlushBits();

		_worldPacket.WriteInt32(Unk);

		// both lists have to have the same size
		_worldPacket.WriteInt32(Heirlooms.Count);
		_worldPacket.WriteInt32(Heirlooms.Count);

		foreach (var item in Heirlooms)
			_worldPacket.WriteUInt32(item.Key);

		foreach (var flags in Heirlooms)
			_worldPacket.WriteUInt32((uint)flags.Value.Flags);
	}
}

class MountSpecial : ClientPacket
{
	public int[] SpellVisualKitIDs;
	public int SequenceVariation;
	public MountSpecial(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellVisualKitIDs = new int[_worldPacket.ReadUInt32()];
		SequenceVariation = _worldPacket.ReadInt32();

		for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
			SpellVisualKitIDs[i] = _worldPacket.ReadInt32();
	}
}

class SpecialMountAnim : ServerPacket
{
	public ObjectGuid UnitGUID;
	public List<int> SpellVisualKitIDs = new();
	public int SequenceVariation;
	public SpecialMountAnim() : base(ServerOpcodes.SpecialMountAnim, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(SpellVisualKitIDs.Count);
		_worldPacket.WriteInt32(SequenceVariation);

		foreach (var id in SpellVisualKitIDs)
			_worldPacket.WriteInt32(id);
	}
}

class CrossedInebriationThreshold : ServerPacket
{
	public ObjectGuid Guid;
	public uint ItemID;
	public uint Threshold;
	public CrossedInebriationThreshold() : base(ServerOpcodes.CrossedInebriationThreshold) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(Threshold);
		_worldPacket.WriteUInt32(ItemID);
	}
}

class SetTaxiBenchmarkMode : ClientPacket
{
	public bool Enable;
	public SetTaxiBenchmarkMode(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Enable = _worldPacket.HasBit();
	}
}

class OverrideLight : ServerPacket
{
	public uint AreaLightID;
	public uint TransitionMilliseconds;
	public uint OverrideLightID;
	public OverrideLight() : base(ServerOpcodes.OverrideLight) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(AreaLightID);
		_worldPacket.WriteUInt32(OverrideLightID);
		_worldPacket.WriteUInt32(TransitionMilliseconds);
	}
}

public class StartTimer : ServerPacket
{
	public uint TotalTime;
	public uint TimeLeft;
	public TimerType Type;
	public StartTimer() : base(ServerOpcodes.StartTimer) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(TotalTime);
		_worldPacket.WriteUInt32(TimeLeft);
		_worldPacket.WriteInt32((int)Type);
	}
}

class ConversationLineStarted : ClientPacket
{
	public ObjectGuid ConversationGUID;
	public uint LineID;

	public ConversationLineStarted(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ConversationGUID = _worldPacket.ReadPackedGuid();
		LineID = _worldPacket.ReadUInt32();
	}
}

class RequestLatestSplashScreen : ClientPacket
{
	public RequestLatestSplashScreen(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class SplashScreenShowLatest : ServerPacket
{
	public uint UISplashScreenID;
	public SplashScreenShowLatest() : base(ServerOpcodes.SplashScreenShowLatest, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(UISplashScreenID);
	}
}

class DisplayToast : ServerPacket
{
	public ulong Quantity;
	public DisplayToastMethod DisplayToastMethod;
	public bool Mailed;
	public DisplayToastType Type = DisplayToastType.Money;
	public uint QuestID;
	public bool IsSecondaryResult;
	public ItemInstance Item;
	public bool BonusRoll;
	public int LootSpec;
	public Gender Gender = Gender.None;
	public uint CurrencyID;

	public DisplayToast() : base(ServerOpcodes.DisplayToast, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt64(Quantity);
		_worldPacket.WriteUInt8((byte)DisplayToastMethod);
		_worldPacket.WriteUInt32(QuestID);

		_worldPacket.WriteBit(Mailed);
		_worldPacket.WriteBits((byte)Type, 2);
		_worldPacket.WriteBit(IsSecondaryResult);

		switch (Type)
		{
			case DisplayToastType.NewItem:
				_worldPacket.WriteBit(BonusRoll);
				Item.Write(_worldPacket);
				_worldPacket.WriteInt32(LootSpec);
				_worldPacket.WriteInt32((int)Gender);

				break;
			case DisplayToastType.NewCurrency:
				_worldPacket.WriteUInt32(CurrencyID);

				break;
			default:
				break;
		}

		_worldPacket.FlushBits();
	}
}

class DisplayGameError : ServerPacket
{
	readonly GameError Error;
	readonly int? Arg;
	readonly int? Arg2;

	public DisplayGameError(GameError error) : base(ServerOpcodes.DisplayGameError)
	{
		Error = error;
	}

	public DisplayGameError(GameError error, int arg) : this(error)
	{
		Arg = arg;
	}

	public DisplayGameError(GameError error, int arg1, int arg2) : this(error)
	{
		Arg = arg1;
		Arg2 = arg2;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Error);
		_worldPacket.WriteBit(Arg.HasValue);
		_worldPacket.WriteBit(Arg2.HasValue);
		_worldPacket.FlushBits();

		if (Arg.HasValue)
			_worldPacket.WriteInt32(Arg.Value);

		if (Arg2.HasValue)
			_worldPacket.WriteInt32(Arg2.Value);
	}
}

class AccountMountUpdate : ServerPacket
{
	public bool IsFullUpdate = false;
	public Dictionary<uint, MountStatusFlags> Mounts = new();
	public AccountMountUpdate() : base(ServerOpcodes.AccountMountUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsFullUpdate);
		_worldPacket.WriteInt32(Mounts.Count);

		foreach (var spell in Mounts)
		{
			_worldPacket.WriteUInt32(spell.Key);
			_worldPacket.WriteBits(spell.Value, 2);
		}

		_worldPacket.FlushBits();
	}
}

class MountSetFavorite : ClientPacket
{
	public uint MountSpellID;
	public bool IsFavorite;
	public MountSetFavorite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MountSpellID = _worldPacket.ReadUInt32();
		IsFavorite = _worldPacket.HasBit();
	}
}

class CloseInteraction : ClientPacket
{
	public ObjectGuid SourceGuid;
	public CloseInteraction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SourceGuid = _worldPacket.ReadPackedGuid();
	}
}

//Structs
struct PhaseShiftDataPhase
{
	public PhaseShiftDataPhase(uint phaseFlags, uint id)
	{
		PhaseFlags = (ushort)phaseFlags;
		Id = (ushort)id;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt16(PhaseFlags);
		data.WriteUInt16(Id);
	}

	public ushort PhaseFlags;
	public ushort Id;
}

class PhaseShiftData
{
	public uint PhaseShiftFlags;
	public List<PhaseShiftDataPhase> Phases = new();
	public ObjectGuid PersonalGUID;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(PhaseShiftFlags);
		data.WriteInt32(Phases.Count);
		data.WritePackedGuid(PersonalGUID);

		foreach (var phaseShiftDataPhase in Phases)
			phaseShiftDataPhase.Write(data);
	}
}