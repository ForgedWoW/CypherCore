using System.Collections.Generic;
using Game.Networking;

namespace Game.Entities;

public class MultiFloorExplore
{
	public List<int> WorldMapOverlayIDs = new();

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(WorldMapOverlayIDs.Count);
		for (int i = 0; i < WorldMapOverlayIDs.Count; ++i)
		{
			data.WriteInt32(WorldMapOverlayIDs[i]);
		}
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt32(WorldMapOverlayIDs.Count);
		for (int i = 0; i < WorldMapOverlayIDs.Count; ++i)
		{
			data.WriteInt32(WorldMapOverlayIDs[i]);
		}
		data.FlushBits();
	}
}