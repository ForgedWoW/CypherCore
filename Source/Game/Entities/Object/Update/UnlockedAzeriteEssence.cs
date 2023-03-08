using Game.Networking;

namespace Game.Entities;

public class UnlockedAzeriteEssence
{
	public uint AzeriteEssenceID;
	public uint Rank;

	public void WriteCreate(WorldPacket data, AzeriteItem owner, Player receiver)
	{
		data.WriteUInt32(AzeriteEssenceID);
		data.WriteUInt32(Rank);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AzeriteItem owner, Player receiver)
	{
		data.WriteUInt32(AzeriteEssenceID);
		data.WriteUInt32(Rank);
	}
}