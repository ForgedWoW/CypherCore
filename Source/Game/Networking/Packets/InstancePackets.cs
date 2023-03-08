// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class UpdateLastInstance : ServerPacket
{
	public uint MapID;
	public UpdateLastInstance() : base(ServerOpcodes.UpdateLastInstance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
	}
}

class InstanceInfoPkt : ServerPacket
{
	public List<InstanceLockPkt> LockList = new();
	public InstanceInfoPkt() : base(ServerOpcodes.InstanceInfo) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(LockList.Count);

		foreach (var lockInfos in LockList)
			lockInfos.Write(_worldPacket);
	}
}

class ResetInstances : ClientPacket
{
	public ResetInstances(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}

class InstanceReset : ServerPacket
{
	public uint MapID;
	public InstanceReset() : base(ServerOpcodes.InstanceReset) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
	}
}

class InstanceResetFailed : ServerPacket
{
	public uint MapID;
	public ResetFailedReason ResetFailedReason;
	public InstanceResetFailed() : base(ServerOpcodes.InstanceResetFailed) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
		_worldPacket.WriteBits(ResetFailedReason, 2);
		_worldPacket.FlushBits();
	}
}

class ResetFailedNotify : ServerPacket
{
	public ResetFailedNotify() : base(ServerOpcodes.ResetFailedNotify) { }

	public override void Write() { }
}

class InstanceSaveCreated : ServerPacket
{
	public bool Gm;
	public InstanceSaveCreated() : base(ServerOpcodes.InstanceSaveCreated) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Gm);
		_worldPacket.FlushBits();
	}
}

class InstanceLockResponse : ClientPacket
{
	public bool AcceptLock;
	public InstanceLockResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AcceptLock = _worldPacket.HasBit();
	}
}

class RaidGroupOnly : ServerPacket
{
	public int Delay;
	public RaidGroupReason Reason;
	public RaidGroupOnly() : base(ServerOpcodes.RaidGroupOnly) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Delay);
		_worldPacket.WriteUInt32((uint)Reason);
	}
}

class PendingRaidLock : ServerPacket
{
	public int TimeUntilLock;
	public uint CompletedMask;
	public bool Extending;
	public bool WarningOnly;
	public PendingRaidLock() : base(ServerOpcodes.PendingRaidLock) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(TimeUntilLock);
		_worldPacket.WriteUInt32(CompletedMask);
		_worldPacket.WriteBit(Extending);
		_worldPacket.WriteBit(WarningOnly);
		_worldPacket.FlushBits();
	}
}

class RaidInstanceMessage : ServerPacket
{
	public InstanceResetWarningType Type;
	public uint MapID;
	public Difficulty DifficultyID;
	public bool Locked;
	public bool Extended;
	public RaidInstanceMessage() : base(ServerOpcodes.RaidInstanceMessage) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Type);
		_worldPacket.WriteUInt32(MapID);
		_worldPacket.WriteUInt32((uint)DifficultyID);
		_worldPacket.WriteBit(Locked);
		_worldPacket.WriteBit(Extended);
		_worldPacket.FlushBits();
	}
}

class InstanceEncounterEngageUnit : ServerPacket
{
	public ObjectGuid Unit;
	public byte TargetFramePriority; // used to set the initial position of the frame if multiple frames are sent
	public InstanceEncounterEngageUnit() : base(ServerOpcodes.InstanceEncounterEngageUnit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt8(TargetFramePriority);
	}
}

class InstanceEncounterDisengageUnit : ServerPacket
{
	public ObjectGuid Unit;
	public InstanceEncounterDisengageUnit() : base(ServerOpcodes.InstanceEncounterDisengageUnit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
	}
}

class InstanceEncounterChangePriority : ServerPacket
{
	public ObjectGuid Unit;
	public byte TargetFramePriority; // used to update the position of the unit's current frame
	public InstanceEncounterChangePriority() : base(ServerOpcodes.InstanceEncounterChangePriority, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt8(TargetFramePriority);
	}
}

class InstanceEncounterStart : ServerPacket
{
	public uint InCombatResCount; // amount of usable battle ressurections
	public uint MaxInCombatResCount;
	public uint CombatResChargeRecovery;
	public uint NextCombatResChargeTime;
	public bool InProgress = true;
	public InstanceEncounterStart() : base(ServerOpcodes.InstanceEncounterStart, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(InCombatResCount);
		_worldPacket.WriteUInt32(MaxInCombatResCount);
		_worldPacket.WriteUInt32(CombatResChargeRecovery);
		_worldPacket.WriteUInt32(NextCombatResChargeTime);
		_worldPacket.WriteBit(InProgress);
		_worldPacket.FlushBits();
	}
}

class InstanceEncounterEnd : ServerPacket
{
	public InstanceEncounterEnd() : base(ServerOpcodes.InstanceEncounterEnd, ConnectionType.Instance) { }

	public override void Write() { }
}

class InstanceEncounterInCombatResurrection : ServerPacket
{
	public InstanceEncounterInCombatResurrection() : base(ServerOpcodes.InstanceEncounterInCombatResurrection, ConnectionType.Instance) { }

	public override void Write() { }
}

class InstanceEncounterGainCombatResurrectionCharge : ServerPacket
{
	public int InCombatResCount;
	public uint CombatResChargeRecovery;
	public InstanceEncounterGainCombatResurrectionCharge() : base(ServerOpcodes.InstanceEncounterGainCombatResurrectionCharge, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(InCombatResCount);
		_worldPacket.WriteUInt32(CombatResChargeRecovery);
	}
}

class BossKill : ServerPacket
{
	public uint DungeonEncounterID;
	public BossKill() : base(ServerOpcodes.BossKill, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(DungeonEncounterID);
	}
}

//Structs
public struct InstanceLockPkt
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(MapID);
		data.WriteUInt32(DifficultyID);
		data.WriteUInt64(InstanceID);
		data.WriteInt32(TimeRemaining);
		data.WriteUInt32(CompletedMask);

		data.WriteBit(Locked);
		data.WriteBit(Extended);
		data.FlushBits();
	}

	public ulong InstanceID;
	public uint MapID;
	public uint DifficultyID;
	public int TimeRemaining;
	public uint CompletedMask;

	public bool Locked;
	public bool Extended;
}