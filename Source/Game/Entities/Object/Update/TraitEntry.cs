using System;
using Game.Networking;

namespace Game.Entities;

public class TraitEntry : IEquatable<TraitEntry>
{
	public int TraitNodeID;
	public int TraitNodeEntryID;
	public int Rank;
	public int GrantedRanks;

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

	public bool Equals(TraitEntry right)
	{
		return TraitNodeID == right.TraitNodeID
		       && TraitNodeEntryID == right.TraitNodeEntryID
		       && Rank == right.Rank
		       && GrantedRanks == right.GrantedRanks;
	}
}