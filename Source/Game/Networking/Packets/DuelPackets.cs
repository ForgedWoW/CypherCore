// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CanDuel : ClientPacket
{
	public ObjectGuid TargetGUID;
	public CanDuel(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TargetGUID = _worldPacket.ReadPackedGuid();
	}
}

public class CanDuelResult : ServerPacket
{
	public ObjectGuid TargetGUID;
	public bool Result;
	public CanDuelResult() : base(ServerOpcodes.CanDuelResult) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WriteBit(Result);
		_worldPacket.FlushBits();
	}
}

public class DuelComplete : ServerPacket
{
	public bool Started;
	public DuelComplete() : base(ServerOpcodes.DuelComplete, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Started);
		_worldPacket.FlushBits();
	}
}

public class DuelCountdown : ServerPacket
{
	readonly uint Countdown;

	public DuelCountdown(uint countdown) : base(ServerOpcodes.DuelCountdown)
	{
		Countdown = countdown;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(Countdown);
	}
}

public class DuelInBounds : ServerPacket
{
	public DuelInBounds() : base(ServerOpcodes.DuelInBounds, ConnectionType.Instance) { }

	public override void Write() { }
}

public class DuelOutOfBounds : ServerPacket
{
	public DuelOutOfBounds() : base(ServerOpcodes.DuelOutOfBounds, ConnectionType.Instance) { }

	public override void Write() { }
}

public class DuelRequested : ServerPacket
{
	public ObjectGuid ArbiterGUID;
	public ObjectGuid RequestedByGUID;
	public ObjectGuid RequestedByWowAccount;
	public DuelRequested() : base(ServerOpcodes.DuelRequested, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ArbiterGUID);
		_worldPacket.WritePackedGuid(RequestedByGUID);
		_worldPacket.WritePackedGuid(RequestedByWowAccount);
	}
}

public class DuelResponse : ClientPacket
{
	public ObjectGuid ArbiterGUID;
	public bool Accepted;
	public bool Forfeited;
	public DuelResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ArbiterGUID = _worldPacket.ReadPackedGuid();
		Accepted = _worldPacket.HasBit();
		Forfeited = _worldPacket.HasBit();
	}
}

public class DuelWinner : ServerPacket
{
	public string BeatenName;
	public string WinnerName;
	public uint BeatenVirtualRealmAddress;
	public uint WinnerVirtualRealmAddress;
	public bool Fled;
	public DuelWinner() : base(ServerOpcodes.DuelWinner, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBits(BeatenName.GetByteCount(), 6);
		_worldPacket.WriteBits(WinnerName.GetByteCount(), 6);
		_worldPacket.WriteBit(Fled);
		_worldPacket.WriteUInt32(BeatenVirtualRealmAddress);
		_worldPacket.WriteUInt32(WinnerVirtualRealmAddress);
		_worldPacket.WriteString(BeatenName);
		_worldPacket.WriteString(WinnerName);
	}
}