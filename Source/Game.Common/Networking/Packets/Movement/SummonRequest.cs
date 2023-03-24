// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Movement;

public class SummonRequest : ServerPacket
{
	public enum SummonReason
	{
		Spell = 0,
		Scenario = 1
	}

	public ObjectGuid SummonerGUID;
	public uint SummonerVirtualRealmAddress;
	public int AreaID;
	public SummonReason Reason;
	public bool SkipStartingArea;
	public SummonRequest() : base(ServerOpcodes.SummonRequest, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SummonerGUID);
		_worldPacket.WriteUInt32(SummonerVirtualRealmAddress);
		_worldPacket.WriteInt32(AreaID);
		_worldPacket.WriteUInt8((byte)Reason);
		_worldPacket.WriteBit(SkipStartingArea);
		_worldPacket.FlushBits();
	}
}
