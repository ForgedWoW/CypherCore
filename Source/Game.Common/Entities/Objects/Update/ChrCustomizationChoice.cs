// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

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
