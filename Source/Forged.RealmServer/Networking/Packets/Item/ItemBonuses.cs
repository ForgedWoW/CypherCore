// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class ItemBonuses
{
	public ItemContext Context;
	public List<uint> BonusListIDs = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt8((byte)Context);
		data.WriteInt32(BonusListIDs.Count);

		foreach (var bonusID in BonusListIDs)
			data.WriteUInt32(bonusID);
	}

	public void Read(WorldPacket data)
	{
		Context = (ItemContext)data.ReadUInt8();
		var bonusListIdSize = data.ReadUInt32();

		BonusListIDs = new List<uint>();

		for (var i = 0u; i < bonusListIdSize; ++i)
		{
			var bonusId = data.ReadUInt32();
			BonusListIDs.Add(bonusId);
		}
	}

	public override int GetHashCode()
	{
		return Context.GetHashCode() ^ BonusListIDs.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is ItemBonuses)
			return (ItemBonuses)obj == this;

		return false;
	}

	public static bool operator ==(ItemBonuses left, ItemBonuses right)
	{
		if (ReferenceEquals(left, right))
			return true;

		if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
			return false;

		if (left.Context != right.Context)
			return false;

		if (left.BonusListIDs.Count != right.BonusListIDs.Count)
			return false;

		return left.BonusListIDs.SequenceEqual(right.BonusListIDs);
	}

	public static bool operator !=(ItemBonuses left, ItemBonuses right)
	{
		return !(left == right);
	}
}