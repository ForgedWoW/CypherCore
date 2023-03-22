// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class TransferAborted : ServerPacket
{
	public uint MapID;
	public byte Arg;
	public uint MapDifficultyXConditionID;
	public TransferAbortReason TransfertAbort;
	public TransferAborted() : base(ServerOpcodes.TransferAborted) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
		_worldPacket.WriteUInt8(Arg);
		_worldPacket.WriteUInt32(MapDifficultyXConditionID);
		_worldPacket.WriteBits(TransfertAbort, 6);
		_worldPacket.FlushBits();
	}
}