// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Instance;

public class InstanceResetFailed : ServerPacket
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
