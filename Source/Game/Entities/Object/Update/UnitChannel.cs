using Game.Networking;

namespace Game.Entities;

public class UnitChannel
{
	public uint SpellID;
	public uint SpellXSpellVisualID;
	public SpellCastVisualField SpellVisual = new();

	public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
	{
		data.WriteUInt32(SpellID);
		SpellVisual.WriteCreate(data, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
	{
		data.WriteUInt32(SpellID);
		SpellVisual.WriteUpdate(data, ignoreChangesMask, owner, receiver);
	}
}