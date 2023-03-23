// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Party;

public class PartyMemberAuraStates
{
	public int SpellID;
	public ushort Flags;
	public uint ActiveFlags;
	public List<float> Points = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(SpellID);
		data.WriteUInt16(Flags);
		data.WriteUInt32(ActiveFlags);
		data.WriteInt32(Points.Count);

		foreach (var points in Points)
			data.WriteFloat(points);
	}
}
