// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Party;

public class OptOutOfLoot : ClientPacket
{
	public bool PassOnLoot;
	public OptOutOfLoot(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PassOnLoot = _worldPacket.HasBit();
	}
}
