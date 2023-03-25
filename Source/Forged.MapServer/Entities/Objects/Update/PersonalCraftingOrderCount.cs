// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class PersonalCraftingOrderCount : IEquatable<PersonalCraftingOrderCount>
{
	public int ProfessionID;
	public uint Count;

	public bool Equals(PersonalCraftingOrderCount right)
	{
		return ProfessionID == right.ProfessionID && Count == right.Count;
	}

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(ProfessionID);
		data.WriteUInt32(Count);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt32(ProfessionID);
		data.WriteUInt32(Count);
	}
}