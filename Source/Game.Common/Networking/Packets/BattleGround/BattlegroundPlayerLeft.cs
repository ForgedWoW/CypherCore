﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.BattleGround;

public class BattlegroundPlayerLeft : ServerPacket
{
	public ObjectGuid Guid;
	public BattlegroundPlayerLeft() : base(ServerOpcodes.BattlegroundPlayerLeft, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
	}
}
