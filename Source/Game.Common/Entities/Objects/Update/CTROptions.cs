using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class CTROptions
{
	public uint ContentTuningConditionMask;
	public uint Field_4;
	public uint ExpansionLevelMask;

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteUInt32(ContentTuningConditionMask);
		data.WriteUInt32(Field_4);
		data.WriteUInt32(ExpansionLevelMask);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteUInt32(ContentTuningConditionMask);
		data.WriteUInt32(Field_4);
		data.WriteUInt32(ExpansionLevelMask);
	}
}
