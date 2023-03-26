// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class BattlegroundPlayerPositions : ServerPacket
{
	public List<BattlegroundPlayerPosition> FlagCarriers = new();
	public BattlegroundPlayerPositions() : base(ServerOpcodes.BattlegroundPlayerPositions, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(FlagCarriers.Count);

		foreach (var pos in FlagCarriers)
			pos.Write(_worldPacket);
	}
}