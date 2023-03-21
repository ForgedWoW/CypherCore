// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

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