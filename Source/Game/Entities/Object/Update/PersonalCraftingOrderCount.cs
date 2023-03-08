using System;
using Game.Networking;

namespace Game.Entities;

public class PersonalCraftingOrderCount : IEquatable<PersonalCraftingOrderCount>
{
	public int ProfessionID;
	public uint Count;

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

	public bool Equals(PersonalCraftingOrderCount right)
	{
		return ProfessionID == right.ProfessionID && Count == right.Count;
	}
}