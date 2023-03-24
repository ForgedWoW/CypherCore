// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Guild;

public class GuildAddRank : ClientPacket
{
	public string Name;
	public int RankOrder;
	public GuildAddRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(7);
		_worldPacket.ResetBitPos();

		RankOrder = _worldPacket.ReadInt32();
		Name = _worldPacket.ReadString(nameLen);
	}
}
