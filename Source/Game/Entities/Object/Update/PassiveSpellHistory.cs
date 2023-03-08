using Game.Networking;

namespace Game.Entities;

public class PassiveSpellHistory
{
	public int SpellID;
	public int AuraSpellID;

	public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
	{
		data.WriteInt32(SpellID);
		data.WriteInt32(AuraSpellID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
	{
		data.WriteInt32(SpellID);
		data.WriteInt32(AuraSpellID);
	}
}