// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class TransferAborted : ServerPacket
{
    public byte Arg;
    public uint MapDifficultyXConditionID;
    public uint MapID;
    public TransferAbortReason TransfertAbort;
    public TransferAborted() : base(ServerOpcodes.TransferAborted) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(MapID);
        WorldPacket.WriteUInt8(Arg);
        WorldPacket.WriteUInt32(MapDifficultyXConditionID);
        WorldPacket.WriteBits(TransfertAbort, 6);
        WorldPacket.FlushBits();
    }
}