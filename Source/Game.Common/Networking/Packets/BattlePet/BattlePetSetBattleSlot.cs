// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.BattlePet;

public class BattlePetSetBattleSlot : ClientPacket
{
	public ObjectGuid PetGuid;
	public byte Slot;
	public BattlePetSetBattleSlot(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
		Slot = _worldPacket.ReadUInt8();
	}
}
