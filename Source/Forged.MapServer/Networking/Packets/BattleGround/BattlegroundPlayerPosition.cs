// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public struct BattlegroundPlayerPosition
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WriteVector2(Pos);
		data.WriteInt8(IconID);
		data.WriteInt8(ArenaSlot);
	}

	public ObjectGuid Guid;
	public Vector2 Pos;
	public sbyte IconID;
	public sbyte ArenaSlot;
}