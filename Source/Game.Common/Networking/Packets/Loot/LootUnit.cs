// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Loot;

public class LootUnit : ClientPacket
{
	public ObjectGuid Unit;
	public bool IsSoftInteract;
	public LootUnit(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Unit = _worldPacket.ReadPackedGuid();
		IsSoftInteract = _worldPacket.HasBit();
	}
}

//Structs