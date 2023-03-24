// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.LFG;

public class LfgPlayerInfo : ServerPacket
{
	public LFGBlackList BlackList = new();
	public List<LfgPlayerDungeonInfo> Dungeons = new();
	public LfgPlayerInfo() : base(ServerOpcodes.LfgPlayerInfo, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Dungeons.Count);
		BlackList.Write(_worldPacket);

		foreach (var dungeonInfo in Dungeons)
			dungeonInfo.Write(_worldPacket);
	}
}
