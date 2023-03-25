// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class LfgPartyInfo : ServerPacket
{
	public List<LFGBlackList> Player = new();
	public LfgPartyInfo() : base(ServerOpcodes.LfgPartyInfo, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Player.Count);

		foreach (var blackList in Player)
			blackList.Write(_worldPacket);
	}
}