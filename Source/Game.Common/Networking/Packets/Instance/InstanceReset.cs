// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Instance;

public class InstanceReset : ServerPacket
{
	public uint MapID;
	public InstanceReset() : base(ServerOpcodes.InstanceReset) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MapID);
	}
}
