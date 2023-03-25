// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Entities;

public class ChrCustomizationChoice : IComparable<ChrCustomizationChoice>
{
	public uint ChrCustomizationOptionID;
	public uint ChrCustomizationChoiceID;

	public int CompareTo(ChrCustomizationChoice other)
	{
		return ChrCustomizationOptionID.CompareTo(other.ChrCustomizationOptionID);
	}

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
}