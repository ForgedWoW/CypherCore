using System;
using Game.Networking;

namespace Game.Entities;

public class ChrCustomizationChoice : IComparable<ChrCustomizationChoice>
{
	public uint ChrCustomizationOptionID;
	public uint ChrCustomizationChoiceID;

	public void WriteCreate(WorldPacket data, WorldObject owner, Player receiver)
	{
		data.WriteUInt32(ChrCustomizationOptionID);
		data.WriteUInt32(ChrCustomizationChoiceID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, WorldObject owner, Player receiver)
	{
		data.WriteUInt32(ChrCustomizationOptionID);
		data.WriteUInt32(ChrCustomizationChoiceID);
	}

	public int CompareTo(ChrCustomizationChoice other)
	{
		return ChrCustomizationOptionID.CompareTo(other.ChrCustomizationOptionID);
	}
}