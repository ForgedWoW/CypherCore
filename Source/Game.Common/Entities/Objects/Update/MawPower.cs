using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class MawPower
{
	public int Field_0;
	public int Field_4;
	public int Field_8;

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(Field_0);
		data.WriteInt32(Field_4);
		data.WriteInt32(Field_8);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt32(Field_0);
		data.WriteInt32(Field_4);
		data.WriteInt32(Field_8);
	}
}
