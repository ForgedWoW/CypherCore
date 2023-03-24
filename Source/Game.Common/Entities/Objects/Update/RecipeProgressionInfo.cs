using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class RecipeProgressionInfo
{
	public ushort RecipeProgressionGroupID;
	public ushort Experience;

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteUInt16(RecipeProgressionGroupID);
		data.WriteUInt16(Experience);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteUInt16(RecipeProgressionGroupID);
		data.WriteUInt16(Experience);
	}
}
