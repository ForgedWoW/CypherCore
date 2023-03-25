// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Common.Networking;

namespace Forged.RealmServer.Entities;

public class TraitEntry : IEquatable<TraitEntry>
{
	public int TraitNodeID;
	public int TraitNodeEntryID;
	public int Rank;
	public int GrantedRanks;

	public bool Equals(TraitEntry right)
	{
		return TraitNodeID == right.TraitNodeID && TraitNodeEntryID == right.TraitNodeEntryID && Rank == right.Rank && GrantedRanks == right.GrantedRanks;
	}

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(TraitNodeID);
		data.WriteInt32(TraitNodeEntryID);
		data.WriteInt32(Rank);
		data.WriteInt32(GrantedRanks);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt32(TraitNodeID);
		data.WriteInt32(TraitNodeEntryID);
		data.WriteInt32(Rank);
		data.WriteInt32(GrantedRanks);
	}
}