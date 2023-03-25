// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

public struct PartyMemberPhase
{
	public PartyMemberPhase(uint flags, uint id)
	{
		Flags = (ushort)flags;
		Id = (ushort)id;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt16(Flags);
		data.WriteUInt16(Id);
	}

	public ushort Flags;
	public ushort Id;
}