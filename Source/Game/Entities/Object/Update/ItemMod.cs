using Game.Networking;

namespace Game.Entities;

public class ItemMod
{
	public uint Value;
	public byte Type;

	public void WriteCreate(WorldPacket data, Item owner, Player receiver)
	{
		data.WriteUInt32(Value);
		data.WriteUInt8(Type);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
	{
		data.WriteUInt32(Value);
		data.WriteUInt8(Type);
	}
}