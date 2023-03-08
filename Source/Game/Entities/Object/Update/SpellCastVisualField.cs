using Game.Networking;

namespace Game.Entities;

public class SpellCastVisualField
{
	public uint SpellXSpellVisualID;
	public uint ScriptVisualID;

	public void WriteCreate(WorldPacket data, WorldObject owner, Player receiver)
	{
		data.WriteUInt32(SpellXSpellVisualID);
		data.WriteUInt32(ScriptVisualID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, WorldObject owner, Player receiver)
	{
		data.WriteUInt32(SpellXSpellVisualID);
		data.WriteUInt32(ScriptVisualID);
	}
}