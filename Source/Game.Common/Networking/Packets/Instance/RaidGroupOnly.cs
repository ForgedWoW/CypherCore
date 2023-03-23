﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Instance;

public class RaidGroupOnly : ServerPacket
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
