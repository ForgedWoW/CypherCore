using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class SpellFlatModByLabel
{
	public int ModIndex;
	public double ModifierValue;
	public int LabelID;

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(ModIndex);
		data.WriteInt32((int)ModifierValue);
		data.WriteInt32(LabelID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt32(ModIndex);
		data.WriteInt32((int)ModifierValue);
		data.WriteInt32(LabelID);
	}
}
