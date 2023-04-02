// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class ReportPvPPlayerAFKResult : ServerPacket
{
    public byte NumBlackMarksOnOffender = 0;

    public byte NumPlayersIHaveReported = 0;

    public ObjectGuid Offender;

    public ResultCode Result = ResultCode.GenericFailure;

    public ReportPvPPlayerAFKResult() : base(ServerOpcodes.ReportPvpPlayerAfkResult, ConnectionType.Instance) { }

    public enum ResultCode
    {
        Success = 0,
        GenericFailure = 1, // there are more error codes but they are impossible to receive without modifying the client
        AFKSystemEnabled = 5,
        AFKSystemDisabled = 6
    }
    public override void Write()
    {
        _worldPacket.WritePackedGuid(Offender);
        _worldPacket.WriteUInt8((byte)Result);
        _worldPacket.WriteUInt8(NumBlackMarksOnOffender);
        _worldPacket.WriteUInt8(NumPlayersIHaveReported);
    }
}