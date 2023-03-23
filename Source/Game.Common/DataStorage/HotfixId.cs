using Game.DataStorage;
using Game.Common.Networking;

namespace Game.Common.DataStorage;

public struct HotfixId
{
	public int PushID;
	public uint UniqueID;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PushID);
		data.WriteUInt32(UniqueID);
	}

	public void Read(WorldPacket data)
	{
		PushID = data.ReadInt32();
		UniqueID = data.ReadUInt32();
	}
}
