// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Movement;

public struct MonsterSplineFilterKey
{
	public void Write(WorldPacket data)
	{
		data.WriteInt16(Idx);
		data.WriteUInt16(Speed);
	}

	public short Idx;
	public ushort Speed;
}
