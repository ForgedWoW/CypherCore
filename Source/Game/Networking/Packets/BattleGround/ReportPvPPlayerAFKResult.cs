// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class ReportPvPPlayerAFKResult : ServerPacket
{
	public enum ResultCode
	{
		Success = 0,
		GenericFailure = 1, // there are more error codes but they are impossible to receive without modifying the client
		AFKSystemEnabled = 5,
		AFKSystemDisabled = 6
	}

	public ObjectGuid Offender;
	public byte NumPlayersIHaveReported = 0;
	public byte NumBlackMarksOnOffender = 0;
	public ResultCode Result = ResultCode.GenericFailure;
	public ReportPvPPlayerAFKResult() : base(ServerOpcodes.ReportPvpPlayerAfkResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Offender);
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WriteUInt8(NumBlackMarksOnOffender);
		_worldPacket.WriteUInt8(NumPlayersIHaveReported);
	}
}